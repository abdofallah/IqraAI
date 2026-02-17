using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.CognitiveServices;
using Azure.ResourceManager.Resources.Models;
using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.TTS;
using IqraCore.Interfaces.AI;
using IqraInfrastructure.Helpers.Audio;
using IqraInfrastructure.Managers.TTS.Helpers;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.Collections.ObjectModel;

namespace IqraInfrastructure.Managers.STT.Providers
{
    public class AzureSpeechSTTConfig
    {
        public string Language { get; set; }
        public List<string>? ContinuousLanguageIdentificationIds { get; set; }
        public bool SpeakerDiarization { get; set; }
        public List<string>? PhrasesList { get; set; }
        public int SilenceTimeout { get; set; }
    }

    public class AzureSpeechSTTService : ISTTService
    {
        private readonly string _tenantId;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _subscriptionId;
        private readonly string _resourceGroupName;
        private readonly string _speechResourceName;
        private readonly string _region;

        private readonly AzureSpeechSTTConfig _config;

        private ArmClient _azureClient;
        private SpeechRecognizer _recognizer;
        private PushAudioInputStream _pushStream;

        // The format coming FROM the platform/user
        private readonly TTSProviderAvailableAudioFormat _inputAudioDetails;

        // The format we determined is best for Azure
        private TTSProviderAvailableAudioFormat _optimalAzureFormat;
        private bool _audioConversionNeeded = false;
        private AudioRequestDetails _targetProviderFormatDetails;

        private event EventHandler<string> _transcriptionResultReceived;

        public event EventHandler<string> TranscriptionResultReceived
        {
            add { _transcriptionResultReceived += value; }
            remove { _transcriptionResultReceived -= value; }
        }

        public event EventHandler<string> OnRecoginizingRecieved;
        public event EventHandler<object> OnRecoginizingCancelled;

        public AzureSpeechSTTService(
            string tenantId,
            string clientId,
            string clientSecret,
            string subscriptionId,
            string resourceGroupName,
            string speechResourceName,
            string region,
            AzureSpeechSTTConfig config,
            TTSProviderAvailableAudioFormat inputAudioDetails
        )
        {
            _tenantId = tenantId;
            _clientId = clientId;
            _clientSecret = clientSecret;
            _subscriptionId = subscriptionId;
            _resourceGroupName = resourceGroupName;
            _speechResourceName = speechResourceName;
            _region = region;

            _config = config;

            _inputAudioDetails = inputAudioDetails;
        }

        public async Task<FunctionReturnResult> Initialize()
        {
            var result = new FunctionReturnResult();

            try
            {
                // 1. Authenticate
                var azureCredientals = new ClientSecretCredential(_tenantId, _clientId, _clientSecret);
                _azureClient = new ArmClient(azureCredientals);

                var subscriptionData = await _azureClient.GetSubscriptions().GetAsync(_subscriptionId);
                if (subscriptionData.Value.Data.State != SubscriptionState.Enabled)
                {
                    return result.SetFailureResult("Initialize:SUBSCRIPTION_NOT_ENABLED", "Azure subscription is not enabled.");
                }

                var resourceId = CognitiveServicesAccountResource.CreateResourceIdentifier(_subscriptionId, _resourceGroupName, _speechResourceName);
                CognitiveServicesAccountResource speechAccount = _azureClient.GetCognitiveServicesAccountResource(resourceId);
                var keys = await speechAccount.GetKeysAsync();
                string retrievedKey = keys.Value.Key1;

                if (string.IsNullOrEmpty(retrievedKey))
                {
                    return result.SetFailureResult("Initialize:KEY_NOT_FOUND", "Could not retrieve a valid key.");
                }

                // 2. Determine Optimal Format
                var bestFallbackOrder = AudiEncoderFallbackSelector.GetFallbackOrder(
                    new AudioRequestDetails()
                    {
                        RequestedEncoding = _inputAudioDetails.Encoding,
                        RequestedBitsPerSample = _inputAudioDetails.BitsPerSample,
                        RequestedSampleRateHz = _inputAudioDetails.SampleRateHz
                    },
                    AzureSupportedFormats
                );

                _optimalAzureFormat = bestFallbackOrder.FirstOrDefault() ?? throw new NotSupportedException(
                     $"Azure STT does not support any format that can be reasonably converted from the input format: " +
                     $"{_inputAudioDetails.Encoding} @ {_inputAudioDetails.SampleRateHz}Hz");

                // 3. Check if conversion is needed
                _audioConversionNeeded = _optimalAzureFormat.Encoding != _inputAudioDetails.Encoding ||
                                         _optimalAzureFormat.SampleRateHz != _inputAudioDetails.SampleRateHz ||
                                         _optimalAzureFormat.BitsPerSample != _inputAudioDetails.BitsPerSample;

                if (_audioConversionNeeded)
                {
                    _targetProviderFormatDetails = new AudioRequestDetails
                    {
                        RequestedEncoding = _optimalAzureFormat.Encoding,
                        RequestedSampleRateHz = _optimalAzureFormat.SampleRateHz,
                        RequestedBitsPerSample = _optimalAzureFormat.BitsPerSample
                    };
                }

                // 4. Configure Speech Config
                var speechConfig = SpeechConfig.FromSubscription(retrievedKey, _region);
                speechConfig.SpeechRecognitionLanguage = _config.Language;
                speechConfig.SetProperty(PropertyId.SpeechServiceResponse_DiarizeIntermediateResults, _config.SpeakerDiarization ? "true" : "false");
                speechConfig.SetProperty(PropertyId.Speech_SegmentationSilenceTimeoutMs, _config.SilenceTimeout.ToString());

                // 5. Configure Audio Stream
                var audioFormat = AudioStreamFormat.GetWaveFormatPCM(
                    (uint)_optimalAzureFormat.SampleRateHz,
                    (byte)_optimalAzureFormat.BitsPerSample,
                    1 // Channels (Mono is standard for STT)
                );

                _pushStream = AudioInputStream.CreatePushStream(audioFormat);
                var audioConfig = AudioConfig.FromStreamInput(_pushStream);

                // 6. Initialize Recognizer
                if (_config.ContinuousLanguageIdentificationIds != null && _config.ContinuousLanguageIdentificationIds.Count > 0)
                {
                    speechConfig.SetProperty(PropertyId.SpeechServiceConnection_LanguageIdMode, "Continuous");
                    var autoDetectSourceLanguageConfig = AutoDetectSourceLanguageConfig.FromLanguages(_config.ContinuousLanguageIdentificationIds.ToArray());
                    _recognizer = new SpeechRecognizer(speechConfig, autoDetectSourceLanguageConfig, audioConfig);
                }
                else
                {
                    _recognizer = new SpeechRecognizer(speechConfig, audioConfig);
                }

                if (_config.PhrasesList != null && _config.PhrasesList.Count > 0)
                {
                    var phraseList = PhraseListGrammar.FromRecognizer(_recognizer);
                    foreach (var phrase in _config.PhrasesList)
                    {
                        phraseList.AddPhrase(phrase);
                    }
                }

                // Events
                _recognizer.Recognizing += OnRecognizing;
                _recognizer.Recognized += OnRecognized;
                _recognizer.Canceled += OnCanceled;
                _recognizer.SessionStarted += OnSessionStarted;
                _recognizer.SessionStopped += OnSessionStopped;
                _recognizer.SpeechEndDetected += OnSpeechEndDetected;

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("Initialize:EXCEPTION", $"Internal error: {ex.Message}");
            }
        }

        public void StartTranscription()
        {
            _recognizer.StartContinuousRecognitionAsync().Wait(100);
        }

        public void StopTranscription()
        {
            StopTranscriptionAsync().GetAwaiter().GetResult();
        }

        public async Task StopTranscriptionAsync()
        {
            if (_recognizer != null)
            {
                await _recognizer.StopContinuousRecognitionAsync();
            }
        }

        public void WriteTranscriptionAudioData(byte[] data)
        {
            if (_pushStream == null) return;

            if (_audioConversionNeeded)
            {
                try
                {
                    var (convertedData, _) = AudioConversationHelper.Convert(
                        data,
                        _inputAudioDetails,
                        _targetProviderFormatDetails,
                        false // Mono
                    );

                    if (convertedData != null)
                    {
                        _pushStream.Write(convertedData);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Audio Conversion Failed: {ex.Message}");
                    return;
                }
            }

            // Write original data if no conversion needed
            _pushStream.Write(data);
        }


        private void OnRecognizing(object? sender, SpeechRecognitionEventArgs e)
        {
            OnRecoginizingRecieved?.Invoke(this, e.Result.Text);
        }

        private void OnRecognized(object? sender, SpeechRecognitionEventArgs e)
        {
            if (e.Result.Reason == ResultReason.RecognizedSpeech)
            {
                _transcriptionResultReceived?.Invoke(this, e.Result.Text);
            }
            else if (e.Result.Reason == ResultReason.NoMatch)
            {
                // Console.WriteLine($"No speech could be recognized.");
            }
        }

        private void OnCanceled(object? sender, SpeechRecognitionCanceledEventArgs e)
        {
            if (e.Reason == CancellationReason.Error)
            {
                // Handle Error
            }
            OnRecoginizingCancelled?.Invoke(this, e);
        }

        private void OnSessionStarted(object? sender, SessionEventArgs e) { }
        private void OnSessionStopped(object? sender, SessionEventArgs e) { }
        private void OnSpeechEndDetected(object? sender, RecognitionEventArgs e) { }


        public string GetProviderFullName() => "Azure AI Speech";
        public InterfaceSTTProviderEnum GetProviderType() => GetProviderTypeStatic();
        public static InterfaceSTTProviderEnum GetProviderTypeStatic() => InterfaceSTTProviderEnum.AzureSpeechServices;

        // STATIC CONFIGURATION: What Azure Supports Best (16-bit PCM)
        private static readonly ReadOnlyCollection<TTSProviderAvailableAudioFormat> AzureSupportedFormats;

        static AzureSpeechSTTService()
        {
            var supportedFormats = new List<TTSProviderAvailableAudioFormat>
            {
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 8000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 16000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 22050, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 24000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 32000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 44100, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 48000, BitsPerSample = 16 }
            };
            AzureSupportedFormats = supportedFormats.AsReadOnly();
        }
    }
}
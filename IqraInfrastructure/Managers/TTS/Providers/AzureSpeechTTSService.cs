using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.CognitiveServices;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.TTS;
using IqraCore.Entities.TTS.Providers.AzureSpeech;
using IqraCore.Interfaces.AI;
using IqraCore.Interfaces.TTS;
using IqraInfrastructure.Helpers.Audio;
using IqraInfrastructure.Managers.TTS.Helpers;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.Collections.ObjectModel;
using System.Drawing;

namespace IqraInfrastructure.Managers.TTS.Providers
{
    public class AzureSpeechTTSService : ITTSService
    {
        private readonly string _tenantId;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _subscriptionId;
        private readonly string _resourceGroupName;
        private readonly string _speechResourceName;
        private readonly string _region;
        private readonly AzureSpeechConfig _serviceConfig;

        private AudioRequestDetails _finalUserRequest;
        private TTSProviderAvailableAudioFormat _optimalAzureFormat;
        private bool _audioConversationNeeded = false;

        private ArmClient _azureClient;
        private SpeechSynthesizer _synthesizer;
        private PullAudioOutputStream _pullStream;

        private bool _loggingEnabled = false;

        public AzureSpeechTTSService(string tenantId, string clientId, string clientSecret, string subscriptionId, string resourceGroupName, string speechResourceName, string region, AzureSpeechConfig config)
        {
            _tenantId = tenantId;
            _clientId = clientId;
            _clientSecret = clientSecret;
            _subscriptionId = subscriptionId;
            _resourceGroupName = resourceGroupName;
            _speechResourceName = speechResourceName;
            _region = region;
            _serviceConfig = config;
        }

        public async Task<FunctionReturnResult> Initialize()
        {
            var result = new FunctionReturnResult();

            try
            {
                var azureCredientals = new ClientSecretCredential(_tenantId, _clientId, _clientSecret);
                _azureClient = new ArmClient(azureCredientals);

                var subscriptionData = await _azureClient.GetSubscriptions().GetAsync(_subscriptionId);
                var subscriptionState = subscriptionData.Value.Data.State;
                if (subscriptionState != SubscriptionState.Enabled)
                {
                    return result.SetFailureResult(
                        "Initialize:SUBSCRIPTION_NOT_ENABLED",
                        $"Azure subscription is not enabled. Current State: {subscriptionState.ToString()}"
                    );
                }            

                var resourceId = CognitiveServicesAccountResource.CreateResourceIdentifier(_subscriptionId, _resourceGroupName, _speechResourceName);
                CognitiveServicesAccountResource speechAccount = _azureClient.GetCognitiveServicesAccountResource(resourceId);

                var keys = await speechAccount.GetKeysAsync();
                string retrievedKey = keys.Value.Key1;

                if (string.IsNullOrEmpty(retrievedKey))
                {
                    return result.SetFailureResult(
                        "Initialize:KEY_NOT_FOUND",
                        "Could not retrieve a valid key for the Speech Service."
                    );
                }

                _finalUserRequest = new AudioRequestDetails
                {
                    RequestedEncoding = _serviceConfig.TargetEncodingType,
                    RequestedSampleRateHz = _serviceConfig.TargetSampleRate,
                    RequestedBitsPerSample = _serviceConfig.TargetBitsPerSample
                };

                // Use the selector to find the best format Azure can provide.
                var bestFallbackOrder = AudiEncoderFallbackSelector.GetFallbackOrder(_finalUserRequest, AzureSupportedFormats);
                _optimalAzureFormat = bestFallbackOrder.FirstOrDefault();
                if (_optimalAzureFormat == null)
                {
                    throw new NotSupportedException(
                        $"Azure TTS does not support any format that can be reasonably converted to the requested format: " +
                        $"{_finalUserRequest.RequestedEncoding} @ {_finalUserRequest.RequestedSampleRateHz}Hz");
                }

                // Find the corresponding Azure SDK enum value for the chosen optimal format.
                var formatKey = (_optimalAzureFormat.Encoding, _optimalAzureFormat.SampleRateHz, _optimalAzureFormat.BitsPerSample);
                if (!FormatMap.TryGetValue(formatKey, out var azureOutputFormat))
                {
                    throw new InvalidOperationException($"Internal error: No mapping found for the selected optimal format: {formatKey}");
                }

                _audioConversationNeeded = _optimalAzureFormat.Encoding != _finalUserRequest.RequestedEncoding ||
                                _optimalAzureFormat.SampleRateHz != _finalUserRequest.RequestedSampleRateHz ||
                                _optimalAzureFormat.BitsPerSample != _finalUserRequest.RequestedBitsPerSample;

                var speechConfig = SpeechConfig.FromSubscription(retrievedKey, _region);
                speechConfig.SpeechSynthesisLanguage = _serviceConfig.Language;
                speechConfig.SpeechSynthesisVoiceName = _serviceConfig.VoiceName;
                speechConfig.SetSpeechSynthesisOutputFormat(azureOutputFormat);

                _pullStream = AudioOutputStream.CreatePullStream();
                var audioConfig = AudioConfig.FromStreamOutput(_pullStream);

                _synthesizer = new SpeechSynthesizer(speechConfig, audioConfig);
                var connection = Connection.FromSpeechSynthesizer(_synthesizer);
                connection.Open(true);

                _synthesizer.SynthesisStarted += OnSynthesisStarted;
                _synthesizer.SynthesisCompleted += OnSynthesisCompleted;
                _synthesizer.SynthesisCanceled += OnSynthesisCanceled;
                _synthesizer.Synthesizing += OnSynthesizing;
                _synthesizer.BookmarkReached += OnBookmarkReached;

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    $"CheckAccount:EXCEPTION",
                    $"Internal server error occured: {ex.Message}"
                );
            }
        }

        public async Task<(byte[]?, TimeSpan?)> SynthesizeTextAsync(string text, CancellationToken cancellationToken, Dictionary<string, object>? metaData)
        {
            var constructSSML = $"<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"{_serviceConfig.Language}\"><voice name=\"{_serviceConfig.VoiceName}\">{text}</voice></speak>";
            var result = await _synthesizer.SpeakSsmlAsync(constructSSML);

            if (result.Reason != ResultReason.SynthesizingAudioCompleted)
            {
                return (new byte[] { }, TimeSpan.Zero);
            }

            (byte[], TimeSpan) finalAudioData = (result.AudioData, result.AudioDuration);

            if (_audioConversationNeeded)
            {
                finalAudioData = AudioConversationHelper.Convert(result.AudioData, _optimalAzureFormat, _finalUserRequest);
            }

            return finalAudioData;
        }

        public async Task StopTextSynthesisAsync()
        {
            await _synthesizer.StopSpeakingAsync();
        }

        private void OnSynthesisStarted(object? sender, SpeechSynthesisEventArgs e)
        {
            if (_loggingEnabled)
            {
                Console.WriteLine($"Synthesis Started: Id={e.Result.ResultId}");
            }
        }

        private void OnSynthesisCompleted(object? sender, SpeechSynthesisEventArgs e)
        {
            if (_loggingEnabled)
            {
                Console.WriteLine($"first byte client latency: \t{e.Result.Properties.GetProperty(PropertyId.SpeechServiceResponse_SynthesisFirstByteLatencyMs)} ms");
                Console.WriteLine($"finish client latency: \t{e.Result.Properties.GetProperty(PropertyId.SpeechServiceResponse_SynthesisFinishLatencyMs)} ms");
                Console.WriteLine($"network latency: \t{e.Result.Properties.GetProperty(PropertyId.SpeechServiceResponse_SynthesisNetworkLatencyMs)} ms");
                Console.WriteLine($"first byte service latency: \t{e.Result.Properties.GetProperty(PropertyId.SpeechServiceResponse_SynthesisServiceLatencyMs)} ms");
                Console.WriteLine($"Synthesis Completed: Id={e.Result.ResultId}");
            }
        }

        private void OnSynthesisCanceled(object? sender, SpeechSynthesisEventArgs e)
        {
            if (_loggingEnabled)
            {
                Console.WriteLine($"Synthesis canceled. Reason: {e.Result.Reason} Id={e.Result.ResultId}");
            }
        }

        private void OnSynthesizing(object? sender, SpeechSynthesisEventArgs e)
        {
            if (_loggingEnabled)
            {
                //Console.WriteLine($"Synthesizing. Id: {e.Result.ResultId}");
            }
        }

        private void OnBookmarkReached(object? sender, SpeechSynthesisBookmarkEventArgs e)
        {
            if (_loggingEnabled)
            {
                //Console.WriteLine($"Bookmark Reached. Text: {e.Text} Id: {e.ResultId}");
            }
        }

        public string GetProviderFullName()
        {
            return "MicrosoftAzureSpeech";
        }

        public InterfaceTTSProviderEnum GetProviderType()
        {
            return GetProviderTypeStatic();
        }

        public ITTSConfig GetCacheableConfig()
        {
            return _serviceConfig;
        }

        public static InterfaceTTSProviderEnum GetProviderTypeStatic()
        {
            return InterfaceTTSProviderEnum.AzureSpeechServices;
        }

        // STATIC ENCODER RELATED
        private static readonly ReadOnlyCollection<TTSProviderAvailableAudioFormat> AzureSupportedFormats;
        private static readonly ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), SpeechSynthesisOutputFormat> FormatMap;

        static AzureSpeechTTSService()
        {
            var supportedFormats = new List<TTSProviderAvailableAudioFormat>
            {
                // Note: We only include formats relevant to our AudioEncodingTypeEnum for clarity.
                // Raw PCM Formats
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 8000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 16000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 22050, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 24000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 44100, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = 48000, BitsPerSample = 16 },

                // MULAW
                new() { Encoding = AudioEncodingTypeEnum.MULAW, SampleRateHz = 8000, BitsPerSample = 8 },

                // ALAW
                new() { Encoding = AudioEncodingTypeEnum.ALAW, SampleRateHz = 8000, BitsPerSample = 8 },

                // OPUS (Ogg container)
                new() { Encoding = AudioEncodingTypeEnum.OPUS, SampleRateHz = 16000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.OPUS, SampleRateHz = 24000, BitsPerSample = 16 },
                new() { Encoding = AudioEncodingTypeEnum.OPUS, SampleRateHz = 48000, BitsPerSample = 16 },
            
                // G722
                new() { Encoding = AudioEncodingTypeEnum.G722, SampleRateHz = 16000, BitsPerSample = 16 },
            };
            AzureSupportedFormats = supportedFormats.AsReadOnly();

            // Create the mapping from our format definition to the Azure SDK enum
            var formatMap = new Dictionary<(AudioEncodingTypeEnum, int, int), SpeechSynthesisOutputFormat>
            {
                { (AudioEncodingTypeEnum.PCM, 8000, 16), SpeechSynthesisOutputFormat.Raw8Khz16BitMonoPcm },
                { (AudioEncodingTypeEnum.PCM, 16000, 16), SpeechSynthesisOutputFormat.Raw16Khz16BitMonoPcm },
                { (AudioEncodingTypeEnum.PCM, 22050, 16), SpeechSynthesisOutputFormat.Raw22050Hz16BitMonoPcm },
                { (AudioEncodingTypeEnum.PCM, 24000, 16), SpeechSynthesisOutputFormat.Raw24Khz16BitMonoPcm },
                { (AudioEncodingTypeEnum.PCM, 44100, 16), SpeechSynthesisOutputFormat.Raw44100Hz16BitMonoPcm },
                { (AudioEncodingTypeEnum.PCM, 48000, 16), SpeechSynthesisOutputFormat.Raw48Khz16BitMonoPcm },
                { (AudioEncodingTypeEnum.MULAW, 8000, 8), SpeechSynthesisOutputFormat.Raw8Khz8BitMonoMULaw },
                { (AudioEncodingTypeEnum.ALAW, 8000, 8), SpeechSynthesisOutputFormat.Raw8Khz8BitMonoALaw },
                { (AudioEncodingTypeEnum.OPUS, 16000, 16), SpeechSynthesisOutputFormat.Ogg16Khz16BitMonoOpus },
                { (AudioEncodingTypeEnum.OPUS, 24000, 16), SpeechSynthesisOutputFormat.Ogg24Khz16BitMonoOpus },
                { (AudioEncodingTypeEnum.OPUS, 48000, 16), SpeechSynthesisOutputFormat.Ogg48Khz16BitMonoOpus },
                { (AudioEncodingTypeEnum.G722, 16000, 16), SpeechSynthesisOutputFormat.G72216Khz64Kbps },
            };
            FormatMap = new ReadOnlyDictionary<(AudioEncodingTypeEnum, int, int), SpeechSynthesisOutputFormat>(formatMap);
        }
    }
}
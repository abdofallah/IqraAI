using IqraInfrastructure.Managers.Telephony;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace IqraInfrastructure.Managers.Conversation.Client
{
    public class TwilioConversationClient : BaseTelephonyConversationClient
    {
        private readonly TwilioManager _twilioManager;
        private readonly string _callSid;
        private readonly string _accountSid;
        private readonly string _authToken;
        private readonly string _twimlCallbackUrl;
        private string? _currentStreamUrl;
        private HttpListener? _streamHttpListener;
        private Task? _streamListenerTask;
        private readonly List<byte[]> _audioBufferQueue = new();
        private readonly object _audioBufferLock = new();
        private bool _isStreamingAudio = false;

        public TwilioConversationClient(
            string clientId,
            string phoneNumber,
            string callSid,
            string accountSid,
            string authToken,
            string twimlCallbackUrl,
            TwilioManager twilioManager,
            ILogger<TwilioConversationClient> logger)
            : base(clientId, phoneNumber, logger)
        {
            _callSid = callSid;
            _accountSid = accountSid;
            _authToken = authToken;
            _twimlCallbackUrl = twimlCallbackUrl;
            _twilioManager = twilioManager;
        }

        public override async Task ConnectAsync(CancellationToken cancellationToken)
        {
            if (_isConnected)
            {
                _logger.LogWarning("Twilio client is already connected for call {CallSid}", _callSid);
                return;
            }

            try
            {
                _connectionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                // For Twilio, we need to set up our side to receive streaming audio
                await SetupStreamingEndpointAsync();

                _logger.LogInformation("Connected Twilio client for call {CallSid}", _callSid);
                _isConnected = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting Twilio client for call {CallSid}", _callSid);
                throw;
            }
        }

        public override async Task DisconnectAsync(string reason)
        {
            if (!_isConnected)
            {
                _logger.LogDebug("Twilio client is already disconnected for call {CallSid}", _callSid);
                return;
            }

            try
            {
                // Cancel any ongoing operations
                _connectionCts?.Cancel();

                // Stop the HTTP listener if it's running
                if (_streamHttpListener != null && _streamHttpListener.IsListening)
                {
                    _streamHttpListener.Stop();
                    _streamHttpListener.Close();
                }

                // End the call on the Twilio side
                await _twilioManager.EndCallAsync(_accountSid, _authToken, _callSid);

                _logger.LogInformation("Disconnected Twilio client for call {CallSid}: {Reason}", _callSid, reason);

                // Clean up resources
                _streamHttpListener = null;
                _connectionCts?.Dispose();
                _connectionCts = null;

                _isConnected = false;
                OnDisconnected(reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting Twilio client for call {CallSid}", _callSid);
            }
        }

        public override async Task SendAudioAsync(byte[] audioData, CancellationToken cancellationToken)
        {
            if (!_isConnected)
            {
                _logger.LogWarning("Cannot send audio because Twilio client is not connected for call {CallSid}", _callSid);
                return;
            }

            try
            {
                // Queue the audio data for sending
                lock (_audioBufferLock)
                {
                    _audioBufferQueue.Add(audioData);
                }

                // Start streaming if not already
                if (!_isStreamingAudio)
                {
                    _isStreamingAudio = true;
                    _ = Task.Run(() => StreamAudioToTwilioAsync(cancellationToken), cancellationToken);
                }

                _logger.LogDebug("Queued {Length} bytes of audio data for call {CallSid}", audioData.Length, _callSid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error queuing audio data for call {CallSid}", _callSid);
                throw;
            }
        }

        public override async Task SendTextAsync(string text, CancellationToken cancellationToken)
        {
            if (!_isConnected)
            {
                _logger.LogWarning("Cannot send text because Twilio client is not connected for call {CallSid}", _callSid);
                return;
            }

            try
            {
                // For Twilio, we generate TwiML with <Say> element
                var twiml = new XDocument(
                    new XElement("Response",
                        new XElement("Say", text)
                    )
                );

                // Send the TwiML to the callback URL
                await SendTwiMLAsync(twiml.ToString(), cancellationToken);

                _logger.LogDebug("Sent text message for call {CallSid}: {Text}", _callSid, text);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending text message for call {CallSid}", _callSid);
                throw;
            }
        }

        private async Task SetupStreamingEndpointAsync()
        {
            try
            {
                // Create a random port for our local HTTP listener
                var random = new Random();
                var port = random.Next(10000, 65535);
                var streamUrl = $"http://localhost:{port}/stream";

                // Create and start the HTTP listener
                _streamHttpListener = new HttpListener();
                _streamHttpListener.Prefixes.Add($"{streamUrl}/");
                _streamHttpListener.Start();

                _currentStreamUrl = streamUrl;
                _logger.LogInformation("Started HTTP listener for Twilio streaming on {StreamUrl}", streamUrl);

                // Start a task to handle incoming requests
                _streamListenerTask = Task.Run(async () => await ProcessIncomingRequestsAsync());

                // Send TwiML to Twilio to connect the stream
                var twiml = new XDocument(
                    new XElement("Response",
                        new XElement("Connect",
                            new XElement("Stream", new XAttribute("url", streamUrl))
                        )
                    )
                );

                await SendTwiMLAsync(twiml.ToString(), CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting up streaming endpoint for call {CallSid}", _callSid);
                throw;
            }
        }

        private async Task ProcessIncomingRequestsAsync()
        {
            try
            {
                while (_streamHttpListener != null && _streamHttpListener.IsListening)
                {
                    var context = await _streamHttpListener.GetContextAsync();
                    _ = Task.Run(() => ProcessRequestAsync(context));
                }
            }
            catch (Exception ex)
            {
                if (_isConnected) // Only log if not caused by intentional shutdown
                {
                    _logger.LogError(ex, "Error in HTTP listener for call {CallSid}", _callSid);
                }
            }
        }

        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            try
            {
                // Handle the different types of requests
                if (context.Request.HttpMethod == "POST" && context.Request.Url!.AbsolutePath.EndsWith("/stream"))
                {
                    await ProcessStreamRequestAsync(context);
                }
                else
                {
                    // Unknown request
                    context.Response.StatusCode = 404;
                    context.Response.Close();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing request for call {CallSid}", _callSid);

                try
                {
                    context.Response.StatusCode = 500;
                    context.Response.Close();
                }
                catch
                {
                    // Ignore errors when closing the response
                }
            }
        }

        private async Task ProcessStreamRequestAsync(HttpListenerContext context)
        {
            try
            {
                // Read the request body
                using (var reader = new StreamReader(context.Request.InputStream))
                {
                    var json = await reader.ReadToEndAsync();
                    var streamData = JsonSerializer.Deserialize<TwilioStreamData>(json);

                    if (streamData?.Event == "media")
                    {
                        // Decode the base64 audio data
                        var audioBytes = Convert.FromBase64String(streamData.Media.Payload);

                        // Notify the conversation session
                        OnAudioReceived(audioBytes);
                    }
                    else if (streamData?.Event == "start")
                    {
                        _logger.LogInformation("Twilio media stream started for call {CallSid}", _callSid);
                    }
                    else if (streamData?.Event == "stop")
                    {
                        _logger.LogInformation("Twilio media stream stopped for call {CallSid}", _callSid);
                        await DisconnectAsync("Media stream stopped by Twilio");
                    }
                }

                // Send a 200 OK response
                context.Response.StatusCode = 200;
                context.Response.Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing stream request for call {CallSid}", _callSid);
                throw;
            }
        }

        private async Task StreamAudioToTwilioAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (_isConnected && !cancellationToken.IsCancellationRequested)
                {
                    byte[]? audioData = null;

                    lock (_audioBufferLock)
                    {
                        if (_audioBufferQueue.Count > 0)
                        {
                            audioData = _audioBufferQueue[0];
                            _audioBufferQueue.RemoveAt(0);
                        }
                    }

                    if (audioData != null)
                    {
                        // Convert to base64
                        var base64Audio = Convert.ToBase64String(audioData);

                        // Create media message
                        var mediaMessage = new
                        {
                            stream = new
                            {
                                track = "outbound",
                                chunk = "1", // Incremental chunk ID - should be managed properly
                                payload = base64Audio
                            }
                        };

                        // Send to Twilio
                        var json = JsonSerializer.Serialize(mediaMessage);
                        var response = await SendMediaToTwilioAsync(json, cancellationToken);

                        if (!response)
                        {
                            _logger.LogWarning("Failed to send media to Twilio for call {CallSid}", _callSid);
                        }
                    }
                    else
                    {
                        // No audio data available, wait a bit
                        await Task.Delay(20, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Normal cancellation
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error streaming audio to Twilio for call {CallSid}", _callSid);
            }
            finally
            {
                _isStreamingAudio = false;
            }
        }

        private async Task<bool> SendMediaToTwilioAsync(string json, CancellationToken cancellationToken)
        {
            // Note: In a real implementation, you would send this data to Twilio's Media API
            // For this example, we're just logging that we would send the data
            _logger.LogDebug("Would send media to Twilio for call {CallSid}: {Json}", _callSid, json);

            // In a real implementation, you might have code like this:
            /*
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "Twilio Media API URL");
                request.Headers.Authorization = new AuthenticationHeaderValue(
                    "Basic", 
                    Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_accountSid}:{_authToken}")));
                
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await client.SendAsync(request, cancellationToken);
                return response.IsSuccessStatusCode;
            }
            */

            return true;
        }

        private async Task<bool> SendTwiMLAsync(string twiml, CancellationToken cancellationToken)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, _twimlCallbackUrl);
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                        "Basic",
                        Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_accountSid}:{_authToken}")));

                    request.Content = new StringContent(twiml, Encoding.UTF8, "application/xml");

                    var response = await client.SendAsync(request, cancellationToken);

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("Failed to send TwiML for call {CallSid}: {StatusCode}",
                            _callSid, response.StatusCode);
                        return false;
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending TwiML for call {CallSid}", _callSid);
                return false;
            }
        }

        private class TwilioStreamData
        {
            public string Event { get; set; } = string.Empty;
            public TwilioStreamMedia? Media { get; set; }
        }

        private class TwilioStreamMedia
        {
            public string Track { get; set; } = string.Empty;
            public string Payload { get; set; } = string.Empty;
        }
    }
}
namespace IqraCore.Interfaces.Conversation
{
    public interface IConversationClientTransport : IDisposable
    {
        /// <summary>
        /// Fired when a binary message (e.g., raw audio) is received from the transport.
        /// </summary>
        event EventHandler<byte[]> BinaryMessageReceived;

        /// <summary>
        /// Fired when a text message (e.g., JSON payload) is received from the transport.
        /// </summary>
        event EventHandler<string> TextMessageReceived;

        /// <summary>
        /// Fired when the transport connection is terminated for any reason.
        /// The string provides the reason for disconnection.
        /// </summary>
        event EventHandler<string> Disconnected;

        /// <summary>
        /// Asynchronously sends binary data over the transport layer.
        /// </summary>
        /// <param name="data">The binary data to send.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        Task SendBinaryAsync(byte[] data, int sampleRate, int bitsPerSample, CancellationToken cancellationToken);

        /// <summary>
        /// Asynchronously sends text data over the transport layer.
        /// </summary>
        /// <param name="text">The string data to send.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        Task SendTextAsync(string text, CancellationToken cancellationToken);

        /// <summary>
        /// Asynchronously closes the transport connection.
        /// </summary>
        /// <param name="reason">The reason for disconnecting.</param>
        Task DisconnectAsync(string reason);
    }
}

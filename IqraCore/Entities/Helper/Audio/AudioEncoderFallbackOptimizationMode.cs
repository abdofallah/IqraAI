namespace IqraCore.Entities.Helper.Audio
{
    public enum AudioEncoderFallbackOptimizationMode
    {
        /// <summary>
        /// Prioritizes the computationally cheapest conversion path to minimize latency.
        /// This is ideal for real-time conversational AI.
        /// </summary>
        Performance,

        /// <summary>
        /// Prioritizes starting from the highest-fidelity source to maximize output quality,
        /// even at the cost of slightly higher latency. Penalizes "fake" quality improvements.
        /// </summary>
        Quality
    }
}

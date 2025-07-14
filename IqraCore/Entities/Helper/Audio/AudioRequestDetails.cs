namespace IqraCore.Entities.Helper.Audio
{
    public class AudioRequestDetails
    {
        public AudioEncodingTypeEnum RequestedEncoding { get; set; }
        public int RequestedSampleRateHz { get; set; }
        public int RequestedBitsPerSample { get; set; }
    }
}

namespace IqraInfrastructure.Managers.TurnEnd
{
    public class SmartTurnService : IDisposable
    {
        public event Action TurnEnded;
        public void ProcessAudio(byte[] audioData) { /* Feeds audio to the model */ }
        public void Dispose() { /* ... */ }
    }

}

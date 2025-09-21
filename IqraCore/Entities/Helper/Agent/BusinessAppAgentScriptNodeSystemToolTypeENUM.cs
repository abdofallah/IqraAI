namespace IqraCore.Entities.Helper.Agent
{
    public enum BusinessAppAgentScriptNodeSystemToolTypeENUM
    {
        Unknown = 0,
        EndCall = 1,
        ChangeLanguage = 2,
        GetDTMFKeypadInput = 3,
        PressDTMFKeypad = 4,
        TransferToAgent = 5,
        TransferToHuman = 6,
        AddScriptToContext = 7,
        SendSMS = 8,
        GoToNode = 9,
        RetrieveKnowledgeBase = 10
    }
}

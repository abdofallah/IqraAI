namespace IqraCore.Interfaces.RAG
{
    public interface ITextSplitter
    {
        List<string> SplitText(string text);
    }
}

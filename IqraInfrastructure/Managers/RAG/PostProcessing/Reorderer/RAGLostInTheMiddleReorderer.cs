using IqraCore.Models.RAG.Retrieval;

namespace IqraInfrastructure.Managers.RAG.PostProcessing.Reorderer
{
    public static class RAGLostInTheMiddleReorderer
    {
        public static List<RAGRetrievalDocumentModal> Reorder(List<RAGRetrievalDocumentModal> documents)
        {
            if (documents == null || documents.Count <= 2)
            {
                return documents;
            }

            var oddIndexed = new List<RAGRetrievalDocumentModal>();
            var evenIndexed = new List<RAGRetrievalDocumentModal>();

            for (int i = 0; i < documents.Count; i++)
            {
                if ((i + 1) % 2 != 0) // 1-based index is odd
                {
                    oddIndexed.Add(documents[i]);
                }
                else // 1-based index is even
                {
                    evenIndexed.Add(documents[i]);
                }
            }

            evenIndexed.Reverse();

            return oddIndexed.Concat(evenIndexed).ToList();
        }
    }
}

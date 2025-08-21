using System.ComponentModel.DataAnnotations;

namespace IqraCore.Models.RAG.Retrieval
{
    public record RAGRetrievalRequestModel
    {
        [Required]
        [MinLength(1)]
        public string Query { get; init; } = string.Empty;
    }
}

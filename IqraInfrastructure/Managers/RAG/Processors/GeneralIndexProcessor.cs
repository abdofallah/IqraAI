using IqraCore.Entities.Business;
using IqraCore.Entities.Business.App.KnowledgeBase;
using IqraCore.Entities.Business.App.KnowledgeBase.Configuration.Chunking;
using IqraCore.Entities.Helpers;
using IqraCore.Interfaces.AI;
using IqraCore.Models.Business.KnowledgeBase;
using IqraCore.Models.KnowledgeBase;
using IqraCore.Models.RAG;
using IqraInfrastructure.Managers.Embedding;
using IqraInfrastructure.Managers.RAG.Cleaning;
using IqraInfrastructure.Managers.RAG.Splitters;
using IqraInfrastructure.Repositories.Business;
using IqraInfrastructure.Repositories.KnowledgeBase.Vector;
using MongoDB.Bson;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace IqraInfrastructure.Managers.RAG.Processors
{
    public class GeneralIndexProcessor : IIndexProcessor
    {
        private readonly TextSplitterFactory _textSplitterFactory;
        private readonly EmbeddingProviderManager _embeddingManager;
        private readonly BusinessKnowledgeBaseDocumentRepository _documentRepository;
        private readonly KnowledgeBaseVectorRepository _vectorRepository;

        public GeneralIndexProcessor(
            TextSplitterFactory textSplitterFactory,
            EmbeddingProviderManager embeddingManager,
            BusinessKnowledgeBaseDocumentRepository documentRepository,
            KnowledgeBaseVectorRepository vectorRepository)
        {
            _textSplitterFactory = textSplitterFactory;
            _embeddingManager = embeddingManager;
            _documentRepository = documentRepository;
            _vectorRepository = vectorRepository;
        }

        public Task<List<ProcessedDocumentChunkModel>> TransformAsync(List<ExtractorDocumentModel> rawDocuments, BusinessAppKnowledgeBase knowledgeBase, long documentId)
        {
            var generalConfig = (BusinessAppKnowledgeBaseConfigurationGeneralChunking)knowledgeBase.Configuration.Chunking;

            // 1. Combine all extracted pages into a single text block.
            var fullText = string.Join("\n\n", rawDocuments.Select(d => d.PageContent));

            // 2. Clean the text based on the user's rules.
            var cleanedText = TextCleaner.Clean(fullText, generalConfig.Preprocess);

            // 3. Create the appropriate text splitter.
            var splitter = _textSplitterFactory.Create(generalConfig);

            // 4. Split the cleaned text into chunks.
            var textChunks = splitter.SplitText(cleanedText);

            // 5. Convert text chunks into our processed DTO.
            var processedChunks = textChunks.Select(textChunk => new ProcessedDocumentChunkModel
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Text = textChunk,
                Hash = GenerateHash(textChunk),
                OriginalDocumentId = documentId,
                Metadata = rawDocuments.FirstOrDefault()?.Metadata ?? new Dictionary<string, object>()
            }).ToList();

            return Task.FromResult(processedChunks);
        }

        public async Task<FunctionReturnResult> LoadAsync(List<ProcessedDocumentChunkModel> chunks, BusinessAppKnowledgeBase knowledgeBase, BusinessAppIntegration embeddingIntegration)
        {
            var result = new FunctionReturnResult();

            try
            {
                // 1. Get embeddings for all chunks in a single batch.
                var textsToEmbed = chunks.Select(c => c.Text).ToList();

                FunctionReturnResult<IEmbeddingService?> embeddingProvider = await _embeddingManager.BuildProviderServiceByIntegration(
                    embeddingIntegration,
                    knowledgeBase.Configuration.Embedding
                );
                if (!embeddingProvider.Success)
                {
                    return result.SetFailureResult(
                        "LoadAsync:EMBEDDING_PROVIDER_BUILD_ERROR",
                        embeddingProvider.Message
                    );
                }

                var vectorsResult = await embeddingProvider.Data.GenerateEmbeddingForTextListAsync(textsToEmbed);
                if (!vectorsResult.Success)
                {
                    return result.SetFailureResult(
                        "LoadAsync:EMBEDDING_GENERATION_ERROR",
                        vectorsResult.Message
                    );
                }
                
                if (vectorsResult.Data.Count != chunks.Count)
                {
                    return result.SetFailureResult(
                        "LoadAsync:EMBEDDING_GENERATION_ERROR",
                        "Number of generated embeddings does not match the number of chunks."
                    );
                }

                for (int i = 0; i < chunks.Count; i++)
                {
                    chunks[i].Vector = vectorsResult.Data[i];
                }

                var collectionName = $"b{knowledgeBase.Id}_kb{knowledgeBase.Id}"; // Assuming your business ID is stored in the KB object or accessible here
                var originalDocument = await _documentRepository.GetDocumentByIdAsync(chunks.First().OriginalDocumentId);

                var chunksForVectorStore = chunks.Select(c => new KnowledgeBaseChunkModel
                {
                    DocumentName = originalDocument?.Name ?? "Unknown",
                    TextChunk = c.Text,
                    Embedding = new ReadOnlyMemory<float>(c.Vector)
                });
                await _vectorRepository.AddChunksAsync(collectionName, chunksForVectorStore);

                var updateRequest = new UpdateChunksRequest
                {
                    Added = chunks.Select(c => new AddedChunkInfo { Id = c.Id, Text = c.Text }).ToList()
                };
                await _documentRepository.UpdateDocumentChunksAsync(chunks.First().OriginalDocumentId, updateRequest);

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "LoadAsync:EXCEPTION",
                    ex.Message
                );
            }
        }

        private string GenerateHash(string text)
        {
            using (var sha256 = SHA256.Create())
            {
                var settings = new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore };
                var serializedText = JsonConvert.SerializeObject(text, settings);
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(serializedText));
                return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}

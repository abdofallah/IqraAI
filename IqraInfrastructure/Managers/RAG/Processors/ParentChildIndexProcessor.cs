using IqraCore.Entities.Business;
using IqraCore.Entities.Business.App.KnowledgeBase;
using IqraCore.Entities.Business.App.KnowledgeBase.Configuration.Chunking;
using IqraCore.Entities.Business.App.KnowledgeBase.Document;
using IqraCore.Entities.Business.App.KnowledgeBase.Document.Chunk;
using IqraCore.Entities.Business.App.KnowledgeBase.ENUM;
using IqraCore.Entities.Helpers;
using IqraCore.Interfaces.AI;
using IqraCore.Interfaces.RAG;
using IqraCore.Models.KnowledgeBase;
using IqraCore.Models.RAG;
using IqraInfrastructure.Managers.Embedding;
using IqraInfrastructure.Managers.RAG.Cleaning;
using IqraInfrastructure.Managers.RAG.Keywords;
using IqraInfrastructure.Managers.RAG.Splitters;
using IqraInfrastructure.Repositories.Business;
using IqraInfrastructure.Repositories.KnowledgeBase.Vector;
using IqraInfrastructure.Repositories.RAG;
using MongoDB.Bson;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace IqraInfrastructure.Managers.RAG.Processors
{
    public class ParentChildIndexProcessor : IIndexProcessor
    {
        private readonly TextSplitterFactory _textSplitterFactory;
        private readonly EmbeddingProviderManager _embeddingManager;
        private readonly BusinessKnowledgeBaseDocumentRepository _documentRepository;
        private readonly KnowledgeBaseVectorRepository _vectorRepository;
        private readonly KeywordExtractor _keywordExtractor;
        private readonly RAGKeywordStore _keywordStore;

        public ParentChildIndexProcessor(
            TextSplitterFactory textSplitterFactory,
            EmbeddingProviderManager embeddingManager,
            BusinessKnowledgeBaseDocumentRepository documentRepository,
            KnowledgeBaseVectorRepository vectorRepository,
            KeywordExtractor keywordExtractor,
            RAGKeywordStore keywordStore
        )
        {
            _textSplitterFactory = textSplitterFactory;
            _embeddingManager = embeddingManager;
            _documentRepository = documentRepository;
            _vectorRepository = vectorRepository;
            _keywordExtractor = keywordExtractor;
            _keywordStore = keywordStore;
        }

        public Task<List<ProcessedDocumentChunkModel>> TransformAsync(List<ExtractorDocumentModel> rawDocuments, BusinessAppKnowledgeBase knowledgeBase, long documentId)
        {
            var parentChildConfig = (BusinessAppKnowledgeBaseConfigurationParentChildChunking)knowledgeBase.Configuration.Chunking;

            // 1. Combine and Clean Text
            var fullText = string.Join("\n\n", rawDocuments.Select(d => d.PageContent));
            var cleanedText = TextCleaner.Clean(fullText, parentChildConfig.Preprocess);

            // 2. Create Parent Chunks
            List<string> parentTextChunks;
            if (parentChildConfig.Parent.Type == KnowledgeBaseChunkingParentChunkType.FullDoc)
            {
                parentTextChunks = new List<string> { cleanedText };
            }
            else // Paragraph
            {
                var parentSplitter = _textSplitterFactory.Create(parentChildConfig, SplitterType.Parent);
                parentTextChunks = parentSplitter.SplitText(cleanedText);
            }

            var allProcessedChunks = new List<ProcessedDocumentChunkModel>();

            // 3. Create Child Chunks for each Parent
            var childSplitter = _textSplitterFactory.Create(parentChildConfig, SplitterType.Child);

            foreach (var parentText in parentTextChunks)
            {
                var parentId = ObjectId.GenerateNewId().ToString();
                var parentChunk = new ProcessedDocumentChunkModel
                {
                    Id = parentId,
                    Text = parentText,
                    Hash = GenerateHash(parentText),
                    OriginalDocumentId = documentId,
                    IsParent = true,
                    Children = new List<ProcessedDocumentChunkModel>()
                };

                var childTextChunks = childSplitter.SplitText(parentText);

                foreach (var childText in childTextChunks)
                {
                    var childChunk = new ProcessedDocumentChunkModel
                    {
                        Id = ObjectId.GenerateNewId().ToString(),
                        Text = childText,
                        Hash = GenerateHash(childText),
                        OriginalDocumentId = documentId,
                        IsParent = false,
                        ParentId = parentId,
                        // Attach parent text to metadata for potential use during retrieval context
                        Metadata = new Dictionary<string, object>
                        {
                            { "parent_text", parentText },
                            { "parent_id", parentId }
                        }
                    };
                    parentChunk.Children.Add(childChunk);
                }

                // Only add the parent if it has children
                if (parentChunk.Children.Any())
                {
                    allProcessedChunks.Add(parentChunk);
                }
            }

            return Task.FromResult(allProcessedChunks);
        }

        public async Task<FunctionReturnResult> LoadAsync(List<ProcessedDocumentChunkModel> chunks, BusinessAppKnowledgeBase knowledgeBase, BusinessAppKnowledgeBaseDocument knowledgeBaseDocument, BusinessAppIntegration embeddingIntegration, long businessId)
        {
            var result = new FunctionReturnResult();

            if (!chunks.Any())
            {
                return result.SetSuccessResult();
            }

            try
            {
                // Flatten the structure to get a list of all child chunks for embedding
                var allChildChunks = chunks.SelectMany(parent => parent.Children!).ToList();

                if (!allChildChunks.Any())
                {
                    return result.SetSuccessResult(); // Nothing to embed or save
                }

                // Get embeddings for all CHILD chunks
                var textsToEmbed = allChildChunks.Select(c => c.Text).ToList();

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

                if (vectorsResult.Data.Count != allChildChunks.Count)
                {
                    return result.SetFailureResult(
                        "LoadAsync:EMBEDDING_COUNT_MISMATCH",
                        "Number of generated embeddings does not match the number of child chunks."
                    );
                }

                for (int i = 0; i < allChildChunks.Count; i++)
                {
                    allChildChunks[i].Vector = vectorsResult.Data[i];
                }

                var childChunkKeywords = new Dictionary<string, List<string>>();
                foreach (var childChunk in allChildChunks)
                {
                    childChunkKeywords[childChunk.Id] = _keywordExtractor.Extract(childChunk.Text);
                }
                await _keywordStore.AddChunksKeywordsAsync(knowledgeBase.Id, childChunkKeywords);

                // Save PARENT and CHILD chunks to the document store
                var parentChunksToSave = chunks.Select(p =>
                {
                    BusinessAppKnowledgeBaseDocumentChunk chunk = new BusinessAppKnowledgeBaseDocumentParentChunk()
                    {
                        Id = p.Id,
                        Text = p.Text,
                        ChildrenIds = p.Children!.Select(c => c.Id).ToList()
                    };

                    return chunk;
                });

                var childChunksToSave = allChildChunks.Select(c =>
                {
                    BusinessAppKnowledgeBaseDocumentChunk chunk = new BusinessAppKnowledgeBaseDocumentChildChunk()
                    {
                        Id = c.Id,
                        Text = c.Text,
                        ParentId = c.ParentId!
                    };

                    return chunk;
                });

                var chunksForMetaStore = parentChunksToSave.Concat(childChunksToSave).ToList();

                bool chunksMetaAdded = await _documentRepository.AddDocumentChunksAsync(knowledgeBaseDocument.Id, chunksForMetaStore);
                if (!chunksMetaAdded)
                {
                    return result.SetFailureResult(
                        "LoadAsync:DOCUMENT_STORE_ERROR",
                        "Failed to add chunks to document store."
                    );
                }

                // Save CHILD chunks to the vector store
                var collectionName = $"b{businessId}_kb{knowledgeBase.Id}";
                var chunksForVectorStore = allChildChunks.Select(c =>
                {
                    var vectorChunk = new VectorKnowledgeBaseChunkModel
                    {
                        DocumentId = knowledgeBaseDocument.Id,
                        ChunkId = c.Id,
                        TextChunk = c.Text,
                        ParentChunkId = c.ParentId,
                        Embedding = new ReadOnlyMemory<float>(c.Vector)
                    };

                    return vectorChunk;
                });
                bool vectorStoreSuccess = await _vectorRepository.AddChunksAsync(collectionName, chunksForVectorStore);
                if (!vectorStoreSuccess)
                {
                    return result.SetFailureResult("LoadAsync:VECTOR_STORE_ERROR", "Failed to add child chunks to vector store.");
                }  

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("LoadAsync:EXCEPTION", ex.Message);
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

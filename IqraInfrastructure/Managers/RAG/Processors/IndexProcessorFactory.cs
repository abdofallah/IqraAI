using IqraCore.Entities.Business.App.KnowledgeBase;
using IqraCore.Entities.Business.App.KnowledgeBase.ENUM;
using IqraCore.Interfaces.RAG;
using IqraInfrastructure.Managers.Embedding;
using IqraInfrastructure.Managers.RAG.Keywords;
using IqraInfrastructure.Managers.RAG.Splitters;
using IqraInfrastructure.Repositories.Business;
using IqraInfrastructure.Repositories.KnowledgeBase.Vector;
using IqraInfrastructure.Repositories.RAG;

namespace IqraInfrastructure.Managers.RAG.Processors
{
    public class IndexProcessorFactory
    {
        private readonly TextSplitterFactory _textSplitterFactory;
        private readonly EmbeddingProviderManager _embeddingManager;
        private readonly BusinessKnowledgeBaseDocumentRepository _documentRepository;
        private readonly KnowledgeBaseVectorRepository _vectorRepository;
        private readonly RAGKeywordStore _keywordStore;
        private readonly KeywordExtractor _keywordExtractor;

        public IndexProcessorFactory(
            TextSplitterFactory textSplitterFactory,
            EmbeddingProviderManager embeddingManager,
            BusinessKnowledgeBaseDocumentRepository documentRepository,
            KnowledgeBaseVectorRepository vectorRepository,
            RAGKeywordStore keywordStore,
            KeywordExtractor keywordExtractor
        )
        {
            _textSplitterFactory = textSplitterFactory;
            _embeddingManager = embeddingManager;
            _documentRepository = documentRepository;
            _vectorRepository = vectorRepository;
            _keywordStore = keywordStore;
            _keywordExtractor = keywordExtractor;
        }

        public IIndexProcessor Create(BusinessAppKnowledgeBase knowledgeBase)
        {
            var chunkingType = knowledgeBase.Configuration.Chunking.Type;

            switch (chunkingType)
            {
                case KnowledgeBaseChunkingType.General:
                    return new GeneralIndexProcessor(
                        _textSplitterFactory,
                        _embeddingManager,
                        _documentRepository,
                        _vectorRepository,
                        _keywordExtractor,
                        _keywordStore
                    );

                case KnowledgeBaseChunkingType.ParentChild:
                    return new ParentChildIndexProcessor(
                        _textSplitterFactory,
                        _embeddingManager,
                        _documentRepository,
                        _vectorRepository,
                        _keywordExtractor,
                        _keywordStore
                    );

                default:
                    throw new NotSupportedException($"Chunking type '{chunkingType}' is not supported by the factory.");
            }
        }
    }
}

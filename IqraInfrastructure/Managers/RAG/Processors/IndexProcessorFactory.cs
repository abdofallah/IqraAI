using IqraCore.Entities.Business.App.KnowledgeBase;
using IqraCore.Entities.Business.App.KnowledgeBase.ENUM;
using IqraCore.Interfaces.RAG;
using IqraInfrastructure.Managers.Embedding;
using IqraInfrastructure.Managers.RAG.Splitters;
using IqraInfrastructure.Repositories.Business;
using IqraInfrastructure.Repositories.KnowledgeBase.Vector;

namespace IqraInfrastructure.Managers.RAG.Processors
{
    public class IndexProcessorFactory
    {
        private readonly TextSplitterFactory _textSplitterFactory;
        private readonly EmbeddingProviderManager _embeddingManager;
        private readonly BusinessKnowledgeBaseDocumentRepository _documentRepository;
        private readonly KnowledgeBaseVectorRepository _vectorRepository;

        public IndexProcessorFactory(
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
                        _vectorRepository
                    );

                case KnowledgeBaseChunkingType.ParentChild:
                    return new ParentChildIndexProcessor(
                        _textSplitterFactory,
                        _embeddingManager,
                        _documentRepository,
                        _vectorRepository
                    );

                default:
                    throw new NotSupportedException($"Chunking type '{chunkingType}' is not supported by the factory.");
            }
        }
    }
}

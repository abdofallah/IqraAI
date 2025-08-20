using IqraCore.Entities.Business.App.KnowledgeBase.Document;
using IqraCore.Entities.Business.App.KnowledgeBase.Document.Chunk;
using IqraCore.Entities.Business.App.KnowledgeBase.ENUM;
using IqraCore.Models.Business.KnowledgeBase;
using IqraInfrastructure.Repositories.Counter;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace IqraInfrastructure.Repositories.Business
{
    public class BusinessKnowledgeBaseDocumentRepository
    {
        private readonly ILogger<BusinessKnowledgeBaseDocumentRepository> _logger;

        private readonly string CollectionName = "BusinessKnowledgeBaseDocuments";
        private readonly IMongoCollection<BusinessAppKnowledgeBaseDocument> _documentsCollection;

        private readonly CounterRepository _counterRepository;

        public BusinessKnowledgeBaseDocumentRepository(ILogger<BusinessKnowledgeBaseDocumentRepository> logger, IMongoClient client, string databaseName)
        {
            _logger = logger;
            var database = client.GetDatabase(databaseName);
            _documentsCollection = database.GetCollection<BusinessAppKnowledgeBaseDocument>(CollectionName);
            _counterRepository = new CounterRepository(client, databaseName);
        }

        public async Task<long> GetNextDocumentId()
        {
            return await _counterRepository.GetNextSequenceValueAsync("documentId");
        }

        public async Task<BusinessAppKnowledgeBaseDocument?> GetDocumentByIdAsync(long documentId)
        {
            var filter = Builders<BusinessAppKnowledgeBaseDocument>.Filter.Eq(d => d.Id, documentId);
            return await _documentsCollection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<List<BusinessAppKnowledgeBaseDocument>?> GetDocumentsByIdsAsync(List<long> documentIds)
        {
            var filter = Builders<BusinessAppKnowledgeBaseDocument>.Filter.In(d => d.Id, documentIds);
            return await _documentsCollection.Find(filter).ToListAsync();
        }

        public async Task<bool> CreateDocumentAsync(BusinessAppKnowledgeBaseDocument document, IClientSessionHandle? session = null)
        {
            try
            {
                if (session != null)
                {
                    await _documentsCollection.InsertOneAsync(session, document);
                }
                else
                {
                    await _documentsCollection.InsertOneAsync(document);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create knowledge base document with ID {DocumentId}", document.Id);
                return false;
            }
        }

        public async Task<bool> DeleteDocumentAsync(long documentId, IClientSessionHandle? session = null)
        {
            var filter = Builders<BusinessAppKnowledgeBaseDocument>.Filter.Eq(d => d.Id, documentId);
            var result = session != null
                ? await _documentsCollection.DeleteOneAsync(session, filter)
                : await _documentsCollection.DeleteOneAsync(filter);

            return result.IsAcknowledged;
        }

        public async Task<bool> UpdateDocumentStatusAsync(long documentId, KnowledgeBaseDocumentStatus status, string? failedReason = null, IClientSessionHandle? session = null)
        {
            var filter = Builders<BusinessAppKnowledgeBaseDocument>.Filter.Eq(d => d.Id, documentId);
            var update = Builders<BusinessAppKnowledgeBaseDocument>.Update.Set(d => d.Status, status).Set(d => d.FailedReason, failedReason);
            var result = session != null
                ? await _documentsCollection.UpdateOneAsync(session, filter, update)
                : await _documentsCollection.UpdateOneAsync(filter, update);
            return result.IsAcknowledged;
        }

        /**
         * 
         * 
         * Document Chunks
         * 
         * 
        **/ 

        public async Task<bool> AddDocumentChunkAsync(long documentId, BusinessAppKnowledgeBaseDocumentChunk chunk, IClientSessionHandle? session = null)
        {
            var filter = Builders<BusinessAppKnowledgeBaseDocument>.Filter.Eq(d => d.Id, documentId);
            var update = Builders<BusinessAppKnowledgeBaseDocument>.Update.Push(d => d.Chunks, chunk);
            var result = session != null
                ? await _documentsCollection.UpdateOneAsync(session, filter, update)
                : await _documentsCollection.UpdateOneAsync(filter, update);
            return result.IsAcknowledged;
        }

        public async Task<bool> AddDocumentChunksAsync(long documentId, IEnumerable<BusinessAppKnowledgeBaseDocumentChunk> chunks, IClientSessionHandle? session = null)
        {
            var filter = Builders<BusinessAppKnowledgeBaseDocument>.Filter.Eq(d => d.Id, documentId);
            var update = Builders<BusinessAppKnowledgeBaseDocument>.Update.PushEach(d => d.Chunks, chunks);
            var result = session != null
                ? await _documentsCollection.UpdateOneAsync(session, filter, update)
                : await _documentsCollection.UpdateOneAsync(filter, update);
            return result.IsAcknowledged;
        }

        public async Task<bool> DeleteDocumentChunkAsync(long documentId, string chunkId, IClientSessionHandle? session = null)
        {
            var filter = Builders<BusinessAppKnowledgeBaseDocument>.Filter.Eq(d => d.Id, documentId);
            var update = Builders<BusinessAppKnowledgeBaseDocument>.Update.PullFilter(
                d => d.Chunks,
                c => c.Id == chunkId
            );

            var result = session != null
                ? await _documentsCollection.UpdateOneAsync(session, filter, update)
                : await _documentsCollection.UpdateOneAsync(filter, update);
            return result.IsAcknowledged;
        }

        public async Task<bool> DeleteDocumentChunksAsync(long documentId, List<string> chunkIds, IClientSessionHandle? session = null)
        {
            var filter = Builders<BusinessAppKnowledgeBaseDocument>.Filter.Eq(d => d.Id, documentId);
            var update = Builders<BusinessAppKnowledgeBaseDocument>.Update.PullFilter(
                d => d.Chunks,
                c => chunkIds.Contains(c.Id)
            );

            var result = session != null
                ? await _documentsCollection.UpdateOneAsync(session, filter, update)
                : await _documentsCollection.UpdateOneAsync(filter, update);
            return result.IsAcknowledged;
        }

        public async Task<bool> UpdateDocumentChunkAsync(long documentId, BusinessAppKnowledgeBaseDocumentChunk chunkData, IClientSessionHandle? session = null)
        {
            var filter = Builders<BusinessAppKnowledgeBaseDocument>.Filter.And(
                Builders<BusinessAppKnowledgeBaseDocument>.Filter.Eq(d => d.Id, documentId),
                Builders<BusinessAppKnowledgeBaseDocument>.Filter.ElemMatch(
                    d => d.Chunks,
                    c => c.Id == chunkData.Id
                )
            );

            var update = Builders<BusinessAppKnowledgeBaseDocument>.Update.Set(
               d => d.Chunks.FirstMatchingElement(),
               chunkData
           );

            var result = session != null
                ? await _documentsCollection.UpdateOneAsync(session, filter, update)
                : await _documentsCollection.UpdateOneAsync(filter, update);

            return result.IsAcknowledged;
        }

        public async Task<bool> UpdateDocumentChunksAsync(long documentId, List<BusinessAppKnowledgeBaseDocumentChunk> chunksData, IClientSessionHandle? session = null)
        {
            var updateModels = new List<WriteModel<BusinessAppKnowledgeBaseDocument>>();

            foreach (var chunk in chunksData)
            {
                var filter = Builders<BusinessAppKnowledgeBaseDocument>.Filter.And(
                    Builders<BusinessAppKnowledgeBaseDocument>.Filter.Eq(d => d.Id, documentId),
                    Builders<BusinessAppKnowledgeBaseDocument>.Filter.ElemMatch(d => d.Chunks, c => c.Id == chunk.Id)
                );

                var update = Builders<BusinessAppKnowledgeBaseDocument>.Update.Set(
                    d => d.Chunks.FirstMatchingElement(),
                    chunk
                );

                updateModels.Add(new UpdateOneModel<BusinessAppKnowledgeBaseDocument>(filter, update));
            }

            var result = session != null
            ? await _documentsCollection.BulkWriteAsync(session, updateModels)
            : await _documentsCollection.BulkWriteAsync(updateModels);

            return result.IsAcknowledged;
        }
    }
}

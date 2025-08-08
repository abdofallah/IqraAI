using IqraCore.Entities.Business.App.KnowledgeBase.Document;
using IqraCore.Entities.Business.App.KnowledgeBase.Document.Chunk;
using IqraCore.Models.Business.KnowledgeBase;
using IqraInfrastructure.Repositories.Counter;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories.Business
{
    public class BusinessKnowledgeBaseDocumentRepository
    {
        private readonly ILogger<BusinessKnowledgeBaseDocumentRepository> _logger;

        private readonly string CollectionName = "BusinessKnowledgeBaseDocuments";
        private readonly IMongoCollection<BusinessAppKnowledgeBaseDocument> _documentsCollection;

        // Using the same CounterRepository pattern for auto-incrementing long IDs
        private readonly CounterRepository _counterRepository;

        public BusinessKnowledgeBaseDocumentRepository(ILogger<BusinessKnowledgeBaseDocumentRepository> logger, IMongoClient client, string databaseName)
        {
            _logger = logger;
            var database = client.GetDatabase(databaseName);
            _documentsCollection = database.GetCollection<BusinessAppKnowledgeBaseDocument>(CollectionName);
            _counterRepository = new CounterRepository(client, databaseName);
        }

        /// <summary>
        /// Atomically increments and retrieves the next available ID for a document.
        /// </summary>
        public async Task<long> GetNextDocumentId()
        {
            return await _counterRepository.GetNextSequenceValueAsync("documentId");
        }

        /// <summary>
        /// Retrieves a single document by its unique ID.
        /// </summary>
        public async Task<BusinessAppKnowledgeBaseDocument?> GetDocumentByIdAsync(long documentId)
        {
            var filter = Builders<BusinessAppKnowledgeBaseDocument>.Filter.Eq(d => d.Id, documentId);
            return await _documentsCollection.Find(filter).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Creates a new document in the collection.
        /// </summary>
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

        /// <summary>
        /// Deletes a document from the collection by its ID.
        /// </summary>
        public async Task<bool> DeleteDocumentAsync(long documentId, IClientSessionHandle? session = null)
        {
            var filter = Builders<BusinessAppKnowledgeBaseDocument>.Filter.Eq(d => d.Id, documentId);
            var result = session != null
                ? await _documentsCollection.DeleteOneAsync(session, filter)
                : await _documentsCollection.DeleteOneAsync(filter);

            return result.IsAcknowledged && result.DeletedCount > 0;
        }

        /// <summary>
        /// Applies a batch of chunk changes (add, edit, delete) to a specific document efficiently.
        /// </summary>
        public async Task<bool> UpdateDocumentChunksAsync(long documentId, UpdateChunksRequest changes, IClientSessionHandle? session = null)
        {
            var filter = Builders<BusinessAppKnowledgeBaseDocument>.Filter.Eq(d => d.Id, documentId);
            var updateDefinitions = new List<UpdateDefinition<BusinessAppKnowledgeBaseDocument>>();

            // 1. Handle Deletions
            if (changes.Deleted != null && changes.Deleted.Any())
            {
                var deleteFilter = Builders<BusinessAppKnowledgeBaseDocumentChunk>.Filter.In(c => c.Id, changes.Deleted);
                updateDefinitions.Add(Builders<BusinessAppKnowledgeBaseDocument>.Update.PullFilter(d => d.Chunks, deleteFilter));
            }

            // 2. Handle Additions
            if (changes.Added != null && changes.Added.Any())
            {
                var newChunks = new List<BusinessAppKnowledgeBaseDocumentChunk>();
                // This logic would be expanded in the manager to determine if a chunk is General, Parent, or Child
                foreach (var addedChunkInfo in changes.Added)
                {
                    // For simplicity, we'll treat them as GeneralChunks here.
                    // The manager layer would construct the correct object type.
                    newChunks.Add(new BusinessAppKnowledgeBaseDocumentGeneralChunk { Id = addedChunkInfo.Id, Text = addedChunkInfo.Text });
                }
                updateDefinitions.Add(Builders<BusinessAppKnowledgeBaseDocument>.Update.PushEach(d => d.Chunks, newChunks));
            }

            // 3. Handle Edits (This is the most complex part)
            if (changes.Edited != null && changes.Edited.Any())
            {
                foreach (var editedChunkInfo in changes.Edited)
                {
                    // Create an update definition for each chunk that needs its text changed.
                    var update = Builders<BusinessAppKnowledgeBaseDocument>.Update.Set("Chunks.$[elem].Text", editedChunkInfo.Text);
                    var arrayFilter = new BsonDocumentArrayFilterDefinition<BsonDocument>(
                        new BsonDocument("elem._id", editedChunkInfo.Id)
                    );

                    var updateOptions = new UpdateOptions { ArrayFilters = new[] { arrayFilter } };

                    // Since we can't combine updates with different arrayFilters, we have to run them sequentially.
                    // A more advanced (but complex) approach might involve bulk writes. For this pattern, sequential updates are clearer.
                    var editResult = session != null
                        ? await _documentsCollection.UpdateOneAsync(session, filter, update, updateOptions)
                        : await _documentsCollection.UpdateOneAsync(filter, update, updateOptions);

                    if (!editResult.IsAcknowledged) return false;
                }
            }

            // Combine and execute updates for add/delete
            if (updateDefinitions.Any())
            {
                var combinedUpdate = Builders<BusinessAppKnowledgeBaseDocument>.Update.Combine(updateDefinitions);
                var result = session != null
                    ? await _documentsCollection.UpdateOneAsync(session, filter, combinedUpdate)
                    : await _documentsCollection.UpdateOneAsync(filter, combinedUpdate);

                return result.IsAcknowledged;
            }

            return true; // Return true if only edits were performed and all were successful
        }
    }
}

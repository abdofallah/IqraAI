using IqraInfrastructure.Managers.RAG.Keywords;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories.RAG
{
    public class KeywordIndex
    {
        [BsonId]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        [BsonElement("KnowledgeBaseId")]
        public string KnowledgeBaseId { get; set; } = string.Empty;

        [BsonElement("Keyword")]
        public string Keyword { get; set; } = string.Empty;

        [BsonElement("ChunkIds")]
        public HashSet<string> ChunkIds { get; set; } = new HashSet<string>();
    }

    public class RAGKeywordStore
    {
        private readonly IMongoCollection<KeywordIndex> _keywordCollection;
        private readonly KeywordExtractor _keywordExtractor;
        private readonly ILogger<RAGKeywordStore> _logger;
        private const string CollectionName = "KnowledgeBaseKeywordIndex";

        public RAGKeywordStore(IMongoClient client, string databaseName, KeywordExtractor keywordExtractor, ILogger<RAGKeywordStore> logger)
        {
            var database = client.GetDatabase(databaseName);
            _keywordCollection = database.GetCollection<KeywordIndex>(CollectionName);
            _keywordExtractor = keywordExtractor;
            _logger = logger;

            // Ensure unique index for efficient upserts and queries
            var indexKeys = Builders<KeywordIndex>.IndexKeys.Ascending(i => i.KnowledgeBaseId).Ascending(i => i.Keyword);
            var indexModel = new CreateIndexModel<KeywordIndex>(indexKeys, new CreateIndexOptions { Unique = true });
            _keywordCollection.Indexes.CreateOne(indexModel);
        }

        public async Task AddChunksKeywordsAsync(string knowledgeBaseId, Dictionary<string, List<string>> chunkKeywords, IClientSessionHandle? session = null)
        {
            if (chunkKeywords == null || !chunkKeywords.Any())
            {
                return;
            }

            var keywordToChunkMap = new Dictionary<string, HashSet<string>>();

            foreach (var chunk in chunkKeywords)
            {
                var chunkId = chunk.Key;
                foreach (var keyword in chunk.Value)
                {
                    if (!keywordToChunkMap.ContainsKey(keyword))
                    {
                        keywordToChunkMap[keyword] = new HashSet<string>();
                    }
                    keywordToChunkMap[keyword].Add(chunkId);
                }
            }

            if (!keywordToChunkMap.Any())
            {
                return;
            }

            var bulkOps = new List<WriteModel<KeywordIndex>>();
            foreach (var entry in keywordToChunkMap)
            {
                var keyword = entry.Key;
                var chunkIds = entry.Value;

                var filter = Builders<KeywordIndex>.Filter.And(
                    Builders<KeywordIndex>.Filter.Eq(i => i.KnowledgeBaseId, knowledgeBaseId),
                    Builders<KeywordIndex>.Filter.Eq(i => i.Keyword, keyword)
                );

                var update = Builders<KeywordIndex>.Update
                    .PushEach(i => i.ChunkIds, chunkIds)
                    .SetOnInsert(i => i.KnowledgeBaseId, knowledgeBaseId)
                    .SetOnInsert(i => i.Keyword, keyword);

                var upsertModel = new UpdateOneModel<KeywordIndex>(filter, update) { IsUpsert = true };
                bulkOps.Add(upsertModel);
            }

            if (session != null)
            {
                await _keywordCollection.BulkWriteAsync(session, bulkOps);
            }
            else
            {
                await _keywordCollection.BulkWriteAsync(bulkOps);
            }
        }

        public async Task UpdateChunkKeywordsAsync(string knowledgeBaseId, string chunkId, List<string> oldKeywords, List<string> newKeywords, IClientSessionHandle session)
        {
            var keywordsToRemove = oldKeywords.Except(newKeywords).ToList();
            var keywordsToAdd = newKeywords.Except(oldKeywords).ToList();

            var bulkOps = new List<WriteModel<KeywordIndex>>();

            // Operation to remove chunkId from old keywords
            if (keywordsToRemove.Any())
            {
                var pullFilter = Builders<KeywordIndex>.Filter.And(
                    Builders<KeywordIndex>.Filter.Eq(i => i.KnowledgeBaseId, knowledgeBaseId),
                    Builders<KeywordIndex>.Filter.In(i => i.Keyword, keywordsToRemove)
                );
                var pullUpdate = Builders<KeywordIndex>.Update.Pull(i => i.ChunkIds, chunkId);
                bulkOps.Add(new UpdateManyModel<KeywordIndex>(pullFilter, pullUpdate));
            }

            // Operation to add chunkId to new keywords (upsert logic)
            if (keywordsToAdd.Any())
            {
                foreach (var keyword in keywordsToAdd)
                {
                    var pushFilter = Builders<KeywordIndex>.Filter.And(
                        Builders<KeywordIndex>.Filter.Eq(i => i.KnowledgeBaseId, knowledgeBaseId),
                        Builders<KeywordIndex>.Filter.Eq(i => i.Keyword, keyword)
                    );
                    var pushUpdate = Builders<KeywordIndex>.Update
                        .AddToSet(i => i.ChunkIds, chunkId) // Use AddToSet for safety
                        .SetOnInsert(i => i.KnowledgeBaseId, knowledgeBaseId)
                        .SetOnInsert(i => i.Keyword, keyword);

                    bulkOps.Add(new UpdateOneModel<KeywordIndex>(pushFilter, pushUpdate) { IsUpsert = true });
                }
            }

            if (!bulkOps.Any()) return;

            await _keywordCollection.BulkWriteAsync(session, bulkOps);
        }

        public async Task RemoveChunkReferencesAsync(string knowledgeBaseId, List<string> chunkIds, IClientSessionHandle session)
        {
            if (chunkIds == null || !chunkIds.Any())
            {
                return;
            }

            var filter = Builders<KeywordIndex>.Filter.And(
                Builders<KeywordIndex>.Filter.Eq(i => i.KnowledgeBaseId, knowledgeBaseId),
                Builders<KeywordIndex>.Filter.AnyIn(i => i.ChunkIds, chunkIds) // Find docs where ChunkIds array contains any of the specified chunkIds
            );

            var update = Builders<KeywordIndex>.Update.PullAll(i => i.ChunkIds, chunkIds);

            await _keywordCollection.UpdateManyAsync(session, filter, update);
        }

        public async Task<List<string>> SearchAsync(string knowledgeBaseId, string query, int topK)
        {
            var queryKeywords = _keywordExtractor.Extract(query);

            if (!queryKeywords.Any())
            {
                return new List<string>();
            }

            var filter = Builders<KeywordIndex>.Filter.And(
                Builders<KeywordIndex>.Filter.Eq(i => i.KnowledgeBaseId, knowledgeBaseId),
                Builders<KeywordIndex>.Filter.In(i => i.Keyword, queryKeywords)
            );

            var matchingKeywords = await _keywordCollection.Find(filter).ToListAsync();

            if (!matchingKeywords.Any())
            {
                return new List<string>();
            }

            var chunkScores = new Dictionary<string, int>();
            foreach (var keywordIndex in matchingKeywords)
            {
                foreach (var chunkId in keywordIndex.ChunkIds)
                {
                    if (chunkScores.ContainsKey(chunkId))
                    {
                        chunkScores[chunkId]++;
                    }
                    else
                    {
                        chunkScores[chunkId] = 1;
                    }
                }
            }

            return chunkScores.OrderByDescending(kv => kv.Value)
                              .Select(kv => kv.Key)
                              .Take(topK)
                              .ToList();
        }
    }
}

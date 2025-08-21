using IqraCore.Interfaces.RAG;
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
        public ObjectId Id { get; set; }

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

        public async Task AddChunksKeywordsAsync(string knowledgeBaseId, Dictionary<string, List<string>> chunkKeywords)
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

            await _keywordCollection.BulkWriteAsync(bulkOps);
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

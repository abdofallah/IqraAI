using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace IqraInfrastructure.Helpers.MongoDB
{
    public class TypeSafeArrayFilter
    {
        public static ArrayFilterDefinition<BsonDocument> Create<TDocument>(string identifier, Expression<Func<TDocument, bool>> filterExpression)
        {
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<TDocument>();

            var renderArgs = new RenderArgs<TDocument>(documentSerializer, serializerRegistry);
            var filter = Builders<TDocument>.Filter.Where(filterExpression);

            var renderedFilter = filter.Render(renderArgs);

            var arrayFilterDocument = new BsonDocument();
            foreach (var element in renderedFilter.Elements)
            {
                var newName = $"{identifier}.{element.Name}";
                arrayFilterDocument.Add(new BsonElement(newName, element.Value));
            }

            return new BsonDocumentArrayFilterDefinition<BsonDocument>(arrayFilterDocument);
        }
    }
}

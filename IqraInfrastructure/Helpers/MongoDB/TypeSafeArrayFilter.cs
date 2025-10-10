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
            // 1. Get the global serializer registry. This is the standard way to access it.
            var serializerRegistry = BsonSerializer.SerializerRegistry;

            // 2. From the registry, get the specific serializer for our document type.
            //    This serializer knows about [BsonId], etc.
            var documentSerializer = serializerRegistry.GetSerializer<TDocument>();

            // 3. EXPLICITLY create the RenderArgs struct, providing the two essential components.
            var renderArgs = new RenderArgs<TDocument>(documentSerializer, serializerRegistry);

            // 4. Use the type-safe builder to create the filter definition.
            var filter = Builders<TDocument>.Filter.Where(filterExpression);

            // 5. Call the "real" Render method with our explicitly constructed args.
            var renderedFilter = filter.Render(renderArgs);

            // 6. The rest of the logic remains the same: prepend the identifier.
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

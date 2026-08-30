using Expresso.Core.Filtering;

namespace Expresso.Tests.Core.Filtering
{
    public class QueryModelTests
    {
        [Fact]
        public void Constructor_FieldAndCollectionNameClash_ThrowsArgumentException()
        {
            var fields = new (string, Type)[] { ("authors", typeof(string)) };
            var collections = new[] { new CollectionModel("authors", QueryModel.Empty) };

            var ex = Assert.Throws<ArgumentException>(() => new QueryModel(fields, collections));
            Assert.Contains("both a field and a collection", ex.Message);
        }

        [Fact]
        public void Constructor_DuplicateCollectionName_ThrowsArgumentException()
        {
            var collections = new[]
            {
                new CollectionModel("authors", QueryModel.Empty),
                new CollectionModel("AUTHORS", QueryModel.Empty),
            };

            var ex = Assert.Throws<ArgumentException>(() => new QueryModel(Array.Empty<(string, Type)>(), collections));
            Assert.Contains("Duplicate collection name", ex.Message);
        }

        [Fact]
        public void TryGetCollection_IsCaseInsensitive()
        {
            var model = new QueryModel(
                new (string, Type)[] { ("year", typeof(int)) },
                new[] { new CollectionModel("authors", QueryModel.Empty) });

            Assert.True(model.TryGetCollection("Authors", out var collection));
            Assert.Equal("authors", collection.Name);
            Assert.True(model.TryGetField("YEAR", out var type));
            Assert.Equal(typeof(int), type);
        }

        [Fact]
        public void FromFields_WrapsScalarCatalog()
        {
            var model = QueryModel.FromFields(new (string, Type)[] { ("title", typeof(string)) });

            Assert.True(model.TryGetField("title", out var type));
            Assert.Equal(typeof(string), type);
            Assert.Empty(model.Collections);
        }
    }
}

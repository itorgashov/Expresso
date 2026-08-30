using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Filtering;

namespace Expresso.Parsing
{
    internal sealed partial class ExpressionParser
    {
        private static bool IsCollectionAwareFunction(string functionName)
        {
            return functionName.ToLowerInvariant() is "any" or "all" or "none" or "count" or "min" or "max" or "sum" or "avg";
        }

        private AbstractExpression ParseCollectionAwareFunction(
            string functionName,
            QueryModel queryModel,
            string? scopePath,
            TokenContainer tokens)
        {
            var name = functionName.ToLowerInvariant();
            var first = ParseNextArgument(queryModel, scopePath, tokens);
            var separator = tokens.GetNextToken();

            if (separator == ")")
            {
                return CreateOneArgCollectionFunction(name, first);
            }

            if (separator != ",")
            {
                throw new ArgumentException($"Unexpected token: '{separator}'.");
            }

            if (first is CollectionRef collectionRef
                && queryModel.TryGetCollection(collectionRef.Name, out var collectionModel))
            {
                var nestedPath = scopePath is null ? collectionRef.Name : scopePath + "." + collectionRef.Name;
                var second = ParseNextArgument(collectionModel.Items, nestedPath, tokens);
                ExpectCloseParen(tokens);
                return CreateTwoArgCollectionFunction(name, collectionRef, second);
            }

            if (name is "min" or "max")
            {
                var second = ParseNextArgument(queryModel, scopePath, tokens);
                ExpectCloseParen(tokens);
                return CreateNumericArithFunction(name == "min" ? typeof(MinFunc) : typeof(MaxFunc), first, second);
            }

            throw new ArgumentException($"First argument of {Capitalize(name)}() must be a collection.");
        }

        private AbstractExpression ParseNextArgument(QueryModel queryModel, string? scopePath, TokenContainer tokens)
        {
            var token = tokens.GetNextToken();
            if (token is null)
            {
                throw new ArgumentException("Unexpected end of expression.");
            }

            if (token is "," or ")")
            {
                throw new ArgumentException($"Unexpected token: '{token}'.");
            }

            tokens.StepBack();
            return ParseExpression(queryModel, scopePath, tokens);
        }

        private static void ExpectCloseParen(TokenContainer tokens)
        {
            var token = tokens.GetNextToken();
            if (token != ")")
            {
                throw new ArgumentException($"Unexpected token: '{token}'.");
            }
        }

        private static AbstractExpression CreateOneArgCollectionFunction(string name, AbstractExpression first)
        {
            var collection = RequireCollectionRef(first, name);
            return name switch
            {
                "any" => new AnyFunc(collection),
                "all" => new AllFunc(collection),
                "none" => new NoneFunc(collection),
                "count" => new CollectionCountFunc(collection),
                "min" => throw new Exception("Min() function should have 2 arguments."),
                "max" => throw new Exception("Max() function should have 2 arguments."),
                "sum" => throw new Exception("Sum() function should have 2 arguments."),
                "avg" => throw new Exception("Avg() function should have 2 arguments."),
                _ => throw new ArgumentException($"Unknown function: {name}"),
            };
        }

        private static AbstractExpression CreateTwoArgCollectionFunction(
            string name,
            CollectionRef collection,
            AbstractExpression second)
        {
            return name switch
            {
                "any" => new AnyFunc(collection, second),
                "all" => new AllFunc(collection, second),
                "none" => new NoneFunc(collection, second),
                "count" => new CollectionCountFunc(collection, second),
                "min" => new CollectionMinFunc(collection, second),
                "max" => new CollectionMaxFunc(collection, second),
                "sum" => new CollectionSumFunc(collection, second),
                "avg" => new CollectionAvgFunc(collection, second),
                _ => throw new ArgumentException($"Unknown function: {name}"),
            };
        }

        private AbstractExpression CreateNumericArithFunction(Type funcType, AbstractExpression first, AbstractExpression second)
        {
            var arguments = new List<AbstractExpression> { first, second };
            if (funcType == typeof(MinFunc))
            {
                return CreateNumericArithFunction<MinFunc>(arguments);
            }

            return CreateNumericArithFunction<MaxFunc>(arguments);
        }

        private static CollectionRef RequireCollectionRef(AbstractExpression expression, string functionName)
        {
            if (expression is CollectionRef collection)
            {
                return collection;
            }

            throw new ArgumentException($"First argument of {Capitalize(functionName)}() must be a collection.");
        }

        private static string Capitalize(string name) =>
            string.IsNullOrEmpty(name) ? name : char.ToUpperInvariant(name[0]) + name.Substring(1);
    }
}

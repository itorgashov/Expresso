using Expresso.Core.CriteriaExpressions;
using System.Globalization;
using Expresso.Core.CriteriaExpressions.Abstract;
using System.Text.RegularExpressions;

namespace Expresso.Parsing
{
    internal sealed partial class ExpressionParser
    {
        private static ExpressionParser Instance = null;
        private static readonly object Instancelock = new object();
        public static ExpressionParser GetInstance()
        {
            if (Instance == null)
            {
                lock (Instancelock)
                {
                    if (Instance == null)
                    {
                        Instance = new ExpressionParser();
                    }
                }
            }

            return Instance;
        }

        private ExpressionParser() { }

        private static readonly Regex _nameRegex = new Regex(@"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);

        public AbstractExpression? Parse(string query, (string, Type)[] validFieldsArray)
        {
            if (query is null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            if (validFieldsArray is null)
            {
                throw new ArgumentNullException(nameof(validFieldsArray));
            }

            Dictionary<string, Type> validFields = new();
            foreach (var tuple in validFieldsArray)
            {
                if (tuple.Item1 is null || tuple.Item2 is null
                    || string.IsNullOrWhiteSpace(tuple.Item1)
                    || validFields.ContainsKey(tuple.Item1))
                {
                    continue;
                }
                validFields[tuple.Item1.ToLower()] = tuple.Item2;
            }

            var tokens = Tokenize(query);
            if (tokens.Count == 0)
            {
                return null;
            }
            tokens.ResetIterator();
            var expression = ParseExpression(validFields, tokens);
            var token = tokens.GetNextToken();
            if (token != null)
            {
                throw new ArgumentException($"Unexpected token: {token}.");
            }
            if (expression is StringLiteral ltrl)
            {
                expression = CreateLiteral(ltrl.Value, GetLiteralType(ltrl.Value));
            }
            return expression;
        }

        private TokenContainer Tokenize(string query)
        {
            var tokens = new TokenContainer();
            var regex = new Regex(@"(?<string>""[^""]*"")|(?<number>-?\d+(\.\d+)?([eE][+-]?\d+)?)|(?<alphanumeric>[a-zA-Z_][a-zA-Z0-9_]*)|(?<special>[(),])|(?<suspicious>[^\sa-zA-Z0-9_\-(),""][^\s(),""]*)");
            var matches = regex.Matches(query);

            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    if (match.Groups["string"].Success)
                    {
                        tokens.Add(match.Groups["string"].Value);
                    }
                    else if (match.Groups["number"].Success)
                    {
                        tokens.Add(match.Groups["number"].Value);
                    }
                    else if (match.Groups["alphanumeric"].Success)
                    {
                        tokens.Add(match.Groups["alphanumeric"].Value);
                    }
                    else if (match.Groups["special"].Success)
                    {
                        tokens.Add(match.Groups["special"].Value);
                    }
                    else if (match.Groups["suspicious"].Success)
                    {
                        throw new ArgumentException($"Invalid token: '{match.Groups["suspicious"].Value}'.");
                    }
                }
            }

            tokens.ResetIterator();
            return tokens;
        }

        private static AbstractExpression ParseExpression(Dictionary<string, Type> validFields, TokenContainer tokens)
        {
            var token = tokens.GetNextToken();
            if (token == null)
            {
                throw new ArgumentException("Unexpected end of expression.");
            }

            if (token == "(" || token == ")" || token == ",")
            {
                throw new ArgumentException($"Unexpected token: '{token}'.");
            }
            if (IsFunction(token, tokens))
            {
                return ParseFunction(token, validFields, tokens);
            }
            else if (IsFieldNameCandidate(token))
            {
                if (validFields.ContainsKey(token.ToLower()))
                {
                    return new Field(token.ToLower(), validFields[token.ToLower()]);
                }
                else
                {
                    throw new ArgumentException($"Illegal field name: '{token}'.");
                }
            }
            else
            {
                return new StringLiteral(token);
            }
        }

        private static bool IsFunction(string token, TokenContainer tokens)
        {
            var nextToken = tokens.GetNextToken();
            if (nextToken == null) return false;
            tokens.StepBack();

            return IsValidName(token) && nextToken == "(";
        }

        private static bool IsFieldNameCandidate(string token)
        {
            var first = token[0];
            var last = token[token.Length - 1];
            return (first >= 'A' && first <= 'Z' || first >= 'a' && first <= 'z' || first == '_')
                && (last >= 'A' && last <= 'Z' || last >= 'a' && last <= 'z' || last >= '0' && last <= '9' || last == '_');
        }

        private static bool IsValidName(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return false; // Empty or null names are invalid
            }

            return _nameRegex.IsMatch(token);
        }

        private static AbstractExpression ParseFunction(string token, Dictionary<string, Type> validFields, TokenContainer tokens)
        {
            var functionName = token;
            tokens.GetNextToken(); // Skip '('

            var arguments = new List<AbstractExpression>();
            string? currentToken;
            bool expressionExpected = true;
            do
            {
                currentToken = tokens.GetNextToken();
                if (currentToken == null)
                {
                    throw new ArgumentException("Unexpected end of expression.");
                }

                if (currentToken == ",")
                {
                    if (expressionExpected)
                    {
                        throw new ArgumentException($"Unexpected token: '{currentToken}'.");
                    }
                    else
                    {
                        expressionExpected = true;
                        continue;
                    }
                }
                if (!expressionExpected && currentToken != "," && currentToken != ")")
                {
                    throw new ArgumentException($"Unexpected token: '{currentToken}'.");
                }

                if (currentToken == ")")
                {
                    if (!expressionExpected) break;
                    else throw new ArgumentException($"Unexpected token: '{currentToken}'.");
                }
                if (currentToken == "(")
                {
                    if (!expressionExpected) break;
                    else throw new ArgumentException($"Unexpected token: '{currentToken}'.");
                }
                tokens.StepBack();
                arguments.Add(ParseExpression(validFields, tokens));
                expressionExpected = false;
            } while (true);

            return CreateFunction(functionName, arguments);
        }

        private static AbstractExpression CreateFunction(string functionName, List<AbstractExpression> arguments)
        {
            switch (functionName.ToLower())
            {
                case "and":
                    if (arguments.Count < 2)
                    {
                        throw new Exception("And() function should at least 2 arguments.");
                    }
                    return new AndFunc(arguments);
                case "or":
                    if (arguments.Count < 2)
                    {
                        throw new Exception("Or() function should at least 2 arguments.");
                    }
                    return new OrFunc(arguments);
                case "not":
                    if (arguments.Count != 1)
                    {
                        throw new Exception("Not() function should have 1.");
                    }
                    return new NotFunc(arguments[0]);
                case "eq":
                    if (arguments.Count != 2)
                    {
                        throw new Exception("Eq() function should have 2 arguments.");
                    }
                    return CreateComparisonFunction<EqFunc>(arguments);
                case "neq":
                    if (arguments.Count != 2)
                    {
                        throw new Exception("Neq() function should have 2 arguments.");
                    }
                    return CreateComparisonFunction<NeqFunc>(arguments);
                case "gt":
                    if (arguments.Count != 2)
                    {
                        throw new Exception("Gt() function should have 2 arguments.");
                    }
                    return CreateComparisonFunction<GtFunc>(arguments);
                case "gte":
                    if (arguments.Count != 2)
                    {
                        throw new Exception("Gte() function should have 2 arguments.");
                    }
                    return CreateComparisonFunction<GteFunc>(arguments);
                case "lt":
                    if (arguments.Count != 2)
                    {
                        throw new Exception("Lt() function should have 2 arguments.");
                    }
                    return CreateComparisonFunction<LtFunc>(arguments);
                case "lte":
                    if (arguments.Count != 2)
                    {
                        throw new Exception("Lte() function should have 2 arguments.");
                    }
                    return CreateComparisonFunction<LteFunc>(arguments);
                case "abs":
                    if (arguments.Count != 1)
                    {
                        throw new Exception("Abs() function should have 1 argument.");
                    }
                    return CreateNumericSingleArgFunction<AbsFunc>(arguments);
                case "add":
                    if (arguments.Count != 2)
                    {
                        throw new Exception("Add() function should have 2 arguments.");
                    }
                    return CreateNumericArithFunction<AddFunc>(arguments);
                case "sub":
                    if (arguments.Count != 2)
                    {
                        throw new Exception("Sub() function should have 2 arguments.");
                    }
                    return CreateNumericArithFunction<SubFunc>(arguments);
                case "mult":
                    if (arguments.Count != 2)
                    {
                        throw new Exception("Mult() function should have 2 arguments.");
                    }
                    return CreateNumericArithFunction<MultFunc>(arguments);
                case "div":
                    if (arguments.Count != 2)
                    {
                        throw new Exception("Div() function should have 2 arguments.");
                    }
                    return CreateNumericArithFunction<DivFunc>(arguments);
                case "in":
                    if (arguments.Count < 2)
                    {
                        throw new Exception("In() function should have at least 2 arguments.");
                    }
                    return CreateInFunction(arguments);
                case "isnull":
                    if (arguments.Count != 1)
                    {
                        throw new Exception("IsNull() function should have 1 argument.");
                    }
                    return new IsNullFunc(arguments[0]);
                default:
                    if (TryCreateStringFunction(functionName, arguments, out var stringFunction))
                    {
                        return stringFunction;
                    }
                    if (TryCreateDateTimeFunction(functionName, arguments, out var dateTimeFunction))
                    {
                        return dateTimeFunction;
                    }
                    if (TryCreateNumericFunction(functionName, arguments, out var numericFunction))
                    {
                        return numericFunction;
                    }
                    throw new ArgumentException($"Unknown function: {functionName}");
            }
        }

        private static AbstractExpression CreateComparisonFunction<T>(List<AbstractExpression> arguments) where T : ComparisonFunction
        {
            Type firstArgType;
            if (arguments[0] is StringLiteral ltrl)
            {
                firstArgType = GetLiteralType(ltrl.Value);
                arguments[0] = CreateLiteral(ltrl.Value, firstArgType);
            }
            else
            {
                firstArgType = arguments[0].ReturnType;
            }

            for (int i = 1; i < arguments.Count; i++)
            {
                if (arguments[i] is StringLiteral stringLiteral)
                {
                    arguments[i] = CreateLiteral(stringLiteral.Value, firstArgType);
                }
                else if (arguments[i].ReturnType != firstArgType)
                {
                    throw new ArgumentException($"Incompatible argument types: expected {firstArgType}, got {arguments[i].ReturnType}.");
                }
            }

            return (T)Activator.CreateInstance(typeof(T), arguments[0], arguments[1]);
        }

        private static AbstractExpression CreateNumericSingleArgFunction<T>(List<AbstractExpression> arguments) where T : NumericSingleArgFunction
        {
            if (arguments[0] is StringLiteral ltrl)
            {
                arguments[0] = CreateLiteral(ltrl.Value, GetLiteralType(ltrl.Value));
            }

            return (T)Activator.CreateInstance(typeof(T), arguments[0]);
        }

        private static AbstractExpression CreateNumericArithFunction<T>(List<AbstractExpression> arguments) where T : NumericArithFunction
        {
            if (arguments[0] is StringLiteral ltrl1)
            {
                arguments[0] = CreateLiteral(ltrl1.Value, GetLiteralType(ltrl1.Value));
            }
            if (arguments[1] is StringLiteral ltrl2)
            {
                arguments[1] = CreateLiteral(ltrl2.Value, GetLiteralType(ltrl2.Value));
            }

            return (T)Activator.CreateInstance(typeof(T), arguments[0], arguments[1]);
        }

        private static AbstractExpression CreateInFunction(List<AbstractExpression> arguments)
        {
            if (arguments.Count < 2)
            {
                throw new ArgumentException($"IN function requires at least 2 arguments.");
            }

            Type firstArgType;
            if (arguments[0] is StringLiteral ltrl)
            {
                firstArgType = GetLiteralType(ltrl.Value);
                arguments[0] = CreateLiteral(ltrl.Value, firstArgType);
            }
            else
            {
                firstArgType = arguments[0].ReturnType;
            }

            // Ensure all arguments are of the same type as the first argument
            for (int i = 1; i < arguments.Count; i++)
            {
                if (arguments[i] is StringLiteral stringLiteral)
                {
                    arguments[i] = CreateLiteral(stringLiteral.Value, firstArgType);
                }
                else if (arguments[i].ReturnType != firstArgType)
                {
                    throw new ArgumentException($"Incompatible argument types: expected {firstArgType}, got {arguments[i].ReturnType}.");
                }
            }

            return new InFunc(arguments);
        }

        private static Type GetLiteralType(string s)
        {
            if (!string.IsNullOrEmpty(s) && s.Length > 0 && s[0] >= '0' && s[0] <= '9')
            {
                return s.IndexOf('.') >= 0 || s.IndexOf('e') >= 0 || s.IndexOf('E') >= 0
                    ? typeof(double)
                    : typeof(int);
            }
            else
            {
                return typeof(string);
            }
        }

        private static AbstractExpression CreateLiteral(string value, Type targetType)
        {
            if (targetType == typeof(byte))
            {
                if (byte.TryParse(value, out var byteValue))
                {
                    return new Literal(byteValue);
                }
                throw new ArgumentException($"Cannot parse '{value}' as {targetType}.");
            }
            if (targetType == typeof(int))
            {
                if (int.TryParse(value, out var intValue))
                {
                    return new Literal(intValue);
                }
                throw new ArgumentException($"Cannot parse '{value}' as {targetType}.");
            }
            else if (targetType == typeof(double))
            {
                if (double.TryParse(value, out var doubleValue))
                {
                    return new Literal(doubleValue);
                }
                throw new ArgumentException($"Cannot parse '{value}' as {targetType}.");
            }
            else if (targetType == typeof(DateTime))
            {
                string strippedValue = StripQuotedToken(value, targetType);
                if (DateTime.TryParse(strippedValue, out var dateValue))
                {
                    return new Literal(dateValue);
                }
                throw new ArgumentException($"Cannot parse '{strippedValue}' as {targetType}.");
            }
            else if (targetType == typeof(Guid))
            {
                string strippedValue = StripQuotedToken(value, targetType);
                if (Guid.TryParse(strippedValue, out var guidValue))
                {
                    return new Literal(guidValue);
                }
                throw new ArgumentException($"Cannot parse '{strippedValue}' as {targetType}.");
            }
#if NET6_0_OR_GREATER
            else if (targetType == typeof(DateOnly))
            {
                string strippedValue = StripQuotedToken(value, targetType);
                if (DateOnly.TryParseExact(strippedValue, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOnlyValue)
                    || DateOnly.TryParse(strippedValue, out dateOnlyValue))
                {
                    return new Literal(dateOnlyValue);
                }
                throw new ArgumentException($"Cannot parse '{strippedValue}' as {targetType}.");
            }
            else if (targetType == typeof(TimeOnly))
            {
                string strippedValue = StripQuotedToken(value, targetType);
                if (TimeOnly.TryParseExact(strippedValue, "HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var timeOnlyValue)
                    || TimeOnly.TryParseExact(strippedValue, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out timeOnlyValue)
                    || TimeOnly.TryParse(strippedValue, out timeOnlyValue))
                {
                    return new Literal(timeOnlyValue);
                }
                throw new ArgumentException($"Cannot parse '{strippedValue}' as {targetType}.");
            }
#endif
            else if (targetType == typeof(string))
            {
                return new Literal(StripQuotedToken(value, targetType));
            }
            else
            {
                throw new ArgumentException($"Unsupported target type: {targetType}.");
            }
        }

        private static string StripQuotedToken(string value, Type targetType)
        {
            if (string.IsNullOrEmpty(value) || value.Length < 2 || value[0] != '"' || value[value.Length - 1] != '"')
            {
                throw new ArgumentException($"Cannot parse token '{value}' as {targetType}.");
            }

            return value.Substring(1, value.Length - 2);
        }

        private class StringLiteral : AbstractExpression
        {
            public string Value { get; }
            public StringLiteral(string @value)
            {
                Value = @value;
            }
        }
    }
}

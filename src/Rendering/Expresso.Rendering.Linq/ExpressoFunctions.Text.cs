namespace Expresso.Rendering.Linq
{
    public static partial class ExpressoFunctions
    {
        /// <summary>Character length (Unicode code points), matching PostgreSQL <c>char_length</c>.</summary>
        public static int Length(string source) => CodePointCount(source);

        /// <summary>Zero-based code-point index of <paramref name="find"/>, or <c>-1</c> when missing (ordinal).</summary>
        public static int IndexOf(string source, string find)
        {
            if (find.Length == 0)
            {
                return 0;
            }

            if (!ContainsSurrogates(source) && !ContainsSurrogates(find))
            {
                return source.IndexOf(find, StringComparison.Ordinal);
            }

            var findPoints = CodePointCount(find);
            var limit = CodePointCount(source) - findPoints + 1;
            for (var index = 0; index < limit; index++)
            {
                if (MatchesAtCodePoint(source, find, index))
                {
                    return index;
                }
            }

            return -1;
        }

        private static bool ContainsSurrogates(string text)
        {
            for (var i = 0; i < text.Length; i++)
            {
                if (char.IsSurrogate(text[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static int CodePointCount(string text)
        {
            if (!ContainsSurrogates(text))
            {
                return text.Length;
            }

            var count = 0;
            for (var i = 0; i < text.Length; i++, count++)
            {
                if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    i++;
                }
            }

            return count;
        }

        private static bool MatchesAtCodePoint(string source, string find, int startCodePoint)
        {
            var sourceIndex = IndexFromCodePoint(source, startCodePoint);
            var findIndex = 0;
            while (findIndex < find.Length)
            {
                if (sourceIndex >= source.Length)
                {
                    return false;
                }

                if (char.IsHighSurrogate(find[findIndex]) && findIndex + 1 < find.Length && char.IsLowSurrogate(find[findIndex + 1]))
                {
                    if (sourceIndex + 1 >= source.Length || source[sourceIndex] != find[findIndex] || source[sourceIndex + 1] != find[findIndex + 1])
                    {
                        return false;
                    }

                    sourceIndex += 2;
                    findIndex += 2;
                    continue;
                }

                if (source[sourceIndex] != find[findIndex])
                {
                    return false;
                }

                sourceIndex++;
                findIndex++;
            }

            return true;
        }

        private static int IndexFromCodePoint(string text, int codePointIndex)
        {
            if (!ContainsSurrogates(text))
            {
                return codePointIndex;
            }

            var index = 0;
            for (var point = 0; point < codePointIndex && index < text.Length; point++, index++)
            {
                if (char.IsHighSurrogate(text[index]) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]))
                {
                    index++;
                }
            }

            return index;
        }

        private static string SliceByCodePoints(string source, int startCodePoint, int lengthCodePoints)
        {
            if (lengthCodePoints <= 0)
            {
                return string.Empty;
            }

            if (!ContainsSurrogates(source))
            {
                return source.Substring(startCodePoint, Math.Min(lengthCodePoints, source.Length - startCodePoint));
            }

            var start = IndexFromCodePoint(source, startCodePoint);
            var end = IndexFromCodePoint(source, startCodePoint + lengthCodePoints);
            return source.Substring(start, end - start);
        }
    }
}

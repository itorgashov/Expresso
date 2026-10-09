namespace Expresso.Parsing.Policies.Runtime;

internal sealed class RuleBits
{
    private ulong _word;
    private readonly ulong[]? _words;
    internal RuleBits(int count) { if (count > 64) _words = new ulong[(count + 63) / 64]; }
    internal bool Has(int bit) => ((_words == null ? _word : _words[bit >> 6]) & (1UL << (bit & 63))) != 0;
    internal bool Add(int bit)
    {
        ulong mask = 1UL << (bit & 63);
        if (_words == null) { bool changed = (_word & mask) == 0; _word |= mask; return changed; }
        int index = bit >> 6; bool added = (_words[index] & mask) == 0; _words[index] |= mask; return added;
    }

    internal IEnumerable<int> SetBits()
    {
        int count = _words?.Length ?? 1;
        for (int wordIndex = 0; wordIndex < count; wordIndex++)
        {
            ulong word = _words == null ? _word : _words[wordIndex];
            int offset = 0;
            while (word != 0)
            {
                while ((word & 1) == 0)
                {
                    word >>= 1;
                    offset++;
                }
                yield return wordIndex * 64 + offset;
                word >>= 1;
                offset++;
            }
        }
    }
}

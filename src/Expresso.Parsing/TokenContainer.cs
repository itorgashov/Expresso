namespace Expresso.Parsing
{
    internal class TokenContainer
    {
        private List<string> _tokens = new();
        private int _readPosition = 0;

        public void Add(string token)
        {
            _tokens.Add(token);
        }
        public void ResetIterator()
        {
            _readPosition = 0;
        }
        public string? GetNextToken()
        {
            return _readPosition < _tokens.Count ? _tokens[_readPosition++] : null;
        }
        public void StepBack()
        {
            _readPosition = _readPosition > 0 ? _readPosition - 1 : 0;
        }
        public int Count => _tokens.Count;
    }
}

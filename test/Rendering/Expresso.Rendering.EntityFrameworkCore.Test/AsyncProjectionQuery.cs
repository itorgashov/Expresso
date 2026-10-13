using System.Collections;
using System.Linq.Expressions;


namespace Expresso.Rendering.EntityFrameworkCore.Test;

internal sealed class AsyncProjectionQuery<T> : EnumerableQuery<T>, IQueryable<T>, IAsyncEnumerable<T>
{
    internal AsyncProjectionQuery(IEnumerable<T> data) : base(data) { }
    public AsyncProjectionQuery(Expression expression) : base(expression) { }
    IQueryProvider IQueryable.Provider => new AsyncProjectionProvider(this);
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
        new Enumerator(this.AsEnumerable().GetEnumerator(), cancellationToken);
    private sealed class Enumerator : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;
        private readonly CancellationToken _token;
        internal Enumerator(IEnumerator<T> inner, CancellationToken token) { _inner = inner; _token = token; }
        public T Current => _inner.Current;
        public ValueTask<bool> MoveNextAsync() { _token.ThrowIfCancellationRequested(); return new ValueTask<bool>(_inner.MoveNext()); }
        public ValueTask DisposeAsync() { _inner.Dispose(); return default; }
    }
}
internal sealed class AsyncProjectionProvider : IQueryProvider
{
    private readonly IQueryProvider _inner;
    internal AsyncProjectionProvider(IQueryProvider inner) => _inner = inner;
    public IQueryable CreateQuery(Expression expression) => (IQueryable)Activator.CreateInstance(
        typeof(AsyncProjectionQuery<>).MakeGenericType(expression.Type.GetGenericArguments()[0]), expression)!;
    public IQueryable<T> CreateQuery<T>(Expression expression) => new AsyncProjectionQuery<T>(expression);
    public object? Execute(Expression expression) => _inner.Execute(expression);
    public T Execute<T>(Expression expression) => _inner.Execute<T>(expression);
}


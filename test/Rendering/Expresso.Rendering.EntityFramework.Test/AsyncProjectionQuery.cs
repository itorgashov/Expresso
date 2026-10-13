using System.Collections;
using System.Linq.Expressions;
using System.Data.Entity.Infrastructure;

namespace Expresso.Rendering.EntityFramework.Test;

internal sealed class AsyncProjectionQuery<T> : EnumerableQuery<T>, IQueryable<T>, IDbAsyncEnumerable<T>
{
    internal AsyncProjectionQuery(IEnumerable<T> data) : base(data) { }
    public AsyncProjectionQuery(Expression expression) : base(expression) { }
    IQueryProvider IQueryable.Provider => new AsyncProjectionProvider(this);
    public IDbAsyncEnumerator<T> GetAsyncEnumerator() => new Enumerator(this.AsEnumerable().GetEnumerator());
    IDbAsyncEnumerator IDbAsyncEnumerable.GetAsyncEnumerator() => GetAsyncEnumerator();
    private sealed class Enumerator : IDbAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;
        internal Enumerator(IEnumerator<T> inner) => _inner = inner;
        public T Current => _inner.Current;
        object IDbAsyncEnumerator.Current => Current!;
        public Task<bool> MoveNextAsync(CancellationToken token) { token.ThrowIfCancellationRequested(); return Task.FromResult(_inner.MoveNext()); }
        public void Dispose() => _inner.Dispose();
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


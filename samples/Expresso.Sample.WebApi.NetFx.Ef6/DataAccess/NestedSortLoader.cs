using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Expresso.Core.Sorting;
using Expresso.Rendering.Linq;
using Expresso.Sample.WebApi.NetFx.Ef6.Entities;

namespace Expresso.Sample.WebApi.NetFx.Ef6.DataAccess;

/// <summary>Loads nested <c>sortfor</c> collections with provider-translated <c>OrderBy</c> instead of in-memory sorting.</summary>
internal static class NestedSortLoader
{
    public static async Task AttachSortedAwardsAsync(
        SampleEf6Context db,
        IExpressionToLinqTransformer transformer,
        SortDirective? sortDirective,
        IReadOnlyList<Author> authors,
        CancellationToken cancellationToken,
        params string[] path)
    {
        if (authors.Count == 0)
        {
            return;
        }

        var nested = sortDirective.ResolveNested(path);
        if (nested is null || nested.Items.Count == 0)
        {
            return;
        }

        var authorIds = authors.Select(a => a.Id).ToList();
        var query = PartitionThenSort(
            db.Awards.Where(a => authorIds.Contains(a.AuthorId)),
            a => a.AuthorId,
            transformer,
            nested,
            BookLinqMappings.Awards);

        var awards = await query.ToListAsync(cancellationToken);
        var byAuthor = new Dictionary<int, List<Award>>();
        foreach (var award in awards)
        {
            if (!byAuthor.TryGetValue(award.AuthorId, out var list))
            {
                list = new List<Award>();
                byAuthor[award.AuthorId] = list;
            }

            list.Add(award);
        }

        foreach (var author in authors)
        {
            author.Awards = byAuthor.TryGetValue(author.Id, out var list) ? list : new List<Award>();
        }
    }

    public static async Task AttachSortedBookAuthorsAsync(
        SampleEf6Context db,
        IExpressionToLinqTransformer transformer,
        SortDirective? sortDirective,
        IReadOnlyList<Book> books,
        CancellationToken cancellationToken)
    {
        if (books.Count == 0)
        {
            return;
        }

        var nested = sortDirective.ResolveNested("authors");
        if (nested is null || nested.Items.Count == 0)
        {
            return;
        }

        var bookIds = books.Select(b => b.Id).ToList();
        var query = db.Authors.Where(a => a.Books.Any(b => bookIds.Contains(b.Id)));
        query = query.OrderByNested(transformer, sortDirective, BookLinqMappings.Authors, "authors");
        var authors = await query.ToListAsync(cancellationToken);
        var order = authors.Select((a, i) => (a.Id, i)).ToDictionary(x => x.Id, x => x.i);

        foreach (var book in books)
        {
            book.Authors = book.Authors
                .OrderBy(a => order.TryGetValue(a.Id, out var index) ? index : int.MaxValue)
                .ToList();
        }
    }

    public static async Task ApplyBookNestedSortAsync(
        SampleEf6Context db,
        IExpressionToLinqTransformer transformer,
        SortDirective sortDirective,
        IReadOnlyList<Book> books,
        CancellationToken cancellationToken)
    {
        await AttachSortedBookAuthorsAsync(db, transformer, sortDirective, books, cancellationToken);

        var nestedAwards = sortDirective.ResolveNested("authors", "awards");
        if (nestedAwards is null || nestedAwards.Items.Count == 0)
        {
            return;
        }

        var authors = books.SelectMany(b => b.Authors).Distinct().ToList();
        await AttachSortedAwardsAsync(db, transformer, sortDirective, authors, cancellationToken, "authors", "awards");
    }

    private static IQueryable<T> PartitionThenSort<T>(
        IQueryable<T> source,
        Expression<Func<T, int>> partitionKey,
        IExpressionToLinqTransformer transformer,
        SortDirective nested,
        LinqQueryMapping<T> mapping)
    {
        var expression = Expression.Call(
            typeof(Queryable),
            nameof(Queryable.OrderBy),
            new[] { typeof(T), typeof(int) },
            source.Expression,
            Expression.Quote(partitionKey));

        foreach (var key in transformer.BuildSortKeys(nested, mapping))
        {
            var method = key.Direction == SortDirection.Descending
                ? nameof(Queryable.ThenByDescending)
                : nameof(Queryable.ThenBy);

            expression = Expression.Call(
                typeof(Queryable),
                method,
                new[] { typeof(T), key.Key.ReturnType },
                expression,
                Expression.Quote(key.Key));
        }

        return source.Provider.CreateQuery<T>(expression);
    }
}

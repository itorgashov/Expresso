using System;
using System.Collections.Generic;

namespace Expresso.Sample.WebApi.NetFx.Ef6.Entities;

public sealed class Publisher
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public string? Location { get; set; }

    public TimeSpan OpensAt { get; set; }

    public TimeSpan ClosesAt { get; set; }

    public ICollection<Book> Books { get; set; } = new List<Book>();
}

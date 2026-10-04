using System;
using System.Collections.Generic;

namespace Expresso.Sample.WebApi.NetFx.Ef6.Entities;

public sealed class Author
{
    public int Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public DateTime? DateOfBirth { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<Book> Books { get; set; } = new List<Book>();

    public ICollection<Award> Awards { get; set; } = new List<Award>();
}

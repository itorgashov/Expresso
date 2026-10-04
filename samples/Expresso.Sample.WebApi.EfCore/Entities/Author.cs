namespace Expresso.Sample.WebApi.EfCore.Entities;

public sealed class Author
{
    public int Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public DateOnly? DateOfBirth { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<Book> Books { get; set; } = new();

    public List<Award> Awards { get; set; } = new();
}

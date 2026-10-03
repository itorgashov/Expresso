namespace Expresso.Rendering.TestCases
{
    /// <summary>Seed row of the <c>widget</c> table.</summary>
    public class Widget
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        public int Age { get; set; }

        public double Amount { get; set; }

        public bool Active { get; set; }

        public DateTime Created { get; set; }

        public Guid ExternalId { get; set; }

        public TimeSpan Opens { get; set; }

        public string? Notes { get; set; }

        public byte Code { get; set; }

        public List<WidgetTag> Tags { get; set; } = new();
    }

    /// <summary>Seed row of the <c>widget_tag</c> table.</summary>
    public class WidgetTag
    {
        public int Id { get; set; }

        public int WidgetId { get; set; }

        public string Label { get; set; } = "";

        public int Score { get; set; }

        public List<WidgetTagMeta> TagMeta { get; set; } = new();
    }

    /// <summary>Seed row of the <c>widget_tag_meta</c> table.</summary>
    public class WidgetTagMeta
    {
        public int Id { get; set; }

        public int TagId { get; set; }

        public string Kind { get; set; } = "";

        public string Value { get; set; } = "";
    }
}

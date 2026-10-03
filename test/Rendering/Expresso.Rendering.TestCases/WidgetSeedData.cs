namespace Expresso.Rendering.TestCases
{
    public static class WidgetSeedData
    {
        public static readonly Guid Guid1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid Guid2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid Guid3 = Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static readonly Guid Guid4 = Guid.Parse("44444444-4444-4444-4444-444444444444");
        public static readonly Guid Guid5 = Guid.Parse("55555555-5555-5555-5555-555555555555");
        public static readonly Guid Guid6 = Guid.Parse("66666666-6666-6666-6666-666666666666");

        public static readonly DateTime Created1 = new(2020, 1, 15, 10, 0, 0, DateTimeKind.Unspecified);
        public static readonly DateTime Created2 = new(2021, 6, 1, 14, 30, 0, DateTimeKind.Unspecified);
        public static readonly DateTime Created3 = new(2020, 1, 15, 10, 0, 0, DateTimeKind.Unspecified);
        public static readonly DateTime Created4 = new(2019, 12, 31, 23, 59, 0, DateTimeKind.Unspecified);
        public static readonly DateTime Created5 = new(2022, 7, 4, 8, 15, 30, DateTimeKind.Unspecified);
        public static readonly DateTime Created6 = new(2020, 12, 25, 0, 0, 0, DateTimeKind.Unspecified);

        public static readonly TimeSpan OpensMorning = new(9, 0, 0);
        public static readonly TimeSpan OpensEvening = new(17, 0, 0);
        public static readonly TimeSpan OpensMidnight = TimeSpan.Zero;
        public static readonly TimeSpan OpensNoon = new(12, 0, 0);
        public static readonly TimeSpan OpensNineThirty = new(9, 30, 0);

        /// <summary>
        /// Fresh object graph of the seed rows (widgets with tags and tag meta). The same rows are
        /// inserted into every integration-test database.
        /// </summary>
        public static List<Widget> CreateWidgets()
        {
            var widgets = new List<Widget>
            {
                W(1, "Alice", 30, 50.5, true, Created1, Guid1, OpensMorning, null, 1),
                W(2, "Bob", 25, 40.0, false, Created2, Guid2, OpensEvening, "100Xoff", 2),
                W(3, "Carol", 30, 61.4, true, Created3, Guid3, OpensMorning, "  pad  ", 3),
                W(4, "Dave", 40, 10.0, true, Created4, Guid4, OpensMidnight, "100%_off", 4),
                W(5, "Eve", 0, -12.7, false, Created5, Guid5, OpensNoon, "n/a", 5),
                W(6, "Frank", 18, 99.9, true, Created6, Guid6, OpensNineThirty, "a\\b", 6),
            };

            var tags = new List<WidgetTag>
            {
                T(1, 1, "red", 10),
                T(2, 1, "blue", 20),
                T(3, 2, "red", 5),
                T(4, 4, "green", 15),
                T(5, 4, "red", 15),
                T(6, 5, "yellow", 0),
                T(7, 6, "red", 100),
            };

            var meta = new List<WidgetTagMeta>
            {
                new() { Id = 1, TagId = 2, Kind = "size", Value = "large" },
                new() { Id = 2, TagId = 1, Kind = "color", Value = "primary" },
            };

            foreach (var tag in tags)
            {
                tag.TagMeta.AddRange(meta.Where(m => m.TagId == tag.Id));
                widgets.Single(w => w.Id == tag.WidgetId).Tags.Add(tag);
            }

            return widgets;
        }

        private static Widget W(int id, string name, int age, double amount, bool active, DateTime created, Guid externalId, TimeSpan opens, string? notes, byte code) =>
            new()
            {
                Id = id,
                Name = name,
                Age = age,
                Amount = amount,
                Active = active,
                Created = created,
                ExternalId = externalId,
                Opens = opens,
                Notes = notes,
                Code = code,
            };

        private static WidgetTag T(int id, int widgetId, string label, int score) =>
            new() { Id = id, WidgetId = widgetId, Label = label, Score = score };
    }
}

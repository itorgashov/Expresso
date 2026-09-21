namespace Expresso.Rendering.Integration.Test
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
    }
}

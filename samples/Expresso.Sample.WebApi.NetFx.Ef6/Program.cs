using System;
using Microsoft.Owin.Hosting;

namespace Expresso.Sample.WebApi.NetFx.Ef6;

internal static class Program
{
    private const string BaseAddress = "http://localhost:5081/";

    private static void Main()
    {
        using (WebApp.Start<Startup>(BaseAddress))
        {
            Console.WriteLine($"Listening on {BaseAddress}");
            Console.WriteLine($"Swagger UI: {BaseAddress}swagger");
            Console.WriteLine("Press Enter to exit.");
            Console.ReadLine();
        }
    }
}

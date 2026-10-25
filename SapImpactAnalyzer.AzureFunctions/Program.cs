using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SapImpactAnalyzer.AzureFunctions;
using SapImpactAnalyzer.Shared;

public partial class Program
{
    public static void Main()
    {
        var host = new HostBuilder()
            .ConfigureFunctionsWorkerDefaults()
            .ConfigureServices((context, services) =>
            {
                // Register all shared services
                services.AddSharedServices(context.Configuration);

                // Register function-specific services
                services.AddScoped<TransportAnalysisFunction>();
            })
            .Build();

        host.Run();
    }
}

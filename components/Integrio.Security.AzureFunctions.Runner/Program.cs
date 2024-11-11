using Integrio.Security.AzureFunctions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication(builder =>
    {
        // builder.UseFunctionAuthorization(
        //     tenantId: "<tenantId>",
        //     validIssuers: ["<issuer1>", ">issuer2>"],
        //     validAudiences: ["<audience1>", "<audience2>"]);
        builder.UseFunctionAuthorization(string.Empty);
    })
    .ConfigureAppConfiguration((context, builder) =>
    {
        builder.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
        var config = builder.Build();
    })
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
    })
    .Build();

host.Run();
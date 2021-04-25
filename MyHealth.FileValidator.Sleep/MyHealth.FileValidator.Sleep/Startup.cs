using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyHealth.Common;
using MyHealth.FileValidator.Sleep;
using MyHealth.FileValidator.Sleep.Functions;
using MyHealth.FileValidator.Sleep.Parsers;
using System.IO;

[assembly: FunctionsStartup(typeof(Startup))]
namespace MyHealth.FileValidator.Sleep
{
    public class Startup : FunctionsStartup
    {
        private static ILogger _logger;

        public override void Configure(IFunctionsHostBuilder builder)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            builder.Services.AddSingleton<IConfiguration>(config);
            builder.Services.AddLogging();
            _logger = new LoggerFactory().CreateLogger(nameof(ValidateIncomingSleepFile));

            builder.Services.AddSingleton<IServiceBusHelpers>(sp =>
            {
                IConfiguration config = sp.GetService<IConfiguration>();
                return new ServiceBusHelpers(config["ServiceBusConnectionString"]);
            });

            builder.Services.AddSingleton<IAzureBlobHelpers>(sp =>
            {
                IConfiguration config = sp.GetService<IConfiguration>();
                return new AzureBlobHelpers(config["BlobStorageConnectionString"], config["MyHealthContainer"]);
            });

            builder.Services.AddSingleton<ITableHelpers>(sp =>
            {
                IConfiguration config = sp.GetService<IConfiguration>();
                return new TableHelpers(config["BlobStorageConnectionString"], config["DuplicateFilesTable"]);
            });

            builder.Services.AddScoped<ISleepRecordParser, SleepRecordParser>();
        }
    }
}

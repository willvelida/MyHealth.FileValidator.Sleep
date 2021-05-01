using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyHealth.Common;
using MyHealth.FileValidator.Sleep.Models;
using MyHealth.FileValidator.Sleep.Parsers;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MyHealth.FileValidator.Sleep.Functions
{
    public class ValidateIncomingSleepFile
    {
        private readonly IConfiguration _configuration;
        private readonly IAzureBlobHelpers _azureBlobHelpers;
        private readonly ISleepRecordParser _sleepRecordParser;
        private readonly IServiceBusHelpers _serviceBusHelpers;
        private readonly ITableHelpers _tableHelpers;

        public ValidateIncomingSleepFile(
            IConfiguration configuration,
            IAzureBlobHelpers azureBlobHelpers,
            ISleepRecordParser sleepRecordParser,
            IServiceBusHelpers serviceBusHelpers,
            ITableHelpers tableHelpers)
        {
            _configuration = configuration;
            _azureBlobHelpers = azureBlobHelpers;
            _sleepRecordParser = sleepRecordParser;
            _serviceBusHelpers = serviceBusHelpers;
            _tableHelpers = tableHelpers;
        }

        [FunctionName(nameof(ValidateIncomingSleepFile))]
        public async Task Run([BlobTrigger("myhealthfiles/sleep_{name}", Connection = "BlobStorageConnectionString")] Stream myBlob, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");

            try
            {
                name = "sleep_" + name;
                SleepFileEntity sleepFileEntity = new SleepFileEntity(name);

                bool isDuplicate = await _tableHelpers.IsDuplicateAsync<SleepFileEntity>(sleepFileEntity.PartitionKey, sleepFileEntity.RowKey);
                if (isDuplicate == true)
                {
                    log.LogInformation($"Duplicate file {sleepFileEntity.RowKey} discarded. Deleting file from Blob Storage Container");
                    await _azureBlobHelpers.DeleteBlobAsync(name);
                    return;
                }
                else
                {
                    log.LogInformation($"Processing new file: {name}");
                    using (var inputStream = await _azureBlobHelpers.DownloadBlobAsStreamAsync(name))
                    {
                        await _sleepRecordParser.ParseSleepStream(inputStream);
                    }
                    log.LogInformation($"{name} file processed.");

                    log.LogInformation("Insert file into duplicate table");
                    await _tableHelpers.InsertEntityAsync(sleepFileEntity);
                    log.LogInformation($"File {sleepFileEntity.RowKey} inserted into table storage");

                    log.LogInformation($"Deleteing {name} from Blob Storage");
                    await _azureBlobHelpers.DeleteBlobAsync(name);
                    log.LogInformation($"File {name} has been deleted from Blob Storage.");
                }
            }
            catch (Exception ex)
            {
                log.LogError($"Exception thrown in {nameof(ValidateIncomingSleepFile)}. Exception: {ex}");
                await _serviceBusHelpers.SendMessageToQueue(_configuration["ExceptionQueue"], ex);
                throw ex;
            }
        }
    }
}

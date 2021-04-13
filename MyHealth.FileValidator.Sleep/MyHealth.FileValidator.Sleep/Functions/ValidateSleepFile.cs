// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}
using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MyHealth.Common;
using Newtonsoft.Json.Linq;
using System.IO;
using CsvHelper;
using System.Globalization;
using mdl = MyHealth.Common.Models;

namespace MyHealth.FileValidator.Sleep.Functions
{
    public class ValidateSleepFile
    {
        private readonly ILogger<ValidateSleepFile> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceBusHelpers _serviceBusHelpers;
        private readonly IAzureBlobHelpers _azureBlobHelpers;

        public ValidateSleepFile(
            ILogger<ValidateSleepFile> logger,
            IConfiguration configuration,
            IServiceBusHelpers serviceBusHelpers,
            IAzureBlobHelpers azureBlobHelpers)
        {
            _logger = logger;
            _configuration = configuration;
            _serviceBusHelpers = serviceBusHelpers;
            _azureBlobHelpers = azureBlobHelpers;
        }

        [FunctionName(nameof(ValidateSleepFile))]
        public async Task Run([EventGridTrigger]EventGridEvent eventGridEvent)
        {
            try
            {
                var eventData = JObject.Parse(eventGridEvent.Data.ToString());
                var fileUrlToken = eventData["url"];

                if (fileUrlToken == null)
                {
                    throw new ApplicationException("Sleep file URL is missing from the incoming event");
                }

                string fileUrl = fileUrlToken.ToString();
                var recievedSleepBlobName = "sleep/" + Path.GetFileName(fileUrl);

                using (var inputStream = await _azureBlobHelpers.DownloadBlobAsStreamAsync(recievedSleepBlobName))
                {
                    inputStream.Seek(0, SeekOrigin.Begin);

                    using (var sleepStream = new StreamReader(inputStream))
                    using (var csv = new CsvReader(sleepStream, CultureInfo.InvariantCulture))
                    {
                        if (csv.Read())
                        {
                            csv.ReadHeader();
                            while (csv.Read())
                            {
                                var sleep = new mdl.Sleep
                                {
                                    StartTime = DateTime.Parse(csv.GetField("Start Time")),
                                    EndTime = DateTime.Parse(csv.GetField("End Time")),
                                    MinutesAsleep = int.Parse(csv.GetField("Minutes Asleep")),
                                    MinutesAwake = int.Parse(csv.GetField("Minutes Awake")),
                                    NumberOfAwakenings = int.Parse(csv.GetField("Number Of Awakening")),
                                    TimeInBed = int.Parse(csv.GetField("Time in Bed")),
                                    MinutesREMSleep = int.Parse(csv.GetField("Minutes REM Sleep")),
                                    MinutesLightSleep = int.Parse(csv.GetField("Minutes Light Sleep")),
                                    MinutesDeepSleep = int.Parse(csv.GetField("Minutes Deep Sleep"))
                                };

                                await _serviceBusHelpers.SendMessageToTopic(_configuration["SleepTopic"], sleep);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown in {nameof(ValidateSleepFile)}. Exception: {ex.Message}");
                await _serviceBusHelpers.SendMessageToTopic(_configuration["ExceptionTopicName"], ex);
            }
        }
    }
}

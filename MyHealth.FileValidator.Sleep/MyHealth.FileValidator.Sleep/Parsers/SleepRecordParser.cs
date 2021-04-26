using CsvHelper;
using Microsoft.Extensions.Configuration;
using MyHealth.Common;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using mdl = MyHealth.Common.Models;

namespace MyHealth.FileValidator.Sleep.Parsers
{
    public class SleepRecordParser : ISleepRecordParser
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceBusHelpers _serviceBusHelpers;

        public SleepRecordParser(
            IConfiguration configuration,
            IServiceBusHelpers serviceBusHelpers)
        {
            _configuration = configuration;
            _serviceBusHelpers = serviceBusHelpers;
        }

        public async Task ParseSleepStream(Stream inputStream)
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
                            StartTime = DateTime.ParseExact(csv.GetField("Start Time"), "d/MM/yyyy H:mm", null),
                            EndTime = DateTime.ParseExact(csv.GetField("End Time"), "d/MM/yyyy H:mm", null),
                            MinutesAsleep = int.Parse(ConvertNaNToZero(csv.GetField("Minutes Asleep"))),
                            MinutesAwake = int.Parse(ConvertNaNToZero(csv.GetField("Minutes Awake"))),
                            NumberOfAwakenings = int.Parse(ConvertNaNToZero(csv.GetField("Number of Awakenings"))),
                            TimeInBed = int.Parse(ConvertNaNToZero(csv.GetField("Time in Bed"))),
                            MinutesREMSleep = int.Parse(ConvertNaNToZero(csv.GetField("Minutes REM Sleep"))),
                            MinutesLightSleep = int.Parse(ConvertNaNToZero(csv.GetField("Minutes Light Sleep"))),
                            MinutesDeepSleep = int.Parse(ConvertNaNToZero(csv.GetField("Minutes Deep Sleep")))
                        };

                        await _serviceBusHelpers.SendMessageToTopic(_configuration["SleepTopic"], sleep);
                    }
                }
            }
        }

        private string ConvertNaNToZero(string csvField)
        {
            if (csvField == "N/A")          
                csvField = "0";

            return csvField;
        }
    }
}

using Microsoft.Azure.Cosmos.Table;

namespace MyHealth.FileValidator.Sleep.Models
{
    public class SleepFileEntity : TableEntity
    {
        public SleepFileEntity()
        {

        }

        public SleepFileEntity(string fileName)
        {
            PartitionKey = "Sleep";
            RowKey = fileName;
        }
    }
}

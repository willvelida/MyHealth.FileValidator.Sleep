using System.IO;
using System.Threading.Tasks;

namespace MyHealth.FileValidator.Sleep.Parsers
{
    public interface ISleepRecordParser
    {
        Task ParseSleepStream(Stream inputStream);
    }
}

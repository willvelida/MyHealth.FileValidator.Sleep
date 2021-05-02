using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using MyHealth.Common;
using MyHealth.FileValidator.Sleep.Parsers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using mdl = MyHealth.Common.Models;

namespace MyHealth.FileValidator.Sleep.UnitTests.ParserTests
{
    public class SleepRecordParserShould
    {
        private Mock<IConfiguration> _mockConfiguration;
        private Mock<IServiceBusHelpers> _mockServiceBusHelpers;
        private Mock<Stream> _mockStream;

        private SleepRecordParser _sut;

        public SleepRecordParserShould()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockServiceBusHelpers = new Mock<IServiceBusHelpers>();
            _mockStream = new Mock<Stream>();

            _sut = new SleepRecordParser(
                _mockConfiguration.Object,
                _mockServiceBusHelpers.Object);
        }

        [Fact]
        public async Task ThrowExceptionWhenStreamStartFails()
        {
            // Arrange
            _mockStream.Setup(x => x.Seek(It.IsAny<long>(), It.IsAny<SeekOrigin>())).Throws(new Exception());            

            // Act
            Func<Task> parserAction = async () => await _sut.ParseSleepStream(_mockStream.Object);

            // Assert
            parserAction.Should().Throw<Exception>();
        }

        [Fact]
        public async Task ThrowExceptionWhenInputDataIsInvalid()
        {
            // Arrange
            var testSleep = new mdl.Sleep();

            byte[] byteArray = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(testSleep));
            MemoryStream memoryStream = new MemoryStream(byteArray);

            // Act
            Func<Task> parserAction = async () => await _sut.ParseSleepStream(memoryStream);

            // Assert
            await parserAction.Should().ThrowAsync<Exception>();
        }
    }
}

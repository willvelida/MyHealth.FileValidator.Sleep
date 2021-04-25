using FluentAssertions;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using MyHealth.Common;
using MyHealth.FileValidator.Sleep.Functions;
using MyHealth.FileValidator.Sleep.Parsers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MyHealth.FileValidator.Sleep.UnitTests.FunctionTests
{
    public class ValidateIncomingSleepFileShould
    {
        private Mock<IConfiguration> _mockConfiguration;
        private Mock<IAzureBlobHelpers> _mockAzureBlobHelpers;
        private Mock<ISleepRecordParser> _mockSleepRecordParser;
        private Mock<ITableHelpers> _mockTableHelpers;
        private Mock<Stream> _mockStream;
        private Mock<ILogger> _mockLogger;

        private ValidateIncomingSleepFile _func;

        public ValidateIncomingSleepFileShould()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockAzureBlobHelpers = new Mock<IAzureBlobHelpers>();
            _mockSleepRecordParser = new Mock<ISleepRecordParser>();
            _mockTableHelpers = new Mock<ITableHelpers>();
            _mockStream = new Mock<Stream>();
            _mockLogger = new Mock<ILogger>();

            _func = new ValidateIncomingSleepFile(
                _mockConfiguration.Object,
                _mockAzureBlobHelpers.Object,
                _mockSleepRecordParser.Object,
                _mockTableHelpers.Object);
        }

        [Fact]
        public async Task CatchAndLogExceptionWhenDownloadBlobAsStreamAsyncThrowsException()
        {
            // Arrange
            _mockTableHelpers.Setup(x => x.IsDuplicateAsync<TableEntity>(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
            _mockAzureBlobHelpers.Setup(x => x.DownloadBlobAsStreamAsync(It.IsAny<string>())).ThrowsAsync(It.IsAny<Exception>());

            // Act
            Func<Task> functionAction = async () => await _func.Run(_mockStream.Object, "TestFileName", _mockLogger.Object);

            // Assert
            await functionAction.Should().ThrowAsync<Exception>(It.IsAny<string>());
        }

        [Fact]
        public async Task CatchAndLogExceptionWhenParseActivityStreamThrowsException()
        {
            // Arrange
            _mockSleepRecordParser.Setup(x => x.ParseSleepStream(It.IsAny<Stream>())).ThrowsAsync(It.IsAny<Exception>());

            // Act
            Func<Task> functionAction = async () => await _func.Run(_mockStream.Object, "TestFileName", _mockLogger.Object);

            // Assert
            await functionAction.Should().ThrowAsync<Exception>(It.IsAny<string>());
        }

        [Fact]
        public async Task CatchAndLogExceptionWhenIsDuplicateAsyncThrowsException()
        {
            // Arrage
            _mockTableHelpers.Setup(x => x.IsDuplicateAsync<TableEntity>(It.IsAny<string>(), It.IsAny<string>())).ThrowsAsync(It.IsAny<Exception>());

            // Act
            Func<Task> functionAction = async () => await _func.Run(_mockStream.Object, "TestFileName", _mockLogger.Object);

            // Assert
            await functionAction.Should().ThrowAsync<Exception>(It.IsAny<string>());
        }

        [Fact]
        public async Task SuccessfullyProcessFile()
        {
            // Arrange
            _mockAzureBlobHelpers.Setup(x => x.DownloadBlobAsStreamAsync(It.IsAny<string>())).ReturnsAsync(It.IsAny<Stream>()).Verifiable();
            _mockSleepRecordParser.Setup(x => x.ParseSleepStream(It.IsAny<Stream>())).Returns(Task.CompletedTask).Verifiable();

            // Act
            Func<Task> functionAction = async () => await _func.Run(_mockStream.Object, "TestFileName", _mockLogger.Object);

            // Assert
            await functionAction.Should().NotThrowAsync<Exception>(It.IsAny<string>());
            _mockAzureBlobHelpers.Verify(x => x.DownloadBlobAsStreamAsync(It.IsAny<string>()), Times.Once);
            _mockAzureBlobHelpers.Verify(x => x.DeleteBlobAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task DeleteFileFromBlobStorageWhenIsDuplicateAsyncReturnsTrue()
        {
            // Arrange
            _mockTableHelpers.Setup(x => x.IsDuplicateAsync<TableEntity>(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            // Act
            Func<Task> functionAction = async () => await _func.Run(_mockStream.Object, "TestFileName", _mockLogger.Object);

            // Assert
            await functionAction.Should().NotThrowAsync<Exception>(It.IsAny<string>());
            _mockAzureBlobHelpers.Verify(x => x.DeleteBlobAsync(It.IsAny<string>()), Times.Once);
        }
    }
}

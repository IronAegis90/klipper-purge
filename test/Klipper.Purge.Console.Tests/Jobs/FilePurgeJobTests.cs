using Klipper.Purge.Console.Jobs;
using Klipper.Purge.Console.Moonraker;
using Klipper.Purge.Console.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Quartz;
using Quartz.Impl;

namespace Klipper.Purge.Console.Tests.Jobs;

public class FilePurgeJobTests
{
    private readonly IOptions<FilePurgeOptions> _options;

    private readonly Mock<ILogger<FilePurgeJob>> _loggerMock;

    public FilePurgeJobTests()
    {
        _options = Microsoft.Extensions.Options.Options.Create(new FilePurgeOptions()
        {
            PurgeOlderThanDays = 3
        });
        _loggerMock = new Mock<ILogger<FilePurgeJob>>();
    }

    [Fact]
    public async void ShouldNotDeleteNewerFile()
    {
        var mockJobExecutionContext = new Mock<IJobExecutionContext>();
        var moonrakerClientMock = new Mock<IMoonrakerClient>();

        moonrakerClientMock.Setup(x => x.ListFilesAsync()).Returns(Task.FromResult<FileListResult?>(new FileListResult()
        {
            Files = new List<Moonraker.File>()
            {
                new Moonraker.File() {
                    Path = "testing",
                    Modified = DateTimeOffset.Now.AddDays(-2).ToUnixTimeSeconds()
                }
            }
        }));

        var filePurgeJob = new FilePurgeJob(_loggerMock.Object, _options, moonrakerClientMock.Object);

        await filePurgeJob.Execute(mockJobExecutionContext.Object);

        moonrakerClientMock.Verify(x => x.DeleteFile(It.IsAny<string>()), Times.Never());
    }

    [Fact]
    public async void ShouldDeleteOlderFile()
    {
        var mockJobExecutionContext = new Mock<IJobExecutionContext>();
        var moonrakerClientMock = new Mock<IMoonrakerClient>();

        moonrakerClientMock.Setup(x => x.ListFilesAsync()).Returns(Task.FromResult<FileListResult?>(new FileListResult()
        {
            Files = new List<Moonraker.File>()
            {
                new Moonraker.File() {
                    Path = "testing",
                    Modified = DateTimeOffset.Now.AddDays(-4).ToUnixTimeSeconds()
                }
            }
        }));

        var filePurgeJob = new FilePurgeJob(_loggerMock.Object, _options, moonrakerClientMock.Object);

        await filePurgeJob.Execute(mockJobExecutionContext.Object);

        moonrakerClientMock.Verify(x => x.DeleteFile(It.IsAny<string>()), Times.Once());
    }

    [Fact]
    public void ShouldNotDeleteQueuedFile()
    {

    }

    [Fact]
    public void ShouldNotDeleteCurrentlyPrinting()
    {

    }
}
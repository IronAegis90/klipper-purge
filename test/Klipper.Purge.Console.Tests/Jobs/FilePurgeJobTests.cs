using Klipper.Purge.Console.Jobs;
using Klipper.Purge.Console.Moonraker;
using Klipper.Purge.Console.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Quartz;

namespace Klipper.Purge.Console.Tests.Jobs;

public class FilePurgeJobTests
{
    private readonly Mock<ILogger<FilePurgeJob>> _loggerMock;

    private readonly Mock<IJobExecutionContext> _jobExecutionContextMock;

    private readonly Mock<IOptions<FilePurgeOptions>> _optionsMock;

    private readonly Mock<IMoonrakerClient> _moonrakerClientMock;

    public FilePurgeJobTests()
    {
        _loggerMock = new Mock<ILogger<FilePurgeJob>>();

        _jobExecutionContextMock = new Mock<IJobExecutionContext>();

        _optionsMock = new Mock<IOptions<FilePurgeOptions>>();

        _optionsMock.Setup(x => x.Value).Returns(new FilePurgeOptions()
        {
            Enabled = true,
            ExcludeQueued = true,
            PurgeOlderThanDays = 3
        });

        _moonrakerClientMock = new Mock<IMoonrakerClient>();

        _moonrakerClientMock.Setup(x => x.GetPrinterStatusAsync()).Returns(Task.FromResult<Printer?>(new Printer()
        {
            Result = new PrinterResult()
            {
                Status = new PrinterStatus()
                {
                    PrintStatus = new PrintStatus()
                    {
                        Filename = "test.gcode",
                        State = "complete"
                    }
                }
            }
        }));

        _moonrakerClientMock.Setup(x => x.ListFilesAsync()).Returns(Task.FromResult<FileListResult?>(new FileListResult()
        {
            Files = new List<Moonraker.File>()
            {
                new Moonraker.File() {
                    Path = "testing.gcode",
                    Modified = DateTimeOffset.Now.AddDays(-4).ToUnixTimeSeconds()
                }
            }
        }));

        _moonrakerClientMock.Setup(x => x.GetJobQueueStatusAsync()).Returns(Task.FromResult<JobQueueStatus?>(new JobQueueStatus()
        {
            Result = new JobQueueResult()
            {
                Jobs = new List<Job>()
            {
                new Job() {
                    Id = "test-id",
                    Path = "test.gcode",
                    AddedOn = DateTime.Now.AddDays(-10)
                }
            }
            }
        }));
    }

    [Fact]
    public async void ShouldNotDeleteNewerFile()
    {
        _moonrakerClientMock.Setup(x => x.ListFilesAsync()).Returns(Task.FromResult<FileListResult?>(new FileListResult()
        {
            Files = new List<Moonraker.File>()
            {
                new Moonraker.File() {
                    Path = "testing.gcode",
                    Modified = DateTimeOffset.Now.AddDays(-2).ToUnixTimeSeconds()
                }
            }
        }));

        var filePurgeJob = new FilePurgeJob(_loggerMock.Object, _optionsMock.Object, _moonrakerClientMock.Object);

        await filePurgeJob.Execute(_jobExecutionContextMock.Object);

        _moonrakerClientMock.Verify(x => x.DeleteFileAsync(It.IsAny<string>()), Times.Never());
    }

    [Fact]
    public async void ShouldDeleteOlderFile()
    {
        var filePurgeJob = new FilePurgeJob(_loggerMock.Object, _optionsMock.Object, _moonrakerClientMock.Object);

        await filePurgeJob.Execute(_jobExecutionContextMock.Object);

        _moonrakerClientMock.Verify(x => x.DeleteFileAsync(It.IsAny<string>()), Times.Once());
    }

    [Fact]
    public async void ShouldNotDeleteQueuedFile()
    {
        _moonrakerClientMock.Setup(x => x.GetJobQueueStatusAsync()).Returns(Task.FromResult<JobQueueStatus?>(new JobQueueStatus()
        {
            Result = new JobQueueResult()
            {
                Jobs = new List<Job>()
            {
                new Job() {
                    Id = "test-id",
                    Path = "testing.gcode",
                    AddedOn = DateTime.Now.AddDays(-10)
                }
            }
            }
        }));

        var filePurgeJob = new FilePurgeJob(_loggerMock.Object, _optionsMock.Object, _moonrakerClientMock.Object);

        await filePurgeJob.Execute(_jobExecutionContextMock.Object);

        _moonrakerClientMock.Verify(x => x.DeleteFileAsync(It.IsAny<string>()), Times.Never());
    }

    [Fact]
    public async void ShouldDeleteQueuedFileAndJobs()
    {
        _optionsMock.Setup(x => x.Value).Returns(new FilePurgeOptions()
        {
            PurgeOlderThanDays = 3,
            ExcludeQueued = false
        });

        _moonrakerClientMock.Setup(x => x.GetJobQueueStatusAsync()).Returns(Task.FromResult<JobQueueStatus?>(new JobQueueStatus()
        {
            Result = new JobQueueResult()
            {
                Jobs = new List<Job>()
                {
                    new Job() {
                        Id = "test-id-1",
                        Path = "testing.gcode",
                        AddedOn = DateTime.Now.AddDays(-10)
                    },
                    new Job() {
                        Id = "test-id-2",
                        Path = "testing.gcode",
                        AddedOn = DateTime.Now.AddDays(-10)
                    },
                    new Job() {
                        Id = "test-id-3",
                        Path = "testing.gcode",
                        AddedOn = DateTime.Now.AddDays(-10)
                    }
                }
            }
        }));

        var filePurgeJob = new FilePurgeJob(_loggerMock.Object, _optionsMock.Object, _moonrakerClientMock.Object);

        await filePurgeJob.Execute(_jobExecutionContextMock.Object);

        _moonrakerClientMock.Verify(x => x.DeleteFileAsync(It.IsAny<string>()), Times.Once());
        _moonrakerClientMock.Verify(x => x.DeleteJobAsync(It.IsAny<string>()), Times.Exactly(3));
    }

    [Fact]
    public async void ShouldNotDeleteCurrentlyPrinting()
    {
        _moonrakerClientMock.Setup(x => x.GetPrinterStatusAsync()).Returns(Task.FromResult<Printer?>(new Printer()
        {
            Result = new PrinterResult()
            {
                Status = new PrinterStatus()
                {
                    PrintStatus = new PrintStatus()
                    {
                        Filename = "testing.gcode",
                        State = "printing"
                    }
                }
            }
        }));

        var filePurgeJob = new FilePurgeJob(_loggerMock.Object, _optionsMock.Object, _moonrakerClientMock.Object);

        await filePurgeJob.Execute(_jobExecutionContextMock.Object);

        _moonrakerClientMock.Verify(x => x.GetPrinterStatusAsync(), Times.AtLeastOnce());
        _moonrakerClientMock.Verify(x => x.DeleteFileAsync(It.IsAny<string>()), Times.Never());
    }

    [Fact]
    public async void ShouldDeleteCompletePrint()
    {
        var filePurgeJob = new FilePurgeJob(_loggerMock.Object, _optionsMock.Object, _moonrakerClientMock.Object);

        await filePurgeJob.Execute(_jobExecutionContextMock.Object);

        _moonrakerClientMock.Verify(x => x.GetPrinterStatusAsync(), Times.AtLeastOnce());
        _moonrakerClientMock.Verify(x => x.DeleteFileAsync(It.IsAny<string>()), Times.Once());
    }

    [Fact]
    public async void ShouldFailWithoutCurrentPrintStatus()
    {
        _moonrakerClientMock.Setup(x => x.GetPrinterStatusAsync()).Returns(Task.FromResult<Printer?>(null));

        var filePurgeJob = new FilePurgeJob(_loggerMock.Object, _optionsMock.Object, _moonrakerClientMock.Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await filePurgeJob.Execute(_jobExecutionContextMock.Object));

        Assert.Equal("Unable to retrieve current print status", exception.Message);
    }

    [Fact]
    public async void ShouldFailWithoutCurrentFileList()
    {
        _moonrakerClientMock.Setup(x => x.ListFilesAsync()).Returns(Task.FromResult<FileListResult?>(null));

        var filePurgeJob = new FilePurgeJob(_loggerMock.Object, _optionsMock.Object, _moonrakerClientMock.Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await filePurgeJob.Execute(_jobExecutionContextMock.Object));

        Assert.Equal("Unable to retrieve file list", exception.Message);
    }

    [Fact]
    public async void ShouldFailWithoutCurrentJobQueueList()
    {
        _moonrakerClientMock.Setup(x => x.GetJobQueueStatusAsync()).Returns(Task.FromResult<JobQueueStatus?>(null));

        var filePurgeJob = new FilePurgeJob(_loggerMock.Object, _optionsMock.Object, _moonrakerClientMock.Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await filePurgeJob.Execute(_jobExecutionContextMock.Object));

        Assert.Equal("Unable to retrieve current job queue", exception.Message);
    }
}
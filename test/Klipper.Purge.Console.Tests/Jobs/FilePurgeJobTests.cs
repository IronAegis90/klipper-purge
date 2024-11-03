using Klipper.Purge.Console.Jobs;
using Klipper.Purge.Console.Moonraker;
using Klipper.Purge.Console.Options;
using Microsoft.Extensions.Logging;
using Moq;
using Quartz;

namespace Klipper.Purge.Console.Tests.Jobs;

public class FilePurgeJobTests
{
    private readonly Mock<ILogger<FilePurgeJob>> _loggerMock;

    public FilePurgeJobTests()
    {
        _loggerMock = new Mock<ILogger<FilePurgeJob>>();
    }

    [Fact]
    public async void ShouldNotDeleteNewerFile()
    {
        var mockJobExecutionContext = new Mock<IJobExecutionContext>();
        var moonrakerClientMock = new Mock<IMoonrakerClient>();
        var options = Microsoft.Extensions.Options.Options.Create(new FilePurgeOptions()
        {
            PurgeOlderThanDays = 3
        });

        moonrakerClientMock.Setup(x => x.ListFilesAsync()).Returns(Task.FromResult<FileListResult?>(new FileListResult()
        {
            Files = new List<Moonraker.File>()
            {
                new Moonraker.File() {
                    Path = "testing.gcode",
                    Modified = DateTimeOffset.Now.AddDays(-2).ToUnixTimeSeconds()
                }
            }
        }));

        moonrakerClientMock.Setup(x => x.GetPrintStatusAsync()).Returns(Task.FromResult<PrintStatusResult?>(new PrintStatusResult()
        {
            Filename = "testing.gcode",
            State = "complete"
        }));

        var filePurgeJob = new FilePurgeJob(_loggerMock.Object, options, moonrakerClientMock.Object);

        await filePurgeJob.Execute(mockJobExecutionContext.Object);

        moonrakerClientMock.Verify(x => x.DeleteFileAsync(It.IsAny<string>()), Times.Never());
    }

    [Fact]
    public async void ShouldDeleteOlderFile()
    {
        var mockJobExecutionContext = new Mock<IJobExecutionContext>();
        var moonrakerClientMock = new Mock<IMoonrakerClient>();
        var options = Microsoft.Extensions.Options.Options.Create(new FilePurgeOptions()
        {
            PurgeOlderThanDays = 3
        });

        moonrakerClientMock.Setup(x => x.ListFilesAsync()).Returns(Task.FromResult<FileListResult?>(new FileListResult()
        {
            Files = new List<Moonraker.File>()
            {
                new Moonraker.File() {
                    Path = "testing.gcode",
                    Modified = DateTimeOffset.Now.AddDays(-4).ToUnixTimeSeconds()
                }
            }
        }));

        moonrakerClientMock.Setup(x => x.GetPrintStatusAsync()).Returns(Task.FromResult<PrintStatusResult?>(new PrintStatusResult()
        {
            Filename = "testing.gcode",
            State = "complete"
        }));

        var filePurgeJob = new FilePurgeJob(_loggerMock.Object, options, moonrakerClientMock.Object);

        await filePurgeJob.Execute(mockJobExecutionContext.Object);

        moonrakerClientMock.Verify(x => x.DeleteFileAsync(It.IsAny<string>()), Times.Once());
    }

    [Fact]
    public async void ShouldNotDeleteQueuedFile()
    {
        var mockJobExecutionContext = new Mock<IJobExecutionContext>();
        var moonrakerClientMock = new Mock<IMoonrakerClient>();
        var options = Microsoft.Extensions.Options.Options.Create(new FilePurgeOptions()
        {
            PurgeOlderThanDays = 3
        });

        moonrakerClientMock.Setup(x => x.ListFilesAsync()).Returns(Task.FromResult<FileListResult?>(new FileListResult()
        {
            Files = new List<Moonraker.File>()
            {
                new Moonraker.File() {
                    Path = "test.gcode",
                    Modified = DateTimeOffset.Now.AddDays(-4).ToUnixTimeSeconds()
                }
            }
        }));

        moonrakerClientMock.Setup(x => x.ListJobsAsync()).Returns(Task.FromResult<JobListResult?>(new JobListResult()
        {
            Jobs = new List<Job>()
            {
                new Job() {
                    Id = "test-id",
                    Path = "test.gcode",
                    AddedOn = DateTime.Now.AddDays(-10)
                }
            }
        }));

        moonrakerClientMock.Setup(x => x.GetPrintStatusAsync()).Returns(Task.FromResult<PrintStatusResult?>(new PrintStatusResult()
        {
            Filename = "testing.gcode",
            State = "complete"
        }));

        var filePurgeJob = new FilePurgeJob(_loggerMock.Object, options, moonrakerClientMock.Object);

        await filePurgeJob.Execute(mockJobExecutionContext.Object);

        moonrakerClientMock.Verify(x => x.DeleteFileAsync(It.IsAny<string>()), Times.Never());
    }

    [Fact]
    public async void ShouldDeleteQueuedFileAndJob()
    {
        var mockJobExecutionContext = new Mock<IJobExecutionContext>();
        var moonrakerClientMock = new Mock<IMoonrakerClient>();
        var options = Microsoft.Extensions.Options.Options.Create(new FilePurgeOptions()
        {
            PurgeOlderThanDays = 3,
            ExcludeQueued = false
        });

        moonrakerClientMock.Setup(x => x.ListFilesAsync()).Returns(Task.FromResult<FileListResult?>(new FileListResult()
        {
            Files = new List<Moonraker.File>()
            {
                new Moonraker.File() {
                    Path = "test.gcode",
                    Modified = DateTimeOffset.Now.AddDays(-4).ToUnixTimeSeconds()
                }
            }
        }));

        moonrakerClientMock.Setup(x => x.ListJobsAsync()).Returns(Task.FromResult<JobListResult?>(new JobListResult()
        {
            Jobs = new List<Job>()
            {
                new Job() {
                    Id = "test-id",
                    Path = "test.gcode",
                    AddedOn = DateTime.Now.AddDays(-10)
                }
            }
        }));

        moonrakerClientMock.Setup(x => x.GetPrintStatusAsync()).Returns(Task.FromResult<PrintStatusResult?>(new PrintStatusResult()
        {
            Filename = "testing.gcode",
            State = "complete"
        }));

        var filePurgeJob = new FilePurgeJob(_loggerMock.Object, options, moonrakerClientMock.Object);

        await filePurgeJob.Execute(mockJobExecutionContext.Object);

        moonrakerClientMock.Verify(x => x.DeleteFileAsync(It.IsAny<string>()), Times.Once());
        moonrakerClientMock.Verify(x => x.DeleteJobAsync(It.IsAny<string>()), Times.Once());
    }

    [Fact]
    public async void ShouldDeleteQueuedFileAndJobs()
    {
        var mockJobExecutionContext = new Mock<IJobExecutionContext>();
        var moonrakerClientMock = new Mock<IMoonrakerClient>();
        var options = Microsoft.Extensions.Options.Options.Create(new FilePurgeOptions()
        {
            PurgeOlderThanDays = 3,
            ExcludeQueued = false
        });

        moonrakerClientMock.Setup(x => x.ListFilesAsync()).Returns(Task.FromResult<FileListResult?>(new FileListResult()
        {
            Files = new List<Moonraker.File>()
            {
                new Moonraker.File() {
                    Path = "test.gcode",
                    Modified = DateTimeOffset.Now.AddDays(-4).ToUnixTimeSeconds()
                }
            }
        }));

        moonrakerClientMock.Setup(x => x.ListJobsAsync()).Returns(Task.FromResult<JobListResult?>(new JobListResult()
        {
            Jobs = new List<Job>()
            {
                new Job() {
                    Id = "test-id-1",
                    Path = "test.gcode",
                    AddedOn = DateTime.Now.AddDays(-10)
                },
                new Job() {
                    Id = "test-id-2",
                    Path = "test.gcode",
                    AddedOn = DateTime.Now.AddDays(-10)
                },
                new Job() {
                    Id = "test-id-3",
                    Path = "test.gcode",
                    AddedOn = DateTime.Now.AddDays(-10)
                }
            }
        }));

        moonrakerClientMock.Setup(x => x.GetPrintStatusAsync()).Returns(Task.FromResult<PrintStatusResult?>(new PrintStatusResult()
        {
            Filename = "testing.gcode",
            State = "complete"
        }));

        var filePurgeJob = new FilePurgeJob(_loggerMock.Object, options, moonrakerClientMock.Object);

        await filePurgeJob.Execute(mockJobExecutionContext.Object);

        moonrakerClientMock.Verify(x => x.DeleteFileAsync(It.IsAny<string>()), Times.Once());
        moonrakerClientMock.Verify(x => x.DeleteJobAsync(It.IsAny<string>()), Times.Exactly(3));
    }

    [Fact]
    public async void ShouldNotDeleteCurrentlyPrinting()
    {
        var mockJobExecutionContext = new Mock<IJobExecutionContext>();
        var moonrakerClientMock = new Mock<IMoonrakerClient>();
        var options = Microsoft.Extensions.Options.Options.Create(new FilePurgeOptions()
        {
            PurgeOlderThanDays = 3
        });

        moonrakerClientMock.Setup(x => x.ListFilesAsync()).Returns(Task.FromResult<FileListResult?>(new FileListResult()
        {
            Files = new List<Moonraker.File>()
            {
                new Moonraker.File() {
                    Path = "testing.gcode",
                    Modified = DateTimeOffset.Now.AddDays(-4).ToUnixTimeSeconds()
                }
            }
        }));

        moonrakerClientMock.Setup(x => x.GetPrintStatusAsync()).Returns(Task.FromResult<PrintStatusResult?>(new PrintStatusResult()
        {
            Filename = "testing.gcode",
            State = "printing"
        }));

        var filePurgeJob = new FilePurgeJob(_loggerMock.Object, options, moonrakerClientMock.Object);

        await filePurgeJob.Execute(mockJobExecutionContext.Object);

        moonrakerClientMock.Verify(x => x.GetPrintStatusAsync(), Times.AtLeastOnce());
        moonrakerClientMock.Verify(x => x.DeleteFileAsync(It.IsAny<string>()), Times.Never());
    }

    [Fact]
    public async void ShouldDeleteCompletePrint()
    {
        var mockJobExecutionContext = new Mock<IJobExecutionContext>();
        var moonrakerClientMock = new Mock<IMoonrakerClient>();
        var options = Microsoft.Extensions.Options.Options.Create(new FilePurgeOptions()
        {
            PurgeOlderThanDays = 3
        });

        moonrakerClientMock.Setup(x => x.ListFilesAsync()).Returns(Task.FromResult<FileListResult?>(new FileListResult()
        {
            Files = new List<Moonraker.File>()
            {
                new Moonraker.File() {
                    Path = "testing.gcode",
                    Modified = DateTimeOffset.Now.AddDays(-4).ToUnixTimeSeconds()
                }
            }
        }));

        moonrakerClientMock.Setup(x => x.GetPrintStatusAsync()).Returns(Task.FromResult<PrintStatusResult?>(new PrintStatusResult()
        {
            Filename = "testing.gcode",
            State = "complete"
        }));

        var filePurgeJob = new FilePurgeJob(_loggerMock.Object, options, moonrakerClientMock.Object);

        await filePurgeJob.Execute(mockJobExecutionContext.Object);

        moonrakerClientMock.Verify(x => x.GetPrintStatusAsync(), Times.AtLeastOnce());
        moonrakerClientMock.Verify(x => x.DeleteFileAsync(It.IsAny<string>()), Times.Once());
    }

    [Fact]
    public async void ShouldFailWithoutCurrentPrintStatus()
    {
        var mockJobExecutionContext = new Mock<IJobExecutionContext>();
        var moonrakerClientMock = new Mock<IMoonrakerClient>();
        var options = Microsoft.Extensions.Options.Options.Create(new FilePurgeOptions()
        {
            PurgeOlderThanDays = 3
        });

        moonrakerClientMock.Setup(x => x.ListFilesAsync()).Returns(Task.FromResult<FileListResult?>(new FileListResult()
        {
            Files = new List<Moonraker.File>()
            {
                new Moonraker.File() {
                    Path = "testing.gcode",
                    Modified = DateTimeOffset.Now.AddDays(-4).ToUnixTimeSeconds()
                }
            }
        }));

        var filePurgeJob = new FilePurgeJob(_loggerMock.Object, options, moonrakerClientMock.Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await filePurgeJob.Execute(mockJobExecutionContext.Object));

        Assert.Equal("Unable to obtain the current print's state", exception.Message);
    }
}
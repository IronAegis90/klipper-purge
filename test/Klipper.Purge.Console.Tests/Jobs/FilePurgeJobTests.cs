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

        _moonrakerClientMock.Setup(x => x.GetPrintStatusAsync()).Returns(Task.FromResult<PrintStatus?>(new PrintStatus()
        {
            Filename = "test.gcode",
            State = "complete"
        }));

        _moonrakerClientMock.Setup(x => x.ListDirectoriesAsync("gcodes")).Returns(Task.FromResult<DirectoryListResult?>(new DirectoryListResult()
        {
            Directories = new List<Moonraker.Directory>()
            {
                new Moonraker.Directory()
                {
                    Name = "child-dir"
                }
            },
            Files = new List<Moonraker.File>()
            {
                new Moonraker.File()
                {
                    Name = "testing.gcode",
                    Modified = DateTimeOffset.Now.AddDays(-4).ToUnixTimeSeconds()
                }
            }
        }));

        _moonrakerClientMock.Setup(x => x.ListDirectoriesAsync("gcodes/child-dir")).Returns(Task.FromResult<DirectoryListResult?>(new DirectoryListResult()
        {
            Directories = new List<Moonraker.Directory>(),
            Files = new List<Moonraker.File>()
            {
                new Moonraker.File()
                {
                    Name = "child-test.gcode",
                    Modified = DateTimeOffset.Now.AddDays(-4).ToUnixTimeSeconds()
                }
            }
        }));

        _moonrakerClientMock.Setup(x => x.ListJobsAsync()).Returns(Task.FromResult<JobListResult?>(new JobListResult()
        {
            Jobs = new List<Job>()
            {
                new Job() {
                    Id = "test-id",
                    Name = "test.gcode",
                    AddedOn =DateTimeOffset.Now.AddDays(-10).ToUnixTimeSeconds()
                }
            }
        }));
    }

    [Fact]
    public async void ShouldNotDeleteNewerFile()
    {
        _moonrakerClientMock.Setup(x => x.ListDirectoriesAsync("gcodes")).Returns(Task.FromResult<DirectoryListResult?>(new DirectoryListResult()
        {
            Files = new List<Moonraker.File>()
            {
                new Moonraker.File() {
                    Name = "testing.gcode",
                    Modified = DateTimeOffset.Now.AddDays(-2).ToUnixTimeSeconds()
                }
            }
        }));

        var filePurgeJob = new FilePurgeJob(_loggerMock.Object, _optionsMock.Object, _moonrakerClientMock.Object);

        await filePurgeJob.Execute(_jobExecutionContextMock.Object);

        _moonrakerClientMock.Verify(x => x.DeleteFileAsync("testing.gcode"), Times.Never());
    }

    [Fact]
    public async void ShouldDeleteOlderFile()
    {
        var filePurgeJob = new FilePurgeJob(_loggerMock.Object, _optionsMock.Object, _moonrakerClientMock.Object);

        await filePurgeJob.Execute(_jobExecutionContextMock.Object);

        _moonrakerClientMock.Verify(x => x.DeleteFileAsync("testing.gcode"), Times.Once());
        _moonrakerClientMock.Verify(x => x.DeleteFileAsync("child-dir/child-test.gcode"), Times.Once());
    }

    [Fact]
    public async void ShouldNotDeleteQueuedFile()
    {
        _moonrakerClientMock.Setup(x => x.ListJobsAsync()).Returns(Task.FromResult<JobListResult?>(new JobListResult()
        {
            Jobs = new List<Job>()
            {
                new Job() {
                    Id = "test-id",
                    Name = "testing.gcode",
                    AddedOn = DateTimeOffset.Now.AddDays(-10).ToUnixTimeSeconds()
                }
            }
        }));

        var filePurgeJob = new FilePurgeJob(_loggerMock.Object, _optionsMock.Object, _moonrakerClientMock.Object);

        await filePurgeJob.Execute(_jobExecutionContextMock.Object);

        _moonrakerClientMock.Verify(x => x.DeleteFileAsync("testing.gcode"), Times.Never());
    }

    [Fact]
    public async void ShouldDeleteQueuedFileAndJobs()
    {
        _optionsMock.Setup(x => x.Value).Returns(new FilePurgeOptions()
        {
            PurgeOlderThanDays = 3,
            ExcludeQueued = false
        });

        _moonrakerClientMock.Setup(x => x.ListJobsAsync()).Returns(Task.FromResult<JobListResult?>(new JobListResult()
        {
            Jobs = new List<Job>()
                {
                    new Job() {
                        Id = "test-id-1",
                        Name = "testing.gcode",
                        AddedOn = DateTimeOffset.Now.AddDays(-10).ToUnixTimeSeconds()
                    },
                    new Job() {
                        Id = "test-id-2",
                        Name = "testing.gcode",
                        AddedOn = DateTimeOffset.Now.AddDays(-10).ToUnixTimeSeconds()
                    },
                    new Job() {
                        Id = "test-id-3",
                        Name = "testing.gcode",
                        AddedOn = DateTimeOffset.Now.AddDays(-10).ToUnixTimeSeconds()
                    }
                }
        }));

        var filePurgeJob = new FilePurgeJob(_loggerMock.Object, _optionsMock.Object, _moonrakerClientMock.Object);

        await filePurgeJob.Execute(_jobExecutionContextMock.Object);

        _moonrakerClientMock.Verify(x => x.DeleteFileAsync("testing.gcode"), Times.Once());
        _moonrakerClientMock.Verify(x => x.DeleteFileAsync("child-dir/child-test.gcode"), Times.Once());
        _moonrakerClientMock.Verify(x => x.DeleteJobAsync("test-id-1"), Times.Once());
        _moonrakerClientMock.Verify(x => x.DeleteJobAsync("test-id-2"), Times.Once());
        _moonrakerClientMock.Verify(x => x.DeleteJobAsync("test-id-3"), Times.Once());
    }

    [Fact]
    public async void ShouldNotDeleteCurrentlyPrinting()
    {
        _moonrakerClientMock.Setup(x => x.GetPrintStatusAsync()).Returns(Task.FromResult<PrintStatus?>(new PrintStatus()
        {
            Filename = "testing.gcode",
            State = "printing"
        }));

        var filePurgeJob = new FilePurgeJob(_loggerMock.Object, _optionsMock.Object, _moonrakerClientMock.Object);

        await filePurgeJob.Execute(_jobExecutionContextMock.Object);

        _moonrakerClientMock.Verify(x => x.GetPrintStatusAsync(), Times.AtLeastOnce());
        _moonrakerClientMock.Verify(x => x.DeleteFileAsync("testing.gcode"), Times.Never());
    }

    [Fact]
    public async void ShouldDeleteCompletePrint()
    {
        var filePurgeJob = new FilePurgeJob(_loggerMock.Object, _optionsMock.Object, _moonrakerClientMock.Object);

        await filePurgeJob.Execute(_jobExecutionContextMock.Object);

        _moonrakerClientMock.Verify(x => x.GetPrintStatusAsync(), Times.AtLeastOnce());
        _moonrakerClientMock.Verify(x => x.DeleteFileAsync("testing.gcode"), Times.Once());
    }

    [Fact]
    public async void ShouldNotDeleteNonEmptyDirectory()
    {
        _moonrakerClientMock.Setup(x => x.ListDirectoriesAsync("gcodes/child-dir")).Returns(Task.FromResult<DirectoryListResult?>(new DirectoryListResult()
        {
            Directories = new List<Moonraker.Directory>(),
            Files = new List<Moonraker.File>()
            {
                new Moonraker.File()
                {
                    Name = "child-test.gcode",
                    Modified = DateTimeOffset.Now.AddDays(-2).ToUnixTimeSeconds()
                }
            }
        }));

        var filePurgeJob = new FilePurgeJob(_loggerMock.Object, _optionsMock.Object, _moonrakerClientMock.Object);

        await filePurgeJob.Execute(_jobExecutionContextMock.Object);

        _moonrakerClientMock.Verify(x => x.DeleteFileAsync("child-dir/child-test.gcode"), Times.Never());
        _moonrakerClientMock.Verify(x => x.DeleteDirectoryAsync("gcodes/child-dir"), Times.Never());
    }

    [Fact]
    public async void ShouldDeleteEmptyDirectory()
    {
        _moonrakerClientMock.SetupSequence(x => x.ListDirectoriesAsync("gcodes/child-dir"))
        .Returns(Task.FromResult<DirectoryListResult?>(new DirectoryListResult()
        {
            Directories = new List<Moonraker.Directory>(),
            Files = new List<Moonraker.File>()
            {
                new Moonraker.File()
                {
                    Name = "child-test.gcode",
                    Modified = DateTimeOffset.Now.AddDays(-4).ToUnixTimeSeconds()
                }
            }
        }))
        .Returns(Task.FromResult<DirectoryListResult?>(new DirectoryListResult()
        {
            Directories = new List<Moonraker.Directory>(),
            Files = new List<Moonraker.File>()
        }));

        var filePurgeJob = new FilePurgeJob(_loggerMock.Object, _optionsMock.Object, _moonrakerClientMock.Object);

        await filePurgeJob.Execute(_jobExecutionContextMock.Object);

        _moonrakerClientMock.Verify(x => x.ListDirectoriesAsync("gcodes/child-dir"), Times.Exactly(2));
        _moonrakerClientMock.Verify(x => x.DeleteFileAsync("child-dir/child-test.gcode"), Times.Once());
        _moonrakerClientMock.Verify(x => x.DeleteDirectoryAsync("gcodes/child-dir"), Times.Once());
    }

        [Fact]
    public async void ShouldNotDeleteNonGCodeFile()
    {
        _moonrakerClientMock.Setup(x => x.ListDirectoriesAsync("gcodes")).Returns(Task.FromResult<DirectoryListResult?>(new DirectoryListResult()
        {
            Directories = new List<Moonraker.Directory>(),
            Files = new List<Moonraker.File>()
            {
                new Moonraker.File()
                {
                    Name = "image.png",
                    Modified = DateTimeOffset.Now.AddDays(-4).ToUnixTimeSeconds()
                }
            }
        }));

        var filePurgeJob = new FilePurgeJob(_loggerMock.Object, _optionsMock.Object, _moonrakerClientMock.Object);

        await filePurgeJob.Execute(_jobExecutionContextMock.Object);

        _moonrakerClientMock.Verify(x => x.DeleteFileAsync("image.png"), Times.Never());
    }

    [Fact]
    public async void ShouldFailWithoutCurrentPrintStatus()
    {
        _moonrakerClientMock.Setup(x => x.GetPrintStatusAsync()).Returns(Task.FromResult<PrintStatus?>(null));

        var filePurgeJob = new FilePurgeJob(_loggerMock.Object, _optionsMock.Object, _moonrakerClientMock.Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await filePurgeJob.Execute(_jobExecutionContextMock.Object));

        Assert.Equal("Unable to retrieve current print status", exception.Message);
    }

    [Fact]
    public async void ShouldFailWithoutDirectoryOrFileList()
    {
        _moonrakerClientMock.Setup(x => x.ListDirectoriesAsync("gcodes")).Returns(Task.FromResult<DirectoryListResult?>(null));

        var filePurgeJob = new FilePurgeJob(_loggerMock.Object, _optionsMock.Object, _moonrakerClientMock.Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await filePurgeJob.Execute(_jobExecutionContextMock.Object));

        Assert.Equal("Unable to retrieve directory metadata", exception.Message);
    }

    [Fact]
    public async void ShouldFailWithoutCurrentJobQueueList()
    {
        _moonrakerClientMock.Setup(x => x.ListJobsAsync()).Returns(Task.FromResult<JobListResult?>(null));

        var filePurgeJob = new FilePurgeJob(_loggerMock.Object, _optionsMock.Object, _moonrakerClientMock.Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await filePurgeJob.Execute(_jobExecutionContextMock.Object));

        Assert.Equal("Unable to retrieve current job queue", exception.Message);
    }
}
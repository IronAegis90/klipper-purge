using Klipper.Purge.Console.Jobs;
using Microsoft.Extensions.Logging;
using Moq;

namespace Klipper.Purge.Console.Tests.Jobs;

public class FilePurgeJobTests
{

    private readonly ILogger<FilePurgeJob> _logger;

    public FilePurgeJobTests()
    {
        var loggerMock = new Mock<ILogger<FilePurgeJob>>();

        _logger = loggerMock.Object;
    }

    [Fact]
    public void Function_ProcessFile_ShouldNotDeleteCurrentlyPrinting()
    {

    }

    [Fact]
    public void Function_ProcessFile_ShouldNotDeleteNewerFile()
    {

    }

    [Fact]
    public void Function_ProcessFile_ShouldNotDeleteQueuedFile()
    {

    }
}
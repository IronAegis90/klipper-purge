using FluentScheduler;
using Klipper.Purge.Console.Jobs;

namespace Klipper.Purge.Console
{
    public class JobRegistry : Registry
    {
        public JobRegistry()
        {
            Schedule<Files>().ToRunEvery(1).Days().At(3, 0);
        }
    }
}
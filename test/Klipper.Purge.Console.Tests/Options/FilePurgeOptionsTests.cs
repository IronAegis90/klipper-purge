

using Klipper.Purge.Console.Options;

namespace Klipper.Purge.Console.Tests.Options
{
    public class FilePurgeOptionsTests
    {
        [Fact]
        public void Property_Enabled_ShouldDefault()
        {
            var instance = new FilePurgeOptions();

            Assert.True(instance.Enabled);
        }

        [Fact]
        public void Property_Enabled_ShouldOverride()
        {
            var instance = new FilePurgeOptions()
            {
                Enabled = false
            };

            Assert.False(instance.Enabled);
        }

        [Fact]
        public void Property_RunOnStartup_ShouldDefault()
        {
            var instance = new FilePurgeOptions();

            Assert.True(instance.RunOnStartup);
        }

        [Fact]
        public void Property_RunOnStartup_ShouldOverride()
        {
            var instance = new FilePurgeOptions()
            {
                RunOnStartup = false
            };

            Assert.False(instance.RunOnStartup);
        }

        [Fact]
        public void Property_Schedule_ShouldDefault()
        {
            var instance = new FilePurgeOptions();

            Assert.Equal("0 0 3 * * * *", instance.Schedule);
        }

        [Fact]
        public void Property_Schedule_ShouldOverride()
        {
            var value = "0 0 5 * * * *";
            var instance = new FilePurgeOptions()
            {
                Schedule = value
            };

            Assert.Equal(value, instance.Schedule);
        }

        [Fact]
        public void Property_PurgeOlderThanDays_ShouldDefault()
        {
            var instance = new FilePurgeOptions();

            Assert.Equal(7, instance.PurgeOlderThanDays);
        }

        [Fact]
        public void Property_PurgeOlderThanDays_ShouldOverride()
        {
            var value = 3;
            var instance = new FilePurgeOptions()
            {
                PurgeOlderThanDays = value
            };

            Assert.Equal(value, instance.PurgeOlderThanDays);
        }

        [Fact]
        public void Property_ExcludeQueued_ShouldDefault()
        {
            var instance = new FilePurgeOptions();

            Assert.True(instance.ExcludeQueued);
        }

        [Fact]
        public void Property_ExcludeQueued_ShouldOverride()
        {
            var instance = new FilePurgeOptions()
            {
                ExcludeQueued = false
            };

            Assert.False(instance.ExcludeQueued);
        }
    }
}
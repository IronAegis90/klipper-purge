### Quick reference
* Maintained by:  
[IronAegis90](https://github.com/IronAegis90)
* Where to get help:  
[GitHub](https://github.com/IronAegis90/klipper-purge/issues)

### What is Klipper-Purge?

Klipper-Purge is a light weight scheduled service designed to clear Klipper instances of files that are out of date and are not queued for future printing.

### How to use this image

`$ docker run --name voron2 ironaegis/klipper-purge -e MOONRAKER_URL='http://10.0.0.242:7125'`

#### Disable file purge

`-e FILE_PURGE_ENABLED=false`

Defaults to `true`. Disables the execution of file purge.

#### Run file purge on initial startup

`-e FILE_PURGE_RUN_ON_STARTUP=true`

Defaults to `true`. Will run the file purge service upon image startup rather than waiting for schedule to trigger.

#### Set the file purge schedule

`-e FILE_PURGE_SCHEDULE='0 0 3 * * * *'`

Default to `0 0 3 * * * *` (Daily at 3 AM). Use CRON expression to set when and how often the file purge service should execute.

#### Exclude file that are in Klipper's job queue

`-e FILE_PURGE_EXCLUDE_QUEUED=false`

Defaults to `true`. Determines if files that are included in the Job Queue should not be deleted.

#### Delete files older than

`-e FILE_PURGE_OLDER_THAN=3`

Defaults to `7`. Delete files that have not been modifed since `Current execution time minus FILE_PURGE_OLDER_THAN`
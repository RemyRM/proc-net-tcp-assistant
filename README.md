# proc-net-tcp-assistant

ProcNetTcp assistant will take the output of `adb.exe shell cat /proc/net/tcp` from any connected (Android) device and convert the standard hex notation of ipv4 addresses:ports to decimal for easier readability at a glance. The assistant will also convert the status code to its string value.

Add ipv4 addresses to `ipFilters` to filter out matching entries. These addresses can either be hard coded or provided upon launch.

Note that `ProcNetTcp.bat` expects `adb.exe` to be added to the environment variables. If this is not the case you need to provide the full path for `adb.exe` in `ProcNetTcp.bat`

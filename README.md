# proc-net-tcp-to-decimal
Simple C# script that converts the IP address output from proc/net/tcp from hexadecimal notation to decimal

ProcNetTcp.bat relies on having `\Android\sdk\platform-tools\` in your "Path" environment variables to execute "adb.exe" from anywhere. If platform-tools is not added to your envornment variables either add it, or supply "adb.exe" with a path inside ProcNetTcp.bat.

if `filePath` is not set inside ProcNetTcpConverter.cs it will promp the user to manually input the full ProcNetTcp.bat path. 
e.g: C:\Dev\Batch\ProcNetTcp.bat.
Note that `filePath` takes a literal string and doesn't need to have its slashes escaped.

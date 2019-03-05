# Release Notes

#### 23.1.0
- New: Additional FTP Server software detection (HP NonStop/Tandem, GlobalScape EFT, Serv-U, Cerberus, CrushFTP, glFTPd)
- New: Assume capabilities for servers that don't support FEAT (wuFTPd)
- Fix: `FileExists` returns false if name listing is used and server lists filenames with the path
- Fix: For OpenVMS absolute paths may not contain slashes but are still absolute
- Fix: For `Download()` methods `restartPosition` should not be mandatory

#### 23.0.0
- New: Ability to cancel all async methods via `CancellationToken` (thanks [WolfspiritM](/WolfspiritM))
- New: `ReadTimeout` is now honored by all async methods (thanks [WolfspiritM](/WolfspiritM))
- New: FTP Server operating system detection (Windows, Unix, VMS, IBM/OS400)
- (.NET core) Fix: GetListing blocking with no timeout (thanks [WolfspiritM](/WolfspiritM))
- (.NET core) Fix async methods by not using the the async read function (thanks [WolfspiritM](/WolfspiritM))

#### 22.0.0
- New: Ability to resume a download via `existsMode` on `DownloadFile()` and `DownloadFiles()` (thanks [n0ix](/n0ix))
- New: Ability to turn off checking for server capabilities using FEAT command (thanks @[nhh-softwarehuset](/nhh-softwarehuset))
- Fix: Add workaround if a server advertises a non-routeable IP in PASV Mode (thanks [n0ix](/n0ix))
- Fix: Recursive directory deletion tries to delete the same file twice (because GetListing is also recursive)

#### 21.0.0
- New: `OnLogEvent` callback to get logs in the context of indivivdual FtpClient connections
- Fix: All logging is done in the context of an `FtpClient` and then passed to `FtpTrace` listeners
- Signature for custom list parsers has changed, `FtpClient` argument added to the end

#### 20.0.0
- New: FTP Server software detection (PureFTPd, VsFTPd, ProFTPD, FileZilla, OpenVMS, WindowsCE, WuFTPd)
- New: Detect if the FTP server supports recursive file listing (LIST -R) command using whitelist
- New: `GetListing` will manually recurse through directories if `FtpListOption.Recursive` is set and server does not support recursion
- New: Added `LastReply` property which returns the last `FtpReply` recieved from the server.
- New: Added new upload option `AppendNoCheck` to append to a file on the server without checking if it exists (thanks @everbalovas)
- Fix: During upload, respond to any error in 5xx series, not just 550 (thanks @stengnath)
- Fix: Various fixes to `UploadFileAsync` based on fixes already implemented in `UploadFile`

#### 19.2.4
- Fix: `UploadFilesAsync` with `errorHandling` deletes the entire directory instead of specific files
- Fix: Server responds to EPSV with 425 "Data connection failed" but connects with PASV (thanks @ejohnsonTKTNET)
- Fix: Use proper async configuration for .NET Async methods (thanks @ejohnsonTKTNET)
- Fix: Improve implementation of upload and download resuming in Async methods (thanks @ejohnsonTKTNET)

#### 19.2.3
- Fix: `UploadFile()` or `UploadFiles()` sometimes fails to create the remote directory if it doesn't exist
- Fix: `DownloadDataType` Binary value ignored on ASCII-configured FTP servers
- Performance improvement: Added `BufferedStream` between `SslStream` and `NetworkStream` (thanks @Lukazoid)
- Fix: When the FTP server sends 550, transfer is received but not confirmed (thanks @stengnath)
- Fix: Make `Dispose` method of `FTPClient` virtual (thanks @martinbu)
- Fix: `OpenPassiveDataStream`/`Async()` uses the target FTP host instead of the configured proxy (thanks @rmja)
- Fix: `FileExists()` for Xlight FTP Server (thanks @oldpepper)
- Fix: FTPD "550 No files found" when folder exists but is empty, only in PASV mode (thanks @olivierSOW)
- Fix: Many unexpected EOF for remote file `IOException` on Android (thanks @jersiovic)
- Fix: Race condition when `BeginInvoke` calls the callback before the `IAsyncResult` is added (thanks @Lukazoid)

#### 19.2.2
- Fix: Prevent socket poll from hammering the server multiple times per second
- Fix: Allow using absolute paths that include drive letters (Windows servers)
- Performance improvement: Only change the FTP data type if different from required type
- Performance improvement: Download all files in EOF mode and skip the file size check, unless download progress is required
- Added all missing async versions of FTP methods to `IFtpClient`
- System: Certain core FTP socket handling operations have been changed to improve reliability & performance.

#### 19.1.4
- Fix: Fix hang in TLS activation because no timeout is set on the underlying `NetworkStream` (thanks @iamjay)

#### 19.1.3
- Added async versions of FTP methods to `IFtpClient` (thanks @peterfortuin)
- Fix: Fixes when `ActivePorts` is specified in active FTP mode (thanks @ToniMontana)
- Fix: Throw `OperationCanceledException` instead of `FtpException` when cancellation is requested (thanks @taoyouh)

#### 19.1.2
- Fix: Add support for checking if file exists on Serv-U FTP Server
- Fix: Make `IFtpClient` inherit from `IDisposable` (thanks @repl-andrew-ovens)
- (UWP) Fix: UWP does not allow `File.Exists()` to run in UI thread (thanks @taoyouh)

#### 19.1.1
- Fix: When downloading files in ASCII mode, file length is unreliable therefore we read until EOF
- Fix: When upload/download progress is indeterminate, send -1 instead of NaN or Infinity
- Fix: `NetStream` was not assigned in `FtpSocketStream` for .NET Standard in active FTP mode (thanks @ralftar)
- Fix: `CurrentDataType` was not set for ASCII transfers in `DownloadFileAsync`/`UploadFileAsync` (thanks @taoyouh)
- Fix: Sometimes `FtpSocketStream` and `FtpDataStream` are not disposed in `FtpSocketStream.Dispose` (thanks @taoyouh)

#### 19.1.0
- New Progress reporting for `UploadFile` & `DownloadFile` methods via `IProgress`
- Fix: `Stream.Position` should not be set in `UploadFileInternal` unless supported

#### 19.0.0
- New Task-based async methods for .NET Standard and .NET Fx 4.5 (thanks @taoyouh)
- New async methods for `UploadFile`, `DownloadFile`, `UploadFiles` & `DownloadFiles` (thanks @artiomchi)
- (UWP) Fix: `FileNotFoundException` with reference `System.Console` (thanks @artiomchi)
- (.NET core) Fix: Thread suspends when calling `UploadFile` or `DownloadFile` (thanks @artiomchi)
- (.NET core) Fix: File download hangs inconsistently when reading data from stream (thanks @artiomchi, @bgroenks96)
- (.NET core) Fix: Stream does not dispose due to wrong handling of closing/disposing (thanks @artiomchi)
- Fix: File upload EOS bug when calling `Stream.Read` (thanks @bgroenks96, @artiomchi, @taoyouh)
- Fix: `DownloadFileInternal` not recognizing the download data type
with `EnableThreadSafeConnections` (thanks @bgroenks96)
- (Backend) Migrate to a single VS 2017 solution for all frameworks (thanks @artiomchi)
- (Backend) Continuous Integration using AppVeyor  (thanks @artiomchi)

#### 18.0.1
- Add `IFtpClient` interface to build unit tests upon main `FtpClient` class (thanks [Kris0](https://github.com/Kris0))
- Disposing `FtpDataStream` reads server reply and closes the underlying stream (thanks [Lukazoid](https://github.com/Lukazoid))

#### 18.0.0
- New `SetModifiedTime` API to change modified date of a server file in local timezone/UTC
- Add type argument to `GetModifiedTime`, allowing for getting dates in UTC/Local timezone
- Breaking changes to Async API of `GetModifiedTime` (addition of type argument)
- `GetModifiedTime` and `SetModifiedTime` now honor the `TimeOffset` property in `FtpClient`
- Add `checkIfFileExists` to `OpenRead`, `OpenAppend` and `OpenWrite` to skip `GetFileSize` check
- Fix issue where `InnerException` is null during a file transfer (upload/download)
- Improve performance of typical uploads/downloads by skipping the extra file exists check

#### 17.6.1
- Fix for `CreateDirectory` and `DirectoryExists` to allow null/blank input path values
- Fix for `GetFtpDirectoryName` to return correct parent folder of simple folder paths (thanks [ww898](https://github.com/ww898))

#### 17.6.0
- Add argument validation for missing/blank arguments in : `Upload, Download, UploadFile(s), DownloadFile(s), GetObjectInfo, DeleteFile, DeleteDirectory, FileExists, DirectoryExists, CreateDirectory, Rename, MoveFile, MoveDirectory, SetFilePermissions, Chmod, GetFilePermissions, GetChmod, GetFileSize, GetModifiedTime, VerifyTransfer, OpenRead, OpenWrite, OpenAppend`
- Disable all async methods on .NET core due to persistant `PlatformUnsupported` exception (if you need async you are free to contribute a non-blocking version of the methods)

#### 17.5.9
- Increase performance of `GetListing` by reading multiple lines at once (BulkListing property, thanks [sierrodc](https://github.com/sierrodc))

#### 17.5.8
- Add support for parsing AS400 listings inside a file (5 fields) (thanks [rharrisxtheta](https://github.com/rharrisxtheta))
- Retry interpreting file listings after encountered invalid date format (thanks [rharrisxtheta](https://github.com/rharrisxtheta))
- Always switch into binary mode when running SIZE command (thanks [rharrisxtheta](https://github.com/rharrisxtheta))

#### 17.5.7
- Honor `UploadDataType` and `DownloadDataType` in all sync/async cases (thanks [rharrisxtheta](https://github.com/rharrisxtheta))
- Force file transfers in BINARY mode for known 0 byte files (thanks [rharrisxtheta](https://github.com/rharrisxtheta))
- Allow file transfers in ASCII mode if the server doesn't support the SIZE command (thanks [rharrisxtheta](https://github.com/rharrisxtheta))

#### 17.5.6
- Fix `NullReferenceException` when arguments are null during `FtpTrace.WriteFunc`

#### 17.5.5
- Remove internal locking for .NET Standard 1.4 version since unsupported on UWP

#### 17.5.4
- Remove dependency on `System.Threading.Thread` for .NET Standard 1.4 version (for UWP)

#### 17.5.3
- Allow transferring files in ASCII/Binary mode with the high-level API (UploadDataType, DownloadDataType)

#### 17.5.2
- Add support for .NET 3.5 and .NET Standard 1.4 (supports Universal Windows Platform 10.0)

#### 17.5.1
- Add `FtpTrace.LogToConsole` and `LogToFile` to control logging in .NET core version

#### 17.5.0
- Add `PlainTextEncryption` API to support FTPS servers and plain-text FTP firewalls (CCC command)
- FluentFTP now uses unsafe code to support the CCC command (inside `FtpSslStream`)
- If you need a "non unsafe" version of the library please add an issue

#### 17.4.4
- Add logging for high-level function calls to improve remote debugging (`FtpTrace.LogFunctions`)
- Add settings to hide sensitive data from logs (`FtpTrace.LogIP`, `LogUserName`, `LogPassword`)
- Add `RecursiveList` to control if recursive listing should be used
- Auto-detect Windows CE and disable recursive listing during `DeleteDirectory()`

#### 17.4.2
- Add `UploadRateLimit` and `DownloadRateLimit` to control the speed of data transfer (thanks [Danie-Brink](https://github.com/Danie-Brink))

#### 17.4.1
- Fix parsing of `LinkTarget` during `GetListing()` on Unix FTP servers
- Improve logging clarity by removing "FluentFTP" prefix in TraceSource

#### 17.4.0
- Add `MoveFile()` and `MoveDirectory()` to move files and directories safely

#### 17.3.0
- Automatically verify checksum of a file after upload/download (thanks [jblacker](https://github.com/jblacker))
- Configurable error handling (abort/throw/ignore) for file transfers (thanks [jblacker](https://github.com/jblacker))
- Multiple log levels for tracing/logging debug output in `FtpTrace` (thanks [jblacker](https://github.com/jblacker))

#### 17.2.0
- Simplify `DeleteDirectory()` API - the `force` and `fastMode` args are no longer required
- `DeleteDirectory()` is faster since it uses one recursive file listing instead of many
- Remove .NET Standard 1.4 to improve nuget update reliability, since we need 1.6 anyway

#### 17.1.0
- Split stream API into `Upload()`/`UploadFile()` and `Download()`/`DownloadFile()`

#### 17.0.0
- Greatly improve performance of `FileExists()` and `GetNameListing()`
- Add new OS-specific directory listing parsers to `GetListing()` and `GetObjectInfo()`
- Support `GetObjectInfo()` even if machine listings are not supported by the server
- Add `existsMode` to `UploadFile()` and `UploadFiles()` allowing for skip/overwrite and append
- Remove all usages of string.Format to fix reliability issues caused with UTF filenames
- Fix issue of broken files when uploading/downloading through a proxy (thanks [Zoltan666](https://github.com/Zoltan666))
- `GetReply()` is now public so users of `OpenRead`/`OpenAppend`/`OpenWrite` can call it after

#### 16.5.0
- Add async/await support to all methods for .NET 4.5 and onwards (thanks [jblacker](https://github.com/jblacker))

#### 16.4.0
- Support for .NET Standard 1.4 added.

#### 16.2.5
- Add `UploadFiles()` and `DownloadFiles()` which is faster than single file transfers
- Allow disabling UTF mode using DisableUTF8 API

#### 16.2.4
- First .NET Core release (DNXCore5.0) using Visual Studio 2017 project and shared codebase.
- Support for .NET 2.0 also added with shims for LINQ commands needed.

#### 16.2.1
- Add `FtpListOption.IncludeSelfAndParent` to `GetListing`

#### 16.1.0
- Use streams during upload/download of files to improve performance with large files

#### 16.0.18
- Support for uploading/downloading to Streams and byte[] with `UploadFile()` and `DownloadFile()`

#### 16.0.17
- Added high-level `UploadFile()` and `DownloadFile()` API. Fixed some race conditions.

#### 16.0.14
- Added support for FTP proxies using HTTP 1.1 and User@Host modes. (thanks [L3Z4](https://github.com/L3Z4))

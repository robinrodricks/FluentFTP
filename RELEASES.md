# Release Notes

#### 50.0.1

 - Fix: Correct sequencing of FTP stream disposing for .NET Framework
 - Fix: Extraneous `GetReply` call in `UploadInternal`/`DownloadInternal`

#### 50.0.0

 - **File Verification** (thanks [DasRaschloch](/DasRaschloch) and [FanDjango](/FanDjango))
   - New: All file transfer methods now support size/checksum/date comparison
   - New: `DownloadDirectory`, `DownloadFile`, `DownloadFiles`, support new size/checksum/date comparison
   - New: `UploadDirectory`, `UploadFile`, `UploadFiles` support new size/checksum/date comparison
   - New: FXP `TransferDirectory`, `TransferFile` support new size/checksum/date comparison
   - New: `FtpVerify` has new options: `Size/Date/Checksum/OnlyChecksum`
 - **File Transfer** (thanks [FanDjango](/FanDjango))
   - Fix: `DownloadFile` with Progress throws `FtpException`
   - Fix: Correctly handle internal errors in `UploadInternal`/`DownloadInternal`
   - Fix: `GetFileSize` provides invalid file length and transfer fails
 - **FTP Proxy** (thanks [zhaohuiyingxue](/zhaohuiyingxue))
   - Fix: `GetListing` does not use the proxy with passive FTP
 - **FTP Disconnection** (thanks [FanDjango](/FanDjango))
   - New: Indicate creating/disposing sync/async connections in log
   - Fix: Disconnection during Connect async needs to be awaited
   - Fix: FTP `BaseClient` is also `IAsyncDisposable` in addition to `IDisposable`
   - Fix: Disposing a connected `AsyncFtpClient` throws non-fatal `InvalidCastException`
   - Fix: Cloning a `AsyncFtpClient` throws non-fatal `InvalidCastException`
   - Change: `AsyncFtpClient` to change all `Close` usage to `await CloseAsync`
   - Change: `FtpDataStream` now supports an async close method
   - Change: Use `DisposeAsync` pattern for `AsyncFtpClient`
   - Change: Call `Dispose`/`DisposeAsync` on `BufferedStream` instead of `Flush`/`FlushAsync`
   - Fix: Restore call to `FtpSslStream` for graceful TLS termination
 - **IBM OS/400** (thanks [FanDjango](/FanDjango))
   - Fix: Async server-specific post-connection commands was missing for OS400

#### 49.0.2

 - **NOOP Daemon** (thanks [FanDjango](/FanDjango))
   - Fix: Improve NOOP daemon logic
   - Fix: More reliable termination
   - Fix: Handle NOOP API exceptions
   - Fix: Handle NOOP situations in `Execute` API
   - Fix: No need for NOOP commands before a QUIT command
   - Fix: Improve `GetReply` logging and handling for stale data and NOOP reactions
   - Fix: If QUIT is stashed and reconnect is pending, defer it.
   - Fix: Recognize special commands & responses in all cases
   - Fix: Delay NOOP connectivity tests until connection fully established
 - **Connection Status** (thanks [FanDjango](/FanDjango))
   - Fix: `IsStillConnected`: Clean up `Connect`/`DisconnectInternal` interface artifacts
   - Fix: `IsStillConnected`: Add log messages
 - **IBM OS/400** (thanks [FanDjango](/FanDjango))
   - Fix: Enhance detection of IBM OS/400 servers
   - Fix: Set `SITE LISTFMT 1` and `SITE NAMEFMT 1` on connect
 - **File Transfer**
   - Fix: `DownloadFile`: `FileCount` Progress updated even when files are skipped (thanks [J0nathan550](/J0nathan550))
   - Fix: `DownloadFile`: `stopPosition` not working in some cases (thanks [alexgubanow](/alexgubanow))
   - Fix: `DownloadFile`: Progress calculation not correct when using `stopPosition` (thanks [alexgubanow](/alexgubanow))

#### 49.0.1

 - Fix: Change semaphore logic to prevent deadlock in NOOP Daemon (thanks [FanDjango](/FanDjango))

#### 49.0.0

 - **NOOP Daemon** (thanks [FanDjango](/FanDjango))
   - New: Revised NOOP handling with new config option `Noop` to enable NOOP daemon
   - New: New config options `NoopInactiveCommands` and `NoopActiveCommands` to set FTP commands usable
   - New: New config option `NoopTestConnectivity` to issue NOOP commands before every FTP command
   - Change: `NoopInterval` now has a default of 3 minutes
   - Change: Update `GetReply` for new NOOP handling logic
 - **Connection Status** (thanks [FanDjango](/FanDjango))
   - New: `IsStillConnected()` API to reliably check if FTP connection is still active
 - **Auto Connection** (mostly thanks [FanDjango](/FanDjango))
   - New: Treat timeouts during `AutoDetect` as failed detection instead of aborting (thanks [FabBadDog](/FabBadDog))
   - Fix: Prevent Auto-Reconnect occurring before connect is complete
   - Fix: `AutoDetect` is thread unsafe, fix `AutoDetectConfig` `IncludeImplicit` logic
 - **Multi-Threading** (thanks [FanDjango](/FanDjango))
   - Change: Remove all internal locking in the sync `FtpClient`
 - **FTP Transfers** (thanks [FanDjango](/FanDjango))
   - Fix: Servers with no server handler used wrong `GetListing()` command
   - Fix: `OpenRead` retry attempt fails due to typo
   - Fix: Missing code in async stale data handler
   - Fix: Missing code in Async `DisableUTF8` API
 - **FTP Proxies**
   - Fix: Correctly set supported method in SOCKS 5 proxy negotiation (thanks [rmja](/rmja))
 - **Codebase Maintainance** (mostly thanks [FanDjango](/FanDjango))
   - Fix: Remove `NET50_OR_LATER` invalid moniker (thanks [sean-bloch](/sean-bloch))
   - Fix: Cleanup and standardize all .NET Target framework markers (TFMs)
   - Fix: Clean up interfaces and implementation for Connect/Disconnect
 - **Testing** (thanks [FanDjango](/FanDjango))
   - Fix: Powershell folder was not being populated, add to GIT ignore list
   - Fix: Docker build process was failing due to debian py image changes
 - **Logging** (thanks [FanDjango](/FanDjango))
   - New: Log selected server handler, if any are detected
   - New: Log improvements for `OpenAppend`, `OpenRead` and `OpenWrite` to help in debugging

#### 48.0.3
 - **Utilities**
   - New: `FtpResult.ToStatus()` API to easily compare result values of `DownloadFile`/`DownloadFiles` and `UploadFile`/`UploadFiles`
 - **File Transfer** (thanks [FanDjango](/FanDjango))
   - Fix: Code cleanup for FTP path and directory handling
   - Fix: `CreateDirectory` sometimes needed in `AutoNavigate` mode
   - Fix: Optimize `CWD`/`PWD` directory navigation in `AutoNavigate` mode
   - Fix: Prevent infinite loop on stale data read when FTP socket stalled
   - Fix: Some FTP servers throw `450` error for empty folders

#### 48.0.1
 - **Directory Navigation** (thanks [FanDjango](/FanDjango))
   - New: Add auto-navigate support to `GetCheckSum`
   - Fix: `UploadDirectory` with `FtpNavigate.Conditional` does not auto-navigate correctly
   - Fix: Not all linux ftp servers support backslash as path separator character
   - Fix: Cancellation token passing and await syntax for `DownloadFile`, `UploadFile`, `GetListing`

#### 48.0.0
 - **Directory Navigation** 
   - New: `Navigate` Config setting to automatically handle FTP directory navigation
   - New: Download and Upload API honors `Navigate` setting
   - New: `GetListing` API honors `Navigate` setting
 - **File Transfer** (thanks [FanDjango](/FanDjango))
   - New: `DownloadUriBytes` API method to directly connect and download a URI/URL
   - Fix: `OpenRead`, `OpenWrite` and `OpenAppend` quirks to handle their stale data
   - Fix: Complete redesign of FTP socket stale data handling and `CheckStaleData` implementation
 - **Auto Connection** (thanks [FanDjango](/FanDjango))
   - New: Overloaded API `AutoDetect` with object-driven configuration using `FtpAutoDetectConfig`
   - New: Add options `RequireEncryption` and `IncludeImplicit` to `AutoDetect` to allow for more configurability during auto-connection
   - Fix: Improve `AutoDetect` behaviour to support various server use-cases
   - Fix: Add `RNFM`/`RNTO` FTP commands to critical-sequence list to fix Auto-Reconnect of SSL sessions
   - Fix: `AutoDetect` empty config is gracefully handled
 - **Logging**
   - New: Function Logging method to support logging objects
   - Fix: Logging strings creation conditional on it being at all in use (thanks [jnyrup](/jnyrup))
   - Fix: Improve logging of FTP socket stale data (thanks [FanDjango](/FanDjango))

#### 47.0.0
 - **Logging** (thanks [FanDjango](/FanDjango))
   - New: Add exact .NET platform build target during the version logging
 - **File Transfer**
   - New: Config API `LocalFileShareOption` to allow setting file sharing mode for uploads
   - New: Connection type `PASVUSE` aka `PassiveAllowUnroutable`
   - New: Add friendlier names for connection types `AutoActive` and `PassiveExtended`
 - **File Hashing** (thanks [FanDjango](/FanDjango))
   - Fix: Parse non-standard FTP hashes for BrickFTP, Files.com, ExaVault.com
 - **FTP Connections** (thanks [FanDjango](/FanDjango))
   - Fix: SSL Buffering: Improve connection logic, update comments, refactor code
   - Fix: SSL Buffering: Cannot connect to FTPS IIS server on Windows 2019 from Azure Functions V4
   - Fix: Disconnection: Improve conditional compiles and test for each target in `FtpSslStream`
   - Fix: Disconnection: Use `ShutDownAsync ` for .NET 4.7 and later
   - Fix: `InnerException` does not get caught during FTPS security exception
   - Fix: Remove dead code in SSL permanent failure detection
   - Fix: Custom Stream: `PolicyErrors` not being set correctly
 - **File Listing** (thanks [FanDjango](/FanDjango))
   - Fix: Improve file name parsing logic for DOS/Windows/IIS servers
   - Fix: Improved null checks for `InfoMessages` (thanks [jnyrup](/jnyrup))
 - **Testing** (thanks [FanDjango](/FanDjango))
   - Fix: `GnuTlsStream` integration tests due to invalid stream detection

#### 46.0.2
 - Fix: Custom stream logging tweak: Message first then close stream
 - Fix: Custom stream: Also log `InnerException` if it exists within the exception
 - Fix: Internal stream null check to avoid exception in `Execute` API methods

#### 46.0.1
 - Fix: Hotfix to remove new `DowloadStream` overload that causes compile failure

#### 46.0.0
 - New: Add `stopPosition` parameter to `DownloadBytes` and `DowloadStream` to allow partial downloads

#### 45.2.0
 - New: Add 9 missing properties to the FTP client interfaces
 - New: Improve log message wording for EPSV & proxies (thanks [FanDjango](/FanDjango))
 - Fix: Improve GetReply to handle connection loss edge cases and timeout exceptions, possibly incurring cpu-loops (thanks [FanDjango](/FanDjango))
 - Fix: Improve NOOP behavior to correctly handle timeout exceptions (thanks [FanDjango](/FanDjango))

#### 45.1.0
 - New: `DiscoverSslSessionLength` API to auto compute a working value for SSL Session length (thanks [FanDjango](/FanDjango))

#### 45.0.4
 - API: Rename `ExecuteGetText` to `ExecuteDownloadText`
 - Fix: `AsyncFtpClient.CreateDirectory` fails on freshly created client instance (thanks [FanDjango](/FanDjango))

#### 45.0.2
 - New: `ExecuteGetText` API to execute an FTP command and return multiline output (thanks [FanDjango](/FanDjango))
 - New: Integration with `FluentFTP.GnuTLS` NuGet package to allow for GnuTLS TLS 1.3 streams

#### 44.0.1
 - **File Transfer**
   - New: `UploadFiles` API in `AsyncFtpClient` which takes an `IEnumerable<FileInfo>`
   - New: `UploadFiles` and `DownloadFiles` now support rules which allow filtering of uploaded/downloaded files
   - New: `UploadFiles` and `DownloadFiles` now return a `List<FtpResult>` with per-file status rather than just a count
   - New: `FtpMissingObjectException` thrown when trying to download a non-existant object
   - New: Download API `DownloadDirectory`, `DownloadFile`, `DownloadBytes`, `DownloadStream` will throw `FtpMissingObjectException` rather than silently failing
   - New: Download API `DownloadFiles` will mark non-existant files as `IsFailed` and add the `Exception` rather than silently failing
   - Tests: New integration tests to check fail conditions of Download API
   - Fix: Correctly detect non-existant files and folders on FileZillla server (thanks [FanDjango](/FanDjango))
 - **Connection**
   - New: Improve reconnect logic to restore working directory and ASCII/Binary data type on automatic reconnection (thanks [FanDjango](/FanDjango))
   - New: Improve `Execute` logic to handle working directory on automatic reconnection (thanks [FanDjango](/FanDjango))
   - New: Do not attempt Reconnect if we have never been connected before (thanks [FanDjango](/FanDjango))
   - Change: Reconnect logging messages elevated from `Info` to `Warn` (thanks [FanDjango](/FanDjango))
   - Fix: Use `ConnectAsync` for `net472` platform where required (thanks [jnyrup](/jnyrup))
 - **Exceptions**
   - Change: Move all exception types into the `FluentFTP.Exceptions` namespace
   - New: Separate the log message from the exception in the handler (thanks [jnyrup](/jnyrup))
   - New: Add support for printing exception messages on a newline for socket exceptions (thanks [FanDjango](/FanDjango))
 - **Logging**
   - New: Setting `Config.LogDurations` to configure if durations are to be logged
   - New: Add FTP command roundtrip duration to every `Response` log message (thanks [FanDjango](/FanDjango))
   - New: Smart rendering of log message durations (hours, minutes, seconds, MS)
   - Fix: Improve exception handling for connection/disconnection and authentication (thanks [FanDjango](/FanDjango))
   - Fix: Simplify exception handling using `when` keyword and new conditional keywords (thanks [jnyrup](/jnyrup))
 - **Quality**
   - Fix: Reduce library warnings by improving code patterns used (thanks [FanDjango](/FanDjango))
   - Fix: Reduce test warnings by improving code patterns used (thanks [FanDjango](/FanDjango))

#### 43.0.0
 - Please read the [Migration Guide](https://github.com/robinrodricks/FluentFTP/wiki/v40-Migration-Guide#logging) for help migrating to the new version!
 - **Packaging** (thanks [jnyrup](/jnyrup))
   - Core FluentFTP package has removed the dependency on MELA (Microsoft.Extensions.Logging.Abstractions)
   - New [FluentFTP.Logging](https://www.nuget.org/packages/FluentFTP.Logging) package released that integrates with MELA
 - **Logging** (thanks [jnyrup](/jnyrup))
   - `FtpClient.Logger` is no longer a MELA `ILogger`
   - `FtpClient.Logger` is now a custom interface called `IFtpLogger`

#### 42.2.0
 - **Connection** (thanks [FanDjango](/FanDjango))
   - New: Save bandwidth on automatic reconnection by skipping `FEAT` command
   - Fix: Implementation of connection/disconnection internal logic
   - Fix: Create a default `ValidateCertificate` handler if none is provided
   - Fix: Auto-reconnect SSL streams after a set number of replies are read
 - **File Transfer** (thanks [FanDjango](/FanDjango))
   - New: Upload: Ability to upload file streams with unknown size
   - Fix: Upload: Timeout detection for file streams with unknown size
 - **File Listings** (thanks [FanDjango](/FanDjango))
   - New: `SetModifiedTime` falls back to `MDTM` if `MFMT` command not available
   - New: `GetListing`: Catch control connection loss and retry once
   - Fix: `GetListing` silently fails and returns empty array if connection lost
   - Fix: IBM OS/400: Correctly handle special chars on EBCDIC code page fault
 - **Tests** (thanks [FanDjango](/FanDjango))
   - New: Docker: Add optional path to allow the user to save disk space
   - Fix: Fix many XML compiler warnings in the testing system

#### 42.1.0
 - **FTP** (thanks [FanDjango](/FanDjango))
   - New: Detect Apache FTP Server (allows for future server-specific handling)
   - New: Major improvements to automatic FTP reconnection on connection loss
   - New: Special handling to prevent automatic FTP reconnection during critical FTP sequences
   - New: `Config.SslSessionLength` setting to perform automatic reconnection to bypass SSL issues
   - Fix: Connect closing has been removed from `ReadStaleData` and moved into `Execute`
   - Fix: Improved NOOP handling by detecting more formats of NOOP FTP replies
   - Fix: `IOException` edge case on FTPS connections after a certain number of FTP commands
   - Fix: Handle early `226 Transfer complete` edge case in FTP file download
   - Fix: Honor `FtpRemoteExists.NoCheck` mode in `MoveFile` to prevent checking for existing files
 - **Tests** (thanks [FanDjango](/FanDjango))
   - New: Improved Docker build process using common images to speed up build times
   - New: Rewrite all first-party Docker images to use pre-built common images
   - New: Support for Apache FTP Server integration test server
   
#### 42.0.2
 - **FTP** (thanks [FanDjango](/FanDjango))
   - New: DNS Caching to prevent DNS server rejecting name resolution for rapidly repeating requests
   - New: Better log message for stream dispose to indicate which stream was disposed
   - Fix: Typo in `FtpException` thrown when creating directories
   - Fix: Do not assume the server path when `CWD` command sent
   - Change: Refactor post-Execute operations and implement parity in sync/async API
 - **Connection** (thanks [FanDjango](/FanDjango))
   - Fix: `AutoConnect` fails with Azure FTP servers due to profile handling
   - Change: Improve exception throwing order for `InvalidOperationException` if unable to connect
   - Change: Complete rewrite of `Connect` API 
   - New: Add check to ensure that the IP version is permitted when connecting to servers
   - Fix: Implement retry logic to check all possibly server addresses before failing with an exception
   - Fix: Implement improved logic to detect timeouts and socket failures
   - Fix: `ConnectTimeout` is not taking effect for `ConnectAsync` API
   - Fix: Reset `CurrentDataType` when re-connected to an FTP server
 - **File Transfer** (thanks [FanDjango](/FanDjango))
   - Fix: Parity in resume logic for upload and download and throw `AggregateException` where required
   - New: Implement the missing `ResumeUpload` for synchronous API
   - New: Add a log message so that the resume operation is actually noticed by users

#### 42.0.1
 - **FTP**
   - New: TLS authentication failures will always throw `AuthenticationException` (thanks [FanDjango](/FanDjango))
   - Fix: Improve handling of stale data on socket after `GetListing` (thanks [FanDjango](/FanDjango))
 - **Tests**
   - New: Redesigned Pureftpd integration test server (thanks [FanDjango](/FanDjango))
 - **Proxies**
   - Fix: Read extra bytes to fix `GetListing` for SOCKS4 and SOCKS4a proxies (thanks [FanDjango](/FanDjango))

#### 42.0.0
 - Please read the [Migration Guide](https://github.com/robinrodricks/FluentFTP/wiki/v40-Migration-Guide) for help migrating to the new version!
 - **API**
   - New: `LastReplies` property to fetch a list of the last 5 server replies (thanks [FanDjango](/FanDjango))
   - Removed: `Config.DisconnectWithShutdown` as it was not required (thanks [FanDjango](/FanDjango))
   - Removed: `FtpListOption.NoImage` as it was not required (thanks [FanDjango](/FanDjango))
   - Removed: Privatize `CurrentDataType` and remove `ForceSetDataType` (thanks [FanDjango](/FanDjango))
 - **FTP**
   - New: SslStream wrapper to send TLS close notifications for .NET and .NET Core (thanks [FanDjango](/FanDjango))
   - Change: SSL Closure Alert is now always sent when a stream is terminated (thanks [FanDjango](/FanDjango))
   - Change: Make SSL Shutdown independant of `Config.DisconnectWithShutdown` (thanks [FanDjango](/FanDjango))
   - Change: `GetReply` redesign: New mode to exhaustively read all `NOOP` replies (thanks [FanDjango](/FanDjango))
 - **Logging**
   - New: Verbose file sizes logged during file upload/download (thanks [FanDjango](/FanDjango))
   - New: FluentFTP version logged after connection (thanks [FanDjango](/FanDjango))
   - New: `GetReply` redesign: Verbose logging for `NOOP` commands (thanks [FanDjango](/FanDjango))
 - **Z/OS**
   - New: IBM OS/400: Support blanks in filename and add unit test cases (thanks [FanDjango](/FanDjango))
 - **Tests**
   - New: Support for Bftpd integration test server (thanks [FanDjango](/FanDjango))
   - New: Support for ProFTPD integration test server (thanks [FanDjango](/FanDjango))
   - New: Support for glFTPd integration test server (thanks [FanDjango](/FanDjango))
   - New: Support for FileZilla integration test server (thanks [FanDjango](/FanDjango))
   - New: Redesigned VsFTPd integration test server (thanks [FanDjango](/FanDjango))
   - New: Ability to run test server containers as FTP or FTPS servers (thanks [FanDjango](/FanDjango))
   - Fix: Cleanup and improve all Dockerfiles and significantly reduce image size (thanks [FanDjango](/FanDjango))

#### 41.0.0
 - Please read the [Migration Guide](https://github.com/robinrodricks/FluentFTP/wiki/v40-Migration-Guide) for help migrating to the new version!
 - **API**
   - New: `EmptyDirectory` API to delete files but leave top-level directory intact (thanks [FanDjango](/FanDjango))
 - **FTPS**
   - Fix: Disable TLS 1.3 as it causes many complex networking issues during data transfer
   - Fix: Unified system to handle permanent failures during `AutoConnect`
   - Fix: Throw `FtpProtocolUnsupportedException` if the FTP server is forcing TLS 1.3 connections
   - Fix: Disable SSL Buffering on control connection to improve `NOOP` handling (thanks [FanDjango](/FanDjango))
   - Fix: Send an additional `NOOP` command after uploading files to resolve issues (thanks [FanDjango](/FanDjango))
 - **FTP**
   - Fix: Log messages pertaining to stale data are improved (thanks [FanDjango](/FanDjango))
   - New: Log the TLS protocol used after making a successful FTPS connection (thanks [FanDjango](/FanDjango))
   - Fix: Correctly forward `CancellationToken` within `DownloadFile`, `UploadFile`, `TransferDirectory`, `DeleteFile`, `OpenRead`, `OpenAppend` (thanks [jnyrup](/jnyrup))
   - Fix: Optimize `SIZE` command usage for `UploadFile` in `NoCheck` and `OverWrite` modes (thanks [FanDjango](/FanDjango))
   - Fix: Allow reusing the `ActivePorts` in FTP Active connection mode (thanks [FanDjango](/FanDjango))
 - **Z/OS**
   - New: Add `LIST` functionality for z/OS JES subsystem (thanks [FanDjango](/FanDjango))
   - New: Switch to using `LISTLEVEL 2` in `GetListing` for more accurate filesizes (thanks [FanDjango](/FanDjango))
   - New: Improve unit test cases for the z/OS file listing parser tests (thanks [FanDjango](/FanDjango))
 - **Proxies**
   - New: Support multiple modes of authentication for SOCKS proxies: GSSAPI, UsernamePassword (thanks [jnyrup](/jnyrup))
   - New: Throw `MissingMethodException` if cannot negotiate an authentication method for SOCKS proxies (thanks [jnyrup](/jnyrup))

#### 40.0.0
 - Please read the [Migration Guide](https://github.com/robinrodricks/FluentFTP/wiki/v40-Migration-Guide) for help migrating to the new version!
 - Special thanks to Robin Rodricks, Michael Stiemke and Jonas Nyrup for this release!
 - **Constructor API**
   - New: 4 new FTP client constructors that accept FTP host, credentials, config and logger
   - Remove extraneous constructors because properties can be used instead
 - **Asynchronous API**
   - New: Split main FTP client classes into `FtpClient` and `AsyncFtpClient`
   - New: Split main FTP client interfaces into `IFtpClient` and `IAsyncFtpClient`
   - New: Split common FTP functionality into `BaseFtpClient`
   - New: Drop `Async` suffix for all async FTP methods in `AsyncFtpClient`
 - **Config API**
   - New: Remove all config settings from FtpClient and move it into `client.Config` object
   - New: Dedicated class to hold config settings `FtpConfig` to cleanup client API
 - **Logging API**
   - New: Remove `client.OnLogEvent` and `FtpTrace` system
   - New: Add logger system `client.Logger` using industry-standard `ILogger` interface
   - New: Add Nuget dependency `Microsoft.Extensions.Logging.Abstractions` v2.1.0
   - Renamed: Legacy logging callback `OnLogEvent` is now renamed to `LegacyLogger`
   - Renamed: Logging settings: `LogIP` renamed to `LogHost`
   - Remove logging setting `LogFunctions` as it is always enabled
 - **FTP Proxies**
   - New: Split FTP proxy classes into `FtpClient*Proxy` and `AsyncFtpClient*Proxy`
   - New: FTP proxy classes moved into `FluentFTP.Proxy.SyncProxy` and `FluentFTP.Proxy.AsyncProxy` NS
   - New: FTP proxy classes with fully async implementations
   - Fix: Properly override `HandshakeAsync` in async FTP proxies (thanks [jnyrup](/jnyrup))
 - **Organization**
   - Completely redesign the FTP client code organization and structure
   - Update all tests and examples to use the new API and `AsyncFtpClient`
   - Hide all internally-used functions behind the interface `IInternalFtpClient`
   - Code style standardization and use new C# language constructs (thanks [jnyrup](/jnyrup))
   - Add styling rules to `.editorconfig` to prevent using IDE defaults (thanks [jnyrup](/jnyrup))
 - **Modernization**
   - Drop support for .NET Standard 1.2, 1.4 and .NET 2.0, 3.5, 4.0 and 4.5
   - Add support for .NET 4.6.2 and 4.7.2
   - Remove conditional compilation statements for unsupported platforms
   - Remove uncommon static methods `FtpClient.Connect` and `FtpClient.GetPublicIP`
   - Remove uncommon method `DereferenceLink` and `DereferenceLinkAsync`
   - Remove uncommon properties `QuickTransferLimit`, `MaximumDereferenceCount`, `EnableThreadSafeDataConnections`, `PlainTextEncryption`
   - Remove uncommon feature `FtpListOption.DerefLinks`
   - Remove obsolete hashing commands `GetHashAlgorithm`, `SetHashAlgorithm`, `GetHash`, etc
   - Remove obsolete async pattern using `IAsyncResult`
   - Fix: Forward cancellation token in UploadDirectory and Proxy HandshakeAsync (thanks [jnyrup](/jnyrup))
   - Fix: Parity in sync/async implementations of `Authenticate` (thanks [FanDjango](/FanDjango))
   - Fix: Improve masking out support for removing sensitive usernames from FTP logs
   - Fix: Change all public fields to properties in classes: `FtpListParser`, `FtpClientState`, `FtpFxpSession`, `FtpFxpSessionAsync`, `FtpListItem`, `FtpProfile`, `FtpResult`
   - Fix: Change all public fields to properties in rules: `FtpFileExtensionRule`, `FtpFileNameRegexRule`, `FtpFileNameRule`, `FtpFolderNameRegexRule`, `FtpFolderNameRule`, `FtpSizeRule`
 - **Server support**
   - Move all IBM zOS logic into the `IBMzOSFtpServer` server handler (thanks [FanDjango](/FanDjango))
   - Move all OpenVMS logic into the `OpenVmsServer` server handler (thanks [FanDjango](/FanDjango))
   - Fix: z/OS: Handle z/OS `GetListing` single line outputs (thanks [FanDjango](/FanDjango))

#### 39.4.0
 - New: Add `SslProtocolActive` property to retrieve the negotiated SSL/TLS protocol version
 - Fix: z/OS: Improve server handling for absolute path calculation (thanks [FanDjango](/FanDjango))
 - Fix: z/OS: Remove direct z/OS dependancy in `GetListing` (thanks [FanDjango](/FanDjango))
 - Fix: z/OS: Add some special cases to handle conversion of remote FTP paths (thanks [FanDjango](/FanDjango))
 - Fix: z/OS: Add missing parser tests for z/OS FTP server (thanks [FanDjango](/FanDjango))
 - Fix: z/OS: Fix file size calculation for non-unix z/OS files (thanks [FanDjango](/FanDjango))
 - Internal: Add integration tests for `GetListingAsyncEnumerable` (thanks [tommysor](/tommysor))
 - Internal: Remove 3 test dockers that do not work (thanks [tommysor](/tommysor))

#### 39.3.0
 - New: Detect D-Link, TP-LINK, Huawei, MicroTik FTP servers (allows for future server-specific handling)
 - Fix: `AutoConnect` now correctly handles internal `AggregateException` (.NET 5.0+)
 - Fix: `AutoConnect` now correctly connects to servers requiring TLS 1.2
 - Internal: Support automated tests for VsFtpD servers (thanks [tommysor](/tommysor))
 - Internal: Add tests for custom FTP server auto connection

#### 39.2.1
 - New: Add 5 missing methods into the `IFtpClient` interface
 - Fix: z/OS: Inconsistent implementations of `GetListing` absolute path conversion

#### 39.2.0
 - New: `GetListingAsyncEnumerable` method to get file listing using `IAsyncEnumerable` pattern
 - New: During `AutoConnect`, throw `FtpProtocolUnsupportedException` if newer .NET required
 - Fix: Support connecting to TLS 1.3 servers using `AutoConnect` (.NET 5.0+)
 - Fix: Support automated tests for PureFTP and ProFTP servers (thanks [tommysor](/tommysor))

#### 39.1.0
 - New: `Clone` method allows for cloning an `FtpClient` connection with all settings copied
 - New: `InternetProtocol` property which returns the current protocol used (IPV4/IPV6)
 - New: `Status` property which returns the current `FtpClientState` flags (advanced use only)
 - Fix: `AutoConnect`/`AutoConnectAsync` now correctly configure Azure FTP servers
 - Internal: Add integration test system using docker to test FluentFTP against many supported FTP servers
 - Internal: Refactor server specific, server strings, cloning and auto connection logic into modules

#### 39.0.1
 - Fix: `ConnectAsync` correctly honors `ConnectTimeout` and `TimeoutException` is correctly thrown
 - Internal: Add unit tests for `Connect`/`ConnectAsync` to ensure `ConnectTimeout` is honored
 - Internal: Refactor internal file listing handlers & proxy implementation (API is unchanged)

#### 39.0.0
 - New: Username/password authentication for SOCKS5 proxy connections
 - New: Correctly fill in `ConnectionType` for all proxy types
 - New: Improved pattern to connect to proxy servers, all details can be specified in `FtpProxyProfile`
 - New: Examples for all types of proxies (`ConnectProxyHttp11`, `ConnectProxySocks4`, `ConnectProxySocks4a`, `ConnectProxySocks5`)
 - Change: Rename class `SocksProxyException` to `FtpProxyException`
 - Change: Rename class `ProxyInfo` to `FtpProxyProfile` and rename members

#### 38.0.0
 - Change: Rename enum `FtpFileSystemObjectType` to `FtpObjectType`
 - Change: Rename enum `FtpFileSystemObjectSubType` to `FtpObjectSubType`
 - Change: Rename API `Upload` to `UploadBytes` and `UploadStream` instead of overloading
 - Change: Rename API `Download` to `DownloadBytes` and `DownloadStream` instead of overloading
 - Change: Logging will always mask host IP, username and password by default (configurable)
 - New: Throw `AggregateException` when an error occuring during resuming a file upload (.NET 4.5+)
 - New: Code generation for parsed file item in order to build accurate unit tests
 - New: Unit tests for Windows, Unix, OpenVMS, NonStop, IBM, Machine listing parsers
 - New: Unit tests for Timezone conversion to UTC and to local time
 - Fix: Improvement to directory parsing for Windows, Unix, OpenVMS, NonStop, Machine listings
 - Fix: Support parsing of links and Unix-symlinks in Machine listings
 - Fix: Enable 2 FTP server specific handlers

#### 37.1.2
 - Fix: Error when downloading large files through SOCKS4 or SOCKS4a proxy (thanks [fire-lizard](/fire-lizard))

#### 37.1.1
 - Fix: Disable SSL Buffering in .NET 6 as it was in .NET 5

#### 37.1.0
 - Major: Added support for SOCKS4 proxy servers using the `FtpClientSocks4Proxy` client class (thanks [fire-lizard](/fire-lizard))
 - Major: Added support for SOCKS4a proxy servers using the `FtpClientSocks4aProxy` client class (thanks [fire-lizard](/fire-lizard))

#### 37.0.6
 - New: Detect Rumpus FTP servers for Mac (allows for future server-specific handling)
 - New: Detect ABB IDAL FTP servers (allows for future server-specific handling)
 - New: Add `DisconnectWithShutdown` property to configure shutdown signal on disconnect
 - Change: Rename `UngracefullDisconnect` to `DisconnectWithQuit`
 - Fix: Resolve long timeouts after socket stream disconnected

#### 37.0.5
 - Fix: IOException in synchronous methods when AUTH TLS is rejected (thanks [yatlor](/yatlor))

#### 37.0.4
 - New: Support for .NET 6.0
 - Fix: For FileZilla FTP Server, TLS socket would be incorrectly closed (thanks [michael-hoedl](/michael-hoedl))
 - Fix: For Windows Server IIS, the space at the beginning of the file name is excluded (thanks [tYoshiyuki](/tYoshiyuki))

#### 37.0.3
 - Change: Remove `Obsolete` from all `OpenRead`/`OpenWrite`/`OpenAppend` API as it is not planned for deletion
 - Fix: Calculate checksum for files with whitespaces in their name (thanks [simonefil](/simonefil))

#### 37.0.2
 - Fix: z/OS: `GetListing`: Handle large file sizes reported in Used column (thanks [FanDjango](/FanDjango))
 - Fix: z/OS: `IsRoot` improved to handle Unix realm as well (thanks [FanDjango](/FanDjango))
 - Fix: `DownloadFile` correctly handles connection interruptions and resumes partially downloaded files (thanks [FanDjango](/FanDjango))

#### 37.0.1
 - Fix: `SetWorkingDirectoryAsync` doesn't set working directory due to missing cache invalidation (thanks [FanDjango](/FanDjango))
 - Fix:  Handle multiline `FEAT` replies to support ProFTPD capability reporting (thanks [FanDjango](/FanDjango))
 - Fix: Correctly disposes `CancellationTokenSource` created during `ConnectAsync` (thanks [jnyrup](/jnyrup))
 - Fix: Printing of error messages in some `ArgumentNullException` and `ArgumentOutOfRangeException` (thanks [jnyrup](/jnyrup))
 - Fix: Wrong file parser being selected for servers: NonStop/Tandem, OpenVMS, Windows CE, IIS (thanks [jnyrup](/jnyrup))
 - Performance: Improve performance of async methods that return constant values (thanks [jnyrup](/jnyrup))

#### 37.0.0
 - New: Detect Titan FTP servers (allows for future server-specific handling)
 - Fix: Validation of short CRC checksum fails due to mismatch of hex hash format
 - Change: Remove redundant and extranous `OpenRead`/`OpenWrite`/`OpenAppend` API and keep only 2 methods each
 - Change: Mark `OpenRead`/`OpenWrite`/`OpenAppend` API as obsolete with warnings and recommend high level API
 - Change: Cleanup dependencies for netstandard2.0, netstandard2.1 and net5.0 targets (thanks [jnyrup](/jnyrup))

#### 36.1.0
 - Change: Restore the older `OpenRead` API to prevent breaking older projects that depend on it (thanks [FanDjango](/FanDjango))

#### 36.0.0
 - New: `GetZOSFileSize` is now removed and superceeded by `GetFileSize` which handles z/OS servers
 - New: Refactor and cleanup z/OS specific logic for: post-connect init, `IsRoot`, `GetFileSize`
 - Change: `OpenRead` API no longer supports `checkIfFileExists` argument (thanks [FanDjango](/FanDjango))
 - Fix: z/OS & ASCII transfers: Don't get filesize if filesize already known during downloading (thanks [FanDjango](/FanDjango))
 - Fix: z/OS `UploadFilesAsync`: Fixed IBM MVS File uploading path calculation (thanks [FanDjango](/FanDjango))
 - Fix: Close underlying FTP socket connection on async cancellation on .NET Core (thanks [datvm](/datvm))
 - Fix: Correctly handle FTP connection timeout on .NET Core (thanks [datvm](/datvm))
 - Fix: Enable detection of IBM OS/400 servers that were disabled during server-specific handling

#### 35.2.3
 - New: Detect PyFtpdLib FTP servers (allows for future server-specific handling)
 - Fix: Pass CancellationToken to all methods that support cancellation (thanks [0xced](/0xced))
 - Fix: Error check on z/OS init commands to ensure they executed correctly (thanks [FanDjango](/FanDjango))
 - Fix: Improve `FileExists` for z/OS: better no SIZE, no MDTM on non HFS files (thanks [FanDjango](/FanDjango))
 - Fix: `DownloadFile` for z/OS: `SetDataType` directly before the `RETR` command (thanks [FanDjango](/FanDjango))
 - Fix: `IsAuthenticated` is not updated when calling `ConnectAsync` (thanks [datvm](/datvm))
 - Fix: Reduce number of times `SetDataType` is called internally to improve performance (thanks [FanDjango](/FanDjango))
 - Fix: Fail to detect z/OS server if `Unix` is also mentioned in welcome message (thanks [FanDjango](/FanDjango))

#### 35.2.2
 - Fix: z/OS GetFileSize: Ignore SIZE capability even if advertised by server as pointless (thanks [FanDjango](/FanDjango))
 - Fix: z/OS DownloadFile: Read to end of stream because filesize is always inaccurate (thanks [FanDjango](/FanDjango))
 - Fix: z/OS DownloadFile: Fix check for infinity or NaN progress values (thanks [FanDjango](/FanDjango))

#### 35.2.1
 - Fix: z/OS GetListing: Path can be null causing an exception (thanks [FanDjango](/FanDjango))
 - Fix: z/OS GetListing: Large files overflow on size calculation resulting in negative file sizes (thanks [FanDjango](/FanDjango))
 - Fix: z/OS GetListing: Listing fails unless users `CWD` to the correct folder of a non-`RECFM=U` PDS (thanks [FanDjango](/FanDjango))
 
#### 35.2.0
 - New: Support for connecting to FTP/FTPS servers via a SOCKS5 proxy (thanks [bjth](/bjth))
 - New: Autoconfigure IBM z/OS FTP server using `SITE DATASETMODE` and `QUOTESOVERRIDE` (thanks [FanDjango](/FanDjango))
 - Fix: `GetListing` item `Fullname` is now correctly calculated for Unix and z/OS systems (thanks [FanDjango](/FanDjango))

#### 35.1.0
 - New: `GetZOSFileSize` APIs to get file size of IBM z/OS file system objects (thanks [FanDjango](/FanDjango))
 - New: `GetZOSListRealm` APIs to get realm of IBM z/OS servers (thanks [FanDjango](/FanDjango))
 - New: Enhance the z/OS listing parser to get LRECL (via XDSS) on behalf of user (thanks [FanDjango](/FanDjango))
 - Fix: `AutoConnect` detects rejected certificates on connection and raises `FtpInvalidCertificateException` (thanks [FanDjango](/FanDjango))
 - Fix: `FtpListOption.ForceList` is not being honored by GetListing and machine listings are used instead (thanks [FanDjango](/FanDjango))
 - Fix: `GetListing` regression causing many untrue parse fail warnings (thanks [FanDjango](/FanDjango))
 - Fix: `GetObjectInfo` is overwriting Modified date of a `FtpListItem` if the consecutive MDTM command fails (thanks [Dylan-DutchAndBold](/Dylan-DutchAndBold))
 - Fix: reusing same FtpClient should reload server capabilities unless its a cloned connection (thanks [FanDjango](/FanDjango))
 - Fix: Executing `CWD` using `Execute` API does not invalidate internal CWD cache (thanks [FanDjango](/FanDjango))

#### 35.0.5
 - Fix: `UploadFile` fails to upload in `FtpRemoteExists.Resume` mode even if stream is seekable

#### 35.0.4
 - Fix: `AutoConnect` loads the newly detected `FtpProfile` to update properties & encryption
 - Fix: Passive connections work in `FtpEncryptionMode.Auto` mode and FTPS connection fails

#### 35.0.3
 - Fix: `AutoDetect` correctly recommends `FtpEncryptionMode.None` if FTPS connection failed
 - Fix: `AutoDetect` crashes because attempting to read socket type after disconnected

#### 35.0.2
 - Major: `AutoDetect` does not cycle through data connection types during connection as it is irrelevant
 - Major: `AutoDetect` calculates a data connection type after connection succeeds (EPSV or PASV)

#### 35.0.1
 - Fix: `ConnectAsync` now correctly creates a FTP server-specific handler to match `Connect` behaviour

#### 35.0.0
 - **Automatic connection**
   - Major: `AutoConnect` takes far fewer connection attempts due to improvements in connection handling
   - Major: `AutoConnect` and `AutoDetect` are much faster and smarter and only try each setting once if possible
   - Major: `AutoDetect` only tries Explicit and Implicit FTPS once and then falls back to plaintext FTP
   - Major: `AutoDetect` only tries UTF-8 and never ASCII because most UTF-8 servers don't advertise it
   - New: `AutoDetect` verifies if the server supports UTF-8 and updates the `FtpProfile` accordingly
   - New: `FtpProfile` code generation adds a warning message if the encoding mode is unverified
   - New: `AutoConnectAsync` now uses true asynchronous connection backed by new `AutoDetectAsync`
   - New: `AutoConnect` now auto computes an FTP port unless a non-standard port is already set
   - New: `AutoConnect` uses the main `FtpClient` connection rather than creating one clone per attempt
   - Fix: `AutoConnect` remains connected to the first working profile rather than connecting twice on success
   - Fix: `AutoConnect` reuses the same connection for FTPS and FTP rather than connecting again
   - Fix: Ensure FTP server capabilities are loaded during `AutoDetect` if original connection is blank
   - Fix: `AutoConnect` and `AutoDetect` will now throw exceptions for permanent failures (bad host/credentials)
   - Fix: `ConnectAsync` now correctly resets the state flags to match `Connect` behaviour
   - Fix: `Port` now correctly calculates the default port 21 when using `FtpEncryptionMode.Auto`
 
 - **Appending and resuming uploads**
   - Major: The setting `FtpLocalExists.Append` is now renamed to `FtpLocalExists.Resume`
   - Major: The setting `FtpRemoteExists.Append` is now renamed to `FtpRemoteExists.Resume`
   - Major: Split `FtpRemoteExists.Append` into two properties with distinct behaviour (`Resume` and `AddToEnd`)
   - Major: Improvements to `UploadFile` and `UploadFileAsync` to support appending and resuming of uploads
   - Major: `UploadFile` always sets the length of the remote file stream before uploading, appending or resuming
   - Major: `UploadFile` skips uploading in `Resume` mode if local and remote file are equal length
   - Fix: Implementation for resuming uploads using `UploadFile` based on fixes in `UploadFileAsync`
 
 - **Machine listings**
   - Major: `GetListing` prefers using Machine Listings over LIST command, unless a custom list parser is set
   - Fix: `ListingParser` property is updated according to auto-detected parser during `Connect` and `ConnectAsync`
   - Fix: `DeleteDirectory` and `DereferenceLink` methods no longer use `ForceList` and so prefer using Machine Listings
   
 - **File hashing**
   - Major: All low-level hash methods are now inaccessible and `GetChecksum` is the only recommended approach
   - Fix: `GetChecksum` now prints function call logs and sanitizes the input path
   - New: `GetChecksum` switches to the first preferred hash algorithm for `HASH` command if no algorithm is specified
   - New: `GetChecksum` validates if the required algorithm is unsupported and throws `FtpHashUnsupportedException`
   - New: `GetChecksum` validates if hashing is unsupported by the server and throws `FtpHashUnsupportedException`
   - Fix: `GetChecksumAsync` now takes the cancellation token last to follow conventions (argument reorder)
   - Fix: Improved extraction of hash checksum when using the HASH command
   - Fix: Improved extraction of hash checksum when using the MD5, SHA1, SHA256, SHA512 or X-series commands
   - New: `SetHashAlgorithm` now only modifies the hash algorithm if it has changed
 
 - **Path sanitization**
   - Fix: All high level API methods sanitize input paths to improve robustness
   - Fix: `GetWorkingDirectory` always sanitizes the returned working path directory
   - Fix: Correctly handle server-specific absolute FTP paths for async operations
   - Fix: All function call logs now print the sanitized path rather than raw input path
 
 - **Path improvements**
   - Major: `GetWorkingDirectory` is now extremely fast and caches the working dir path for subsequent calls
   - Fix: `FileExists` supports checking name listings for Windows NT servers which use invalid slashes
   - Fix: Root directory FTP paths no longer return `./` and instead return `/`
 
 - **Other improvements**
   - Major: All legacy asynchronous methods using `IAsyncResult` pattern have been removed (outdated since 2012)
   - Fix: FXP file transfers for glFTPd server always try PASV and CPSV commands to get passive port
   - Fix: Add logging for skipped files in `UploadFile`
   - Fix: Add file path details in skipped files logged by `UploadFile` and `DownloadFile`
   - New: `GetNameListing` to print results of name listing as verbose logs, similar to `GetListing`
   - New: `GetFileSize` and `GetFileSizeAsync` to support a configurable return value if the file does not exist
   - New: `FtpFolderNameRule` now supports `startSegment` to skip checking root directory folder names
   - New: `FtpFolderNameRegexRule` now supports `startSegment` to skip checking root directory folder names

#### 34.0.2
 - New: Add support for `IsAuthenticated` property which enables detection of FTP connection and authentication
 - Fix: Improved file path calculation when uploading files to IBM zOS running MVS (thanks [arafuls](/arafuls))
 - Fix: Detection of file exists for Windows NT Servers fails due to invalid slashes
 - Fix: Ensure `CompletionCode` is always null-checked to fix edge cases and comply with existing implementation
 - Fix: Possible wrong IP when connecting to Azure FTP Server using `EPSV` command (thanks [jsantos74](/jsantos74))

#### 34.0.1
 - Fix file path calculation when uploading files to IBM zOS running MVS (thanks [arafuls](/arafuls))
 - Detect fully-qualified directory when using data sets on IBM zOS running MVS (thanks [arafuls](/arafuls))

#### 34.0.0
 - Major: Refactor and cleanup helpers & extension methods
 - Major: Changed namespace of `FtpTrace` to `FluentFTP.Helpers`
 - Major: Changed namespace of `FtpListParser` to `FluentFTP.Helpers`
 - Major: You need to import `FluentFTP.Helpers` to gain access to FTP extension methods
 - Major: Changed visibility of various internal FTP helpers to `internal` to prevent external access
 - New: Ability to set a local IP address to be used for FTP connections (thanks [daviddenis-stx](/daviddenis-stx))
 - New: Properties for local IP: `SocketLocalIp`, `SocketLocalEndPoint`, `SocketRemoteEndPoint` (thanks [daviddenis-stx](/daviddenis-stx))

#### 33.2.0
 - New: Ability to block certain server ports from being used during passive FTP connection (PASV/EPSV)
 - New: `PassiveBlockedPorts` and `PassiveMaxAttempts` properties to configure passive blocked ports

#### 33.1.8
 - Fix: Add support for another server specific string to detect if file exists
 - Fix: Prevent memory leaks with FTPS on AKS by disabling `ValidateCertificateRevocation` by default
 
#### 33.1.7
 - Fix: `DownloadFile` should fail if directory path is passed in `localPath`
 - Fix: `LastReply` is not set when using async methods
 - Fix: Ambiguous call when using LINQ extension methods and EF core
 - Fix: Remove dependency on `System.Linq.Async` and disable `GetListingAsyncEnumerable`

#### 33.1.6
 - Fix: Use .NET Core API for .NET 5 builds to fix various networking and connectivity issues
 - New: Support detection of FRITZ!Box FTP servers (allows for server specific commands)

#### 33.1.5
 - Fix: Do not force users to `Connect` if capabilities are not loaded (only force if `Capabilities` are read before connecting)

#### 33.1.4
 - Fix: Conditionally hard-abort `AutoConnect` only if the credentials were incorrect

#### 33.1.3
 - New: `LogToFile` and `LogToConsole` are now available on .NET Framework / .NET 5.0
 - Fix: `Capabilities` & `HashAlgorithms` are returned if they are loaded regardless of connection status

#### 33.1.2
 - Fix: Prevent `Capabilities` & `HashAlgorithms` from causing sync-over-async path
 - Fix: `NullReferenceException` during `UploadFile` if the error message is unknown
 - Fix: If incorrect credentials are passed to `AutoConnect`, it does not hard abort
 - Fix: Initializing TLS Authentication hangs in .NET 5 if `SslBuffering` is enabled

#### 33.1.1
 - New: Support for .NET 5.0 platform
 - New: Support for `IAsyncEnumerable` pattern in .NET Standard 2.0+ and .NET 5.0 (thanks [hez2010](/hez2010))
 - New: Async variant of `GetListingAsync` introduced: `GetListingAsyncEnumerable`

#### 33.0.3
- Fix: URI ports not being respected when Uri constructor used (thanks [julian94](/julian94))
- Fix: Respect `332 Need account for login` response during FTP client authorization (thanks [novak-as](/novak-as))
- Fix: Set default value for `restartPosition` in `Download` method to match `FtpClient` method (thanks [AdamLewisGMSL](/AdamLewisGMSL))
- Fix: Improved handling for resuming upload when the connection drops (thanks [pradu71](/pradu71))

#### 33.0.2
 - Fix: Honor custom parsers if server-specific handler is used

#### 33.0.1
 - Fix: Create server-specific handler if a custom handler has not been set (thanks [Adhara3](/Adhara3))

#### 33.0.0
 - New: Reworked timezone conversion API (simply set `TimeConversion` and `TimeZone`)
 - New: Options to convert server timestamps into the format of your choice (`ServerTime`, `LocalTime` and `UTC`)
 - New: Support for conversion to local timezone in .NET core (set `LocalTimeZone`)
 - New: `GetListing` honors the time conversion settings of the active client
 - New: `GetModifiedDate` honors the time conversion settings of the active client
 - New: `SetModifiedDate` honors the time conversion settings of the active client
 - New: Reworked API for Custom file listing parsers (simply set `ListingCustomParser` on the client)
 - Fix: Drop support for legacy file listing parsing routines (`FtpParser.Legacy` will no longer work)
 - Fix: Unexpected time conversion occuring in `GetModifiedTimeAsync` 
 - Change: Breaking changes to the `TimeConversion` property.
 - Change: Breaking changes to the `TimeOffset` property, which has been replaced by `TimeZone`

#### 32.4.7
 - Fix: "The connection was terminated before a greeting could be read"

#### 32.4.6
 - Platform: Add support for .NET Standard 2.1
 - Platform: Upgrade `System.Net.Security` from version 4.3.0 to 4.3.2 to fix security issues
 - Performance: Quickly abort detection if host is unavailable during `AutoDetect`/`AutoConnect`
 - Fix: `UploadDirectory` fails for some files with "Unable to read data from the transport connection" (thanks [manuelxmarquez](/manuelxmarquez))
 - Fix: Comment for `UploadFile`/`UploadFileAsync`/`DownloadFile`/`DownloadFileAsync` methods
 - Fix: Stack overflow during connection when server responds incorrectly to PASS command

#### 32.4.5
 - Fix: Uncatchable NullReferenceException is occasionally thrown from `ConnectAsync`

#### 32.4.4
  - New: Automatic FTPS connection mode: `FtpEncryptionMode.Auto` which connects in FTP and attempts to upgrade to FTPS
  - New: `IsEncrypted` property to check if FTPS encryption is currently active for this connection
  - Fix: `ValidateCertificateRevocation` property was not being honored in async version (thanks [kolorotur](/kolorotur))

#### 32.4.3
  - Fix: Ensure file is retried sucessfully when first upload/download fails with an `IOException` (thanks [manuelxmarquez](/manuelxmarquez))
  - Fix: Ensure file streams read and write correctly even when no `FtpClient` is provided (thanks [manuelxmarquez](/manuelxmarquez))
  - Fix: Clear custom parser when removing parser or clearing all parsers (thanks [rubenhuisman](/rubenhuisman))

#### 32.4.1
  - New: `LocalFileBufferSize` property to control size of file buffer during local file I/O

#### 32.4.0
  - New: `UploadDirectoryDeleteExcluded` property to control if excluded files are deleted during Upload (thanks [philippjenni](/philippjenni))
  - New: `DownloadDirectoryDeleteExcluded` property to control if excluded files are deleted during Download (thanks [philippjenni](/philippjenni))
  - Fix: Dispose AsyncWaitHandles to stop handle leak in .NET Framework 4.5 (thanks [sdiaman1](/sdiaman1))
  - Fix: Implement proper cancellation support in `UploadDirectory` (once file transfer begins it cannot be cancelled)
  - Fix: Implement proper cancellation support in `DownloadDirectory` (once file transfer begins it cannot be cancelled)
  - Fix: Implement proper cancellation support in FXP `TransferDirectory`
  - Fix: Implement proper cancellation support in recursive `GetListing`
  - Fix: Correctly resume when unexpectedEOF error received during uploading a file (thanks [mrcopperbeard](/mrcopperbeard))
  - Fix: Hide internal properties in `FtpClient` that are not meant to be exposed
  - Fix: Update `IFtpClient` with the latest set of public properties that are meant to be exposed

#### 32.3.3
  - Fix: Downloading or uploading a directory can generate incorrect local paths

#### 32.3.2
  - Fix: Downloading or uploading a directory can generate incorrect local paths
  - Fix: Expose `LoadProfile` API so it can be called by the generated code from `AutoDetect`

#### 32.3.1
  - New: `ListingDataType` property to get file listings in ASCII/Binary
  - New: `DownloadZeroByteFiles` property to control if zero-byte files should be downloaded or skipped
  - Fix: Downloading 0-byte files crashes since no data downloaded

#### 32.3.0
  - New: All server-specific handlers moved to dedicated classes that extend `FtpBaseServer`
  - New: Ability to handle custom non-standard FTP servers by extending `FtpBaseServer`
  - Fix: Only overwrite local file after the first bytes downloaded of a remote file

#### 32.2.2
  - New: Tracking progress with FXP transfers is supported for all transfer modes
  - New: Track low-level progress with new `TransferredBytes` in `FtpProgress` class (thanks [Adhara3](/Adhara3))
  - New: `FXPProgressInterval` property to control how often FXP progress reports are sent
  - Fix: Hide `TransferFileFXPInternal` because its an internal transfer method and not to be used directly

#### 32.2.1
  - Fix: `FtpFileExtensionRule` was failing to compare extensions unless they were prefixed with a dot

#### 32.2.0
  - New: `GetChecksum` allows you to specify a hash algorithm to be run on the server if supported
  - New: `GetChecksum` has special support for switching the server-side algorithm for HASH command support
  - New: FXP file transfer now validates the file using the first mutually supported algorithm

#### 32.1.1
  - Fix: Incorrectly formatted string returned by utility method `TransferSpeedToString`

#### 32.1.0
  - New: `CompareFile` and `CompareFileAsync` methods to quickly perform various equality checks on a uploaded/downloaded file

#### 32.0.0
  - Fix: When download fails and we need to retry on failed verification, ensure that file is re-downloaded
  - Fix: When FXP transfer fails and we need to retry on failed verification, ensure that file is re-transfered
  - Fix: When uploaded file is skipped, `FtpStatus.Failed` is returned instead of `FtpStatus.Skipped`
  - Fix: Properly handle 4xx and 5xx series of errors and indicate failure when uploading or downloading files
  - Fix: Correctly detect if server-side recursion is supported otherwise fallback to manual directory recursion
  - Fix: Only resume download of files if Append mode is selected (in Overwrite mode we restart the download)
  - Change: `Upload` and `UploadAsync` now returns `FtpStatus` to indicate skipped, success or failed

#### 31.3.2
  - Fix: Proper session handling for FXP connections and disconnection of cloned connections
  - Performance: Reduce redundant file size check in `DownloadFile` when appending is used

#### 31.3.1
  - New: `AutoDetect` and `AutoConnect` now auto-configure for Azure FTP servers using known connection settings
  - Improve code generation of `FtpProfile` to use LoadProfile rather than setting each property individually
  - Add advanced Timeout and Socket settings to `FtpProfile` for Azure auto configuration
  - Fix: All exception classes now inherit from `FtpException`
  - All exceptions and `FtpProfile` are now serializable in .NET Framework

#### 31.3.0
  - New: `TransferFile` and `TransferDirectory` methods to transfer files from server to server (thanks [n0ix](/n0ix))
  - New: FXP (File Transfer Protocol) implementation to support direct server-to-server transfers (thanks [n0ix](/n0ix))

#### 31.2.0
  - New: Predefined rules for filtering on file name using regular expressions (thanks [n0ix](/n0ix))
  - New: Predefined rules for filtering on folder name using regular expressions (thanks [n0ix](/n0ix))
  - Fix: Don't calculate ETA and percentage of `FtpProgress` if file size is zero (thanks [Adhara3](/Adhara3))
  - Fix: `GetFilePermissions` should use `GetObjectInfo` instead of `GetListing` to prevent incorrect filepaths

#### 31.1.0
  - New: Support for MMD5 file hashing command to validate downloaded/uploaded files. (thanks [n0ix](/n0ix))
  - Change: Disable all `Begin*` and `End*` methods for .NET 4.5 and onwards as `async`/`await` is supported.
  - Improve: `GetHashAlgorithmAsync` and `SetHashAlgorithmAsync` implemented as true async methods with cancellation support
  - Improve: `GetObjectInfoAsync` implemented as true async methods with cancellation support

#### 31.0.0
  - New: Download and upload file methods indicate if file was transferred, skipped or failed to transfer
  - New: C# and VB.NET Examples for all file and folder transfer methods
  - New: VB.NET Examples for all methods (not included in Nuget package but available on Github)
  - Change: `DownloadFile` and `UploadFile` return `FtpStatus` instead of boolean flag for tri-state feedback

#### 30.2.0
  - New: Support for XCRC FTP Command and CRC32 hash support to validate downloaded/uploaded files (thanks [n0ix](/n0ix))

#### 30.1.1
  - Fix: Calculation of local file path during DownloadFolder sometimes ignores base directory

#### 30.1.0
  - New: Support multi-file progress tracking by indicating file index and local & remote path of the file
  - New: `UploadDirectory` and `DownloadDirectory` now supports tracking progress of the entire task 
  - New: `UploadFiles` and `DownloadFiles` now supports tracking progress for both sync/async methods
  - Fix: Update `IFtpClient` interface by adding new `UploadDirectory` and `DownloadDirectory` methods
  - Fix: Correctly determine file exists on servers that don't support SIZE command and return error 550
  - Fix: Support more strings to determine if file exists using SIZE command

#### 30.0.0
  - New: `UploadDirectory` and `UploadDirectoryAsync` methods to recursively upload or mirror a directory
  - New: `DownloadDirectory` and `DownloadDirectoryAsync` methods to recursively download or mirror a directory
  - New: Rule engine to filter files that should be uploaded/downloaded according to multiple user-defined rules
  - New: Predefined rules for filtering on folder name, useful for blacklisting certain system folders
  - New: Predefined rules for filtering on file name or file extensions, useful for transferring a subset of files
  - New: Predefined rules for filtering on file size, useful for filtering out very large files
  - New: Ability to determine parent/self/child directories in listing using `SubType` property of `FtpListItem`
  - Fix: Machine listings sometimes cause infinite recursion in `GetListing` when recursing into self directory
  - Change: `CreateDirectory` and `CreateDirectoryAsync` now return a flag indicating if it was created or skipped
  - Change: Use public fields instead of public properties for `FtpListItem`
  - Change: Improve performance of `CreateDirectory` by skipping the directory exists check

#### 29.0.4
  - Fix: Detect "file size not allowed in ASCII" string for French FTP servers

#### 29.0.3
  - Fix: TimeoutException when trying to read FTP server reply after Download/Upload

#### 29.0.2
  - New: Add `SendHost` and `SendHostDomain` to control if HOST command is sent after handshake (thanks [dansharpe83](/dansharpe83))

#### 29.0.1
  - Fix: Read stale NOOP responses after file transfer and also after `226 Transfer complete` (thanks [aliquid](/aliquid))
  - Fix: Correct default value for `TimeConversion` property to assume UTC timestamps

#### 29.0.0
  - New: Support .NET Standard 2.0
  - New: Keep control socket alive during long file transfers using NOOP (thanks [aliquid](/aliquid))
  - New: Add `NoopInterval` property to control interval of NOOP commands (thanks [aliquid](/aliquid))
  - New: Add `TimeConversion` property to control if timestamps are converted from UTC into local time
  - Refactor: Rename `FtpExists` to `FtpRemoteExists` to make its usage clear
  - New: Support detection of IBM z/OS and MVS FTP OS and server (allows for server specific commands)
  - New: New constructors for `FtpClient` to support hostnames in `Uri` format
  - Fix: Always send progress reports after file download, even for zero-length files
  
#### 28.0.5
  - New: `ValidateCertificateRevocation` property to control if certificate revocation is checked.

#### 28.0.4
  - New: `ValidateAnyCertificate` property to validate any received server certificate, useful for Powershell
  - Fix: Default SSL protocol used in .NET 4.5+ release is now TLS 1.2 (latest supported protocol)

#### 28.0.3
  - New: Override the server-specific recursive LIST detection by setting `RecursiveList`
  - Fix typo in IP parsing regex that causes fallback to Host IP to fail (thanks Andy Whitfield)

#### 28.0.2
  - Fix: Verification of the MD5 Hash when file name contains spaces (thanks [Nimelo](/Nimelo))

#### 28.0.1
  - Fix: Safely absorb TimeoutException thrown after the file has fully uploaded/downloaded

#### 28.0.0
  - New: Progress reporting for synchronous methods `Upload`, `Download`, `UploadFile` and `DownloadFile` are now sent via delegates
  - Fix: Correctly send progress for synchronous methods and retain `IProgress` for async methods

#### 27.1.4
  - Fix: Correctly assume Unix file listing parser for SunOS & Solaris servers
  - Fix: Safely absorb TimeoutException thrown after the file has fully uploaded/downloaded

#### 27.1.3
  - New: Support detection of Sun OS Solaris FTP OS and server (allows for server specific commands)
  - Fix: UploadFile fails when destination folder is empty on SunOS (550 error)

#### 27.1.2
  - Fix: Unable to upload files to OpenVMS servers if path contains numeric characters
  - Fix: Assume FTP commands supported by OpenVMS HGFTP server if FEAT not supported
  - FiX: Improve detection of OpenVMS absolute paths
  - Fix: `Connect` & `ConnectAsync` throw ArgumentException when passing an incomplete `FtpProfile`

#### 27.1.1
  - New: Auto-detect the correct FTP listing parser when SYST command fails (IIS, Azure, OpenVMS)
  - New: Assume FTP commands supported by OpenVMS HGFTP server
  - FiX: Support edge case for OpenVMS absolute paths (directive can be alpha-numeric)

#### 27.1.0
  - New: Improved transfer rate throttling when using an upload/download speed limit (thanks [wakabayashik](/wakabayashik))

#### 27.0.3
  - New: Support detection of XLight FTP server software (allows for server specific commands)
  - New: Partial support for getting directory listing using STAT command (`GetListing` supports new `FtpListOption.UseStat`)
  - Fix: `GetFileSize` always returns 0 instead of correct file size (thanks [RadiatorTwo](/RadiatorTwo))
  
#### 27.0.2
  - Fix: `FileExists` and `FileExistsAsync` support switching to binary mode for servers that need it
  
#### 27.0.1
  - Fix: Error using BlueCoat proxy to an FTP server on a port other than port 21
  - Fix: Error using UserAtHost proxy to an FTP server on a port other than port 21
  
#### 27.0.0
  - New: Change `Capability` API to return a list instead of bitwise enum (to support more than 32 distinct capabilities)
  - New: Change custom parsers to take capabilities as a list instead of bitwise enum (to match client implementation)
  - New: Support detection of FTP2S3 gateway server software (allows for server specific commands)
  - New: Support detection of server-specific capabilities of Serv-U FTP Gateway
  - New: Support `RMDA` command to quickly and recursively delete a directory from Serv-U FTP Gateway
  
#### 26.0.2
  - Fix: Improve performance of `GetFileSize` to only switch to Binary for servers that require it
  - Fix: Ensure data type (ASCII/Binary) is correctly set during `GetFileSize` for servers that require it
  - Fix: Ensure data type (ASCII/Binary) is correctly set for cloned connections
  - Fix: Ensure data type (ASCII/Binary) is correctly set during `GetListing` and `GetNameListing`
  - Fix: Reset server detection state flags whenever we connect to a server, to allow for reuse of `FtpClient`
  - Fix: Copy server detection state flags to cloned connections to improve performance
  - Fix: Retry `GetListing` if temporary error "Received an unexpected EOF or 0 bytes from the transport stream"

#### 26.0.1
  - Fix: Prefer using Passive/Active modes rather than Enhanced Active/Passive during auto-detection
  - Fix: Some FTP servers do not open a port when listing an empty folder with `GetNameListing`
  - Fix: Hard catch and suppress all exceptions during disposing to solve all random exceptions

#### 26.0.0
  - New: Automatic FTP connection negotiation using `AutoConnect()`
  - New: Automatic detection of working FTP connection settings using `AutoDetect()`
  - New: C# code generation of working connection settings using `FtpProfile.ToCode()`
  - New: Support more capability detection commands: EPSV, CPSV, NOOP, CLNT, SSCN, SITE commands for ProFTPd
  - New: Improve transfer performance by only attempting EPSV once and then never using it again for that connection
  - New: Support MKDIR & RMDIR commands specially for ProFTPd to quickly create and delete a directory on the server-side
  - New: Support PRET command before downloading or uploading files for servers like ProFTPd & DrFTPd
  - New: Support detection of BFTPd server software (allows for server specific commands)
  - Fix: When uploading files in `FtpExists.NoCheck` mode, file size check should not be done
  - Fix: Some FTP servers do not open a port when listing an empty folder (thanks [Mortens4444](/Mortens4444))
  - Fix: `OpenRead` with `EnableThreadSafeDataConnections` always transfers in ASCII (thanks [ts678](/ts678))
  - Refactor: Delete legacy static methods: `OpenRead`, `OpenWrite`, `OpenAppend` (dynamic versions still exist)
  - Refactor: Move `CalcChmod` from `FtpClient` to `FtpExtensions` (as part of repository cleanup task)

#### 25.0.6
  - Fix: Async methods do not work with Active FTP mode and SSL/encryption (thanks [Mortens4444](/Mortens4444))
  - Fix: For OpenVMS absolute paths may not contain slashes but are still absolute (3rd revision)

#### 25.0.5
  - Fix: Divide-by-zero exceptions while calculating progress of file uploads/downloads

#### 25.0.4
  - Fix: Supress all exceptions when Disposing the underlying FtpSocketStream

#### 25.0.3
  - Fix: Received an unexpected EOF or 0 bytes from the transport stream (thanks [mikemeinz](/mikemeinz))
  - Fix: `UploadFile()` progress callback is not called if the file already exists on the server
  - (.NET core) Fix: `Connect()` method sometimes causes the thread to hang indefinitely (thanks [radiy](/radiy))
  - Fix: Regression of #288 where upload hangs with only a few bytes left (thanks [cw-andrews](/cw-andrews))

#### 25.0.1
  - New: `FtpAuthenticationException` for authentication errors (thanks [erik-wramner](/erik-wramner))
  - New: Added support to detect Homegate FTP Server

#### 25.0.0
  - New: SSL Buffering is now switchable via the `SslBuffering` parameter
  - Fix: SSL Buffering is automatically disabled when using FTP proxies, and enabled in all other cases
  - Fix: Revert PR #383 as it was causing regression issues in SSL connectivity
  - Fix: Disable automatic IP correction to fix connectivity issues via BlueCoat proxy servers (thanks [CMIGIT](/CMIGIT))
  - Refactor: Rename `FtpClientUserAtHostProxyBlueCoat` to `FtpClientBlueCoatProxy`
  - Fix: For OpenVMS absolute paths may not contain slashes but are still absolute (2nd revision) (thanks [tonyhawe](/tonyhawe))
  - Fix: Detect file existence string `"Can't find file"` to fix FileExists check on some servers (thanks [reureu](/reureu))
  - Fix: Feature parity between `FileExists` and `FileExistsAsync` methods, added support for FtpReply 550 check (thanks [reureu](/reureu))
  - Fix: Feature parity between `UploadFile` and `UploadFileAsync` methods, added support for AppendNoCheck handling (thanks [reureu](/reureu))

#### 24.0.0
  - New: Get detailed progress information for uploads/downloads via the `FtpProgress` object (thanks [n0ix](/n0ix))
  - New: Get transfer speed and ETA (estimated time of arrival) for uploads/downloads (thanks [n0ix](/n0ix))
  - Fix: Files were uploaded in Write mode instead of Append mode when the exists mode is `AppendNoCheck` and we couldn't read the offset position (thanks @everbalovas)
  - Fix: Swap `SslStream` and `BufferedStream` so proxied connections with `FtpClientHttp11Proxy` are to connect (thanks @rmja)
  
#### 23.1.0
  - New: Additional FTP Server software detection (HP NonStop/Tandem, GlobalScape EFT, Serv-U, Cerberus, CrushFTP, glFTPd)
  - New: Assume capabilities for servers that don't support FEAT (wuFTPd)
  - Fix: `FileExists` returns false if name listing is used and server lists filenames with the path
  - Fix: For OpenVMS absolute paths may not contain slashes but are still absolute
  - Fix: For `Download()` methods `restartPosition` should not be mandatory
  
#### 23.0.0
  - New: Ability to cancel all async methods via `CancellationToken` (thanks [WolfspiritM](https://github.com/WolfspiritM))
  - New: `ReadTimeout` is now honored by all async methods (thanks [WolfspiritM](https://github.com/WolfspiritM))
  - New: FTP Server operating system detection (Windows, Unix, VMS, IBM/OS400)
  - (.NET core) Fix: GetListing blocking with no timeout (thanks [WolfspiritM](https://github.com/WolfspiritM))
  - (.NET core) Fix async methods by not using the the async read function (thanks [WolfspiritM](https://github.com/WolfspiritM))
  
#### 22.0.0
  - New: Ability to resume a download via `existsMode` on `DownloadFile()` and `DownloadFiles()` (thanks [n0ix](https://github.com/n0ix))
  - New: Ability to turn off checking for server capabilities using FEAT command (thanks [nhh-softwarehuset](https://github.com/nhh-softwarehuset))
  - Fix: Add workaround if a server advertises a non-routeable IP in PASV Mode (thanks [n0ix](https://github.com/n0ix))
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
  - Fix: During upload, respond to any error in 5xx series, not just 550 (thanks [stengnath](https://github.com/stengnath))
  - Fix: Various fixes to `UploadFileAsync` based on fixes already implemented in `UploadFile`
  
#### 19.2.4
  - Fix: `UploadFilesAsync` with `errorHandling` deletes the entire directory instead of specific files
  - Fix: Server responds to EPSV with 425 "Data connection failed" but connects with PASV (thanks [ejohnsonTKTNET](https://github.com/ejohnsonTKTNET))
  - Fix: Use proper async configuration for .NET Async methods (thanks [ejohnsonTKTNET](https://github.com/ejohnsonTKTNET))
  - Fix: Improve implementation of upload and download resuming in Async methods (thanks [ejohnsonTKTNET](https://github.com/ejohnsonTKTNET))
  
#### 19.2.3
  - Fix: `UploadFile()` or `UploadFiles()` sometimes fails to create the remote directory if it doesn't exist
  - Fix: `DownloadDataType` Binary value ignored on ASCII-configured FTP servers
  - Performance improvement: Added `BufferedStream` between `SslStream` and `NetworkStream` (thanks [stengnath](https://github.com/Lukazoid))
  - Fix: When the FTP server sends 550, transfer is received but not confirmed (thanks [stengnath](https://github.com/stengnath))
  - Fix: Make `Dispose` method of `FTPClient` virtual (thanks @martinbu)
  - Fix: `OpenPassiveDataStream`/`Async()` uses the target FTP host instead of the configured proxy (thanks @rmja)
  - Fix: `FileExists()` for Xlight FTP Server (thanks @oldpepper)
  - Fix: FTPD "550 No files found" when folder exists but is empty, only in PASV mode (thanks [stengnath](https://github.com/olivierSOW))
  - Fix: Many unexpected EOF for remote file `IOException` on Android (thanks @jersiovic)
  - Fix: Race condition when `BeginInvoke` calls the callback before the `IAsyncResult` is added (thanks [stengnath](https://github.com/Lukazoid))
  
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
  - Fix: Throw `OperationCanceledException` instead of `FtpException` when cancellation is requested (thanks [taoyouh](https://github.com/taoyouh))
  
#### 19.1.2
  - Fix: Add support for checking if file exists on Serv-U FTP Server
  - Fix: Make `IFtpClient` inherit from `IDisposable` (thanks @repl-andrew-ovens)
  - (UWP) Fix: UWP does not allow `File.Exists()` to run in UI thread (thanks [taoyouh](https://github.com/taoyouh))
  
#### 19.1.1
  - Fix: When downloading files in ASCII mode, file length is unreliable therefore we read until EOF
  - Fix: When upload/download progress is indeterminate, send -1 instead of NaN or Infinity
  - Fix: `NetStream` was not assigned in `FtpSocketStream` for .NET Standard in active FTP mode (thanks @ralftar)
  - Fix: `CurrentDataType` was not set for ASCII transfers in `DownloadFileAsync`/`UploadFileAsync` (thanks [taoyouh](https://github.com/taoyouh))
  - Fix: Sometimes `FtpSocketStream` and `FtpDataStream` are not disposed in `FtpSocketStream.Dispose` (thanks [taoyouh](https://github.com/taoyouh))
  
#### 19.1.0
  - New Progress reporting for `UploadFile` & `DownloadFile` methods via `IProgress`
  - Fix: `Stream.Position` should not be set in `UploadFileInternal` unless supported
  
#### 19.0.0
  - New Task-based async methods for .NET Standard and .NET Fx 4.5 (thanks [taoyouh](https://github.com/taoyouh))
  - New async methods for `UploadFile`, `DownloadFile`, `UploadFiles` & `DownloadFiles` (thanks [artiomchi](https://github.com/artiomchi))
  - (UWP) Fix: `FileNotFoundException` with reference `System.Console` (thanks [artiomchi](https://github.com/artiomchi))
  - (.NET core) Fix: Thread suspends when calling `UploadFile` or `DownloadFile` (thanks [artiomchi](https://github.com/artiomchi))
  - (.NET core) Fix: File download hangs inconsistently when reading data from stream (thanks @artiomchi, [bgroenks96](https://github.com/bgroenks96))
  - (.NET core) Fix: Stream does not dispose due to wrong handling of closing/disposing (thanks [artiomchi](https://github.com/artiomchi))
  - Fix: File upload EOS bug when calling `Stream.Read` (thanks [bgroenks96](https://github.com/bgroenks96), @artiomchi, @taoyouh)
  - Fix: `DownloadFileInternal` not recognizing the download data type
  with `EnableThreadSafeConnections` (thanks [bgroenks96](https://github.com/bgroenks96))
  - (Backend) Migrate to a single VS 2017 solution for all frameworks (thanks [artiomchi](https://github.com/artiomchi))
  - (Backend) Continuous Integration using AppVeyor  (thanks [artiomchi](https://github.com/artiomchi))
  
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

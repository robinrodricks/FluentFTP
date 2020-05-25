# Release Notes

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

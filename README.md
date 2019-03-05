
# FluentFTP

[![Version](https://img.shields.io/nuget/vpre/FluentFTP.svg)](https://www.nuget.org/packages/FluentFTP)

FluentFTP is a fully managed FTP and FTPS library for .NET & .NET Standard, optimized for speed. It provides extensive FTP commands, File uploads/downloads, SSL/TLS connections, Automatic directory listing parsing, File hashing/checksums, File permissions/CHMOD, FTP proxies, UTF-8 support, Async/await support and more.

It is written entirely in C#, with no external dependencies. FluentFTP is released under the permissive MIT License, so it can be used in both proprietary and free/open source applications.

## Features

- Full support for [FTP](#ftp-support), [FTPS](#faq_ftps) (FTP over SSL), [FTPS with client certificates](#faq_certs) and [FTPS with CCC](#faq_ccc) (for FTP firewalls)
- **File management:**
  - File and directory listing for [all major server types](#faq_listings) (Unix, Windows/IIS, Azure, Pure-FTPd, ProFTPD, Vax, VMS, OpenVMS, Tandem, HP NonStop Guardian, IBM OS/400, AS400, Windows CE, Serv-U, etc)
  - Easily upload and download a file from the server with [progress tracking](#faq_progress)
  - Automatically [verify the hash](#faq_verifyhash) of a file & retry transfer if hash mismatches
  - Configurable error handling (ignore/abort/throw) for multi-file transfers
  - Easily read and write file data from the server using standard streams
  - Create, append, read, write, rename, move and delete files and folders
  - Recursively deletes folders and all its contents
  - Get file/folder info (exists, size, security flags, modified date/time)
  - Get and set [file permissions](#file-permissions) (owner, group, other)
  - Absolute or relative paths (relative to the ["working directory"](#file-management))
  - Get the [hash/checksum](#file-hashing) of a file (SHA-1, SHA-256, SHA-512, and MD5)
  - Dereference of symbolic links to calculate the linked file/folder
- **FTP protocol:**
  - Automatic detection of the [FTP server software](#faq_servertype) and its [capabilities](#faq_recursivelist)
  - Extensive support for [FTP commands](#ftp-support), including some server-specific commands
  - Easily send [server-specific](https://github.com/hgupta9/FluentFTP/issues/88) FTP commands using the `Execute()` method
  - Explicit and Implicit [SSL connections](#faq_ftps) are supported for the control and data connections using .NET's `SslStream`
  - Passive and active data connections (PASV, EPSV, PORT and EPRT)
  - Supports DrFTPD's PRET command, and the Unix CHMOD command
  - Supports [FTP Proxies](#faq_loginproxy) (User@Host, HTTP 1.1)
  - [FTP command logging](#faq_log) using `TraceListeners` (passwords omitted) to [trace](#faq_trace) or [log output](#faq_logfile) to a file
  - SFTP is not supported as it is FTP over SSH, a completely different protocol (use [SSH.NET](https://github.com/sshnet/SSH.NET) for that)
- **Asynchronous support:**
  - Synchronous and asynchronous methods using `async`/`await` for all operations
  - Asynchronous methods for .NET 4.0 and below using `IAsyncResult` pattern (Begin*/End*)
  - All asynchronous methods can be cancelled midway by passing a `CancellationToken`
  - All asynchronous methods honor the `ReadTimeout` and automatically cancel themselves if timed out
  - Improves thread safety by cloning the FTP control connection for file transfers (optional)
  - Implements its own internal locking in an effort to keep transactions synchronized
- **Extensible:**
  - Easily add support for more proxy types (simply extend [`FTPClientProxy`](https://github.com/hgupta9/FluentFTP/blob/master/FluentFTP/Proxy/FtpClientProxy.cs))
  - Easily add unsupported directory listing parsers (see the [`CustomParser`](https://github.com/hgupta9/FluentFTP/blob/f48af030b565237ddd5d7c8937378884d20e1627/FluentFTP.Examples/CustomParser.cs) example)
  - Easily add custom logging/tracing functionality using [`FtpTrace.AddListener`](#faq_log)

## Releases

Stable binaries are released on NuGet, and contain everything you need to use FTP/FTPS in your .Net/CLR application. For usage see the [Example Usage](#example-usage) section and the [Documentation](#documentation) section below.

- [Nuget](https://www.nuget.org/packages/FluentFTP) (latest)
- [Releases](https://github.com/hgupta9/FluentFTP/releases) (occasionally updated)

FluentFTP works on .NET and .NET Standard/.NET Core.

| Platform      		| Binaries Folder	| 
|---------------		|-----------		|
| **.NET 2.0**      	| net20     		| 
| **.NET 3.5**      	| net35     		| 
| **.NET 4.0**      	| net40     		| 
| **.NET 4.5**      	| net45     		| 
| **.NET Standard 1.4** | netstandard1.4	| 
| **.NET Standard 1.6** | netstandard1.6	| 

FluentFTP is also supported on these platforms: (via .NET Standard)

- **Mono** 4.6
- **Xamarin.iOS** 10.0
- **Xamarin.Android** 10.0
- **Universal Windows Platform** 10.0

Binaries for all platforms are built from a single VS 2017 Project. You will need VS 2017 to build or contribute to FluentFTP.

## Example Usage

```csharp
// create an FTP client
FtpClient client = new FtpClient("123.123.123.123");

// if you don't specify login credentials, we use the "anonymous" user account
client.Credentials = new NetworkCredential("david", "pass123");

// begin connecting to the server
client.Connect();

// get a list of files and directories in the "/htdocs" folder
foreach (FtpListItem item in client.GetListing("/htdocs")) {
	
	// if this is a file
	if (item.Type == FtpFileSystemObjectType.File){
		
		// get the file size
		long size = client.GetFileSize(item.FullName);
		
	}
	
	// get modified date/time of the file or folder
	DateTime time = client.GetModifiedTime(item.FullName);
	
	// calculate a hash for the file on the server side (default algorithm)
	FtpHash hash = client.GetHash(item.FullName);
	
}

// upload a file
client.UploadFile(@"C:\MyVideo.mp4", "/htdocs/MyVideo.mp4");

// rename the uploaded file
client.Rename("/htdocs/MyVideo.mp4", "/htdocs/MyVideo_2.mp4");

// download the file again
client.DownloadFile(@"C:\MyVideo_2.mp4", "/htdocs/MyVideo_2.mp4");

// delete the file
client.DeleteFile("/htdocs/MyVideo_2.mp4");

// delete a folder recursively
client.DeleteDirectory("/htdocs/extras/");

// check if a file exists
if (client.FileExists("/htdocs/big2.txt")){ }

// check if a folder exists
if (client.DirectoryExists("/htdocs/extras/")){ }

// upload a file and retry 3 times before giving up
client.RetryAttempts = 3;
client.UploadFile(@"C:\MyVideo.mp4", "/htdocs/big.txt", FtpExists.Overwrite, false, FtpVerify.Retry);

// disconnect! good bye!
client.Disconnect();
```
	
## Documentation

- [API Documentation](#api)
    - [Connection](#connection)
    - [Directory Listing](#directory-listing)
    - [File Transfer](#file-transfer)
    - [File Management](#file-management)
    - [File Permissions](#file-permissions)
    - [File Hashing](#file-hashing)
    - [FTPS](#ftps)
    - [Settings](#advanced-settings)
    - [Utilities](#utilities)
    - [Logging](#logging)
- [FTP Support Table](#ftp-support)
- [Examples](https://github.com/hgupta9/FluentFTP/tree/master/FluentFTP.Examples)
- [Release Notes](https://github.com/robinrodricks/FluentFTP/blob/master/RELEASES.md)
- [Notes](https://github.com/robinrodricks/FluentFTP/blob/master/NOTES.md)

## FAQs

**Logging FAQs**
- [How do I trace FTP commands for debugging?](#faq_trace)
- [How do I log all FTP commands to a file for debugging?](#faq_logfile)
- [How do I log only critical errors to a file?](#faq_logfile2)
- [How do I disable logging of function calls?](#faq_logfunc)
- [How do I omit sensitive information from the logs?](#faq_hidelog)
- [How do I use third-party logging frameworks like NLog?](#faq_log)

**Connection FAQs**
- [How do I connect with SSL/TLS? / How do I use FTPS?](#faq_ftps)
- [How do I validate the server's certificate when using FTPS?](#faq_ftps)
- [How do I connect with FTPS and then switch back down to plaintext FTP?](#faq_ccc)
- [How do I connect with SFTP?](#faq_sftp)
- [How do I login with an anonymous FTP account?](#faq_loginanon)
- [How do I login with an FTP proxy?](#faq_loginproxy)
- [How do I detect the type of server I'm connecting to?](#faq_servertype)
- [How do I use client certificates to login with FTPS?](#faq_certs)
- [How do I bundle an X509 certificate from a file?](#faq_x509)

**File Transfer FAQs**
- [How can I track the progress of file transfers?](#faq_progress)
- [How can I upload data created on the fly?](#faq_uploadbytes)
- [How can I download data without saving it to disk?](#faq_downloadbytes)
- [How can I resume downloading a file?](#faq_resumedownload)
- [How can I resume uploading a file?](#faq_uploadmissing)
- [How can I throttle the speed of upload/download?](#faq_throttle)
- [How do I verify the hash/checksum of a file and retry if the checksum mismatches?](#faq_verifyhash)
- [How do I append to a file?](#faq_append)
- [How do I download files using the low-level API?](#faq_downloadlow)
- [How can I upload/download files with Unicode filenames when my server does not support UTF8?](#faq_utf)

**File Management FAQs**
- [How does GetListing() work internally?](#faq_listings)
- [How does GetListing() return a recursive file listing?](#faq_recursivelist)
- [What kind of hashing commands are supported?](#faq_hashing)

**Misc FAQs**
- [What does `EnableThreadSafeDataConnections` do?](#faq_etsdc)
- [How can I contribute some changes to FluentFTP?](#faq_fork)
- [How do I submit a pull request?](#faq_fork)

**Common Issues**
- [I'm getting login errors but I can login fine in Firefox/Filezilla](#faq_loginanon)
- [FluentFTP fails to install in Visual Studio 2010 : 'System.Runtime' already has a dependency defined for 'FluentFTP'.](#trouble_install)
- [After uploading a file with special characters like "Caffè.png" it appears as "Caff?.bmp" on the FTP server. The server supports only ASCII but "è" is ASCII. FileZilla can upload this file without problems.](#trouble_specialchars)
- [I cannot delete a file if the filename contains Russian letters. FileZilla can delete this file without problems.](#trouble_specialchars2)
- [I keep getting TimeoutException's in my Azure WebApp](#trouble_azure)
- [Many commands don't work on Windows CE](#trouble_windowsce)
- [After successfully transfering a single file with OpenWrite/OpenAppend, the subsequent files fail with some random error, like "Malformed PASV response"](#trouble_getreply)
- [SSL Negotiation is very slow during FTPS login](#trouble_ssl)
- [Unable to read data from the transport connection : An existing connection was forcibly closed by the remote host](#trouble_closedhost)


## API

Complete API documentation for the `FtpClient` class, which handles all FTP/FTPS functionality.

**Note:** All methods support synchronous and asynchronous versions. Simply add the "Async" postfix to a method for `async`/`await` syntax in .NET 4.5+, or add the "Begin"/"End" prefix to a method for .NET 4.0 and below.

### Connection

- **new FtpClient**() - Creates and returns a new FTP client instance.

- **Host** - The FTP server IP or hostname. Required.

- **Port** - The FTP port to connect to. **Default:** Auto (21 or 990 depending on FTPS config)

- **Credentials** - The FTP username & password to use. Must be a valid user account registered with the server. **Default:** `anonymous/anonymous`

- **Connect**() - Connects to an FTP server (uses TLS/SSL if configured).

- **Disconnect**() - Closes the connection to the server immediately.

- **Execute**() - Execute a custom or unspported command.

- **SystemType** - Gets the type of system/server that we're connected to.

- **ServerType** - Gets the type of the FTP server software that we're connected to, using the `FtpServer` enum. If it does not detect your specific server software, please contribute a [detection script](#faq_recursivelist). **Default:** `FtpServer.Unknown`

- **ServerOS** - Gets the operating system of the FTP server software that we're connected to, using the `FtpOS` enum. **Default:** `FtpOS.Unknown`

- **IsConnected** - Checks if the connection is still alive.

- **Capabilities** - Gets the server capabilties (represented by flags).

- **HasFeature**() - Checks if a specific feature (`FtpCapability`) is supported by the server.

- **LastReply** - Returns the last `FtpReply` recieved from the server.

### Directory Listing

- **GetListing**() - Get a [file listing](#faq_listings) of the given directory. Add `FtpListOption.Recursive` to recursively list all the sub-directories as well. Returns one `FtpListItem` per file or folder with all available properties set. Each item contains:

	- `Type` : The type of the object. (File, Directory or Link)
	
	- `Name` : The name of the object. (minus the path)

	- `FullName` : The full file path of the object.

	- `Created ` : The created date/time of the object. **Default:** `DateTime.MinValue` if not provided by server.

	- `Modified` : The last modified date/time of the object. If you get incorrect values, try adding the `FtpListOption.Modify` flag which loads the modified date/time using another `MDTM` command. **Default:** `DateTime.MinValue` if not provided by server.

	- `Size` : The size of the file in bytes. If you get incorrect values, try adding the `FtpListOption.Size` flag which loads the file size using another `SIZE` command. **Default:** `0` if not provided by server.

	- `LinkTarget` : The full file path the link points to. Only filled for symbolic links. 

	- `LinkObject` : The file/folder the link points to. Only filled for symbolic links if `FtpListOption.DerefLink` flag is used.

	- `SpecialPermissions` : Gets special permissions such as Stiky, SUID and SGID.

	- `Chmod` : The CHMOD permissions of the object. For example 644 or 755. **Default:** `0` if not provided by server.

	- `OwnerPermissions` : User rights. Any combination of 'r', 'w', 'x' (using the `FtpPermission` enum). **Default:** `FtpPermission.None` if not provided by server.

	- `GroupPermissions` : Group rights. Any combination of 'r', 'w', 'x' (using the `FtpPermission` enum). **Default:** `FtpPermission.None` if not provided by server.

	- `OtherPermissions` : Other rights. Any combination of 'r', 'w', 'x' (using the `FtpPermission` enum). **Default:** `FtpPermission.None` if not provided by server.

	- `RawPermissions` : The raw permissions string received for this object. Use this if other permission properties are blank or invalid.

	- `Input` : The raw string that the server returned for this object. Helps debug if the above properties have been correctly parsed.
	
- **GetNameListing**() - A simple command that only returns the list of file paths in the given directory, using the NLST command.

- **GetObjectInfo()** - Get information for a single file or directory as an `FtpListItem`. It includes the type, date created, date modified, file size, permissions/chmod and link target (if any).


### File Transfer

<a name="highlevel"></a>
*High-level API:*

- **Upload**() - Uploads a Stream or byte[] to the server. Returns true if succeeded, false if failed or file does not exist. Exceptions are thrown for critical errors. Supports very large files since it uploads data in chunks.

- **Download**() - Downloads a file from the server to a Stream or byte[]. Returns true if succeeded, false if failed or file does not exist. Exceptions are thrown for critical errors. Supports very large files since it downloads data in chunks.

- **UploadFile**() - Uploads a file from the local file system to the server. Use `FtpExists.Append` to resume a partial upload. Returns true if succeeded, false if failed or file does not exist. Exceptions are thrown for critical errors. Supports very large files since it uploads data in chunks. Optionally [verifies the hash](#faq_verifyhash) of a file & retries transfer if hash mismatches.

- **DownloadFile**() - Downloads a file from the server to the local file system. Use `FtpLocalExists.Append` to resume a partial download. Returns true if succeeded, false if failed or file does not exist. Exceptions are thrown for critical errors. Supports very large files since it downloads data in chunks. Local directories are created if they do not exist. Optionally [verifies the hash](#faq_verifyhash) of a file & retries transfer if hash mismatches.

- **UploadFiles**() - Uploads multiple files from the local file system to a single folder on the server. Returns the number of files uploaded. Skipped files are not counted. User-defined error handling for exceptions during file upload (ignore/abort/throw).  Optionally [verifies the hash](#faq_verifyhash) of a file & retries transfer if hash mismatches. Faster than calling `UploadFile()` multiple times.

- **DownloadFiles**() - Downloads multiple files from server to a single directory on the local file system. Returns the number of files downloaded. Skipped files are not counted. User-defined error handling for exceptions during file download (ignore/abort/throw). Optionally [verifies the hash](#faq_verifyhash) of a file & retries transfer if hash mismatches.

<a name="lowlevel"></a>
*Low-level API:*

- **OpenRead**() - *(Prefer using `Download()` for downloading to a `Stream` or `byte[]`)* Open a stream to the specified file for reading. Returns a [standard `Stream`](#stream-handling). Please call `GetReply()` after you have successfully transfered the file to read the "OK" command sent by the server and prevent stale data on the socket.

- **OpenWrite**() - *(Prefer using `Upload()` for uploading a `Stream` or `byte[]`)* Opens a stream to the specified file for writing. Returns a [standard `Stream`](#stream-handling), any data written will overwrite the file, or create the file if it does not exist. Please call `GetReply()` after you have successfully transfered the file to read the "OK" command sent by the server and prevent stale data on the socket.

- **OpenAppend**() - *(Prefer using `Upload()` with `FtpExists.Append` for uploading a `Stream` or `byte[]`)* Opens a stream to the specified file for appending. Returns a [standard `Stream`](#stream-handling), any data written wil be appended to the end of the file. Please call `GetReply()` after you have successfully transfered the file to read the "OK" command sent by the server and prevent stale data on the socket.


### File Management

*Working directory (relative paths are relative to this directory):*

- **GetWorkingDirectory**() - Gets the full path of the current working directory.

- **SetWorkingDirectory**() - Sets the full path of the current working directory. All relative paths are relative to the working directory.

*Directories:*

- **DirectoryExists**() - Check if a directory exists on the server.

- **CreateDirectory**() - Creates a directory on the server. If the parent directories do not exist they are also created.

- **DeleteDirectory**() - Deletes the specified directory on the server. If it is not empty then all subdirectories and files are recursively deleted.

- **MoveDirectory**() - Moves a directory from one place to another on the server. The destination directory is deleted before moving if `FtpExists.Overwrite` is used. Only throws exceptions for critical errors.

*Files:*

- **FileExists**() - Check if a file exists on the server.

- **DeleteFile**() - Deletes the specified file on the server.

- **MoveFile**() - Moves a file from one directory to another on the server. The destination file is deleted before moving if `FtpExists.Overwrite` is used. Only throws exceptions for critical errors.

- **Rename**() - Renames the file/directory on the server. Low level method that should NOT be used in most cases. Prefer `MoveFile()` and `MoveDirectory()`. Throws exceptions if the source does not exist, or if the destination already exists.

- **GetModifiedTime**() - Gets the last modified date/time of the file or folder. Result may be in server timezone, local timezone or UTC, depending on `type` argument.

- **SetModifiedTime**() - Modifies the last modified date/time of the file or folder. Input may be in server timezone, local timezone or UTC, depending on `type` argument.

- **GetFileSize**() - Gets the size of the file in bytes, or -1 if not found.

- **DereferenceLink**() - Recursively dereferences a symbolic link and returns the full path if found. The `MaximumDereferenceCount` property controls how deep we recurse before giving up.


### File Permissions

*Standard commands supported by most servers*

- **GetChmod**() - Gets the CHMOD permissions of the file/folder, or 0 if not found.

- **GetFilePermissions**() - Gets the permissions of the given file/folder as an FtpListItem object with all "Permission" properties set, or null if not found.

*Only supported by UNIX FTP servers which have the CHMOD extension installed and enabled.*

- **Chmod**() - Modifies the permissions of the given file/folder, given the CHMOD value.

- **SetFilePermissions**() - Modifies the permissions of the given file/folder, given seperate owner/group/other values (`FtpPermission` enum).


### File Hashing

*(Note: The [high-level file transfer API](#file-transfer) supports automatic hashing after upload/download).*

*Standard commands supported by most servers*

- **HashAlgorithms** - Get the hash types supported by the server, if any (represented by flags).

- **GetHash**() - Gets the hash of an object on the server using the currently selected hash algorithm. Supported algorithms are available in the `HashAlgorithms` property. You should confirm that it's not equal to `FtpHashAlgorithm.NONE` (which means the server does not support the HASH command).

- **GetHashAlgorithm**() - Query the server for the currently selected hash algorithm for the HASH command. 

- **SetHashAlgorithm**() - Selects a hash algorithm for the HASH command, and stores this selection on the server. 

*Non-standard commands supported by certain servers only. [Learn more](#faq_hashing)*

- **GetChecksum**() - Retrieves a checksum of the given file using a checksumming method that the server supports, if any. The algorithm used goes in this order : HASH, MD5, XMD5, XSHA1, XSHA256, XSHA512, XCRC.

- **GetMD5**() - Retrieves the MD5 checksum of the given file, if the server supports it.

- **GetXMD5**() - Retrieves the MD5 checksum of the given file, if the server supports it.

- **GetXSHA1**() - Retrieves the SHA1 checksum of the given file, if the server supports it.

- **GetXSHA256**() - Retrieves the SHA256 checksum of the given file, if the server supports it.

- **GetXSHA512**() - Retrieves the SHA512 checksum of the given file, if the server supports it.

- **GetXCRC**() - Retrieves the CRC32 checksum of the given file, if the server supports it.


### FTPS

- **EncryptionMode** - Type of SSL to use, or none. Explicit is TLS, Implicit is SSL. **Default:** FtpEncryptionMode.None.

- **DataConnectionEncryption** - Indicates if data channel transfers should be encrypted. **Default:** true.

- **SslProtocols** - Encryption protocols to use. **Default:** SslProtocols.Default.

- **ClientCertificates** - X509 client certificates to be used in SSL authentication process. [Learn more.](#faq_certs)

- **ValidateCertificate** - Event is fired to validate SSL certificates. If this event is not handled and there are errors validating the certificate the connection will be aborted.

- **PlainTextEncryption** - Disable encryption immediately after connecting with FTPS, using the CCC command. This is useful when you have a FTP firewall that requires plaintext FTP, but your server mandates FTPS connections. **Default:** false.


### Advanced Settings

*FTP Protocol*

- **DataConnectionType** - Active or Passive connection. **Default:** FtpDataConnectionType.AutoPassive (tries EPSV then PASV then gives up)

- **Encoding** - Text encoding (ASCII or UTF8) used when talking with the server. ASCII is default, but upon connection, we switch to UTF8 if supported by the server. Manually setting this value overrides automatic detection. **Default:** Auto.

- **InternetProtocolVersions** - Whether to use IPV4 and/or IPV6 when making a connection. All addresses returned during name resolution are tried until a successful connection is made. **Default:** Any.

- **MaximumDereferenceCount** - The maximum depth of recursion that `DereferenceLink()` will follow symbolic links before giving up. **Default:** 20.

- **UngracefullDisconnection** - Disconnect from the server without sending QUIT. **Default:** false.

- **RetryAttempts** - The retry attempts allowed when a verification failure occurs during download or upload. **Default:** 1.

- **IsClone** - Checks if this control connection is a clone. **Default:** false.



*File Listings*

- **ListingParser** - File listing parser to be used. Automatically calculated based on the type of the server, unless changed. File listing parsing has improved in 2017, but to use the older parsing routines please use `FtpParser.Legacy`. **Default:** `FtpParser.Auto`.

- **ListingCulture** - Culture used to parse file listings. **Default:** `CultureInfo.InvariantCulture`.

- **TimeOffset** - Time difference between server and client, in hours. If the server is located in Amsterdam and you are in Los Angeles then the time difference is 9 hours. **Default:** 0.

- **RecursiveList** - Check if your server supports a recursive LIST command (`LIST -R`).

- **BulkListing** - If true, increases performance of GetListing by reading multiple lines of the file listing at once. If false then GetListing will read file listings line-by-line. If GetListing is having issues with your server, set it to false. **Default:** true.

- **BulkListingLength** - Bytes to read during GetListing. Only honored if BulkListing is true. **Default:** 128.



*File Transfer*

- **TransferChunkSize** - Chunk size (in bytes) used during upload/download of files. **Default:** 65536 (65 KB).

- **UploadRateLimit** - Rate limit for uploads (in kbyte/s), honored by [high level API](#highlevel). **Default:** 0 (Unlimited).

- **DownloadRateLimit** - Rate limit for downloads (in kbyte/s), honored by [high level API](#highlevel). **Default:** 0 (Unlimited).

- **UploadDataType** - Upload files in ASCII or Binary mode? **Default:** FtpDataType.Binary.

- **DownloadDataType** - Download files in ASCII or Binary mode? **Default:** FtpDataType.Binary.



*Active FTP*

- **ActivePorts** - List of ports to try using for Active FTP connections, or null to automatically select a port. **Default:** null.

- **AddressResolver** - Delegate used for resolving local address, used for active data connections. This can be used in case you're behind a router, but port forwarding is configured to forward the ports from your router to your internal IP. In that case, we need to send the router's IP instead of our internal IP.


*Timeouts*

- **ConnectTimeout** - Time to wait (in milliseconds) for a connection attempt to succeed, before giving up. **Default:** 15000 (15 seconds).

- **ReadTimeout** - Time to wait (in milliseconds) for data to be read from the underlying stream, before giving up. Honored by all asynchronous methods as well. **Default:** 15000 (15 seconds).

- **DataConnectionConnectTimeout** - Time to wait (in milliseconds) for a data connection to be established, before giving up. **Default:** 15000 (15 seconds).

- **DataConnectionReadTimeout** - Time to wait (in milliseconds) for the server to send data on the data channel, before giving up. **Default:** 15000 (15 seconds).

- **SocketPollInterval** - Time that must pass (in milliseconds) since the last socket activity before calling `Poll()` on the socket to test for connectivity. Setting this interval too low will have a negative impact on perfomance. Setting this interval to 0 disables Poll()'ing all together. **Default:** 15000 (15 seconds).


*Socket Settings*

- **SocketKeepAlive** - Set `SocketOption.KeepAlive` on all future stream sockets. **Default:** false.

- **StaleDataCheck** - Check if there is stale (unrequested data) sitting on the socket or not. In some cases the control connection may time out but before the server closes the connection it might send a 4xx response that was unexpected and can cause synchronization errors with transactions. To avoid this problem the Execute() method checks to see if there is any data available on the socket before executing a command. **Default:** true.

- **EnableThreadSafeDataConnections** - Creates a new FTP connection for every file download and upload. This is slower but is a thread safe approach to make asynchronous operations on a single control connection transparent. Set this to `false` if your FTP server allows only one connection per username. [Learn more](#faq_etsdc)  **Default:** false.


### Utilities

Please import `FluentFTP` to use these extension methods, or access them directly under the `FtpExtensions` class.

- **GetFtpPath**(path) - Converts the specified local file/directory path into a valid FTP file system path

- **GetFtpPath**(path, segments) - Creates a valid FTP path by appending the specified segments to this string

- **GetFtpDirectoryName**(path) - Gets the parent directory path of the given file path

- **GetFtpFileName**(path) - Gets the file name and extension (if any) from the path

- **GetFtpDate**(date, styles) - Tries to convert the string FTP date representation into a date time object

- **FileSizeToString**(bytes) - Converts a file size in bytes to a string representation (eg. `12345` becomes `12.3 KB`)

Please access these static methods directly under the `FtpClient` class.

- **GetPublicIP**() - Use the Ipify service to calculate your public IP. Useful if you are behind a router or don't have a static IP.


### Logging

Please see these [FAQ entries](#faq_trace) for help on logging & debugging.

- client.**OnLogEvent** - A property of `FtpClient`. Assign this to a callback that will be fired every time a message is logged.

- FtpTrace.**LogFunctions** - Include high-level function calls in logs? **Default:** true.

- FtpTrace.**LogIP** - Include server IP addresses in logs? **Default:** true.

- FtpTrace.**LogUserName** - Include FTP usernames in logs? **Default:** true.

- FtpTrace.**LogPassword** - Include FTP passwords in logs? **Default:** false.

- FtpTrace.**LogPrefix** - Log all messages prefixed with "FluentFTP". **Default:** false.

- FtpTrace.**WriteLine** - Log a message or error to all registered listeners.

*.NET Standard only*

- FtpTrace.**LogToConsole** - Should FTP communication be be logged to the console? **Default:** false.

- FtpTrace.**LogToFile** - Set this to a file path to append all FTP communication to it. **Default:** false.

*.NET Framework only*

- FtpTrace.**FlushOnWrite** - Flush trace listeners after writing each command. **Default:** true.

- FtpTrace.**AddListener** - Add a logger to the system. [Learn more](#faq_trace)

- FtpTrace.**RemoveListener** - Remove a logger from the system.


## FTP Support

Mapping table documenting supported FTP commands and the corresponding API..

*Connection commands*

| Command  	    		| API						| Description                  	|
|---------------		|-----------				|---------------------------	|
| **USER, PASS**  		| Credentials				| Login with username & password|
| **QUIT**  			| Disconnect()				| Disconnect	|
| **PASV, EPSV, EPRT**  | DataConnectionType		| Passive & Active FTP modes	|
| **FEAT**  			| HasFeature()				| Get the features supported by server |
| **SYST**  			| GetSystem()				| Get the server system type 	|
| **OPTS UTF8 ON**  	| Encoding 					| Enables UTF-8 filenames	|
| **OPTS UTF8 OFF**  	| Encoding, DisableUTF8() 	| Disables UTF-8 filenames	|
| **AUTH TLS**  		| EncryptionMode			| Switch to TLS/FTPS 	|
| **PBSZ, PROT**  		| EncryptionMode and<br>DataConnectionEncryption | Configure TLS/FTPS connection 	|
| **CCC**				| PlainTextEncryption		| Switch to plaintext FTP |
| **PRET**      		| *Automatic* 				| Pre-transfer file information |
| **TYPE A**  			| UploadDataType and<br>DownloadDataType	| Transfer data in ASCII	|
| **TYPE I**  			| UploadDataType and<br>DownloadDataType 	| Transfer data in Binary	|

*File Management commands*

| Command      			| API					| Description                  	|
|---------------		|-----------			|---------------------------	|
| **MLSD**  			| GetListing()			| Get directory machine list 	|
| **LIST**  			| GetListing() with FtpListOption.ForceList		| Get directory file list 	|
| **NLST**  			| GetNameListing()<br>GetListing() with FtpListOption.ForceNameList	| Get directory name list 	|
| **MLST**				| GetObjectInfo()		| Get file information			|
| **DELE**      		| DeleteFile()			| Delete a file |
| **MKD**      			| CreateDirectory() 	| Create a directory |
| **RMD**      			| DeleteDirectory() 	| Delete a directory |
| **CWD**      			| SetWorkingDirectory() | Change the working directory |
| **PWD**      			| GetWorkingDirectory() | Get the working directory |
| **SIZE**      		| GetFileSize() 		| Get the filesize in bytes |
| **MDTM**   			| GetModifiedTime()<br>GetListing() with FtpListOption.Modify<br>GetObjectInfo() with dateModified | Get the file modified date  |
| **MFMT**   			| SetModifiedTime()		 | Modify file modified date  |
| **SITE CHMOD**      	| Chmod() or SetFilePermissions() | Modify file permissions |

*File Hashing commands*

| Command      			| API							| Description                  	|
|---------------		|-----------					|---------------------------	|
| **HASH**  			| GetHash() 					| Gets the hash of a file	|
| **OPTS HASH**  		| GetHashAlgorithm() / SetHashAlgorithm() | Selects a hash algorithm	for HASH command |
| **MD5**  				| GetChecksum() or GetMD5()		| Gets the MD5 hash of a file	|
| **XMD5**  			| GetChecksum() or GetXMD5()	| Gets the MD5 hash of a file	|
| **XSHA1**  			| GetChecksum() or GetXSHA1()	| Gets the SHA-1 hash of a file	|
| **XSHA256**  			| GetChecksum() or GetXSHA256()	| Gets the SHA-256 hash of a file	|
| **XSHA512**  			| GetChecksum() or GetXSHA512()	| Gets the SHA-512 hash of a file	|

## FAQ

<a name="faq_ftps"></a>
**How do I connect with SSL/TLS? / How do I use FTPS?**

Use this code:
```cs
FtpClient client = new FtpClient(hostname, username, password); // or set Host & Credentials
client.EncryptionMode = FtpEncryptionMode.Explicit;
client.SslProtocols = SslProtocols.Tls;
client.ValidateCertificate += new FtpSslValidation(OnValidateCertificate);
client.Connect();

void OnValidateCertificate(FtpClient control, FtpSslValidationEventArgs e) {
    // add logic to test if certificate is valid here
    e.Accept = true;
}
```

--------------------------------------------------------
<a name="faq_ftps"></a>
**How do I validate the server's certificate when using FTPS?**

First you must discover the string of the valid certificate. Use this code to save the valid certificate string to a file:
```cs
void OnValidateCertificate(FtpClient control, FtpSslValidationEventArgs e) {
    File.WriteAllText(@"C:\cert.txt", e.Certificate.GetRawCertDataString());
}
```
Then finally use this code to check if the received certificate matches the one you trust:
```cs
string ValidCert = "<insert contents of cert.txt>";
void OnValidateCertificate(FtpClient control, FtpSslValidationEventArgs e)  {
    if (e.PolicyErrors == SslPolicyErrors.None || e.Certificate.GetRawCertDataString() == ValidCert) {
        e.Accept = true;
    }else{
        throw new Exception("Invalid certificate : " + e.PolicyErrors);
    }
}
```

--------------------------------------------------------
<a name="faq_ccc"></a>
**How do I connect with FTPS and then switch back down to plaintext FTP?**

This is useful when you have a FTP firewall that requires plaintext FTP, but your server mandates FTPS connections. We use the CCC command to instruct the server to revert back to FTP.

Set this option before calling Connect() or any other method on the FtpClient class.

```cs
client.PlainTextEncryption = true;
```

--------------------------------------------------------
<a name="faq_sftp"></a>
**How do I connect with SFTP?**

SFTP is not supported as it is FTP over SSH, a completely different protocol. Use [SSH.NET](https://github.com/sshnet/SSH.NET) for that.


--------------------------------------------------------
<a name="faq_loginanon"></a>
**How do I login with an anonymous FTP account? / I'm getting login errors but I can login fine in Firefox/Filezilla**

Do NOT set the `Credentials` property, so we can login anonymously. Or you can manually specify the following:
```cs
client.Credentials = new NetworkCredential("anonymous", "anonymous");
```

--------------------------------------------------------
<a name="faq_loginproxy"></a>
**How do I login with an FTP proxy?**

Create a new instance of `FtpClientHttp11Proxy` or `FtpClientUserAtHostProxy` and use FTP properties/methods like normal.


--------------------------------------------------------
<a name="faq_progress"></a>
**How can I track the progress of file transfers?**

All of the [high-level methods](#highlevel) provide a `progress` argument that can be used to track upload/download progress.

First create and configure a `ProgressBar` such that the `Minimum` is 0 and `Maximum` is 100. Then create a callback to provide to the Upload/Download method. This will be called with a value, where 0 to 100 indicates the percentage transfered, and -1 indicates unknown progress.

```cs
Progress<double> progress = new Progress<double>(x => {
	// When progress in unknown, -1 will be sent
	if (x < 0){
		progressBar.IsIndeterminate = true;
	}else{
		progressBar.IsIndeterminate = false;
		progressBar.Value = x;
	}
});
```

Now call the Upload/Download method providing the new `progress` object that you just created.

*Using the asynchronous API:*
```cs
await client.DownloadFileAsync(localPath, remotePath, FtpLocalExists.Overwrite, FluentFTP.FtpVerify.Retry, progress);
```

*Using the synchronous API:*
```cs
client.DownloadFile(localPath, remotePath, FtpLocalExists.Overwrite, FluentFTP.FtpVerify.Retry, progress);
```

For .NET 2.0 users, pass an implementation of the `IProgress` class. The `Report()` method of the object you pass will be called with the progress value.


--------------------------------------------------------
<a name="faq_uploadbytes"></a>
**How can I upload data created on the fly?**

Use Upload() for uploading a `Stream` or `byte[]`.


--------------------------------------------------------
<a name="faq_downloadbytes"></a>
**How can I download data without saving it to disk?**

Use Download() for downloading to a `Stream` or `byte[]`.


--------------------------------------------------------
<a name="faq_resumedownload"></a>
**How can I resume downloading a file?**

Use DownloadFile() or DownloadFiles() with the `existsMode` set to `FtpLocalExists.Append`.

```cs
// download only the missing part of the file
// by comparing its file size to the size of the local file
client.DownloadFile(@"C:\MyVideo.mp4", "/htdocs/MyVideo.mp4", FtpLocalExists.Append);
```

Other options are:

- `FtpLocalExists.Skip` - If the local file exists, we blindly skip downloading it without any more checks.

- `FtpLocalExists.Overwrite` - If the local file exists, we restart the download and overwrite the file. 

- `FtpLocalExists.Append` - If the local file exists, we resume the download by checking the local file size, and append the missing data to the file.


--------------------------------------------------------
<a name="faq_uploadmissing"></a>
**How can I resume uploading a file?**

Using the new UploadFile() API:
```cs
// we compare the length of the offline file vs the online file,
// and only write the missing part to the server
client.UploadFile("C:\bigfile.iso", "/htdocs/bigfile.iso", FtpExists.Append);
```


--------------------------------------------------------
<a name="faq_throttle"></a>
**How can I throttle the speed of upload/download?**

Set the `UploadRateLimit` and `DownloadRateLimit` properties to control the speed of data transfer. Only honored by the [high-level API](#highlevel), for both the synchronous and async versions, such as:

- Upload() / Download()
- UploadFile() / DownloadFile()
- UploadFiles() / DownloadFiles()


--------------------------------------------------------
<a name="faq_verifyhash"></a>
**How do I verify the hash/checksum of a file and retry if the checksum mismatches?**

Add the `FtpVerify` options to UploadFile() or DownloadFile() to enable automatic checksum verification.
```cs
// retry 3 times when uploading a file
client.RetryAttempts = 3;

// upload a file and retry 3 times before giving up
client.UploadFile(@"C:\MyVideo.mp4", "/htdocs/MyVideo.mp4", FtpExists.Overwrite, false, FtpVerify.Retry);
```

All the possible configurations are:

- `FtpVerify.OnlyChecksum` - Verify checksum, return true/false based on success.

- `FtpVerify.Delete` - Verify checksum, delete target file if mismatch.

- `FtpVerify.Retry` - Verify checksum, retry copying X times and then give up.

- `FtpVerify.Retry | FtpVerify.Throw` - Verify checksum, retry copying X times, then throw an error if still mismatching.

- `FtpVerify.Retry | FtpVerify.Delete` - Verify checksum, retry copying X times, then delete target file if still mismatching.

- `FtpVerify.Retry | FtpVerify.Delete | FtpVerify.Throw` - Verify checksum, retry copying X times, delete target file if still mismatching, then throw an error


--------------------------------------------------------
<a name="faq_append"></a>
**How do I append to a file?**

Using the UploadFile() API:
```cs
// append data to an existing copy of the file
File.AppendAllText(@"C:\readme.txt", "text to be appended" + Environment.NewLine);

// only the new part of readme.txt will be written to the server
client.UploadFile("C:\readme.txt", "/htdocs/readme.txt", FtpExists.Append);
```

Using the stream-based OpenAppend() API:
```cs
using (FtpClient conn = new FtpClient()) {
	conn.Host = "localhost";
	conn.Credentials = new NetworkCredential("ftptest", "ftptest");
	
	using (Stream ostream = conn.OpenAppend("/full/or/relative/path/to/file")) {
		try {
			ostream.Position = ostream.Length;
			var sr = new StreamWriter(ostream);
			sr.WriteLine(...);
		}
		finally {
			ostream.Close();
			conn.GetReply(); // to read the success/failure response from the server
		}
	}
}
```


--------------------------------------------------------
<a name="faq_downloadlow"></a>
**How do I download files using the low-level API?**

Using the OpenRead() API:

```cs
// create remote FTP stream and local file stream
using (var remoteFileStream = client.OpenRead(remotePath, FtpDataType.Binary)){
	using (var newFileStream = File.Create(localPath)){
	
		// read 8KB at a time (you can increase this)
		byte[] buffer = new byte[8 * 1024];

		// download file to local stream
		int len;
		while ((len = remoteFileStream.Read(buffer, 0, buffer.Length)) > 0){
			newFileStream.Write(buffer, 0, len);
		}
	}
}

// read the FTP response and prevent stale data on the socket
client.GetReply();
```


--------------------------------------------------------
<a name="faq_utf"></a>
**How can I upload/download files with Unicode filenames when my server does not support UTF8?**

Set the connection encoding manually to ensure that special characters work properly.

The default codepage that you should use is `1252 Windows Western`. It has support for English + European characters (accented chars).

```cs
client.Encoding = System.Text.Encoding.GetEncoding(1252); // ANSI codepage 1252 (Windows Western)
```

Here is the full list of codepages based on the charset you need:

- 874 – English + Thai
- 1250 – English + Central Europe
- 1251 – English + Cyrillic (Russian)
- 1252 – English + European (accented characters)
- 1253 – English + Greek
- 1254 – English + Turkish
- 1255 – English + Hebrew
- 1256 – English + Arabic
- 1257 – English + Baltic
- 1258 – English + Vietnamese


--------------------------------------------------------
<a name="faq_listings"></a>
**How does GetListing() work internally?**

1. When you call `GetListing()`, FluentFTP first attempts to use **machine listings** (MLSD command) if they are supported by the server. These are most accurate and you can expect correct file size and modification date (UTC). You may also force this mode using `client.ListingParser = FtpParser.Machine`, and disable it with the `FtpListOption.ForceList` flag. You should also include the `FtpListOption.Modify` flag for the most accurate modification dates (down to the second). 

2. If machine listings are not supported we fallback to the appropriate **OS-specific parser** (LIST command), listed below. You may force usage of a specific parser using `client.ListingParser = FtpParser.*`.

   - **Unix** parser : Works for Pure-FTPd, ProFTPD, vsftpd, etc. If you encounter errors you can always try the alternate Unix parser using `client.ListingParser = FtpParser.UnixAlt`.
   
   - **Windows** parser : Works for IIS, DOS, Azure, FileZilla Server, etc.
   
   - **VMS** parser : Works for Vax, VMS, OpenVMS, etc.
   
   - **NonStop** parser : Works for Tandem, HP NonStop Guardian, etc.
   
   - **IBM** parser : Works for IBM OS/400, etc.

3. And if none of these satisfy you, you can fallback to **name listings** (NLST command), which are *much* slower than either LIST or MLSD. This is because NLST only sends a list of filenames, without any properties. The server has to be queried for the file size, modification date, and type (file/folder) on a file-by-file basis. Name listings can be forced using the `FtpListOption.ForceNameList` flag.


--------------------------------------------------------
<a name="faq_recursivelist"></a>
**How does GetListing() return a recursive file listing?**

In older versions of FluentFTP, we assumed that all servers supported recursive listings via the `LIST -R` command. However this caused numerous issues with various FTP servers that did not support recursive listings: The `GetListing()` call would simply return the contents of the first directory without any of the child directories included.

Therefore, since version 20.0.0, we try to detect the FTP server software and if we determine that it does not support recursive listing, we do our own manual recursion. We begin by assuming that all servers do not support recursive listing, and then whitelist specific server types.

If you feel that `GetListing()` is too slow when using recursive listings, and you know that your FTP server software supports the `LIST -R` command, then please contribute support for your server:

1. Add your FTP server type in the `FtpServer` enum.

2. Add code in `FtpClient.DetectFtpServer()` to detect your FTP server software.

3. Add code in `FtpClient.RecursiveList()` to return `true` for the detected server.



--------------------------------------------------------
<a name="faq_hashing"></a>
**What kind of hashing commands are supported?**

We support XCRC, XMD5, and XSHA which are non-standard commands and contain no kind of formal specification. They are not guaranteed to work and you are strongly encouraged to check the FtpClient.Capabilities flags for the respective flag (XCRC, XMD5, XSHA1, XSHA256, XSHA512) before calling these methods.

Support for the MD5 command as described [here](http://tools.ietf.org/html/draft-twine-ftpmd5-00#section-3.1) has also been added. Again, check for FtpFeature.MD5 before executing the command.

Support for the HASH command has been added to FluentFTP. It supports retrieving SHA-1, SHA-256, SHA-512, and MD5 hashes from servers that support this feature. The returned object, FtpHash, has a method to check the result against a given stream or local file. You can read more about HASH in [this draft](http://tools.ietf.org/html/draft-bryan-ftpext-hash-02).


--------------------------------------------------------
<a name="faq_trace"></a>
**How do I trace FTP commands for debugging?**

Do this at program startup (since its static it takes effect for all FtpClient instances.)

*.NET Framework version*
```cs
FtpTrace.AddListener(new ConsoleTraceListener());

FtpTrace.LogUserName = false; 	// hide FTP user names
FtpTrace.LogPassword = false; 	// hide FTP passwords
FtpTrace.LogIP = false; 	// hide FTP server IP addresses
```

*.NET Standard version*
```cs
FtpTrace.LogToConsole = true;

FtpTrace.LogUserName = false; 	// hide FTP user names
FtpTrace.LogPassword = false; 	// hide FTP passwords
FtpTrace.LogIP = false; 	// hide FTP server IP addresses
```

Alternatively you can hook onto `client.OnLogEvent` to get a callback every time a message is logged in the context of an individual `FtpClient` instance.



--------------------------------------------------------
<a name="faq_logfile"></a>
**How do I log all FTP commands to a file for debugging?**

Do this at program startup (since its static it takes effect for all FtpClient instances.)

*.NET Framework version*
```cs
FtpTrace.AddListener(new TextWriterTraceListener("log_file.txt"));

FtpTrace.LogUserName = false; 	// hide FTP user names
FtpTrace.LogPassword = false; 	// hide FTP passwords
FtpTrace.LogIP = false; 	// hide FTP server IP addresses
```

*.NET Standard version*
```cs
FtpTrace.LogToFile = "log_file.txt";

FtpTrace.LogUserName = false; 	// hide FTP user names
FtpTrace.LogPassword = false; 	// hide FTP passwords
FtpTrace.LogIP = false; 	// hide FTP server IP addresses
```


--------------------------------------------------------
<a name="faq_logfile2"></a>
**How do I log only critical errors to a file?**

This is the recommended configuration for a production server. Only supported in .NET Framework version.

Do this at program startup (since its static it takes effect for all FtpClient instances.)
```cs
FtpTrace.LogFunctions = false;
FtpTrace.AddListener(new TextWriterTraceListener("log_file.txt"){
	Filter = new EventTypeFilter(SourceLevels.Error)
});
```


--------------------------------------------------------
<a name="faq_logfunc"></a>
**How do I disable logging of function calls?**

Do this at program startup (since its static it takes effect for all FtpClient instances.)
```cs
FtpTrace.LogFunctions = false;
```


--------------------------------------------------------
<a name="faq_hidelog"></a>
**How do I omit sensitive information from the logs?**

Use these settings to control what data is included in the logs:
- `FtpTrace.LogUserName` - Log FTP user names?
- `FtpTrace.LogPassword` - Log FTP passwords?
- `FtpTrace.LogIP` - Log FTP server IP addresses?


--------------------------------------------------------
<a name="faq_log"></a>
**How do I use third-party logging frameworks like NLog?**

FluentFTP has a built-in [`TraceSource`](https://msdn.microsoft.com/en-us/library/system.diagnostics.tracesource(v=vs.110).aspx) named "FluentFTP" that can be used for debugging and logging purposes.  This is currently available for all .NET Framework versions except for .NET Standard.  Any implementation of [`TraceListener`](https://msdn.microsoft.com/en-us/library/system.diagnostics.tracelistener(v=vs.110).aspx) can be attached to the library either programmatically or via configuration in your app.config or web.config file.  This will allow for direct logging or forwarding to third-party logging frameworks.

Most tracing messages are of type `Verbose` or `Information` and can typically be ignored unless debugging.  Most ignored exceptions are classified as `Warning`, but methods that return boolean for success/failure will log the failure reasons with the `Error` level.  If you are using .NET Standard and the DEBUG flag is set, then all logging messages will be issued via `Debug.Write(message)`.

Attaching TraceListener in code:

```cs
TraceListener console = ConsoleTraceListener() {
	Filter = new EventTypeFilter(SourceLevels.Verbose | SourceLevels.ActivityTracking)
};

FtpTrace.AddListener(console);
```
Attaching via configuration file:

```xml
<system.diagnostics>
    <trace autoflush="true"></trace>
    <sources>
        <source name="FluentFTP">
	    <listeners>
	        <clear />
	        <!-- Attach a Console Listener -->
		<add name="console />
		<!-- Attach a File Trace Listener -->
		<add name="file" />
		<!-- Attach a Custom Listener -->
		<add name="myLogger" />
		<!--Attach NLog Trace Listener -->
		<add name="nlog" />	
	    </listeners>
	</source>
    </sources>
    <sharedListeners>
        <!--Define Console Listener -->
	<add name="console" type="System.Diagnostics.ConsoleTraceListener" />
	<!--Define File Listener -->
	<add name="file" type="System.Diagnostics.TextWriterTraceListener
	 initializeData="outputFile.log">
	    <!--Only write errors -->
	    <filter type="System.Diagnostics.EventTypeFilter" initializeData="Error" />
	</add>
	<!--Define Custom Listener -->
	<add name="custom" type="MyNamespace.MyCustomTraceListener />
	<!-- Define NLog Logger -->
	<add name="nlog" type="NLog.NLogTraceListener, NLog" />
    </sharedListeners>
</system.diagnostics>
```


--------------------------------------------------------
<a name="faq_etsdc"></a>
**What does `EnableThreadSafeDataConnections` do?**

EnableThreadSafeDataConnections is an older feature built by the original author. If true, it opens a new FTP client instance (and reconnects to the server) every time you try to upload/download a file. It used to be the default setting, but it affects performance terribly so I disabled it and found many issues were solved as well as performance was restored. I believe if devs want multi-threaded uploading they should just start a new BackgroundWorker and create/use FtpClient within that thread. Try that if you want concurrent uploading, it should work fine.


--------------------------------------------------------
<a name="faq_fork"></a>
**How can I contribute some changes to FluentFTP? / How do I submit a pull request?**

First you must "fork" FluentFTP, then make changes on your local version, then submit a "pull request" to request me to merge your changes. To do this:

1. Click **Fork** on the top right of this page
2. Open your version here : https://github.com/YOUR_GITHUB_USERNAME/FluentFTP
3. Download [Github Desktop](https://desktop.github.com/) and login to your account
4. Click **+** (top left) then **Clone** and select FluentFTP and click Clone/OK
5. Select a folder on your PC to place the files
6. Edit the files using any editor
7. Click **FluentFTP** on the list (left pane) in Github Desktop
8. Click **Changes** (top)
9. Type a Summary, and click **Commit** (bottom)
10. Click **Sync** (top right)
11. Open the [pull requests](https://github.com/hgupta9/FluentFTP/pulls) page to create a PR
12. Click **New pull request** (top right)
13. Click **compare across forks** (blue link, top right)
14. On the right "head fork" select the fork with your username
15. Click **Create pull request**
16. Summarize the changes you made in the title
17. Type details about the changes you made in the description
18. Click **Create pull request**
19. Thank you!



--------------------------------------------------------
<a name="faq_servertype"></a>
**How do I detect the type of server I'm connecting to?**

You can read `ServerType` to get the exact type of FTP server software that you've connected to. We dynamically detect the FTP server software based on the welcome message it sends when you've just connected to it. We can currently detect:

- PureFTPd
- VsFTPd
- ProFTPD
- WuFTPd
- FileZilla Server
- OpenVMS
- Windows Server/IIS
- Windows CE
- GlobalScape EFT
- HP NonStop/Tandem
- Serv-U
- Cerberus
- CrushFTP
- glFTPd

You can also read `ServerOS` to get the operating system of the FTP server you've connected to. We can detect:

- Windows
- Unix
- VMS
- IBM OS/400


--------------------------------------------------------
<a name="faq_certs"></a>
**How do I use client certificates to login with FTPS?**

Add your certificate into `ClientCertificates` and then `Connect()`.
```cs
client.EncryptionMode = FtpEncryptionMode.Explicit;
client.SslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;
client.SocketKeepAlive = false;
client.ClientCertificates.Add(new X509Certificate2("C:\mycert.cer"));
client.ValidateCertificate += (control, e) => {
	e.Accept = e.PolicyErrors == SslPolicyErrors.None;
};
client.Connect();
```

And ensure that:

1. You use `X509Certificate2` objects, not the incomplete `X509Certificate` implementation.

2. You do not use pem certificates, use p12 instead. See this [Stack Overflow thread](http://stackoverflow.com/questions/13697230/ssl-stream-failed-to-authenticate-as-client-in-apns-sharp) for more information. If you get SPPI exceptions with an inner exception about an unexpected or badly formatted message, you are probably using the wrong type of certificate.



--------------------------------------------------------
<a name="faq_x509"></a>
**How do I bundle an X509 certificate from a file?**

You need the certificate added into your local store, and then do something like this:

```cs
FluentFTP.FtpClient client = new FluentFTP.FtpClient("WWW.MYSITE.COM", "USER","PASS");

// Select certificate and add to client
X509Store store = new X509Store("MY", StoreLocation.LocalMachine);
store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
X509Certificate2Collection collection = (X509Certificate2Collection)store.Certificates;
X509Certificate2Collection fcollection = (X509Certificate2Collection)collection.Find(X509FindType.FindByTimeValid, DateTime.Now, false);
X509Certificate2Collection scollection = X509Certificate2UI.SelectFromCollection(fcollection, "Select a certificate", "Select a certificate", X509SelectionFlag.MultiSelection); 

if (scollection.Count != 1)
{
    throw new Exception("Error: You have not chosen exactly one certificate");
 }
foreach (X509Certificate2 x509 in scollection)
{
    client.ClientCertificates.Add(x509);
}
store.Close();

//client.ReadTimeout = 10000;
client.Connect();
```

This is another way. And use X509Certificate2. I've been unable to get X509Certificate to work and from my reading it's because it's an incomplete implementation.

```cs
public void InitSFTP(){

    FluentFTP.FtpClient client = new FluentFTP.FtpClient("WWW.MYSITE.COM", "USER", "PASS");
    X509Certificate2 cert_grt = new X509Certificate2("C:\mycert.xyz"); 
    conn.EncryptionMode = FtpEncryptionMode.Explicit; 
    conn.DataConnectionType = FtpDataConnectionType.PASV; 
    conn.DataConnectionEncryption = true; 
    conn.ClientCertificates.Add(cert_grt); 
    conn.ValidateCertificate += new FtpSslValidation(OnValidateCertificate); 
    conn.Connect();
}       

private void OnValidateCertificate(FtpClient control, FtpSslValidationEventArgs e)
{
    e.Accept = true;
}
```

## Troubleshooting

<a name="trouble_install"></a>
**FluentFTP fails to install in Visual Studio 2010 (VS2010) > 'System.Runtime' already has a dependency defined for 'FluentFTP'.**

Your VS has an older version of `nuget.exe` so it cannot properly install the latest FluentFTP. You must download nuget.exe` manually and run these commands:

> cd D:\Projects\MyProjectDir\
> C:\Nuget\nuget.exe install FluentFTP


--------------------------------------------------------
<a name="trouble_specialchars"></a>
**After uploading a file with special characters like "Caffè.png" it appears as "Caff?.bmp" on the FTP server. The server supports only ASCII but "è" is ASCII. FileZilla can upload this file without problems.**

Set the connection encoding manually to ensure that special characters work properly.

The default codepage that you should use is `1252 Windows Western`. It has support for English + European characters (accented characters).

```cs
client.Encoding = System.Text.Encoding.GetEncoding(1252); // ANSI codepage 1252 (Windows Western)
```


--------------------------------------------------------
<a name="trouble_specialchars2"></a>
**I cannot delete a file if the filename contains Russian letters. FileZilla can delete this file without problems.**

Set the connection encoding manually to ensure that special characters work properly.

For Russian you need to use the codepage [`1251 Windows Cyrillic`](https://en.wikipedia.org/wiki/Code_page#Windows_code_pages)

```cs
client.Encoding = System.Text.Encoding.GetEncoding(1251); // ANSI codepage 1251 (Windows Cyrillic)
```


--------------------------------------------------------
<a name="trouble_azure"></a>
**I keep getting TimeoutException's in my Azure WebApp**

First try reducing the socket polling interval, which Azure needs.
```cs
client.SocketPollInterval = 1000;
```

If that doesn't work then try reducing the timeouts too.
```cs
client.SocketPollInterval = 1000;
client.ConnectTimeout = 2000;
client.ReadTimeout = 2000;
client.DataConnectionConnectTimeout = 2000;
client.DataConnectionReadTimeout = 2000;
```

If none of these work, remember that Azure has in intermittent bug wherein it changes the IP-address during a FTP request. The connection is established with IP-address A and for the data transfer Azure uses IP-address B and this isn't allowed on many firewalls. This is a known Azure bug.



--------------------------------------------------------
<a name="trouble_windowsce"></a>
**Many commands don't work on Windows CE**

According to [this](https://msdn.microsoft.com/en-us/library/ms881872.aspx) from MSDN the Windows CE implementation of FTP is the bare minimum, and open to customization via source code. Many advanced commands such as CHMOD are unsupported.



--------------------------------------------------------
<a name="trouble_getreply"></a>
**After successfully transfering a single file with OpenWrite/OpenAppend, the subsequent files fail with some random error, like "Malformed PASV response"**

You need to call `FtpReply status = GetReply()` after you finish transfering a file to ensure no stale data is left over, which can mess up subsequent commands.


--------------------------------------------------------
<a name="trouble_ssl"></a>
**SSL Negotiation is very slow during FTPS login**

FluentFTP uses `SslStream` under the hood which is part of the .NET framework. `SslStream` uses a feature of windows for updating root CA's on the fly, which can cause a long delay in the certificate authentication process. This can cause issues in FluentFTP related to the `SocketPollInterval` property used for checking for ungraceful disconnections between the client and server. This [MSDN Blog](http://blogs.msdn.com/b/alejacma/archive/2011/09/27/big-delay-when-calling-sslstream-authenticateasclient.aspx) covers the issue with `SslStream` and talks about how to disable the auto-updating of the root CA's.

FluentFTP logs the time it takes to authenticate. If you think you are suffering from this problem then have a look at Examples\Debug.cs for information on retrieving debug information.


--------------------------------------------------------
<a name="trouble_closedhost"></a>
**Unable to read data from the transport connection : An existing connection was forcibly closed by the remote host**

This means that on the server the [FTP daemon] service isn't running (probably not the case) or the service is currently still busy performing another operation. It almost sounds like the server is returning a message indicating it is still performing the last operation.

Try reducing the polling interval to ensure that the connection does not time-out.

```cs
client.SocketPollInterval = 1000;
```
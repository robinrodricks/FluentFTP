
# FluentFTP

[![Version](https://img.shields.io/nuget/vpre/FluentFTP.svg)](https://www.nuget.org/packages/FluentFTP)

FluentFTP is a fully managed FTP and FTPS library for .NET & .NET Standard, optimized for speed. It provides extensive FTP commands, File uploads/downloads, SSL/TLS connections, Automatic directory listing parsing, File hashing/checksums, File permissions/CHMOD, FTP proxies, UTF-8 support, Async/await support and more.

It is written entirely in C#, with no external dependencies. FluentFTP is released under the permissive MIT License, so it can be used in both proprietary and free/open source applications.

## Features

- Full support for [FTP](#ftp-support), [FTPS](#faq_ftps) (FTP over SSL), [FTPS with client certificates](#faq_certs) and [FTPS with CCC](#faq_ccc) (for FTP firewalls)
- **File management:**
  - File and directory listing for [all major server types](#faq_listings) (Unix, Windows/IIS, Azure, Pure-FTPd, ProFTPD, Vax, VMS, OpenVMS, Tandem, HP NonStop Guardian, IBM OS/400, AS400, Windows CE, etc)
  - Easily upload and download a file from the server
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

| Platform      		| Binaries Folder	| Solution                  	|
|---------------		|-----------		|---------------------------	|
| **.NET 2.0**      	| net20     		| FluentFTP_NET_VS2012.sln  	|
| **.NET 3.5**      	| net35     		| FluentFTP_NET_VS2012.sln  	|
| **.NET 4.0**      	| net40     		| FluentFTP_NET_VS2012.sln  	|
| **.NET 4.5**      	| net45     		| FluentFTP_NET_VS2012.sln  	|
| **.NET Standard 1.4** | netstandard1.4	| FluentFTP_Core14_VS2017.sln 	|
| **.NET Standard 1.6** | netstandard1.6	| FluentFTP_Core16_VS2017.sln 	|
| **.NET Core 5.0** 	| dnxcore50 		| FluentFTP_Core16_VS2017.sln 	|

FluentFTP is also supported on these platforms: (via .NET Standard)

- **Mono** 4.6
- **Xamarin.iOS** 10.0
- **Xamarin.Android** 10.0
- **Universal Windows Platform** 10.0

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
client.UploadFile(@"C:\MyVideo.mp4", "/htdocs/big.txt");

// rename the uploaded file
client.Rename("/htdocs/big.txt", "/htdocs/big2.txt");

// download the file again
client.DownloadFile(@"C:\MyVideo_2.mp4", "/htdocs/big2.txt");

// delete the file
client.DeleteFile("/htdocs/big2.txt");

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
- [Release Notes](#release-notes)
- [Misc Notes](#notes)
- [Credits](#credits)

## FAQs

**Connection FAQs**
- [How do I connect with SSL/TLS? / How do I use FTPS?](#faq_ftps)
- [How do I validate the server's certificate when using FTPS?](#faq_ftps)
- [How do I connect with FTPS and then switch back down to plaintext FTP?](#faq_ccc)
- [How do I connect with SFTP?](#faq_sftp)
- [How do I login with an anonymous FTP account?](#faq_loginanon)
- [How do I login with an FTP proxy?](#faq_loginproxy)
- [How do I use client certificates to login with FTPS?](#faq_certs)
- [How do I bundle an X509 certificate from a file?](#faq_x509)

**File Transfer FAQs**
- [How can I upload data created on the fly?](#faq_uploadbytes)
- [How can I download data without saving it to disk?](#faq_downloadbytes)
- [How can I throttle the speed of upload/download?](#faq_throttle)
- [How do I verify the hash/checksum of a file and retry if the checksum mismatches?](#faq_verifyhash)
- [How do I upload only the missing part of a file?](#faq_uploadmissing)
- [How do I append to a file?](#faq_append)
- [How do I download files using the low-level API?](#faq_downloadlow)
- [How can I upload/download files with Unicode filenames when my server does not support UTF8?](#faq_utf)

**File Management FAQs**
- [How does GetListing() work internally?](#faq_listings)
- [What kind of hashing commands are supported?](#faq_hashing)

**Logging FAQs**
- [How do I trace FTP commands for debugging?](#faq_trace)
- [How do I log all FTP commands to a file for debugging?](#faq_logfile)
- [How do I log only critical errors to a file?](#faq_logfile2)
- [How do I disable logging of function calls?](#faq_logfunc)
- [How do I omit sensitive information from the logs?](#faq_hidelog)
- [How do I use third-party logging frameworks like NLog?](#faq_log)

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

- **IsConnected** - Checks if the connection is still alive.

- **Capabilities** - Gets the server capabilties (represented by flags).

- **HasFeature**() - Checks if a specific feature (`FtpCapability`) is supported by the server.


### Directory Listing

- **GetListing**() - Get a [file listing](#faq_listings) of the given directory. Returns one `FtpListItem` per file or folder with all available properties set. Each item contains:

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

	- `RawPermissions` : The raw permissions string recieved for this object. Use this if other permission properties are blank or invalid.

	- `Input` : The raw string that the server returned for this object. Helps debug if the above properties have been correctly parsed.
	
- **GetNameListing**() - A simple command that only returns the list of file paths in the given directory, using the NLST command.

- **GetObjectInfo()** - Get information for a single file or directory as an `FtpListItem`. It includes the type, date created, date modified, file size, permissions/chmod and link target (if any).


### File Transfer

<a name="highlevel"></a>
*High-level API:*

- **Upload**() - Uploads a Stream or byte[] to the server. Returns true if succeeded, false if failed or file does not exist. Exceptions are thrown for critical errors. Supports very large files since it uploads data in chunks.

- **Download**() - Downloads a file from the server to a Stream or byte[]. Returns true if succeeded, false if failed or file does not exist. Exceptions are thrown for critical errors. Supports very large files since it downloads data in chunks.

- **UploadFile**() - Uploads a file from the local file system to the server. Use `FtpExists.Append` to append to a file. Returns true if succeeded, false if failed or file does not exist. Exceptions are thrown for critical errors. Supports very large files since it uploads data in chunks. Optionally [verifies the hash](#faq_verifyhash) of a file & retries transfer if hash mismatches.

- **DownloadFile**() - Downloads a file from the server to the local file system. Returns true if succeeded, false if failed or file does not exist. Exceptions are thrown for critical errors. Supports very large files since it downloads data in chunks. Local directories are created if they do not exist. Optionally [verifies the hash](#faq_verifyhash) of a file & retries transfer if hash mismatches.

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

- **GetModifiedTime**() - Gets the last modified date/time of the file or folder.

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

- **RecursiveList** - Check if your server supports a recursive LIST command (`LIST -R`). If you know for sure that this is unsupported, set it to false. **Default:** true.

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

- **ReadTimeout** - Time to wait (in milliseconds) for data to be read from the underlying stream, before giving up. **Default:** 15000 (15 seconds).

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

- FtpTrace.**LogFunctions** - Log all high-level function calls. **Default:** true.

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

<a name="faq_ftps"></a>
**How do I validate the server's certificate when using FTPS?**

First you must discover the string of the valid certificate. Use this code to save the the valid certificate string to a file:
```cs
void OnValidateCertificate(FtpClient control, FtpSslValidationEventArgs e) {
    File.WriteAllText(@"C:\cert.txt", e.Certificate.GetRawCertDataString());
}
```
Then finally use this code to check if the recieved certificate matches the one you trust:
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

<a name="faq_ccc"></a>
**How do I connect with FTPS and then switch back down to plaintext FTP?**

This is useful when you have a FTP firewall that requires plaintext FTP, but your server mandates FTPS connections. We use the CCC command to instruct the server to revert back to FTP.

Set this option before calling Connect() or any other method on the FtpClient class.

```cs
client.PlainTextEncryption = true;
```


<a name="faq_sftp"></a>
**How do I connect with SFTP?**

SFTP is not supported as it is FTP over SSH, a completely different protocol. Use [SSH.NET](https://github.com/sshnet/SSH.NET) for that.


<a name="faq_loginanon"></a>
**How do I login with an anonymous FTP account? / I'm getting login errors but I can login fine in Firefox/Filezilla**

Do NOT set the `Credentials` property, so we can login anonymously. Or you can manually specify the following:
```cs
client.Credentials = new NetworkCredential("anonymous", "anonymous");
```

<a name="faq_loginproxy"></a>
**How do I login with an FTP proxy?**

Create a new instance of `FtpClientHttp11Proxy` or `FtpClientUserAtHostProxy` and use FTP properties/methods like normal.


<a name="faq_uploadbytes"></a>
**How can I upload data created on the fly?**

Use Upload() for uploading a `Stream` or `byte[]`.


<a name="faq_downloadbytes"></a>
**How can I download data without saving it to disk?**

Use Download() for downloading to a `Stream` or `byte[]`.


<a name="faq_throttle"></a>
**How can I throttle the speed of upload/download?**

Set the `UploadRateLimit` and `DownloadRateLimit` properties to control the speed of data transfer. Only honored by the [high-level API](#highlevel), for both the synchronous and async versions, such as:

- Upload() / Download()
- UploadFile() / DownloadFile()
- UploadFiles() / DownloadFiles()


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


<a name="faq_uploadmissing"></a>
**How do I upload only the missing part of a file?**

Using the new UploadFile() API:
```cs
// we compare the length of the offline file vs the online file,
// and only write the missing part to the server
client.UploadFile("C:\bigfile.iso", "/htdocs/bigfile.iso", FtpExists.Append);
```

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


<a name="faq_hashing"></a>
**What kind of hashing commands are supported?**

We support XCRC, XMD5, and XSHA which are non-standard commands and contain no kind of formal specification. They are not guaranteed to work and you are strongly encouraged to check the FtpClient.Capabilities flags for the respective flag (XCRC, XMD5, XSHA1, XSHA256, XSHA512) before calling these methods.

Support for the MD5 command as described [here](http://tools.ietf.org/html/draft-twine-ftpmd5-00#section-3.1) has also been added. Again, check for FtpFeature.MD5 before executing the command.

Support for the HASH command has been added to FluentFTP. It supports retrieving SHA-1, SHA-256, SHA-512, and MD5 hashes from servers that support this feature. The returned object, FtpHash, has a method to check the result against a given stream or local file. You can read more about HASH in [this draft](http://tools.ietf.org/html/draft-bryan-ftpext-hash-02).


<a name="faq_trace"></a>
**How do I trace FTP commands for debugging?**

Do this at program startup (since its static it takes effect for all FtpClient instances.)

*.NET Framework version*
```cs
FtpTrace.AddListener(new ConsoleTraceListener());
```

*.NET Standard version*
```cs
FtpTrace.LogToConsole = true;
```


<a name="faq_logfile"></a>
**How do I log all FTP commands to a file for debugging?**

Do this at program startup (since its static it takes effect for all FtpClient instances.)

*.NET Framework version*
```cs
FtpTrace.AddListener(new TextWriterTraceListener("log_file.txt"));
```

*.NET Standard version*
```cs
FtpTrace.LogToFile = "log_file.txt";
```


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


<a name="faq_logfunc"></a>
**How do I disable logging of function calls?**

Do this at program startup (since its static it takes effect for all FtpClient instances.)
```cs
FtpTrace.LogFunctions = false;
```


<a name="faq_hidelog"></a>
**How do I omit sensitive information from the logs?**

Use these settings to control what data is included in the logs:
- `FtpTrace.LogUserName` - Log FTP user names?
- `FtpTrace.LogPassword` - Log FTP passwords?
- `FtpTrace.LogIP` - Log FTP server IP addresses?


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


<a name="faq_etsdc"></a>
**What does `EnableThreadSafeDataConnections` do?**

EnableThreadSafeDataConnections is an older feature built by the original author. If true, it opens a new FTP client instance (and reconnects to the server) every time you try to upload/download a file. It used to be the default setting, but it affects performance terribly so I disabled it and found many issues were solved as well as performance was restored. I believe if devs want multi-threaded uploading they should just start a new BackgroundWorker and create/use FtpClient within that thread. Try that if you want concurrent uploading, it should work fine.


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

<a name="faq_certs"></a>
**How do I use client certificates to login with FTPS?**

When you are using Client Certificates, be sure that:

1. You use `X509Certificate2` objects, not the incomplete `X509Certificate` implementation.

2. You do not use pem certificates, use p12 instead. See this [Stack Overflow thread](http://stackoverflow.com/questions/13697230/ssl-stream-failed-to-authenticate-as-client-in-apns-sharp) for more information. If you get SPPI exceptions with an inner exception about an unexpected or badly formatted message, you are probably using the wrong type of certificate.


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

<a name="trouble_specialchars"></a>
**After uploading a file with special characters like "Caffè.png" it appears as "Caff?.bmp" on the FTP server. The server supports only ASCII but "è" is ASCII. FileZilla can upload this file without problems.**

Set the connection encoding manually to ensure that special characters work properly.

The default codepage that you should use is `1252 Windows Western`. It has support for English + European characters (accented characters).

```cs
client.Encoding = System.Text.Encoding.GetEncoding(1252); // ANSI codepage 1252 (Windows Western)
```

<a name="trouble_specialchars2"></a>
**I cannot delete a file if the filename contains Russian letters. FileZilla can delete this file without problems.**

Set the connection encoding manually to ensure that special characters work properly.

For Russian you need to use the codepage [`1251 Windows Cyrillic`](https://en.wikipedia.org/wiki/Code_page#Windows_code_pages)

```cs
client.Encoding = System.Text.Encoding.GetEncoding(1251); // ANSI codepage 1251 (Windows Cyrillic)
```

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


<a name="trouble_windowsce"></a>
**Many commands don't work on Windows CE**

According to [this](https://msdn.microsoft.com/en-us/library/ms881872.aspx) from MSDN the Windows CE implementation of FTP is the bare minimum, and open to customization via source code. Many advanced commands such as CHMOD are unsupported.


<a name="trouble_getreply"></a>
**After successfully transfering a single file with OpenWrite/OpenAppend, the subsequent files fail with some random error, like "Malformed PASV response"**

You need to call `FtpReply status = GetReply()` after you finish transfering a file to ensure no stale data is left over, which can mess up subsequent commands.


<a name="trouble_ssl"></a>
**SSL Negotiation is very slow during FTPS login**

FluentFTP uses `SslStream` under the hood which is part of the .NET framework. `SslStream` uses a feature of windows for updating root CA's on the fly, which can cause a long delay in the certificate authentication process. This can cause issues in FluentFTP related to the `SocketPollInterval` property used for checking for ungraceful disconnections between the client and server. This [MSDN Blog](http://blogs.msdn.com/b/alejacma/archive/2011/09/27/big-delay-when-calling-sslstream-authenticateasclient.aspx) covers the issue with `SslStream` and talks about how to disable the auto-updating of the root CA's.

FluentFTP logs the time it takes to authenticate. If you think you are suffering from this problem then have a look at Examples\Debug.cs for information on retrieving debug information.


<a name="trouble_closedhost"></a>
**Unable to read data from the transport connection : An existing connection was forcibly closed by the remote host**

This means that on the server the [FTP daemon] service isn't running (probably not the case) or the service is currently still busy performing another operation. It almost sounds like the server is returning a message indicating it is still performing the last operation.

Try reducing the polling interval to ensure that the connection does not time-out.

```cs
client.SocketPollInterval = 1000;
```

## Notes

### Stream Handling

FluentFTP returns a `Stream` object for file transfers. This stream **must** be properly closed when you are done. Do not leave it for the GC to cleanup otherwise you can end up with uncatchable exceptions, i.e., a program crash. The stream objects are actually wrappers around `NetworkStream` and `SslStream` which perform cleanup routines on the control connection when the stream is closed. These cleanup routines can trigger exceptions so it's vital that you properly dispose the objects when you are done, no matter what. A proper implementation should go along the lines of:

``````
try {
   using(Stream s = ftpClient.OpenRead()) {
       // perform your transfer
   }
   ftpClient.GetReply() // read success/failure messages from server
}
catch(Exception) {
   // Typical exceptions here are IOException, SocketException, or a FtpCommandException
}
``````

The using statement above will ensure that `Dispose()` is called on the stream which in turn will call `Close()` so that the necessary cleanup routines on the control connection can be performed. If an exception is triggered you will have a chance to catch and handle it. Another valid approach might look like so:

``````
Stream s = null;

try {
	s = ftpClient.OpenRead();
	// perform transfer
}
finally {
	if(s != null)
		s.Close()
	ftpClient.GetReply() // read success/failure messages from server
}
``````

The finally block above ensures that `Close()` is always called on the stream even if a problem occurs. When `Close()` is called any resulting exceptions can be caught and handled accordingly.

### Exception Handling

FluentFTP includes exception handling in key places where uncatchable exceptions could occur, such as the `Dispose()` methods. The problem is that part of the cleanup process involves closing out the internal sockets and streams. If `Dispose()` was called because of an exception and triggers another exception while trying to clean-up you could end up with an un-catchable exception resulting in an application crash. To deal with this `FtpClient.Dispose()` and `FtpSocketStream.Dispose()` are setup to handle `SocketException` and `IOException` and discard them. The exceptions are written to the FtpTrace `TraceListeners` for debugging purposes, in an effort to not hide important errors while debugging problems with the code.

The exception that propagates back to your code should be the root of the problem and any exception caught while disposing would be a side affect however while testing your project pay close attention to what's being logged via FtpTrace. See the Debugging example for more information about using `TraceListener` objects with FluentFTP.

### Handling Ungraceful Interruptions in the Control Connection

FluentFTP uses `Socket.Poll()` to test for connectivity after a user-definable period of time has passed since the last activity on the control connection. When the remote host closes the connection there is no way to know, without triggering an exception, other than using `Poll()` to make an educated guess. When the connectivity test fails the connection is automatically re-established. This process helps a great deal in gracefully reconnecting however it does not eliminate your responsibility for catching IOExceptions related to an ungraceful interruption in the connection. Usually, maybe always, when this occurs the InnerException will be a SocketException. How you want to handle the situation from there is up to you.

```````
try {
    // ftpClient.SomeMethod();
}
catch(IOException e) {
    if(e.InnertException is SocketException) {
         // the control connection was interrupted
    }
}
```````

### Pipelining

If you just wanting to enable pipelining (in `FtpClient` and `FtpControlConnection`), set the `EnablePipelining` property to true. Hopefully this is all you need but it may not be. Some servers will drop the control connection if you flood it with a lot of commands. This is where the `MaxPipelineExecute` property comes into play. The default value here is 20, meaning that if you have 100 commands queued, 20 of the commands will be written to the underlying socket and 20 responses will be read, then the next 20 will be executed, and so forth until the command queue is empty. The value 20 is not a magic number, it's just the number that I deemed stable in most scenarios. If you increase the value, do so knowing that it could break your control connection.

### Pipelining your own Commands

Pipelining your own commands is not dependent on the `EnablePipelining` feature. The `EnablePipelining` property only applies to internal pipelining performed by FtpClient and FtpControlConnection. You can use the facilities for creating pipelines at your own discretion. 

If you need to cancel your pipeline in the middle of building your queue, you use the `CancelPipeline()` method. These methods are implemented in the `FtpControlConnection` class so people that are extending this class also have access to them. This feature is also used in `FtpClient.GetListing()` to retrieve last write times of the files in the listing when the LIST command is used. 

You don't need to worry about locking the command channel (`LockControlConnection()` or `UnlockControlConnection()`) because the code that handles executing the pipeline does so for you.

Here's a quick example:

`````
FtpClient cl = new FtpClient();

...

// initalize the pipeline
cl.BeginExecute();

// execute commands as normal
cl.Execute("foo");
cl.Execute("bar");
cl.Execute("baz");

...

// execute the queued commands
FtpCommandResult[] res = cl.EndExecute();

// check the result status of the commands
foreach(FtpCommandResult r in res) {
	if(!r.ResponseStatus) {
          // we have a failure
	}
}
``````

### Bulk Downloads

When doing a large number of transfers, one needs to be aware of some inherit issues with data streams. When a socket is opened and then closed, the socket is left in a linger state for a period of time defined by the operating system. The socket cannot reliably be re-used until the operating system takes it out of the TIME WAIT state. This matters because a data stream is opened when it's needed and closed as soon as that specific task is done:
- Download File
  - Open Data Stream
    - Read bytes
  - Close Data Stream

This is not a bug in FluentFTP. RFC959 says that EOF on stream mode transfers is signaled by closing the connection. On downloads and file listings, the sockets being used on the server will stay in the TIME WAIT state because the server closes the socket when it's done sending the data. On uploads, the client sockets will go into the TIME WAIT state because the client closes the connection to signal EOF to the server.

## Release Notes

#### 17.6.1
- Fix for CreateDirectory and DirectoryExists to allow null/blank input path values
- Fix for GetFtpDirectoryName to return correct parent folder of simple folder paths (thanks [ww898](https://github.com/ww898))

#### 17.6.0
- Add argument validation for missing/blank arguments in : Upload, Download, UploadFile(s), DownloadFile(s), GetObjectInfo, DeleteFile, DeleteDirectory, FileExists, DirectoryExists, CreateDirectory, Rename, MoveFile, MoveDirectory, SetFilePermissions, Chmod, GetFilePermissions, GetChmod, GetFileSize, GetModifiedTime, VerifyTransfer, OpenRead, OpenWrite, OpenAppend
- Disable all async methods on .NET core due to persistant PlatformUnsupported exception (if you need async you are free to contribute a non-blocking version of the methods)

#### 17.5.9
- Increase performance of GetListing by reading multiple lines at once (BulkListing property, thanks [sierrodc](https://github.com/sierrodc))

#### 17.5.8
- Add support for parsing AS400 listings inside a file (5 fields) (thanks [rharrisxtheta](https://github.com/rharrisxtheta))
- Retry interpreting file listings after encountered invalid date format (thanks [rharrisxtheta](https://github.com/rharrisxtheta))
- Always switch into binary mode when running SIZE command (thanks [rharrisxtheta](https://github.com/rharrisxtheta))

#### 17.5.7
- Honor UploadDataType and DownloadDataType in all sync/async cases (thanks [rharrisxtheta](https://github.com/rharrisxtheta))
- Force file transfers in BINARY mode for known 0 byte files (thanks [rharrisxtheta](https://github.com/rharrisxtheta))
- Allow file transfers in ASCII mode if the server doesn't support the SIZE command (thanks [rharrisxtheta](https://github.com/rharrisxtheta))

#### 17.5.6
- Fix NullReferenceException when arguments are null during FtpTrace.WriteFunc

#### 17.5.5
- Remove internal locking for .NET Standard 1.4 version since unsupported on UWP

#### 17.5.4
- Remove dependency on System.Threading.Thread for .NET Standard 1.4 version (for UWP)

#### 17.5.3
- Allow transferring files in ASCII/Binary mode with the high-level API (UploadDataType, DownloadDataType)

#### 17.5.2
- Add support for .NET 3.5 and .NET Standard 1.4 (supports Universal Windows Platform 10.0)

#### 17.5.1
- Add FtpTrace.LogToConsole and LogToFile to control logging in .NET core version

#### 17.5.0
- Add PlainTextEncryption API to support FTPS servers and plain-text FTP firewalls (CCC command)
- FluentFTP now uses unsafe code to support the CCC command (inside FtpSslStream)
- If you need a "non unsafe" version of the library please add an issue

#### 17.4.4
- Add logging for high-level function calls to improve remote debugging (FtpTrace.LogFunctions)
- Add settings to hide sensitive data from logs (FtpTrace.LogIP, LogUserName, LogPassword)
- Add RecursiveList to control if recursive listing should be used
- Auto-detect Windows CE and disable recursive listing during DeleteDirectory()

#### 17.4.2
- Add UploadRateLimit and DownloadRateLimit to control the speed of data transfer (thanks [Danie-Brink](https://github.com/Danie-Brink))

#### 17.4.1
- Fix parsing of LinkTarget during GetListing() on Unix FTP servers
- Improve logging clarity by removing "FluentFTP" prefix in TraceSource

#### 17.4.0
- Add MoveFile() and MoveDirectory() to move files and directories safely

#### 17.3.0
- Automatically verify checksum of a file after upload/download (thanks [jblacker](https://github.com/jblacker))
- Configurable error handling (abort/throw/ignore) for file transfers (thanks [jblacker](https://github.com/jblacker))
- Multiple log levels for tracing/logging debug output in FtpTrace (thanks [jblacker](https://github.com/jblacker))

#### 17.2.0
- Simplify DeleteDirectory() API - the `force` and `fastMode` args are no longer required
- DeleteDirectory() is faster since it uses one recursive file listing instead of many
- Remove .NET Standard 1.4 to improve nuget update reliability, since we need 1.6 anyway

#### 17.1.0
- Split stream API into Upload()/UploadFile() and Download()/DownloadFile()

#### 17.0.0
- Greatly improve performance of FileExists() and GetNameListing()
- Add new OS-specific directory listing parsers to GetListing() and GetObjectInfo()
- Support GetObjectInfo() even if machine listings are not supported by the server
- Add `existsMode` to UploadFile() and UploadFiles() allowing for skip/overwrite and append
- Remove all usages of string.Format to fix reliability issues caused with UTF filenames
- Fix issue of broken files when uploading/downloading through a proxy (thanks [Zoltan666](https://github.com/Zoltan666))
- GetReply() is now public so users of OpenRead/OpenAppend/OpenWrite can call it after

#### 16.5.0
- Add async/await support to all methods for .NET 4.5 and onwards (thanks [jblacker](https://github.com/jblacker))

#### 16.4.0
- Support for .NET Standard 1.4 added.

#### 16.2.5
- Add UploadFiles() and DownloadFiles() which is faster than single file transfers
- Allow disabling UTF mode using DisableUTF8 API

#### 16.2.4
- First .NET Core release (DNXCore5.0) using Visual Studio 2017 project and shared codebase.
- Support for .NET 2.0 also added with shims for LINQ commands needed.

#### 16.2.1
- Add FtpListOption.IncludeSelfAndParent to GetListing()

#### 16.1.0
- Use streams during upload/download of files to improve performance with large files

#### 16.0.18
- Support for uploading/downloading to Streams and byte[] with UploadFile() and DownloadFile()

#### 16.0.17
- Added high-level UploadFile() and DownloadFile() API. Fixed some race conditions.

#### 16.0.14
- Added support for FTP proxies using HTTP 1.1 and User@Host modes. (thanks [L3Z4](https://github.com/L3Z4))

## Credits

- [J.P. Trosclair](https://github.com/jptrosclair) - Original creator, owner upto 2016
- [Harsh Gupta](https://github.com/hgupta9) - Owner and maintainer from 2016 onwards
- [Jordan Blacker](https://github.com/jblacker) - `async`/`await` support for all methods, post-transfer hash verification, configurable error handling, multiple log levels
- [Atif Aziz](https://github.com/atifaziz) & Joseph Albahari - LINQBridge (allows LINQ in .NET 2.0)
- [R. Harris](https://github.com/rharrisxtheta)
- [Roberto Sarati](https://github.com/sierrodc)
- [Amer Koleci](https://github.com/amerkoleci)
- [Tim Horemans](https://github.com/worstenbrood)
- [Nerijus Dzindzeleta](https://github.com/NerijusD)
- [Rune Ibsen](https://github.com/ibsenrune)

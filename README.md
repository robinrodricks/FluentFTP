
# FluentFTP

[![Version](https://img.shields.io/nuget/vpre/FluentFTP.svg)](https://www.nuget.org/packages/FluentFTP)
[![Downloads](https://img.shields.io/nuget/dt/FluentFTP.svg)](https://www.nuget.org/packages/FluentFTP)
[![GitHub contributors](https://img.shields.io/github/contributors/robinrodricks/FluentFTP.svg)](https://github.com/robinrodricks/FluentFTP/graphs/contributors)
[![Codacy grade](https://img.shields.io/codacy/grade/2d6bb3604aae4c1e892a8386363a5bfc.svg)](https://app.codacy.com/project/robinrodricks/FluentFTP/dashboard)
[![License](https://img.shields.io/github/license/robinrodricks/FluentFTP.svg)](https://github.com/robinrodricks/FluentFTP/blob/master/LICENSE.TXT)

FluentFTP is a fully managed FTP and FTPS library for .NET & .NET Standard, optimized for speed. It provides extensive FTP commands, File uploads/downloads, SSL/TLS connections, Automatic directory listing parsing, File hashing/checksums, File permissions/CHMOD, FTP proxies, UTF-8 support, Async/await support, Powershell support and more.

It is written entirely in C#, with no external dependencies. FluentFTP is released under the permissive MIT License, so it can be used in both proprietary and free/open source applications.

**If you need priority support for a specific issue, consider funding it using [IssueHunt](https://issuehunt.io/r/robinrodricks/FluentFTP).**


## Features

  - Full support for [FTP](https://github.com/robinrodricks/FluentFTP/wiki/FTP-Support), [FTPS](https://github.com/robinrodricks/FluentFTP/wiki/FTPS-Connection#faq_ftps) (FTP over SSL), [FTPS with client certificates](https://github.com/robinrodricks/FluentFTP/wiki/FTPS-Connection#faq_certs) and [FTPS with CCC](https://github.com/robinrodricks/FluentFTP/wiki/FTPS-Connection#faq_ccc) (for FTP firewalls)
  - **File management:**
    - File and directory listing for [all major server types](https://github.com/robinrodricks/FluentFTP/wiki/Directory-Listing#faq_listings) (Unix, Windows/IIS, Azure, Pure-FTPd, ProFTPD, Vax, VMS, OpenVMS, Tandem, HP NonStop Guardian, IBM OS/400, AS400, Windows CE, Serv-U, etc)
	- Fully recursive directory listing and directory deletion (manual recursion and server-side recursion)
    - Easily upload and download a file from the server with [progress tracking](https://github.com/robinrodricks/FluentFTP/wiki/Directory-Listing#faq_progress)
    - Easily upload and download a directory from the server with easy synchronization modes
    - Automatically [verify the hash](https://github.com/robinrodricks/FluentFTP/wiki/File-Hashing#faq_verifyhash) of a file & retry transfer if hash mismatches
    - Configurable error handling (ignore/abort/throw) for multi-file transfers
    - Easily read and write file data from the server using standard streams
    - Create, append, read, write, rename, move and delete files and folders
    - Recursively deletes folders and all its contents
    - Get file/folder info (exists, size, security flags, modified date/time)
    - Get and set [file permissions](https://github.com/robinrodricks/FluentFTP/wiki/File-Permissions) (owner, group, other)
    - Absolute or relative paths (relative to the ["working directory"](https://github.com/robinrodricks/FluentFTP/wiki/File-Management))
    - Get the [hash/checksum](https://github.com/robinrodricks/FluentFTP/wiki/File-Hashing) of a file (SHA-1, SHA-256, SHA-512, and MD5)
    - Dereference of symbolic links to calculate the linked file/folder
	- [Throttling](https://github.com/robinrodricks/FluentFTP/wiki/File-Transfer#faq_throttle) of uploads and downloads with configurable speed limit
  - **FTP protocol:**
    - Automatic detection of [working connection settings](https://github.com/robinrodricks/FluentFTP/wiki/Automatic-Connection#faq_autodetect) and automatic [connection negotiation](https://github.com/robinrodricks/FluentFTP/wiki/Automatic-Connection#faq_autoconnect)
    - Automatic detection of the [FTP server software](https://github.com/robinrodricks/FluentFTP/wiki/Server-Information#faq_servertype) and its [capabilities](https://github.com/robinrodricks/FluentFTP/wiki/Directory-Listing#faq_recursivelist)
    - Extensive support for [FTP commands](https://github.com/robinrodricks/FluentFTP/wiki/FTP-Support), including some server-specific commands
    - Easily send [server-specific](https://github.com/robinrodricks/FluentFTP/issues/88) FTP commands using the `Execute()` method
    - Explicit and Implicit [SSL connections](https://github.com/robinrodricks/FluentFTP/wiki/FTPS-Connection#faq_ftps) are supported for the control and data connections using .NET's `SslStream`
    - Passive and active data connections (PASV, EPSV, PORT and EPRT)
    - Supports Unix CHMOD, PRET, ProFTPD's SITE MKDIR and RMDIR commands, Serv-U's RMDA command
    - Supports [FTP Proxies](https://github.com/robinrodricks/FluentFTP/wiki/FTPS-Proxies#faq_loginproxy) (User@Host, HTTP 1.1, BlueCoat)
    - [FTP command logging](https://github.com/robinrodricks/FluentFTP/wiki/Logging#faq_log) using `TraceListeners` (passwords omitted) to [trace](https://github.com/robinrodricks/FluentFTP/wiki/Logging#faq_trace) or [log output](https://github.com/robinrodricks/FluentFTP/wiki/Logging#faq_logfile) to a file
    - SFTP is not supported as it is FTP over SSH, a completely different protocol (use [SSH.NET](https://github.com/sshnet/SSH.NET) for that)
  - **Asynchronous support:**
    - Synchronous and asynchronous methods using `async`/`await` for all operations
    - Asynchronous methods for .NET 4.0 and below using `IAsyncResult` pattern (Begin*/End*)
    - All asynchronous methods can be cancelled midway by passing a `CancellationToken`
    - All asynchronous methods honor the `ReadTimeout` and automatically cancel themselves if timed out
    - Improves thread safety by cloning the FTP control connection for file transfers (optional)
    - Implements its own internal locking in an effort to keep transactions synchronized
  - **Extensible:**
    - Easily add support for more proxy types (simply extend [`FTPClientProxy`](https://github.com/robinrodricks/FluentFTP/blob/master/FluentFTP/Proxy/FtpClientProxy.cs))
    - Easily add unsupported directory listing parsers (see the [`CustomParser`](https://github.com/robinrodricks/FluentFTP/blob/f48af030b565237ddd5d7c8937378884d20e1627/FluentFTP.Examples/CustomParser.cs) example)
    - Easily add custom logging/tracing functionality using [`FtpTrace.AddListener`](https://github.com/robinrodricks/FluentFTP/wiki/Logging#faq_log)
	- Easily add your own Powershell commands by extending the scripts in `FluentFTP.ps1`

	
## Releases

Stable binaries are released on NuGet, and contain everything you need to use FTP/FTPS in your .Net/CLR application. For usage see the [Example Usage](#example-usage) section and the [Documentation](https://github.com/robinrodricks/FluentFTP/wiki) wiki.

  - [Nuget](https://www.nuget.org/packages/FluentFTP) (latest)
  - [Release Notes](https://github.com/robinrodricks/FluentFTP/blob/master/RELEASES.md) (features and fixes per release)

FluentFTP works on .NET and .NET Standard/.NET Core.

| Platform      		| Binaries Folder	| 
|---------------		|-----------		|
| **.NET 2.0**      	| net20     		| 
| **.NET 3.5**      	| net35     		| 
| **.NET 4.0**      	| net40     		| 
| **.NET 4.5**      	| net45     		| 
| **.NET Standard 1.4** | netstandard1.4	| 
| **.NET Standard 1.6** | netstandard1.6	| 
| **.NET Standard 2.0** | netstandard2.0	| 

FluentFTP is also supported on these platforms: (via .NET Standard)

  - **Mono** 4.6
  - **Xamarin.iOS** 10.0
  - **Xamarin.Android** 10.0
  - **Universal Windows Platform** 10.0

Binaries for all platforms are built from a single VS 2017 Project. You will need [VS 2017](https://visualstudio.microsoft.com/downloads/) to build or contribute to FluentFTP.


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

// upload a folder and all its files
client.UploadDirectory(@"C:\website\videos\", @"/public_html/videos", FtpFolderSyncMode.Update);

// upload a folder and all its files, and delete extra files on the server
client.UploadDirectory(@"C:\website\assets\", @"/public_html/assets", FtpFolderSyncMode.Mirror);

// download a folder and all its files
client.DownloadDirectory(@"C:\website\logs\", @"/public_html/logs", FtpFolderSyncMode.Update);

// download a folder and all its files, and delete extra files on disk
client.DownloadDirectory(@"C:\website\dailybackup\", @"/public_html/", FtpFolderSyncMode.Mirror);

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


## Documentation and FAQs

Check the [Wiki](https://github.com/robinrodricks/FluentFTP/wiki).
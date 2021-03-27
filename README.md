
![FluentFTP](https://github.com/robinrodricks/FluentFTP/raw/master/.github/logo-new.png)

[![Version](https://img.shields.io/nuget/vpre/FluentFTP.svg)](https://www.nuget.org/packages/FluentFTP)
[![Downloads](https://img.shields.io/nuget/dt/FluentFTP.svg)](https://www.nuget.org/packages/FluentFTP)
[![GitHub contributors](https://img.shields.io/github/contributors/robinrodricks/FluentFTP.svg)](https://github.com/robinrodricks/FluentFTP/graphs/contributors)
[![Codacy grade](https://img.shields.io/codacy/grade/2d6bb3604aae4c1e892a8386363a5bfc.svg)](https://app.codacy.com/project/robinrodricks/FluentFTP/dashboard)
[![License](https://img.shields.io/github/license/robinrodricks/FluentFTP.svg)](https://github.com/robinrodricks/FluentFTP/blob/master/LICENSE.TXT)

FluentFTP is a fully managed FTP and FTPS library for .NET & .NET Standard, optimized for speed. It provides extensive FTP commands, File uploads/downloads, SSL/TLS connections, Automatic directory listing parsing, File hashing/checksums, File permissions/CHMOD, FTP proxies, FXP transfers, UTF-8 support, Async/await support, Powershell support and more.

It is written entirely in C#, with no external dependencies. FluentFTP is released under the permissive MIT License, so it can be used in both proprietary and free/open source applications.

![Features](https://github.com/robinrodricks/FluentFTP/raw/master/.github/features-2.png)


## Features

  - Full support for [FTP](https://github.com/robinrodricks/FluentFTP/wiki/FTP-Support), [FXP](https://github.com/robinrodricks/FluentFTP/wiki/FXP-Server-To-Server#how-does-fxp-transfer-work), [FTPS](https://github.com/robinrodricks/FluentFTP/wiki/FTPS-Connection#faq_ftps) (FTP over SSL), [FTPS with client certificates](https://github.com/robinrodricks/FluentFTP/wiki/FTPS-Connection#faq_certs) and [FTPS with CCC](https://github.com/robinrodricks/FluentFTP/wiki/FTPS-Connection#faq_ccc) (for FTP firewalls)
  - **File management:**
    - File and directory listing for [all major server types](https://github.com/robinrodricks/FluentFTP/wiki/Directory-Listing#faq_listings) (Unix, Windows/IIS, Azure, Pure-FTPd, ProFTPD, Vax, VMS, OpenVMS, Tandem, HP NonStop Guardian, IBM OS/400, AS400, Windows CE, Serv-U, etc)
	- Fully recursive directory listing and directory deletion (manual recursion and server-side recursion)
    - Easily upload and download a file from the server with [progress tracking](https://github.com/robinrodricks/FluentFTP/wiki/File-Transfer#how-can-i-track-the-progress-of-file-transfers)
    - Easily upload and download a directory from the server with [easy synchronization modes](https://github.com/robinrodricks/FluentFTP/wiki/Directory-Transfer#what-is-the-difference-between-the-mirror-and-update-modes)
	- Easily transfer a file or folder directly from [one server to another](https://github.com/robinrodricks/FluentFTP/wiki/FXP-Server-To-Server#how-does-fxp-transfer-work) using the FXP protocol
	- Conditionally transfer files using [rule based whitelisting and blacklisting](https://github.com/robinrodricks/FluentFTP/wiki/Rules#what-kinds-of-rules-are-supported-and-how-do-rules-work)
    - Automatically [verify the hash](https://github.com/robinrodricks/FluentFTP/wiki/File-Hashing#faq_verifyhash) of a file & retry transfer if hash mismatches
    - Configurable error handling (ignore/abort/throw) for multi-file transfers
    - Easily read and write file data from the server using standard streams
    - Create, append, read, write, rename, move and delete files and folders
    - Recursively deletes folders and all its contents
    - Get file/folder info (exists, size, security flags, modified date/time)
    - Get and set [file permissions](https://github.com/robinrodricks/FluentFTP/wiki/File-Permissions) (owner, group, other)
    - Absolute or relative paths (relative to the ["working directory"](https://github.com/robinrodricks/FluentFTP/wiki/File-Management))
    - Compare a local file against a remote file using the [hash/checksum](https://github.com/robinrodricks/FluentFTP/wiki/File-Hashing#faq_comparefile) (MD5, CRC32, SHA-1, SHA-256, SHA-512)
    - Dereference of symbolic links to calculate the linked file/folder
	- [Throttling](https://github.com/robinrodricks/FluentFTP/wiki/File-Transfer#faq_throttle) of uploads and downloads with configurable speed limit
  - **FTP protocol:**
    - Automatic detection of [working connection settings](https://github.com/robinrodricks/FluentFTP/wiki/Automatic-Connection#faq_autodetect) and automatic [connection negotiation](https://github.com/robinrodricks/FluentFTP/wiki/Automatic-Connection#faq_autoconnect)
    - Automatic detection of the [FTP server software](https://github.com/robinrodricks/FluentFTP/wiki/Server-Information#faq_servertype) and its [capabilities](https://github.com/robinrodricks/FluentFTP/wiki/Server-Information#faq_serverspecific)
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
    - Asynchronous support for the `IAsyncEnumerable` pattern for `GetListing` methods (see `GetListingAsyncEnumerable`)
    - All asynchronous methods can be cancelled midway by passing a `CancellationToken`
    - All asynchronous methods honor the `ReadTimeout` and automatically cancel themselves if timed out
    - Improves thread safety by cloning the FTP control connection for file transfers (optional)
    - Implements its own internal locking in an effort to keep transactions synchronized
  - **Extensible:**
    - Easily add support for custom non-standard FTP servers (see the [Custom Servers](https://github.com/robinrodricks/FluentFTP/wiki/Custom-Servers) page)
    - Easily add support for more file or directory filtering rules (simply extend [`FtpRule`](https://github.com/robinrodricks/FluentFTP/wiki/Class-FtpRule))
    - Easily add support for more proxy types (simply extend [`FtpClientProxy`](https://github.com/robinrodricks/FluentFTP/blob/master/FluentFTP/Proxy/FtpClientProxy.cs))
    - Easily add unsupported directory listing parsers (see the [`CustomParser`](https://github.com/robinrodricks/FluentFTP/blob/master/FluentFTP.CSharpExamples/CustomParser.cs) example)
    - Easily add custom logging/tracing functionality using [`FtpTrace.AddListener`](https://github.com/robinrodricks/FluentFTP/wiki/Logging#faq_log)
	- Easily add your own Powershell commands by extending the scripts in [`FluentFTP.ps1`](https://github.com/robinrodricks/FluentFTP/wiki/Powershell)

	
## Releases

Stable binaries are released on NuGet, and contain everything you need to use FTP/FTPS in your .Net/CLR application. For usage see the [Quick Start Example](https://github.com/robinrodricks/FluentFTP/wiki/Quick-Start-Example) and the [Documentation](https://github.com/robinrodricks/FluentFTP/wiki) wiki.

  - [Nuget](https://www.nuget.org/packages/FluentFTP) (latest)
  - [Release Notes](https://github.com/robinrodricks/FluentFTP/blob/master/RELEASES.md) (features and fixes per release)

FluentFTP works on .NET and .NET Standard/.NET Core.

| Platform      		| Binaries Folder	| 
|---------------		|-----------		|
| **.NET 2.0**      	| net20     		| 
| **.NET 3.5**      	| net35     		| 
| **.NET 4.0**      	| net40     		| 
| **.NET 4.5**      	| net45     		| 
| **.NET 5.0**      	| net50     		| 
| **.NET Standard 1.4** | netstandard1.4	| 
| **.NET Standard 1.6** | netstandard1.6	| 
| **.NET Standard 2.0** | netstandard2.0	| 
| **.NET Standard 2.1** | netstandard2.1	| 

FluentFTP is also supported on these platforms: (via .NET Standard)

  - **Mono** 4.6
  - **Xamarin.iOS** 10.0
  - **Xamarin.Android** 10.0
  - **Universal Windows Platform** 10.0

Binaries for all platforms are built from a single Visual Studio 2019 Project. You will need [VS 2019](https://visualstudio.microsoft.com/downloads/) to build or contribute to FluentFTP.


## Example Usage

To get started, check out the [Quick start example in C#](https://github.com/robinrodricks/FluentFTP/wiki/Quick-Start-Example).

We also have extensive examples for all methods in [C#](https://github.com/robinrodricks/FluentFTP/tree/master/FluentFTP.CSharpExamples) and [VB.NET](https://github.com/robinrodricks/FluentFTP/tree/master/FluentFTP.VBExamples).

## Documentation and FAQs

Check the [Wiki](https://github.com/robinrodricks/FluentFTP/wiki).

## Contributors

Special thanks to these awesome people who helped create FluentFTP!

<!---
https://contributors-img.firebaseapp.com/image?repo=robinrodricks/FluentFTP
-->

<a href="https://github.com/robinrodricks/FluentFTP/graphs/contributors">
  <img src="https://github.com/robinrodricks/FluentFTP/raw/master/.github/contributors.png" />
</a>

## Supporters

FluentFTP is supported by these generous organizations:

<table>
<tr>
	<td width="200px">
		<a href="https://www.jetbrains.com/">
		<img src="https://github.com/robinrodricks/FluentFTP/raw/master/.github/jetbrains-logo.png" />
		</a>
	</td>
	<td width="200px">
		<a href="https://www.balsamiq.com/">
		<img src="https://github.com/robinrodricks/FluentFTP/raw/master/.github/balsamiq-logo.png" />
		</a>
	</td>
	<td width="200px">
		<a href="https://www.yourkit.com/">
		<img src="https://github.com/robinrodricks/FluentFTP/raw/master/.github/yourkit-logo.png" />
		</a>
	</td>
</tr>
<tr>
	<td width="200px">
		JetBrains provides cutting-edge IDE and developer productivity tools.
	</td>
	<td width="200px">
		Balsamiq provides rapid and effective wireframing and UI design tools.
	</td>
	<td width="200px">
		YourKit provides a market-leading intelligent <a href="https://www.yourkit.com/features/">Java Profiler</a> and <a href="https://www.yourkit.com/dotnet/features/">.NET Profiler</a>.
	</td>
</tr>
</table>

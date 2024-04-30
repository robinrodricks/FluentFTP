
![FluentFTP](https://github.com/robinrodricks/FluentFTP/raw/master/.github/logo-new.png)

[![Version](https://img.shields.io/nuget/vpre/FluentFTP.svg)](https://www.nuget.org/packages/FluentFTP)
[![Downloads](https://img.shields.io/nuget/dt/FluentFTP.svg)](https://www.nuget.org/packages/FluentFTP)
[![GitHub contributors](https://img.shields.io/github/contributors/robinrodricks/FluentFTP.svg)](https://github.com/robinrodricks/FluentFTP/graphs/contributors)
[![Codacy Badge](https://app.codacy.com/project/badge/Grade/8bc33aa55cb8494da3a7a07dba5316f7)](https://app.codacy.com/gh/robinrodricks/FluentFTP/dashboard)
[![License](https://img.shields.io/github/license/robinrodricks/FluentFTP.svg)](https://github.com/robinrodricks/FluentFTP/blob/master/LICENSE.TXT)
[![OpenSSF Best Practices](https://www.bestpractices.dev/badge_static/passing)](https://www.bestpractices.dev/projects/6661)

FluentFTP is a fully managed FTP and FTPS client library for .NET & .NET Standard, optimized for speed. It provides extensive FTP commands, File uploads/downloads, SSL/TLS connections, Automatic directory listing parsing, File hashing/checksums, File permissions/CHMOD, FTP proxies, FXP transfers, UTF-8 support, Async/await support, Powershell support and more.

It is written entirely in C#, with no external dependency. It has an extensive automated test suite which tests all its functionality against local FTP server docker containers.

FluentFTP is released under the permissive MIT License, so it can be used in both proprietary and free/open source applications. 

![Features](https://github.com/robinrodricks/FluentFTP/raw/master/.github/features-5.png)


## Features

  - Full support for [FTP](https://github.com/robinrodricks/FluentFTP/wiki/FTP-Support), [FXP](https://github.com/robinrodricks/FluentFTP/wiki/FXP-Server-To-Server#how-does-fxp-transfer-work), [FTPS](https://github.com/robinrodricks/FluentFTP/wiki/FTP-Connection#faq_ftps), [FTPS with TLS 1.3](https://github.com/robinrodricks/FluentFTP/wiki/FTPS-Connection-using-GnuTLS), [FTPS with client certificates](https://github.com/robinrodricks/FluentFTP/wiki/FTP-Connection#faq_certs) and [FTPS Proxies](https://github.com/robinrodricks/FluentFTP/wiki/FTPS-Proxies)
  - Full support for over 30 [FTP server types](#server-support) with integration tests for all major servers
  - **File management:**
    - File and directory listing for [all major server types](https://github.com/robinrodricks/FluentFTP/wiki/Directory-Listing#faq_listings) (Unix, Windows/IIS, Azure, Pure-FTPd, ProFTPD, Vax, VMS, OpenVMS, Tandem, HP NonStop Guardian, [IBM z/OS and OS/400](https://github.com/robinrodricks/FluentFTP/wiki/IBM-zOS-and-OS-400-Support), Windows CE, Serv-U, etc)
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
    - Automatic [FTP/FTPS connection negotiation](https://github.com/robinrodricks/FluentFTP/wiki/Automatic-Connection#faq_autoconnect) and detection of [working connection settings](https://github.com/robinrodricks/FluentFTP/wiki/Automatic-Connection#faq_autodetect)
    - Automatic detection of the [FTP server software](https://github.com/robinrodricks/FluentFTP/wiki/Server-Information#faq_servertype) and its [capabilities](https://github.com/robinrodricks/FluentFTP/wiki/Server-Information#faq_serverspecific)
	- Automatic [reconnection of FTP connections](https://github.com/robinrodricks/FluentFTP/wiki/Automatic-Reconnection) for broken or degraded sockets
    - Extensive support for [FTP commands](https://github.com/robinrodricks/FluentFTP/wiki/FTP-Support), including some server-specific commands
    - Easily send [server-specific](https://github.com/robinrodricks/FluentFTP/issues/88) FTP commands using the `Execute()` method
    - Explicit and Implicit [SSL connections](https://github.com/robinrodricks/FluentFTP/wiki/FTP-Connection#faq_ftps) are supported for the control and data connections using .NET's `SslStream`
    - Passive and active data connections (PASV, EPSV, PORT and EPRT)
    - Supports Unix CHMOD, PRET, ProFTPD's SITE MKDIR and RMDIR commands, Serv-U's RMDA command
    - Supports Realm and directory navigation for [IBM z/OS and OS/400](https://github.com/robinrodricks/FluentFTP/wiki/IBM-zOS-and-OS-400-Support)
    - Supports all types of [FTP Proxies](https://github.com/robinrodricks/FluentFTP/wiki/FTPS-Proxies) (HTTP 1.1, SOCKS4, SOCKS4a, SOCKS5, User@Host, BlueCoat)
    - [FTP command logging](https://github.com/robinrodricks/FluentFTP/wiki/Logging#faq_log) using `TraceListeners` (passwords omitted) to [trace](https://github.com/robinrodricks/FluentFTP/wiki/Logging#faq_trace) or [log output](https://github.com/robinrodricks/FluentFTP/wiki/Logging#faq_logfile) to a file
    - SFTP is not supported as it is FTP over SSH, a completely different protocol (use [SSH.NET](https://github.com/sshnet/SSH.NET) for that)
  - **Asynchronous support:**
    - Synchronous and asynchronous methods using `async`/`await` for all operations
    - Asynchronous support for the `IAsyncEnumerable` pattern for `GetListing` methods (see `GetListingAsyncEnumerable`)
    - All asynchronous methods can be cancelled midway by passing a `CancellationToken`
    - All asynchronous methods honor the `ReadTimeout` and automatically cancel themselves if timed out
    - Asynchronous support for [progress tracking](https://github.com/robinrodricks/FluentFTP/wiki/File-Transfer#how-can-i-track-the-progress-of-file-transfers) of file transfers during data upload/download
    - Implements its own internal locking in an effort to keep transactions synchronized
  - **Extensible:**
    - Easily add custom logging/tracing functionality using industry-standard [`ILogger`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger?view=dotnet-plat-ext-6.0) interface
    - Easily add support for custom non-standard FTP servers (see the [Custom Servers](https://github.com/robinrodricks/FluentFTP/wiki/Custom-Servers) page)
    - Easily add support for more file or directory filtering rules (simply extend [`FtpRule`](https://github.com/robinrodricks/FluentFTP/wiki/Class-FtpRule))
    - Easily add support for more proxy types (simply extend [`FtpClientProxy`](https://github.com/robinrodricks/FluentFTP/blob/master/FluentFTP/Proxy/FtpClientProxy.cs))
    - Easily add unsupported directory listing parsers (see the [`CustomParser`](https://github.com/robinrodricks/FluentFTP/blob/master/FluentFTP.CSharpExamples/CustomParser.cs) example)
	- Easily add your own Powershell commands by extending the scripts in [`FluentFTP.ps1`](https://github.com/robinrodricks/FluentFTP/wiki/Powershell)

	
## Releases

Stable binaries are released on NuGet, and contain everything you need to use FTP/FTPS in your .Net/CLR application.

| Package      		| Latest Version	|  Downloads	|   Docs	| 
|---------------		|-----------	|-----------	|-----------|
| **[FluentFTP](https://www.nuget.org/packages/FluentFTP)**      	|     [![Version](https://img.shields.io/nuget/vpre/FluentFTP.svg)](https://www.nuget.org/packages/FluentFTP) 		|  [![Downloads](https://img.shields.io/nuget/dt/FluentFTP.svg)](https://www.nuget.org/packages/FluentFTP) | [FluentFTP Docs](https://github.com/robinrodricks/FluentFTP/wiki) |
| **[FluentFTP.Logging](https://www.nuget.org/packages/FluentFTP.Logging)**      	|    [![Version](https://img.shields.io/nuget/vpre/FluentFTP.Logging.svg)](https://www.nuget.org/packages/FluentFTP.Logging)  		| [![Downloads](https://img.shields.io/nuget/dt/FluentFTP.Logging.svg)](https://www.nuget.org/packages/FluentFTP.Logging) | [Logging Docs](https://github.com/robinrodricks/FluentFTP/wiki/Logging) |
| **[FluentFTP.GnuTLS](https://www.nuget.org/packages/FluentFTP.GnuTLS)**      	|    [![Version](https://img.shields.io/nuget/vpre/FluentFTP.GnuTLS.svg)](https://www.nuget.org/packages/FluentFTP.GnuTLS)  		| [![Downloads](https://img.shields.io/nuget/dt/FluentFTP.GnuTLS.svg)](https://www.nuget.org/packages/FluentFTP.GnuTLS) | [GnuTLS Docs](https://github.com/robinrodricks/FluentFTP/wiki/FTPS-Connection-using-GnuTLS) |

For usage see the [Quick Start Example](https://github.com/robinrodricks/FluentFTP/wiki/Quick-Start-Example) and the [Documentation](https://github.com/robinrodricks/FluentFTP/wiki) wiki.

For features and fixes per release see [Release Notes](https://github.com/robinrodricks/FluentFTP/blob/master/RELEASES.md).



## Server Support

FluentFTP is a client library that can connect to these FTP servers and perform all FTP operations.

| Server             | FTP/FTPS | Integration Tests | Special Features  |
| ------------------ | --------- | ------------ |---------- |
| Azure FTP          |  ![i](https://img.shields.io/badge/-supported-brightgreen)️        | ![i](https://img.shields.io/badge/-commercial-orange) | ✔️ [Detected by Domain](https://github.com/robinrodricks/FluentFTP/wiki/Server-Information#azure), Auto Configuration |
| Apache FTP         |  ![i](https://img.shields.io/badge/-supported-brightgreen)️        | ![i](https://img.shields.io/badge/-supported-brightgreen)️ |  |
| ABB IDAL           |  ![i](https://img.shields.io/badge/-supported-brightgreen)️        | ![i](https://img.shields.io/badge/-commercial-orange) |  |
| BFTPd              |  ![i](https://img.shields.io/badge/-supported-brightgreen)️        | ![i](https://img.shields.io/badge/-supported-brightgreen) |  |
| Cerberus           |  ![i](https://img.shields.io/badge/-supported-brightgreen)️        | ![i](https://img.shields.io/badge/-commercial-orange) |  |
| CrushFTP           |  ![i](https://img.shields.io/badge/-supported-brightgreen)️        | ![i](https://img.shields.io/badge/-commercial-orange) |  |
| FileZilla Server   |  ![i](https://img.shields.io/badge/-supported-brightgreen)️        | ![i](https://img.shields.io/badge/-supported-brightgreen) |  |
| FritzBox           |  ![i](https://img.shields.io/badge/-supported-brightgreen)️        | ![i](https://img.shields.io/badge/-hardware%20required-red) |  |
| FTP2S3 Gateway     |  ![i](https://img.shields.io/badge/-supported-brightgreen)️        | ![i](https://img.shields.io/badge/-commercial-orange) |  |
| glFTPd             |  ![i](https://img.shields.io/badge/-supported-brightgreen)️        | ![i](https://img.shields.io/badge/-supported-brightgreen) |  |
| GlobalScape EFT    |  ![i](https://img.shields.io/badge/-supported-brightgreen)️        | ![i](https://img.shields.io/badge/-commercial-orange) |  |
| Homegate FTP       |  ![i](https://img.shields.io/badge/-supported-brightgreen)️        | ![i](https://img.shields.io/badge/-contributions%20welcome-blue) |  |
| Huawei HG5xxx      |  ![i](https://img.shields.io/badge/-supported-brightgreen)️        | ![i](https://img.shields.io/badge/-hardware%20required-red) |  |
| HP NonStop/Tandem  |  ![i](https://img.shields.io/badge/-supported-brightgreen)️        | ![i](https://img.shields.io/badge/-hardware%20required-red) | ✔️ NonStop File Listing |
| IBM z/OS   |  ![i](https://img.shields.io/badge/-supported-brightgreen)️ | ![i](https://img.shields.io/badge/-privately%20performed-yellow)   | ✔️ [zOS API](https://github.com/robinrodricks/FluentFTP/wiki/IBM-zOS-and-OS-400-Support), File Listing, File Size, Paths |
| MikroTik RouterOS  |  ![i](https://img.shields.io/badge/-supported-brightgreen)️        | ![i](https://img.shields.io/badge/-hardware%20required-red) |  |
| OpenVMS            |  ![i](https://img.shields.io/badge/-supported-brightgreen)️        | ![i](https://img.shields.io/badge/-commercial-orange) | ✔️ VMS File Listing, Paths, Capability Assumption |
| ProFTPD            |  ![i](https://img.shields.io/badge/-supported-brightgreen)️        | ![i](https://img.shields.io/badge/-supported-brightgreen) | ✔️ `SITE RMDIR` and `SITE MKDIR` Commands |
| PureFTPd           |  ![i](https://img.shields.io/badge/-supported-brightgreen)️        | ![i](https://img.shields.io/badge/-supported-brightgreen) |  |
| PyFtpdLib          |  ![i](https://img.shields.io/badge/-supported-brightgreen)️        | ![i](https://img.shields.io/badge/-supported-brightgreen) |  |
| Rumpus             |  ![i](https://img.shields.io/badge/-supported-brightgreen)️        | ![i](https://img.shields.io/badge/-commercial-orange) |  |
| Serv-U             |  ![i](https://img.shields.io/badge/-supported-brightgreen)️        | ![i](https://img.shields.io/badge/-commercial-orange) | ✔️ `RMDA` Command |
| Solaris FTP        |  ![i](https://img.shields.io/badge/-supported-brightgreen)️        | ![i](https://img.shields.io/badge/-commercial-orange) |  |
| Titan FTP          |  ![i](https://img.shields.io/badge/-supported-brightgreen)️        | ![i](https://img.shields.io/badge/-commercial-orange) |  |
| TP-LINK            |  ![i](https://img.shields.io/badge/-supported-brightgreen)️        | ![i](https://img.shields.io/badge/-hardware%20required-red) |  |
| VsFTPd             |  ![i](https://img.shields.io/badge/-supported-brightgreen)️        | ![i](https://img.shields.io/badge/-supported-brightgreen) |  |
| Windows CE         |  ![i](https://img.shields.io/badge/-supported-brightgreen)️        | ![i](https://img.shields.io/badge/-commercial-orange) | ✔️ Windows File Listing |
| Windows Server/IIS |  ![i](https://img.shields.io/badge/-supported-brightgreen)️        | ![i](https://img.shields.io/badge/-commercial-orange) | ✔️ Windows File Listing |
| WS_FTP             |  ![i](https://img.shields.io/badge/-supported-brightgreen)️        | ![i](https://img.shields.io/badge/-commercial-orange) |  |
| WuFTPd             |  ![i](https://img.shields.io/badge/-supported-brightgreen)️        | ![i](https://img.shields.io/badge/-contributions%20welcome-blue) | ✔️ Capability Assumption |
| XLight             |  ![i](https://img.shields.io/badge/-supported-brightgreen)️        | ![i](https://img.shields.io/badge/-contributions%20welcome-blue) |  |




## Platform Support

FluentFTP works on .NET and .NET Standard/.NET Core.

| Platform      		| Binaries Folder	| 
|---------------		|-----------		|
| **.NET 4.6.2**      	| net462     		| 
| **.NET 4.7.2**      	| net472     		| 
| **.NET 5.0**      	| net50     		| 
| **.NET 6.0**      	| net60     		| 
| **.NET Standard 2.0** | netstandard2.0	| 
| **.NET Standard 2.1** | netstandard2.1	| 

FluentFTP is also supported on these platforms: (via .NET Standard)

  - **Mono** 4.6
  - **Xamarin.iOS** 10.0
  - **Xamarin.Android** 10.0
  - **Universal Windows Platform** 10.0

Binaries for all platforms are built from a single Visual Studio Project. You will need the latset [Visual Studio](https://visualstudio.microsoft.com/downloads/) to build or contribute to FluentFTP.


## Example Usage

To get started, check out the [Quick start example in C#](https://github.com/robinrodricks/FluentFTP/wiki/Quick-Start-Example).

We also have extensive examples for all methods in [C#](https://github.com/robinrodricks/FluentFTP/tree/master/FluentFTP.CSharpExamples) and [VB.NET](https://github.com/robinrodricks/FluentFTP/tree/master/FluentFTP.VBExamples).

## Documentation and FAQs

Check the [Wiki](https://github.com/robinrodricks/FluentFTP/wiki).

## Tests

We have an extensive [automated test suite](https://github.com/robinrodricks/FluentFTP/wiki/Automated-Testing) that tests FluentFTP against many servers. We use docker to orchestrate containerized FTP servers that are used for testing.


## Sponsorship

If FluentFTP helped you or your organization, consider [sponsoring the project](https://github.com/sponsors/robinrodricks) by donating a small amount per month. Even $20 goes a long way! Everything I receive goes into household expenses and paying the bills. 

I have been a freelancer for more than a decade, and your contributions go towards supporting my work and my family. I only recently started asking for donations to fund the time I spend on these open source projects. 

## Contributors

Special thanks to these awesome people who helped create FluentFTP!

<!--
https://contributors-img.web.app/image?repo=robinrodricks/FluentFTP
-->

<a href="https://github.com/robinrodricks/FluentFTP/graphs/contributors">
  <img src="https://github.com/robinrodricks/FluentFTP/raw/master/.github/contributors-3.png" />
</a>


## Reporting Vulnerabilities

Any security vulnerabilities that are found can be reported to the project owner at `r o b i n r o d r i c k s 7 at g m a i l`. No other direct correspondence will be entertained.

## Software Support

FluentFTP has received free software from these generous organizations:

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

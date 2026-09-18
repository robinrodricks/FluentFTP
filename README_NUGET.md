**FluentFTP** is a fully managed FTP and FTPS client library for .NET & .NET Standard, optimized for speed. It provides extensive FTP commands, File uploads/downloads, SSL/TLS connections, Automatic directory listing parsing, File hashing/checksums, File permissions/CHMOD, FTP proxies, FXP transfers, UTF-8 support, Async/await support, Powershell support and more.

It is written entirely in C#, with no external dependency. It has an extensive automated test suite which tests all its functionality against local FTP server docker containers.

FluentFTP is released under the permissive MIT License, so it can be used in both proprietary and free/open source applications. 

## Release Notes

### 55.0.0
 - **AOT Support**
   - New: FluentFTP is now fully AOT ready! This means you can use FluentFTP in projects that use Native Ahead-of-Time (AOT) compilation
   - New: Rewrite core classes for AOT compatibility (`Logger`, `ValuePrinter`, `FtpAutoDetectConfig`, `FtpSslStream`)
   - New: Add new C# project to test AOT compilation (`FluentFTP.AotExamples`)
 - **BouncyCastle SSL Stream**
   - New: FluentFTP now supports a new SSL stream backed by BouncyCastle, which allows us to fix some longstanding issues like SSL session reuse/resume which were failing with the default .NET TlsStream
   - New: Integration tests for BouncyCastle stream under `FluentFTP.Tests.Integration` NS
   - New: Unit tests for BouncyCastle stream under `FluentFTP.Tests.Unit.BouncyCastle` NS

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
	- FTP monitors to [monitor folders on FTP servers](https://github.com/robinrodricks/FluentFTP/wiki/FTPS-Monitor) and trigger events when files are added/changed/removed
  - **FTP protocol:**
    - Automatic [FTP/FTPS connection negotiation](https://github.com/robinrodricks/FluentFTP/wiki/Automatic-Connection#faq_autoconnect) and detection of [working connection settings](https://github.com/robinrodricks/FluentFTP/wiki/Automatic-Connection#faq_autodetect)
    - Automatic detection of the [FTP server software](https://github.com/robinrodricks/FluentFTP/wiki/Server-Information#faq_servertype) and its [capabilities](https://github.com/robinrodricks/FluentFTP/wiki/Server-Information#faq_serverspecific)
	- Automatic [reconnection of FTP connections](https://github.com/robinrodricks/FluentFTP/wiki/Automatic-Reconnection) for broken or degraded sockets
    - Extensive support for [FTP commands](https://github.com/robinrodricks/FluentFTP/wiki/FTP-Support), including some server-specific commands
    - Easily send [server-specific](https://github.com/robinrodricks/FluentFTP/issues/88) FTP commands using the `Execute()` method
	- State-of-the-art [security system](https://github.com/robinrodricks/FluentFTP/wiki/Security) to prevent FTP command injection, directory traversal attacks, encoding bypasses, and parser confusion attacks
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
    - Easily add support for more proxy types (simply extend [`FtpClientProxy`](https://github.com/robinrodricks/FluentFTP/blob/master/FluentFTP/Proxy/SyncProxy/FtpClientProxy.cs))
    - Easily add unsupported directory listing parsers (see the [`CustomParser`](https://github.com/robinrodricks/FluentFTP/blob/master/FluentFTP.CSharpExamples/CustomParser.cs) example)
	- Easily add your own Powershell commands by extending the scripts in [`FluentFTP.ps1`](https://github.com/robinrodricks/FluentFTP/wiki/Powershell)


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



## Example Usage

To get started, check out the [Quick start example in C#](https://github.com/robinrodricks/FluentFTP/wiki/Quick-Start-Example).

We also have extensive examples for all methods in [C#](https://github.com/robinrodricks/FluentFTP/tree/master/FluentFTP.CSharpExamples) and [VB.NET](https://github.com/robinrodricks/FluentFTP/tree/master/FluentFTP.VBExamples).

## Documentation and FAQs

Check the [Wiki](https://github.com/robinrodricks/FluentFTP/wiki).

## Tests

We have an extensive [automated test suite](https://github.com/robinrodricks/FluentFTP/wiki/Automated-Testing) that tests FluentFTP against many servers. We use docker to orchestrate containerized FTP servers that are used for testing.

## Security

We have a state-of-the-art [security system](https://github.com/robinrodricks/FluentFTP/wiki/Security) to prevent many known FTP attacks.
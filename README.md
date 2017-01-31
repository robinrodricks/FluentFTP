
# FluentFTP

[![Version](https://img.shields.io/nuget/vpre/FluentFTP.svg)](https://www.nuget.org/packages/FluentFTP)

FluentFTP is a fully managed FTP client that is designed to be easy to use and easy to extend. It supports file and directory listing, uploading and dowloading files and SSL/TLS connections. It can connect to Unix and Windows/IIS based FTP servers. This project is entirely developed in managed C#. All credits go to [J.P. Trosclair](https://github.com/jptrosclair) for developing and maintaining the library till 2016. FluentFTP is released under the permissive MIT License, so it can be used in both proprietary and free/open source applications.

## Features

- Full support for FTP, FTPS (FTP over SSL) and [FTPS with client certificates](#client-certificates)
- File and directory listing for [various servers](#file-listings) (UNIX, IIS, DOS, etc)
- Easily upload and download a file from the server
- Easily read and write file data from the server using standard streams
- Create, append, read, write, rename and delete files and folders
- Recursively deletes folders and all its contents
- Get file/folder info (exists, size, security flags, modified date/time)
- Absolute or relative paths (relative to the "working directory")
- Get the hash/checksum of a file (SHA-1, SHA-256, SHA-512, and MD5)
- Supports DrFTPD's PRET command
- Supports FTP Proxies (User@Host, HTTP 1.1)
- Dereferencing of symbolic links
- Passive and active data connections (PASV, EPSV, PORT and EPRT)
- Synchronous and asynchronous methods (`IAsyncResult` pattern) for all operations 
- Explicit and Implicit SSL connections are supported for the control and data connections using .NET's `SslStream`
- Improves thread safety by cloning the FTP control connection for file transfers (optional)
- Implements its own internal locking in an effort to keep transactions synchronized
- Includes support for non-standard hashing/checksum commands when supported by the server
- Easily issue any unsupported FTP command using the `Execute()` method with the exception of those requiring a data connection (file listings and transfers).
- Easily add support for more proxy types (simply extend [`FTPClientProxy`](https://github.com/hgupta9/FluentFTP/blob/master/FluentFTP/Proxy/FtpClientProxy.cs))
- Easily add unsupported directory listing parsers (see the [`CustomParser`](https://github.com/hgupta9/FluentFTP/blob/f48af030b565237ddd5d7c8937378884d20e1627/FluentFTP.Examples/CustomParser.cs) example)
- Transaction logging using `TraceListeners` (passwords are automatically omitted)
- Examples for nearly all methods (see [Examples](https://github.com/hgupta9/FluentFTP/tree/master/FluentFTP.Examples))
- SFTP is not supported as it is FTP over SSH, a completely different protocol (use [SSH.NET](https://github.com/sshnet/SSH.NET) for that)

## Releases

Stable binaries are released on NuGet, and contain everything you need to use FTP/FTPS in your .Net/CLR application. For usage see the [Example Usage](#example-usage) section and the [Documentation](#documentation) section below.

- [Nuget](https://www.nuget.org/packages/FluentFTP) (latest)
- [Releases](https://github.com/hgupta9/FluentFTP/releases) (occasionally updated)

## Example Usage

```csharp
// create an FTP client
FtpClient client = new FtpClient();
client.Host = "123.123.123.123";

// if you don't specify login credentials, we use the "anonymous" user account
client.Credentials = new NetworkCredential("david", "pass123");

// begin connecting to the server
client.Connect();

// get a list of files and directories in the "/htdocs" folder
foreach (FtpListItem item in client.GetListing("/htdocs") {
	
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
cl.UploadFile(@"C:\MyVideo.mp4", "/htdocs/big.txt");

// rename the uploaded file
cl.Rename("/htdocs/big.txt", "/htdocs/big2.txt");

// download the file again
cl.DownloadFile(@"C:\MyVideo_2.mp4", "/htdocs/big2.txt");

// delete the file
client.DeleteFile("/htdocs/big2.txt");

// delete a folder recursively
client.DeleteDirectory("/htdocs/extras/", true);

// check if a file exists
if (client.FileExists("/htdocs/big2.txt")){ }

// check if a folder exists
if (client.DirectoryExists("/htdocs/extras/")){ }

// disconnect! good bye!
client.Disconnect();
```
	
See more examples [here](https://github.com/hgupta9/FluentFTP/tree/master/FluentFTP.Examples).


# Documentation

Quick API documentation for the `FtpClient` class, which handles all FTP/FTPS functionality.

**Note:** All methods support synchronous and asynchronous versions. Simply add the "Begin" prefix to a method for the async version.

## Connection

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

## File Management

- **GetListing**() - Get a file listing of the given directory. Returns one `FtpListItem` per file or folder with all available properties set. Each item contains:

	- `Type` : The type of the object. (File, Directory or Link)
	
	- `Name` : The name of the object. (minus the path)

	- `FullName` : The full file path of the object.

	- `Created ` : The created date/time of the object. **Default:** `DateTime.MinValue` if not provided by server.

	- `Modified` : The last modified date/time of the object. If you get incorrect values, try adding the `FtpListOption.Modify` flag which loads the modified date/time using another `MDTM` command. **Default:** `DateTime.MinValue` if not provided by server.

	- `Size` : The size of the file in bytes. If you get incorrect values, try adding the `FtpListOption.Size` flag which loads the file size using another `SIZE` command. **Default:** `0` if not provided by server.

	- `LinkTarget` : The full file path the link points to. Only filled for symbolic links. 

	- `LinkObject` : The file/folder the link points to. Only filled for symbolic links if `FtpListOption.DerefLink` flag is used.

	- `SpecialPermissions` : Gets special permissions such as Stiky, SUID and SGID. **(*NIX only)**

	- `OwnerPermissions` : User rights. Any combination of 'r', 'w', 'x' (using the `FtpPermission` enum). **(*NIX only)**

	- `GroupPermissions` : Group rights. Any combination of 'r', 'w', 'x' (using the `FtpPermission` enum). **(*NIX only)**

	- `OtherPermissions` : Other rights. Any combination of 'r', 'w', 'x' (using the `FtpPermission` enum). **(*NIX only)**

	- `Input` : The raw string that the server returned for this object. Helps debug if the above properties have been correctly parsed.

- **GetNameListing**() - A simple command that only returns the list of file paths in the given directory, using the NLST command.

- **UploadFile**() - Uploads a file from the local file system to the server. Returns true if succeeded, false if failed or file does not exist. Supports very large files since it uploads data in chunks of 65KB. Remote directories are NOT created if they do not exist.

- **DownloadFile**() - Downloads a file from the server to the local file system. Returns true if succeeded, false if failed or file does not exist. Supports very large files since it downloads data in chunks of 65KB. Local directories are created if they do not exist.

- **GetWorkingDirectory**() - Gets the full path of the current working directory.

- **SetWorkingDirectory**() - Sets the full path of the current working directory.

- **CreateDirectory**() - Creates a directory on the server. If the parent directories do not exist they are also created.

- **DeleteDirectory**() - Deletes the specified directory on the server. If it is not empty then all subdirectories and files are recursively deleted.

- **DeleteFile**() - Deletes the specified file on the server.

- **Rename**() - Renames the file/directory on the server.

- **FileExists**() - Check if a file exists on the server.

- **DirectoryExists**() - Check if a directory exists on the server.

- **GetModifiedTime**() - Gets the last modified date/time of the file or folder.

- **GetFileSize**() - Gets the size of the file in bytes, or -1 if not found.

- **DereferenceLink**() - Recursively dereferences a symbolic link and returns the full path if found. The `MaximumDereferenceCount` property controls how deep we recurse before giving up.

- **OpenRead**() - Low level. Not recommended for general usage. Open a stream to the specified file for reading. Returns a standard `Stream`.

- **OpenWrite**() - Low level. Not recommended for general usage. Opens a stream to the specified file for writing. Returns a standard `Stream`, any data written will overwrite the file, or create the file if it does not exist.

- **OpenAppend**() - Low level. Not recommended for general usage. Opens a stream to the specified file for appending. Returns a standard `Stream`, any data written wil be appended to the end of the file.

## File Hashing

- **HashAlgorithms** - Get the hash types supported by the server, if any (represented by flags).

- **GetHash**() - Gets the hash of an object on the server using the currently selected hash algorithm. Supported algorithms are available in the `HashAlgorithms` property. You should confirm that it's not equal to `FtpHashAlgorithm.NONE` (which means the server does not support the HASH command).

- **GetHashAlgorithm**() - Query the server for the currently selected hash algorithm for the HASH command. 

- **SetHashAlgorithm**() - Selects a hash algorithm for the HASH command, and stores this selection on the server. 

**Non-standard commands supported by certain servers only**

Please import `FluentFTP.Extensions` to use these.

- **GetChecksum**() - Retrieves a checksum of the given file using a checksumming method that the server supports, if any. The algorithm used goes in this order : HASH, MD5, XMD5, XSHA1, XSHA256, XSHA512, XCRC.

- **GetMD5**() - Retrieves the MD5 checksum of the given file, if the server supports it.

- **GetXMD5**() - Retrieves the MD5 checksum of the given file, if the server supports it.

- **GetXSHA1**() - Retrieves the SHA1 checksum of the given file, if the server supports it.

- **GetXSHA256**() - Retrieves the SHA256 checksum of the given file, if the server supports it.

- **GetXSHA512**() - Retrieves the SHA512 checksum of the given file, if the server supports it.

- **GetXCRC**() - Retrieves the CRC32 checksum of the given file, if the server supports it.


## FTPS

- **EncryptionMode** - Type of SSL to use, or none. Explicit is TLS, Implicit is SSL. **Default:** FtpEncryptionMode.None.

- **DataConnectionEncryption** - Indicates if data channel transfers should be encrypted. **Default:** true.

- **SslProtocols** - Encryption protocols to use. **Default:** SslProtocols.Default.

- **ClientCertificates** - X509 client certificates to be used in SSL authentication process.

- **ValidateCertificate** - Event is fired to validate SSL certificates. If this event is not handled and there are errors validating the certificate the connection will be aborted.

## Advanced Settings

- **GetDataType**() - Checks if the transfer data type is ASCII or binary.

- **SetDataType**() - Sets the transfer data type to ASCII or binary. Internally called during file reads, writes and appends.

- **DataConnectionType** - Active or Passive connection. **Default:** FtpDataConnectionType.AutoPassive (tries EPSV then PASV then gives up)

- **UngracefullDisconnection** - Disconnect from the server without sending QUIT. **Default:** false.

- **Encoding** - Text encoding (ASCII or UTF8) used when talking with the server. ASCII is default, but upon connection, we switch to UTF8 if supported by the server. Manually setting this value overrides automatic detection. **Default:** Auto.

- **InternetProtocolVersions** - Whether to use IPV4 and/or IPV6 when making a connection. All addresses returned during name resolution are tried until a successful connection is made. **Default:** Any.

- **SocketPollInterval** - Time that must pass (in milliseconds) since the last socket activity before calling `Poll()` on the socket to test for connectivity. Setting this interval too low will have a negative impact on perfomance. Setting this interval to 0 disables Poll()'ing all together. **Default:** 15000 (15 seconds).

- **ConnectTimeout** - Time to wait (in milliseconds) for a connection attempt to succeed, before giving up. **Default:** 15000 (15 seconds).

- **ReadTimeout** - Time to wait (in milliseconds) for data to be read from the underlying stream, before giving up. **Default:** 15000 (15 seconds).

- **DataConnectionConnectTimeout** - Time to wait (in milliseconds) for a data connection to be established, before giving up. **Default:** 15000 (15 seconds).

- **DataConnectionReadTimeout** - Time to wait (in milliseconds) for the server to send data on the data channel, before giving up. **Default:** 15000 (15 seconds).

- **SocketKeepAlive** - Set `SocketOption.KeepAlive` on all future stream sockets. **Default:** false.

- **StaleDataCheck** - Check if there is stale (unrequested data) sitting on the socket or not. In some cases the control connection may time out but before the server closes the connection it might send a 4xx response that was unexpected and can cause synchronization errors with transactions. To avoid this problem the Execute() method checks to see if there is any data available on the socket before executing a command. **Default:** true.

- **TransferChunkSize** - Chunk size (in bytes) used during upload/download of files. **Default:** 65536 (65 KB).

- **MaximumDereferenceCount** - The maximum depth of recursion that `DereferenceLink()` will follow symbolic links before giving up. **Default:** 20.

- **EnableThreadSafeDataConnections** - Creates a new FTP connection for every file download and upload. This is slower but is a thread safe approach to make asynchronous operations on a single control connection transparent. Set this to `false` if your FTP server allows only one connection per username. **Default:** false.

- **IsClone** - Checks if this control connection is a clone. **Default:** false.


# Notes
## Stream Handling

FluentFTP returns a `Stream` object for file transfers. This stream **must** be properly closed when you are done. Do not leave it for the GC to cleanup otherwise you can end up with uncatchable exceptions, i.e., a program crash. The stream objects are actually wrappers around `NetworkStream` and `SslStream` which perform cleanup routines on the control connection when the stream is closed. These cleanup routines can trigger exceptions so it's vital that you properly dispose the objects when you are done, no matter what. A proper implementation should go along the lines of:

``````
try {
   using(Stream s = ftpClient.OpenRead()) {
       // perform your transfer
   }
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
}
``````

The finally block above ensures that `Close()` is always called on the stream even if a problem occurs. When `Close()` is called any resulting exceptions can be caught and handled accordingly.

## Exception Handling during Dispose()

FluentFTP includes exception handling in key places where uncatchable exceptions could occur, such as the `Dispose()` methods. The problem is that part of the cleanup process involves closing out the internal sockets and streams. If `Dispose()` was called because of an exception and triggers another exception while trying to clean-up you could end up with an un-catchable exception resulting in an application crash. To deal with this `FtpClient.Dispose()` and `FtpSocketStream.Dispose()` are setup to handle `SocketException` and `IOException` and discard them. The exceptions are written to the FtpTrace `TraceListeners` for debugging purposes, in an effort to not hide important errors while debugging problems with the code.

The exception that propagates back to your code should be the root of the problem and any exception caught while disposing would be a side affect however while testing your project pay close attention to what's being logged via FtpTrace. See the Debugging example for more information about using `TraceListener` objects with FluentFTP.

## File Listings

Some of you may already be aware that RFC959 does not specify any particular format for file listings (LIST). As time has passed extensions have been added to address this problem. Here's what you need to know about the situation:

1. **UNIX File Listings :** UNIX style file listings are NOT reliable. Most FTP servers respond to LIST with a format that strongly resembles the output of ls -l on UNIX and UNIX-like operating systems. This format is difficult to parse and has shortcomings with date/time values in which assumptions have to made in order to try to guess an as accurate as possible value. FluentFTP provides a LIST parser but there is no guarantee that it will work right 100% of the time. You can add your own parser if it doesn't. See the examples project in the source code. You should include the `FtpListOption.Modify` flag for the most accurate modification dates (down to the second). MDTM will fail on directories on most but not all servers. An attempt is made by FluentFTP to get the modification time of directories using this command but do not be surprised if it fails. Modification times of directories should not be important most of the time. If you think they are you might want to reconsider your reasoning or upgrade the server software to one that supports MLSD (machine listings: the best option).

2. **DOS/IIS File Listings :** DOS style file listings (default IIS LIST response) are mostly supported. There is one issue where a file or directory that begins with a space won't be correctly parsed because of the arbitrary amount of spacing IIS throws in its directory listings. IIS can be configured to throw out UNIX style listings so if this is an issue for you, you might consider enabling that option. If you know a better way to parse the listing you can roll your own parser per the examples included with the downloads. If it works share it! It's also worth noting that date/times in DOS style listings don't include seconds. As mentioned above, if you pass the `FtpListOption.Modify` flag to GetListing() MDTM will be used to get the accurate (to the second) date/time however MDTM on directories does not work with IIS.

3. **Machine Listings :** FluentFTP prefers machine listings (MLST/MLSD) which are an extension added to the protocol. This format is reliable and is always used over LIST when the server advertises it in its FEATure list unless you override the behavior with `FtpListOption` flags. With machine listings you can expect a correct file size and modification date (UTC). If you run across a case that you are not it's very possible it's due to a bug in the machine listing parser and you should report the issue along with a sample of the file listing (see the debugging example in the source).

4. **Name Listings :** Name Listings (NLST) are the next best thing when machine listings are not available however they are MUCH slower than either LIST or MLSD. This is because NLST sends a list of objects in the directory and the server has to be queried for the rest of the information on file-by-file basis, such as the file size, the modification time and an attempt to determine if the object is a file or directory. Name listings can be forced using `FtpListOption` flags. The best way to handle falling back to NLST is to query the server features (FtpClient.Capabilities) for the FtpCapability.MLSD flag. If it's not there, then pass the necessary flags to GetListing() to force a name listing.

## Client Certificates

When you are using Client Certificates, be sure that:

1. You use X509Certificate2 objects, not the incomplete X509Certificate implementation.

2. You do not use pem certificates, use p12 instead. See this [Stack Overflow thread](http://stackoverflow.com/questions/13697230/ssl-stream-failed-to-authenticate-as-client-in-apns-sharp) for more information. If you get SPPI exceptions with an inner exception about an unexpected or badly formatted message, you are probably using the wrong type of certificate.

## Slow SSL Negotiation

FluentFTP uses `SslStream` under the hood which is part of the .NET framework. `SslStream` uses a feature of windows for updating root CA's on the fly, at least that's the way I understand it. These updates can cause a long delay in the certificate authentication process which can cause issues in FluentFTP related to the SocketPollInterval property used for checking for ungraceful disconnections between the client and server. This [MSDN Blog](http://blogs.msdn.com/b/alejacma/archive/2011/09/27/big-delay-when-calling-sslstream-authenticateasclient.aspx) covers the issue with SslStream and talks about how to disable the auto-updating of the root CA's.

The latest builds of FluentFTP log the time it takes to authenticate. If you think you are suffering from this problem then have a look at Examples\Debug.cs for information on retrieving debug information.

## Handling Ungraceful Interruptions in the Control Connection

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

## XCRC / XMD5 / XSHA

XCRC, XMD5, and XSHA are non standard commands and to the best that I can tell contain no kind of formal specification. Support for them exists as extension methods in the FluentFTP.Extensions namespace as of the latest revision. They are not guaranteed to work and you are strongly encouraged to check the FtpClient.Capabilities flags for the respective flag (XCRC, XMD5, XSHA1, XSHA256, XSHA512) before calling these methods.

Support for the MD5 command as described [here](http://tools.ietf.org/html/draft-twine-ftpmd5-00#section-3.1) has also been added. Again, check for FtpFeature.MD5 before executing the command.

Experimental support for the HASH command has been added to FluentFTP. It supports retrieving SHA-1, SHA-256, SHA-512, and MD5 hashes from servers that support this feature. The returned object, FtpHash, has a method to check the result against a given stream or local file. You can read more about HASH in [this draft](http://tools.ietf.org/html/draft-bryan-ftpext-hash-02).

## Pipelining

If you just wanting to enable pipelining (in `FtpClient` and `FtpControlConnection`), set the `EnablePipelining` property to true. Hopefully this is all you need but it may not be. Some servers will drop the control connection if you flood it with a lot of commands. This is where the `MaxPipelineExecute` property comes into play. The default value here is 20, meaning that if you have 100 commands queued, 20 of the commands will be written to the underlying socket and 20 responses will be read, then the next 20 will be executed, and so forth until the command queue is empty. The value 20 is not a magic number, it's just the number that I deemed stable in most scenarios. If you increase the value, do so knowing that it could break your control connection.

## Pipelining your own Commands

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

## Bulk Downloads

When doing a large number of transfers, one needs to be aware of some inherit issues with data streams. When a socket is opened and then closed, the socket is left in a linger state for a period of time defined by the operating system. The socket cannot reliably be re-used until the operating system takes it out of the TIME WAIT state. This matters because a data stream is opened when it's needed and closed as soon as that specific task is done:
- Download File
  - Open Data Stream
    - Read bytes
  - Close Data Stream

This is not a bug in FluentFTP. RFC959 says that EOF on stream mode transfers is signaled by closing the connection. On downloads and file listings, the sockets being used on the server will stay in the TIME WAIT state because the server closes the socket when it's done sending the data. On uploads, the client sockets will go into the TIME WAIT state because the client closes the connection to signal EOF to the server.

RFC959 defines another data mode called block that allows persistent data connections but it is not implemented by this library and will not be in the foreseeable future. Support for block transfers on the server side of things is not that common. I know IIS supports them however I cannot name a single other server that implements MODE B. I cannot justify making an already complicated process more so by adding in a feature that just isn't that likely to be used.
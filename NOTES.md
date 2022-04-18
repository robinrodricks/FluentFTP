# Notes

### Semantic Versioning

FluentFTP uses [semantic versioning](http://semver.org/), a package numbering scheme that indicates API compatibility between releases. A version consists of `MAJOR.MINOR.PATCH`, that use this scheme:

  - **Major** version changed when incompatible/breaking changes are made to the API
    - eg: Methods/properties are removed, Method arguments are removed/refactored
 	
  - **Minor** version changed when functionality has been added in a backwards-compatible manner
    - eg: Methods/properties are added, New arguments added into methods
 	
  - **Patch** version changed when backwards-compatible bug fixes are released 
    - eg: Fixes/minor features are added

### Stream Handling

FluentFTP returns a `Stream` object for file transfers. This stream **must** be properly closed when you are done. Do not leave it for the GC to cleanup otherwise you can end up with uncatchable exceptions, i.e., a program crash. The stream objects are actually wrappers around `NetworkStream` and `SslStream` which perform cleanup routines on the control connection when the stream is closed. These cleanup routines can trigger exceptions so it's vital that you properly dispose the objects when you are done, no matter what. A proper implementation should go along the lines of:

``````
try {
   using(Stream s = ftpClient.OpenRead()) {
       // perform your transfer
   }
   ftpClient.GetReply(); // read success/failure messages from server
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
		s.Close();
	ftpClient.GetReply(); // read success/failure messages from server
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

// initialize the pipeline
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



# Credits

  - [J.P. Trosclair](https://github.com/jptrosclair) - Original creator, owner up to 2016, FTP/FTPS support, User authentication, Low level upload/download/append API, Basic file management commands, File hashing & checksums
  - [Robin Rodricks](https://github.com/robinrodricks) - Owner and maintainer from 2016 onwards, Nuget package, .NET 2.0 version, .NET core version, documentation (API docs, FTP support table, FAQ), MSBuild automation, High level upload/download API, Reliable chunked file transfer, Byte/stream upload/download API, Multi file upload/download, OS-specific directory listing parsers, Chmod/file permissions, CCC command support, New commands (SetModifiedTime, MoveFile, MoveDirectory), Rewritte DeleteDirectory & FileExists, Server timezone conversion, Hiding sensitive data from logs, Argument validation, Numerous fixes and maintenance
  - [Artiom Chilaru](https://github.com/artiomchi) - Migrate to a single VS 2017 solution, Continuous Integration using AppVeyor, New async methods for UploadFile/DownloadFile/UploadFiles/DownloadFiles, Numerous fixes and improvements for .NET core
  - [Jordan Blacker](https://github.com/jblacker) - `async`/`await` support for all methods, post-transfer hash verification, configurable error handling, multiple log levels
  - [Zhaoquan Huang](https://github.com/taoyouh) - Async methods for .NET Standard, Fixes and improvements
  - [Atif Aziz](https://github.com/atifaziz) & Joseph Albahari - LINQBridge (allows LINQ in .NET 2.0)
  - [R. Harris](https://github.com/rharrisxtheta) - Fixes and improvements
  - [Roberto Sarati](https://github.com/sierrodc) - Fixes and improvements
  - [Amer Koleci](https://github.com/amerkoleci) - Fixes and improvements
  - [Tim Horemans](https://github.com/worstenbrood) - Fixes and improvements
  - [Nerijus Dzindzeleta](https://github.com/NerijusD) - Fixes and improvements
  - [Rune Ibsen](https://github.com/ibsenrune) - Fixes and improvements
  - [Lukazoid](https://github.com/Lukazoid) - Fixes to FtpDataStream

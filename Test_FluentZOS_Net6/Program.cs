using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;
using FluentFTP.Client.BaseClient;

namespace Test_FluentZOS {
	internal class Program {

		static void Main(string[] args) {
			// Note: .GetAwaiter().GetResult()

			/*
				Task.Run(async () =>
				{
					My_ZOS_Test();
				}).GetAwaiter().GetResult();
				*/

			// My_ZOS_Test();

			My_FTPS_Test();

			// My_941_Test();

			// My_946_Test();


			Thread.Sleep(1000);

			Console.WriteLine("Finished");

			Console.ReadLine();
		}

		/*
		public class CustomConsoleLogger : ILogger {
			public CustomConsoleLogger() {
			}

			public IDisposable BeginScope<TState>(TState state) {
				return null;
			}

			public bool IsEnabled(LogLevel logLevel) {
				return true;
			}

			public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) {
				Console.WriteLine($"{logLevel.ToString()} - {eventId.Id} - {formatter(state, exception)}");
				//Console.WriteLine($"{formatter(state, exception)}");
			}
		}
		public class CustomLoggerProvider : ILoggerProvider {
			public CustomLoggerProvider() { 
			}

			public ILogger CreateLogger(string name) {
				return new CustomConsoleLogger();
			}

			public void Dispose() {
			}
		}
		*/

		static void My_FTPS_Test() {
			FtpClient FTP_Sess = new FtpClient();

			string uriHost = "ftps";
			int myPort = 21;

			if (uriHost.StartsWith("ftps://"))
				myPort = 990;
			else if (uriHost.StartsWith("ftp://"))
				myPort = 21;
			else {
				uriHost = "ftp://" + uriHost;
				myPort = 21;
			}

			Uri uri = null;

			try {
				uri = new Uri(uriHost);
			}
			catch (UriFormatException) {

			}

			if (!uri.IsDefaultPort)
				myPort = uri.Port;

			void progress(FtpProgress p) {
				//Console.WriteLine(p.Progress.ToString("n0") + "% complete");
				//Console.SetCursorPosition(0, Console.CursorTop - 1);
			}

			Console.WriteLine("Scheme: " + uri.Scheme);
			Console.WriteLine("HostNameType: " + uri.HostNameType);
			Console.WriteLine("Host: " + uri.Host);
			Console.WriteLine("Port: " + myPort);

			FTP_Sess.Config.LogToConsole = true;

			FTP_Sess.Host = uri.DnsSafeHost;
			FTP_Sess.Port = myPort;

			FTP_Sess.Credentials.UserName = "mike";
			FTP_Sess.Credentials.Password = "7u8i9o0p";

			FTP_Sess.Config.ValidateAnyCertificate = true;

			FTP_Sess.ValidateCertificate += new FtpSslValidation(OnValidateCertificate);

			FTP_Sess.Config.LogToConsole = false;

			FTP_Sess.Config.LogPassword = false;
			FTP_Sess.Config.LogUserName = false;

			FTP_Sess.LegacyLogger = FTPLogEvent;

			FTP_Sess.Config.DataConnectionType = FtpDataConnectionType.AutoPassive;

			FTP_Sess.Config.SslBuffering = FtpsBuffering.On;

			FTP_Sess.Config.NoopInterval = 500;

			FTP_Sess.AutoConnect();

			/*
			FTP_Sess.SetWorkingDirectory("/home/mike/[].folder/dia grams/old");
			FtpListItem[] Items = FTP_Sess.GetListing("", FtpListOption.ForceList | FtpListOption.NoPath);
			FTP_Sess.GetFileSize("/home/mike/test.bin");
			FtpHash hash = FTP_Sess.GetChecksum("/home/mike/te st.bin");
			FTP_Sess.GetFileSize("/home/mike/te st.bin");
			*/

			/*
			try {
				FTP_Sess.DeleteFile("/home/mike/test1");
				FTP_Sess.DeleteFile("/home/mike/test2");
				FTP_Sess.DeleteFile("/home/mike/test3");
			}
			catch { }
			

			FTP_Sess.Config.DataConnectionType = FluentFTP.FtpDataConnectionType.PORT;
			FTP_Sess.Config.ActivePorts = new int[] { 8082 };

			FTP_Sess.UploadFile(@"D:\temp\test1", "/home/mike/test1", FluentFTP.FtpRemoteExists.AddToEnd, false);
			FTP_Sess.UploadFile(@"D:\temp\test2", "/home/mike/test2", FluentFTP.FtpRemoteExists.AddToEnd, false);
			FTP_Sess.UploadFile(@"D:\temp\test3", "/home/mike/test3", FluentFTP.FtpRemoteExists.AddToEnd, false);
			*/


			// Download fails on ASCII if size is 2GB - need to check.
			FTP_Sess.Config.DownloadDataType = FtpDataType.Binary;
			FtpStatus status1 = FTP_Sess.DownloadFile("D:\\temp\\test1.bin", "/home/mike/test1", FtpLocalExists.Overwrite, FtpVerify.None, progress);
			FTP_Sess.Execute("FEAT");
			FtpStatus status2 = FTP_Sess.DownloadFile("D:\\temp\\test2.bin", "/home/mike/test2", FtpLocalExists.Overwrite, FtpVerify.None, progress);


			/*
			FTP_Sess.DeleteDirectory("/home/mike/ghostscript", FtpListOption.Auto);
			*/

			FTP_Sess.Disconnect();
		}

		static bool My_ZOS_Test() {
			FtpClient FTP_Sess = new FtpClient();

			string uriHost = "deve";
			int myPort = 21;

			if (uriHost.StartsWith("ftps://"))
				myPort = 990;
			else if (uriHost.StartsWith("ftp://"))
				myPort = 21;
			else {
				uriHost = "ftp://" + uriHost;
				myPort = 21;
			}

			Uri uri = null;

			try {
				uri = new Uri(uriHost);
			}
			catch (UriFormatException) {

			}

			if (!uri.IsDefaultPort)
				myPort = uri.Port;

			string used = "99999";
			var size = long.Parse(used) * 56664;
			long sizel = 16777215L * 56664L;

			void progress(FtpProgress p) {
				Console.WriteLine(p.Progress.ToString() + "% complete");
				Console.SetCursorPosition(0, Console.CursorTop - 1);
			}

			Console.WriteLine("Scheme: " + uri.Scheme);
			Console.WriteLine("HostNameType: " + uri.HostNameType);
			Console.WriteLine("Host: " + uri.Host);
			Console.WriteLine("Port: " + myPort);

			FTP_Sess.Host = uri.DnsSafeHost;
			FTP_Sess.Port = myPort;

			/*
			// Either:

			// FTP_Sess.Config.LogToConsole = true;

			// Or:

			ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
			{
			//builder.AddConsole();
			//builder.AddDebug();
			});
    
			loggerFactory.AddProvider(new CustomLoggerProvider());
    
			ILogger<BaseFtpClient> logger = loggerFactory.CreateLogger<BaseFtpClient>();
    
			FTP_Sess.Logger = logger;
			*/


			FTP_Sess.Credentials.UserName = "ansys";
			FTP_Sess.Credentials.Password = "callisto";
			FTP_Sess.Config.ValidateAnyCertificate = true;

			FTP_Sess.ValidateCertificate += new FtpSslValidation(OnValidateCertificate);

			FTP_Sess.Config.LogPassword = false;
			FTP_Sess.Config.LogUserName = true;

			FTP_Sess.LegacyLogger = FTPLogEvent;

			FTP_Sess.AutoConnect();

			FTP_Sess.Config.DataConnectionType = FtpDataConnectionType.AutoPassive;

			FtpReply reply;
			FtpStatus status;

			FTP_Sess.GetFileSize("/projects/test.bin");

			FTP_Sess.Execute("TYPE I");

			FTP_Sess.Config.DownloadDataType = FtpDataType.Binary;
			status = FTP_Sess.DownloadFile("D:\\temp\\test.bin", "/projects/test.bin", FtpLocalExists.Overwrite, FtpVerify.None, progress);

			FTP_Sess.Config.DownloadDataType = FtpDataType.ASCII;
			status = FTP_Sess.DownloadFile("D:\\temp\\test.bin", "/projects/test.bin", FtpLocalExists.Overwrite, FtpVerify.None, progress);

			status = FTP_Sess.DownloadFile("D:\\temp\\test.bin", "'ANSYS.$WEBEX.TRCLIB(CTRACE)'", FtpLocalExists.Overwrite, FtpVerify.None, progress);

			reply = FTP_Sess.Execute("FEAT");

			bool exist1 = FTP_Sess.FileExists("$WEBEX.TRCLIB(CTRACE)");
			FTP_Sess.SetWorkingDirectory("/projects");
			bool exist2 = FTP_Sess.FileExists("test.bin");
			bool exist3 = FTP_Sess.FileExists("/projects/test.bin");

			FTP_Sess.Config.DownloadDataType = FtpDataType.Binary;
			status = FTP_Sess.DownloadFile("D:\\temp\\test.bin", "test.bin", FtpLocalExists.Overwrite, FtpVerify.None, progress);

			FTP_Sess.Config.DownloadDataType = FtpDataType.ASCII;
			status = FTP_Sess.DownloadFile("D:\\temp\\test.bin", "test.bin", FtpLocalExists.Overwrite, FtpVerify.None, progress);

			FTP_Sess.Config.DownloadDataType = FtpDataType.Binary;
			status = FTP_Sess.DownloadFile("D:\\temp\\test.bin", "test.bin", FtpLocalExists.Overwrite, FtpVerify.None, progress);

			FTP_Sess.SetWorkingDirectory("'ANSYS'");

			FTP_Sess.Config.DownloadDataType = FtpDataType.ASCII;
			//reply = FTP_Sess.Execute("SITE NORDW");
			//FTP_Sess.DownloadFile("D:\\temp\\test.data", "$WEBEX.TRCLIB(CTRACE)", FtpLocalExists.Overwrite, FtpVerify.None, progress);


			// with path
			FtpListItem[] Listu1 = FTP_Sess.GetListing("/projects");

			// with NoPath option
			FTP_Sess.SetWorkingDirectory("/projects");
			bool isroot1 = FTP_Sess.IsRoot();

			FtpListItem[] Listn1 = FTP_Sess.GetListing("", FtpListOption.NoPath);

			// back to z/OS Realm
			FTP_Sess.SetWorkingDirectory("'ANSYS'");

			// with path
			FtpListItem[] Lista1 = FTP_Sess.GetListing("$2CIPDEV.*");
			FtpListItem[] Lista2 = FTP_Sess.GetListing("$2CIPDEV.CCLIB(*)");
			FtpListItem[] Lista3 = FTP_Sess.GetListing("$2CIPDEV.CCLIB");
			FtpListItem[] Lista4 = FTP_Sess.GetListing("'ANSYS.$2CIPDEV.*'");
			FtpListItem[] Lista5 = FTP_Sess.GetListing("'ANSYS.$2CIPDEV.CCLIB(*)'");
			FtpListItem[] List6 = FTP_Sess.GetListing("'ANSYS.$2CIPDEV.CCLIB'");

			// with NoPath option
			FTP_Sess.SetWorkingDirectory("'ANSYS'");
			bool isroot2 = FTP_Sess.IsRoot();
			FTP_Sess.SetWorkingDirectory("$2CIPDEV");
			FtpListItem[] Listb1 = FTP_Sess.GetListing("", FtpListOption.NoPath);
			FTP_Sess.SetWorkingDirectory("$2CIPDEV.CCLIB");
			FtpListItem[] Listb2 = FTP_Sess.GetListing("", FtpListOption.NoPath);

			FTP_Sess.SetWorkingDirectory("'SYS1'");
			FtpListItem[] Listh1 = FTP_Sess.GetListing("", FtpListOption.NoPath);

			FTP_Sess.SetWorkingDirectory("'ANSYS.$WEBEX.VITTRACE'");

			FtpListItem[] listm = FTP_Sess.GetListing("'ANSYS.$WEBEX.TRCLIB(CTRACE)'", FtpListOption.ForceList);

			long size1 = FTP_Sess.GetFileSize("'ANSYS.KDTR.HUK.ONTOP.SY1.SYSTCPDA'");
			long size2 = FTP_Sess.GetFileSize("'ANSYS.$WEBEX.TRCLIB(CTRACE)'");

			//FTP_Sess.DownloadDataType = FtpDataType.Binary;
			//status = FTP_Sess.DownloadFile("D:\\temp\\test.bin", "'ANSYS.KDTR.HUK.ONTOP.SY1.SYSTCPDA'", FtpLocalExists.Overwrite, FtpVerify.None, progress);

			FTP_Sess.Config.UploadDataType = FtpDataType.ASCII;




			FTP_Sess.SetWorkingDirectory("'ANSYS.FLUENT'");

			reply = FTP_Sess.Execute("SITE LRECL=80 BLKSIZE=3120 RECFM=FB PRI=100 CYL");

			status = FTP_Sess.UploadFile("D:\\temp\\test.bin", "HUK.ONTOP.SY1.SYSTCPDA", FtpRemoteExists.AddToEndNoCheck, false, FtpVerify.None, progress);



			string[] uploadFiles = new string[3] { "D:\\temp\\test1", "D:\\temp\\test2", "D:\\temp\\test3" };

			//FTP_Sess.SetWorkingDirectory("'ANSYS.FLUENT.FILES'");

			Console.WriteLine("Listing realm is " + FTP_Sess.zOSListingRealm);

			var counter1 = FTP_Sess.UploadFiles(uploadFiles, "'ANSYS.FLUENT.FILES'", FtpRemoteExists.NoCheck, false, FtpVerify.None, FtpError.None, progress);
			Console.WriteLine("Listing realm is " + FTP_Sess.zOSListingRealm);

			var counter2 = FTP_Sess.UploadFiles(uploadFiles, "/projects", FtpRemoteExists.NoCheck, false, FtpVerify.None, FtpError.None, progress);
			Console.WriteLine("Listing realm is " + FTP_Sess.zOSListingRealm);





			FTP_Sess.SetWorkingDirectory("/projects/md:");

			string[] downloadFiles = new string[3] { "test1", "test2", "test3" };

			FTP_Sess.DownloadFiles("D:\\temp\\testdir", downloadFiles, FtpLocalExists.Overwrite, FtpVerify.None, FtpError.None);


			//FTP_Sess.ListingParser = FtpParser.IBMzOS; // If you don't do this, it will fail. PLS Debug

			FTP_Sess.DownloadDirectory("D:\\temp\\testdir1", "/projects/md:", FtpFolderSyncMode.Mirror, FtpLocalExists.Overwrite, FtpVerify.None, null);

			FTP_Sess.Disconnect();

			return true;
		}


		static void My_941_Test() {
			FtpClient FTP_Sess = new FtpClient();

			string uriHost = "ftps";
			int myPort = 21;

			if (uriHost.StartsWith("ftps://"))
				myPort = 990;
			else if (uriHost.StartsWith("ftp://"))
				myPort = 21;
			else {
				uriHost = "ftp://" + uriHost;
				myPort = 21;
			}

			Uri uri = null;

			try {
				uri = new Uri(uriHost);
			}
			catch (UriFormatException) {

			}

			if (!uri.IsDefaultPort)
				myPort = uri.Port;

			void progress(FtpProgress p) {
				Console.WriteLine(p.Progress.ToString() + "% complete");
				Console.SetCursorPosition(0, Console.CursorTop - 1);
			}

			Console.WriteLine("Scheme: " + uri.Scheme);
			Console.WriteLine("HostNameType: " + uri.HostNameType);
			Console.WriteLine("Host: " + uri.Host);
			Console.WriteLine("Port: " + myPort);

			FTP_Sess.Config.LogToConsole = true;

			FTP_Sess.Host = uri.DnsSafeHost;
			FTP_Sess.Port = myPort;

			FTP_Sess.Credentials.UserName = "mike";
			FTP_Sess.Credentials.Password = "7u8i9o0p";

			FTP_Sess.Config.ValidateAnyCertificate = true;

			FTP_Sess.ValidateCertificate += new FtpSslValidation(OnValidateCertificate);

			FTP_Sess.Config.LogToConsole = false;

			FTP_Sess.Config.LogPassword = false;
			FTP_Sess.Config.LogUserName = false;

			FTP_Sess.LegacyLogger = FTPLogEvent;
			var sw = new System.Diagnostics.Stopwatch();

			//FTP_Sess.Config.SocketLocalIp = IPAddress.Parse("192.168.1.106");

			sw.Start();

			try {
				FTP_Sess.AutoConnect();
				for (int i = 0; i < 1000; i++) {
					FTP_Sess.UploadFile("d:\\temp\\D220412A.pcapng", "/home/mike/test1", FtpRemoteExists.Overwrite, true, FtpVerify.None, progress);
				}

			}
			catch (Exception ex) {
				Console.WriteLine(ex.Message);
			}
			finally {
				FTP_Sess.Disconnect();
			}

			sw.Stop();

			var ts = sw.Elapsed;
			Console.WriteLine($"{ts}");
		}

		static void My_946_Test() {
			FtpClient FTP_Sess = new FtpClient();

			string uriHost = "deve";
			int myPort = 21;

			if (uriHost.StartsWith("ftps://"))
				myPort = 990;
			else if (uriHost.StartsWith("ftp://"))
				myPort = 21;
			else {
				uriHost = "ftp://" + uriHost;
				myPort = 21;
			}

			Uri uri = null;

			try {
				uri = new Uri(uriHost);
			}
			catch (UriFormatException) {

			}

			if (!uri.IsDefaultPort)
				myPort = uri.Port;

			string used = "99999";
			var size = long.Parse(used) * 56664;
			long sizel = 16777215L * 56664L;

			void progress(FtpProgress p) {
				Console.WriteLine(p.Progress.ToString() + "% complete");
				Console.SetCursorPosition(0, Console.CursorTop - 1);
			}

			Console.WriteLine("Scheme: " + uri.Scheme);
			Console.WriteLine("HostNameType: " + uri.HostNameType);
			Console.WriteLine("Host: " + uri.Host);
			Console.WriteLine("Port: " + myPort);

			FTP_Sess.Host = uri.DnsSafeHost;
			FTP_Sess.Port = myPort;

			FTP_Sess.Credentials.UserName = "ansys";
			FTP_Sess.Credentials.Password = "callisto";
			FTP_Sess.Config.ValidateAnyCertificate = true;

			FTP_Sess.ValidateCertificate += new FtpSslValidation(OnValidateCertificate);

			FTP_Sess.Config.LogPassword = false;
			FTP_Sess.Config.LogUserName = true;

			FTP_Sess.LegacyLogger = FTPLogEvent;

			FTP_Sess.AutoConnect();

			FTP_Sess.Config.DataConnectionType = FtpDataConnectionType.AutoPassive;


			FTP_Sess.SetWorkingDirectory("/projects");

			FtpListItem[] List1 = FTP_Sess.GetListing("downloads", FtpListOption.Auto);




			FTP_Sess.Disconnect();
		}

		private static void FTPLogEvent(FtpTraceLevel ftpTraceLevel, string logMessage) {
			//Console.WriteLine("*** " + ftpTraceLevel + " " + logMessage);
			Console.WriteLine(logMessage);
		}
		private static void OnValidateCertificate(BaseFtpClient control, FtpSslValidationEventArgs e) {
			if (e.PolicyErrors != System.Net.Security.SslPolicyErrors.None) {
				// invalid cert
				Console.WriteLine("Certificate validation failure\r\nDetails:\r\n\r\n" + e.Certificate.ToString());
				Console.WriteLine("Error during connect");

				e.Accept = false;
			}
			else {
				e.Accept = true;
			}
		}
	}

}

/*
 * Question: Using FTPS, after uploading a very large file, my next directory listing fails:

  425 Unable to build data connection: Operation not permitted
The TLSLog contains:
  client did not reuse SSL session, rejecting data connection (see the NoSessionReuseRequired TLSOptions parameter)
but I do not want to use that option, and would like to rely on the additional security protection provided by requring SSL session reuse. And my FTPS client is correctly reusing SSL session IDs (as earlier data transfers were working properly). So why is my data transfer failing after the upload of a very large file?
Answer: The answer involves SSL session caching on the server side (i.e. mod_tls), cache timeouts, and session renegotiations.
By default, mod_tls uses OpenSSL's "internal" session cache, which is an in-memory caching of SSL session IDs. And by default, OpenSSL's internal session cache has a cache timeout of 5 minutes; after that amount of time in the internal session cache, a cached SSL session ID is considered stale and is available for reuse.

This means that 5 minutes or more into an FTPS session, even if your FTPS client reused an SSL session ID, the OpenSSL internal session cache will time out that SSL session ID. The next time your FTPS client goes to reuse that session ID for a data transfer, mod_tls won't find it in the OpenSSL internal session cache, and will think that your FTPS client is not reusing the SSL session ID as is required, and fail the transfer.

Fixing this situation requires two parts: a) the ability to change the cache timeout used for the OpenSSL internal session cache, and b) renegotiating the SSL session ID with the FTPS client periodically, to keep the SSL session ID up-to-date in the session cache.

The first part, configuring the session cache timeout for the OpenSSL internal session cache, is only possible in ProFTPD 1.3.4rc2 and later (see Bug#3580). The TLSSessionCache directive was modified to allow a configuration such as:

  TLSSessionCache internal: 1800
(Unfortunately, the ':' after "internal" is necessary.) This configures mod_tls such that the OpenSSL internal session cache uses a cache timeout of 1800 seconds (30 minutes), rather than the default of 300 seconds (5 minutes).
No matter how long you configure the cache timeout, eventually you will have a session which lasts longer than that timeout. Which brings us to the second part of the solution: renegotiating a new SSL session ID periodically, which keeps it fresh in the session cache. The TLSRenegotiate directive is needed for this. For example, the following configuration should address the issue of failed data transfers after very large uploads:

  TLSRenegotiate ctrl 1500 timeout 300
  TLSSessionCache internal: 1800
This tells mod_tls to request a renegotiation of the SSL session on the control channel every 1500 seconds (25 minutes), and to allow 300 seconds (5 minutes) for the client to perform the renegotiation. It also tells mod_tls to cache the SSL session data for 1800 seconds (30 minutes), i.e. longer than the renegotiation time of 1500 seconds.
This way, as long as your client supports renegotiations and is updating the SSL session ID properly for data transfers, when a data transfer is requested, the SSL session ID presented by the client should always be fresh and in the session cache.
*/
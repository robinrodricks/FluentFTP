using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using FluentFTP;
using System.Threading;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
#if ASYNC
using System.Threading.Tasks;
#endif
using System.IO.Compression;
using System.Text;
using FluentFTP.Proxy;
using System.Security.Authentication;
using Xunit;
using FluentFTP.Helpers;

namespace Tests {
	public class Tests {
		private const string Category_Code = "Code";
		private const string Category_PublicFTP = "PublicFTP";
		private const string Category_CustomFTP = "CustomerFTP";

		// SET THESE BEFORE RUNNING ANY TESTS!
		private const string m_host = "";
		private const string m_user = "";
		private const string m_pass = "";

		private const string m_proxy_host = "";
		private const int m_proxy_port = 0;

		private static readonly int[] connectionTypes = new[] {
			(int) FtpDataConnectionType.EPSV,
			(int) FtpDataConnectionType.EPRT,
			(int) FtpDataConnectionType.PASV,
			(int) FtpDataConnectionType.PORT
		};

		#region Helpers

		private void OnValidateCertificate(FtpClient control, FtpSslValidationEventArgs e) {
			e.Accept = true;
		}

		private static FtpClient NewFtpClient(string host = m_host, string username = m_user, string password = m_pass) {
			return new FtpClient(host, new NetworkCredential(username, password));
		}

		private static FtpClient NewFtpClient_NetBsd() {
			return NewFtpClient("ftp.netbsd.org", "ftp", "ftp");
		}

		private static FtpClient NewFtpClient_Tele2SpeedTest() {
			return new FtpClient("ftp://speedtest.tele2.net/");
		}
		private static FtpClient NewFtpClient_Inacessible() {
			return new FtpClient("ftp://bad.unknown.com/");
		}
		private static FtpClient NewFtpClient_Inacessible2() {
			return new FtpClient("ftp://192.168.0.0/");
		}

		#endregion

		
		public void TestListPathWithHttp11Proxy() {
			using (FtpClient cl = new FtpClientHttp11Proxy(new FtpProxyProfile {ProxyHost = "127.0.0.1", ProxyPort = 3128,})) // Credential = new NetworkCredential() 
			{
				FtpTrace.WriteLine("FTPClient::ConnectionType = '" + cl.ConnectionType + "'");

				cl.Credentials = new NetworkCredential(m_user, m_pass);
				cl.Host = m_host;
				cl.ValidateCertificate += OnValidateCertificate;
				cl.DataConnectionType = FtpDataConnectionType.PASV;
				cl.Connect();

				foreach (var item in cl.GetListing(null, FtpListOption.SizeModify | FtpListOption.ForceNameList)) {
					FtpTrace.WriteLine(item.Modified.Kind);
					FtpTrace.WriteLine(item.Modified);
				}
			}
		}

		
		public void TestListPath() {
			using (var cl = NewFtpClient()) {
				cl.EncryptionMode = FtpEncryptionMode.None;

				cl.GetListing();
				FtpTrace.WriteLine("Path listing succeeded");
				cl.GetListing(null, FtpListOption.NoPath);
				FtpTrace.WriteLine("No path listing succeeded");
			}
		}

		
		public void TestCheckCapabilities() {
			using (var cl = NewFtpClient()) {
				cl.CheckCapabilities = false;
				Debug.Assert(cl.HasFeature(FtpCapability.NONE), "Excepted FTP capabilities to be NONE.");
			}
		}

#if ASYNC
		
		public async Task TestListPathAsync() {
			using (var cl = NewFtpClient()) {
				cl.EncryptionMode = FtpEncryptionMode.None;

				await cl.GetListingAsync();
				FtpTrace.WriteLine("Path listing succeeded");
				await cl.GetListingAsync(null, FtpListOption.NoPath);
				FtpTrace.WriteLine("No path listing succeeded");
			}
		}
#endif

		
		public void StreamResponses() {
			using (var cl = NewFtpClient()) {
				cl.EncryptionMode = FtpEncryptionMode.None;
				cl.ValidateCertificate += OnValidateCertificate;

				using (var s = (FtpDataStream) cl.OpenWrite("test.txt")) {
					var r = s.CommandStatus;

					FtpTrace.WriteLine("");
					FtpTrace.WriteLine("Response to STOR:");
					FtpTrace.WriteLine("Code: " + r.Code);
					FtpTrace.WriteLine("Message: " + r.Message);
					FtpTrace.WriteLine("Informational: " + r.InfoMessages);

					r = s.Close();
					FtpTrace.WriteLine("");
					FtpTrace.WriteLine("Response after close:");
					FtpTrace.WriteLine("Code: " + r.Code);
					FtpTrace.WriteLine("Message: " + r.Message);
					FtpTrace.WriteLine("Informational: " + r.InfoMessages);
				}
			}
		}

#if ASYNC
		
		public async Task StreamResponsesAsync() {
			using (var cl = NewFtpClient()) {
				cl.EncryptionMode = FtpEncryptionMode.None;
				cl.ValidateCertificate += OnValidateCertificate;

				using (var s = (FtpDataStream) await cl.OpenWriteAsync("test.txt")) {
					var r = s.CommandStatus;

					FtpTrace.WriteLine("");
					FtpTrace.WriteLine("Response to STOR:");
					FtpTrace.WriteLine("Code: " + r.Code);
					FtpTrace.WriteLine("Message: " + r.Message);
					FtpTrace.WriteLine("Informational: " + r.InfoMessages);

					r = s.Close();
					FtpTrace.WriteLine("");
					FtpTrace.WriteLine("Response after close:");
					FtpTrace.WriteLine("Code: " + r.Code);
					FtpTrace.WriteLine("Message: " + r.Message);
					FtpTrace.WriteLine("Informational: " + r.InfoMessages);
				}
			}
		}
#endif

		
		public void TestUnixListing() {
			using (var cl = NewFtpClient()) {
				if (!cl.FileExists("test.txt")) {
					using (var s = cl.OpenWrite("test.txt")) {
					}
				}

				foreach (var i in cl.GetListing(null, FtpListOption.ForceList)) {
					FtpTrace.WriteLine(i);
				}
			}
		}

		
		[Trait("Category", Category_Code)]
		public void TestFtpPath() {
			var path = "/home/sigurdhj/errors/16.05.2014/asdasd/asd asd asd aa asd/Kooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooo::asdasd";

			FtpTrace.WriteLine(path.GetFtpDirectoryName());
			FtpTrace.WriteLine("/foobar/boo".GetFtpDirectoryName());
			FtpTrace.WriteLine("./foobar/boo".GetFtpDirectoryName());
			FtpTrace.WriteLine("./foobar".GetFtpDirectoryName());
			FtpTrace.WriteLine("/foobar".GetFtpDirectoryName());
			FtpTrace.WriteLine("foobar".GetFtpDirectoryName());
			FtpTrace.WriteLine(path.GetFtpFileName());
			FtpTrace.WriteLine("/foo/bar".GetFtpFileName());
			FtpTrace.WriteLine("./foo/bar".GetFtpFileName());
			FtpTrace.WriteLine("./bar".GetFtpFileName());
			FtpTrace.WriteLine("/bar".GetFtpFileName());
			FtpTrace.WriteLine("bar".GetFtpFileName());
		}

		
		public void TestGetObjectInfo() {
			using (var client = NewFtpClient()) {
				FtpTrace.WriteLine(client.GetObjectInfo("/public_html/temp/README.md"));
			}
		}

#if ASYNC && !CORE
		
		public async Task TestGetObjectInfoAsync() {
			using (var cl = NewFtpClient()) {
				cl.Encoding = Encoding.UTF8;

				var item = await cl.GetObjectInfoAsync("/Examples/OpenRead.cs");
				FtpTrace.WriteLine(item.ToString());
			}
		}
#endif

		
		public void TestManualEncoding() {
			using (var cl = NewFtpClient()) {
				cl.Encoding = Encoding.UTF8;

				using (var s = cl.OpenWrite("test.txt")) {
				}
			}
		}

		
		public void TestServer() {
			using (var cl = NewFtpClient()) {
				cl.EncryptionMode = FtpEncryptionMode.Explicit;
				cl.ValidateCertificate += OnValidateCertificate;

				foreach (var i in cl.GetListing("/")) {
					FtpTrace.WriteLine(i.FullName);
				}
			}
		}

		private void TestServerDownload(FtpClient client, string path) {
			foreach (var i in client.GetListing(path)) {
				switch (i.Type) {
					case FtpObjectType.Directory:
						TestServerDownload(client, i.FullName);
						break;

					case FtpObjectType.File:
						using (var s = client.OpenRead(i.FullName)) {
							var b = new byte[8192];
							var read = 0;
							long total = 0;

							try {
								while ((read = s.Read(b, 0, b.Length)) > 0) {
									total += read;

									Console.Write("\r{0}/{1} {2:p}          ",
										total, s.Length, (double) total / (double) s.Length);
								}

								Console.Write("\r{0}/{1} {2:p}       ",
									total, s.Length, (double) total / (double) s.Length);
							}
							finally {
								FtpTrace.WriteLine("");
							}
						}

						break;
				}
			}
		}

#if ASYNC
		private async Task TestServerDownloadAsync(FtpClient client, string path) {
			foreach (var i in await client.GetListingAsync(path)) {
				switch (i.Type) {
					case FtpFileSystemObjectType.Directory:
						await TestServerDownloadAsync(client, i.FullName);
						break;

					case FtpFileSystemObjectType.File:
						using (var s = await client.OpenReadAsync(i.FullName)) {
							var b = new byte[8192];
							var read = 0;
							long total = 0;

							try {
								while ((read = await s.ReadAsync(b, 0, b.Length)) > 0) {
									total += read;

									Console.Write("\r{0}/{1} {2:p}          ",
										total, s.Length, (double) total / (double) s.Length);
								}

								Console.Write("\r{0}/{1} {2:p}       ",
									total, s.Length, (double) total / (double) s.Length);
							}
							finally {
								FtpTrace.WriteLine("");
							}
						}

						break;
				}
			}
		}
#endif

#if !CORE14
		
		[Trait("Category", Category_PublicFTP)]
		public void TestDisposeWithMultipleThreads() {
			using (var cl = NewFtpClient_NetBsd()) {
				var t1 = new Thread(() => { cl.GetListing(); });

				var t2 = new Thread(() => { cl.Dispose(); });

				t1.Start();
				Thread.Sleep(2000);
				t2.Start();

				t1.Join();
				t2.Join();
			}
		}
#endif

		
		[Trait("Category", Category_Code)]
		public void TestConnectionFailure() {
			try {
				using (var cl = NewFtpClient("somefakehost")) {
					cl.ConnectTimeout = 5000;
					cl.Connect();
				}
			}
			catch (System.Net.Sockets.SocketException) {
			} // Expecting this
#if ASYNC
			catch (AggregateException ex) when (ex.InnerException is System.Net.Sockets.SocketException) {
			}

#endif
		}

		
		[Trait("Category", Category_PublicFTP)]
		public void TestNetBSDServer() {
			using (var client = NewFtpClient_NetBsd()) {
				foreach (var item in client.GetListing(null,
					FtpListOption.ForceList | FtpListOption.Modify | FtpListOption.DerefLinks)) {
					FtpTrace.WriteLine(item);

					if (item.Type == FtpObjectType.Link && item.LinkObject != null) {
						FtpTrace.WriteLine(item.LinkObject);
					}
				}
			}
		}

		
		public void TestGetListing() {
			using (var client = NewFtpClient()) {
				client.Connect();
				foreach (var i in client.GetListing("/public_html/temp/", FtpListOption.ForceList | FtpListOption.Recursive)) {
					//FtpTrace.WriteLine(i);
				}
			}
		}

		
		[Trait("Category", Category_PublicFTP)]
		public void TestGetListingBSDServer() {
			using (var client = NewFtpClient_NetBsd()) {
				client.Connect();

				// machine listing
				var listing = client.GetListing();

				// unix listing
				var listing2 = client.GetListing("/", FtpListOption.ForceList);
			}
		}

#if ASYNC
		
		[Trait("Category", Category_PublicFTP)]
		public async void TestGetListingBSDServerAsync() {
			using (var client = NewFtpClient_NetBsd()) {

				// machine listing
				var listing = await client.GetListingAsync();

				// unix listing
				var listing2 = await client.GetListingAsync("/", FtpListOption.ForceList);
			}
		}
#endif

		
		[Trait("Category", Category_PublicFTP)]
		// FIX : #768 NullOrEmpty is valid, means "use working directory".
		public void TestGetListingWorkingDirectoryBsdServer() {
			using (var client = NewFtpClient_NetBsd()) {
				// Setup
				client.Connect();

				const string directoryNetBsdName = "NetBSD";
				const string directoryPubName = "pub";
				var directoryPubNameFull = $"/{directoryPubName}";
				var directoryNetBsdNameFull = $"{directoryPubNameFull}/{directoryNetBsdName}";

				// Act
				client.SetWorkingDirectory(directoryPubName);
				var resultGetListing = client.GetListing();
				//todo test GetListingAsync()
				//var resultGetListingAsync = await client.GetListingAsync();
				var resultGetNameListing = client.GetNameListing();
				//todo test GetNameListingAsync()
				//var resultGetNameListingAsync = await client.GetNameListingAsync();

				// Check helper
				void CheckListing(string methodName, IList<string> list) {
					var directoryNetBsdFound = false;
					var directoryPubFound = false;
					foreach (var item in list) {
						if (item.Equals(directoryNetBsdNameFull, StringComparison.OrdinalIgnoreCase))
							directoryNetBsdFound = true;
						if (item.Equals(directoryPubNameFull, StringComparison.OrdinalIgnoreCase))
							directoryPubFound = true;
					}

					if (!directoryNetBsdFound)
						throw new Exception($"Did not find expected directory: '{directoryNetBsdNameFull}' using {methodName}.");
					if (directoryPubFound)
						throw new Exception($"Found unexpected directory '{directoryPubNameFull}' using {methodName}.");
				}

				// Check
				var resultGetListingNames = new List<string>();
				foreach (var item in resultGetListing)
					resultGetListingNames.Add(item.FullName);
				CheckListing("GetListing", resultGetListingNames);
				//todo test GetListingAsync()
				//CheckListing("GetListingAsync", resultGetListingAsync);
				CheckListing("GetNameListing", resultGetNameListing);
				//todo test GetNameListingAsync()
				//CheckListing("GetNameListingAsync", resultGetNameListingAsync);
			}
		}

		
		[Trait("Category", Category_PublicFTP)]
		public void TestGetListingTeleServer() {
			using (var client = NewFtpClient_Tele2SpeedTest()) {

				// machine listing
				var listing = client.GetListing();

				// unix listing
				var listing2 = client.GetListing("/", FtpListOption.ForceList);
			}
		}

		
		[Trait("Category", Category_PublicFTP)]
		public void TestConnectVariousModes() {

			try {
				// Implicit TLS
				FtpClient client = NewFtpClient_Tele2SpeedTest();
				client.EncryptionMode = FtpEncryptionMode.Implicit;
				client.Connect();
			}
			catch (Exception ex) {
			}

			try {
				// Implicit TLS
				FtpClient client = NewFtpClient_NetBsd();
				client.EncryptionMode = FtpEncryptionMode.Implicit;
				client.Connect();
			}
			catch (Exception ex) {
			}

			try {
				// Explicit SSL
				FtpClient client = NewFtpClient_Tele2SpeedTest();
				client.EncryptionMode = FtpEncryptionMode.Explicit;
				client.Connect();
			}
			catch (Exception ex) {
			}

			try {
				// Explicit SSL
				FtpClient client = NewFtpClient_NetBsd();
				client.EncryptionMode = FtpEncryptionMode.Explicit;
				client.Connect();
			}
			catch (Exception ex) {
			}

		}

#if ASYNC
		
		[Trait("Category", Category_PublicFTP)]
		public async void TestGetListingTeleServerAsync() {
			using (var client = NewFtpClient_Tele2SpeedTest()) {

				// machine listing
				var listing = await client.GetListingAsync();

				// unix listing
				var listing2 = await client.GetListingAsync("/", FtpListOption.ForceList);
			}
		}
#endif

		
		[Trait("Category", Category_PublicFTP)]
		public void TestGetListingTeleServerTimezone() {

			using (var client = NewFtpClient_Tele2SpeedTest()) {

				client.AutoConnect();

				// original date = 19/Feb/2020 00:00 (12 am)

				// convert to UTC (assume Tokyo to UTC)
				client.TimeConversion = FtpDate.UTC;
				client.TimeZone = 9;

				var listing = client.GetListing();
				if (listing[0].Modified != DateTime.Parse("18-Feb-2016 3:00:00 PM")) {
					throw new Exception("Timezone conversion failed!");
				}

				// convert to local time (assume Tokyo to Mumbai)
				client.TimeConversion = FtpDate.LocalTime;
				client.TimeZone = 9;

				var listing2 = client.GetListing();
				if (listing2[0].Modified == DateTime.Parse("18-Feb-2016 12:00:00 AM")) {
					throw new Exception("Timezone conversion failed!");
				}
			}
		}

#if !CORE
		
		public void TestGetListingCCC() {
			using (var client = NewFtpClient()) {
				client.EncryptionMode = FtpEncryptionMode.Explicit;
				//client.PlainTextEncryption = true;
				client.SslProtocols = SslProtocols.Tls;
				client.ValidateCertificate += OnValidateCertificate;
				client.Connect();

				foreach (var i in client.GetListing("/public_html/temp/", FtpListOption.ForceList | FtpListOption.Recursive)) {
					//FtpTrace.WriteLine(i);
				}

				// 100 K file
				client.UploadFile(@"D:\Github\FluentFTP\README.md", "/public_html/temp/README.md");
				client.DownloadFile(@"D:\Github\FluentFTP\README2.md", "/public_html/temp/README.md");
			}
		}
#endif

		
		public void TestGetMachineListing() {
			using (var client = NewFtpClient()) {
				client.ListingParser = FtpParser.Machine;
				client.Connect();
				foreach (var i in client.GetListing("/public_html/temp/", FtpListOption.Recursive)) {
					//FtpTrace.WriteLine(i);
				}
			}
		}

		
		public void TestFileZillaKick() {
			using (var cl = NewFtpClient()) {
				cl.EnableThreadSafeDataConnections = false;

				if (cl.FileExists("TestFile.txt")) {
					cl.DeleteFile("TestFile.txt");
				}

				try {
					var s = cl.OpenWrite("TestFile.txt");
					for (var i = 0; true; i++) {
						s.WriteByte((byte) i);
#if CORE14
						Task.Delay(100).Wait();
#else
						Thread.Sleep(100);
#endif
					}

					//s.Close();
				}
				catch (FtpCommandException ex) {
					FtpTrace.WriteLine("Exception caught!");
					FtpTrace.WriteLine(ex.ToString());
				}
			}
		}

		
		public void TestDirectoryWithDots() {
			using (var cl = NewFtpClient()) {
				cl.Connect();

				// FTP server set to timeout after 5 seconds.
				//Thread.Sleep(6000);

				cl.GetListing("Test.Directory", FtpListOption.ForceList);
				cl.SetWorkingDirectory("Test.Directory");
				cl.GetListing(null, FtpListOption.ForceList);
			}
		}

		
		public void TestDispose() {
			using (var cl = NewFtpClient()) {
				cl.Connect();

				// FTP server set to timeout after 5 seconds.
				//Thread.Sleep(6000);

				foreach (var item in cl.GetListing()) {
				}
			}
		}

		
		//public void TestHash() {
			/*using (var cl = NewFtpClient()) {
				cl.Connect();

				FtpTrace.WriteLine("Supported HASH algorithms: " + cl.HashAlgorithms);
				FtpTrace.WriteLine("Current HASH algorithm: " + cl.GetHashAlgorithm());

				foreach (FtpHashAlgorithm alg in Enum.GetValues(typeof(FtpHashAlgorithm))) {
					if (alg != FtpHashAlgorithm.NONE && cl.HashAlgorithms.HasFlag(alg)) {
						FtpHash hash = null;

						cl.SetHashAlgorithm(alg);
						hash = cl.GetHash("LICENSE.TXT");

						if (hash.IsValid) {
							Debug.Assert(hash.Verify(@"C:\FTPTEST\LICENSE.TXT"), "The computed hash didn't match or the hash object was invalid!");
						}
					}
				}
			}*/
		//}

#if ASYNC
		
		//public async Task TestHashAsync() {
			/*using (var cl = NewFtpClient()) {
				await cl.ConnectAsync();

				FtpTrace.WriteLine("Supported HASH algorithms: " + cl.HashAlgorithms);
				FtpTrace.WriteLine("Current HASH algorithm: " + await cl.GetHashAlgorithmAsync());

				foreach (FtpHashAlgorithm alg in Enum.GetValues(typeof(FtpHashAlgorithm))) {
					if (alg != FtpHashAlgorithm.NONE && cl.HashAlgorithms.HasFlag(alg)) {
						FtpHash hash = null;

						await cl.SetHashAlgorithmAsync(alg);
						hash = await cl.HashCommandInternalAsync("LICENSE.TXT");

						if (hash.IsValid) {
							Debug.Assert(hash.Verify(@"C:\FTPTEST\LICENSE.TXT"), "The computed hash didn't match or the hash object was invalid!");
						}
					}
				}
			}*/
		//}
#endif

		
		public void TestReset() {
			using (var cl = NewFtpClient()) {
				cl.Connect();

				using (var istream = cl.OpenRead("LICENSE.TXT", FtpDataType.Binary, 10)) {
				}
			}
		}

		
		[Trait("Category", Category_PublicFTP)]
		public void GetPublicFTPServerListing() {
			using (var cl = NewFtpClient_Tele2SpeedTest()) {
				cl.Connect();

				FtpTrace.WriteLine(cl.Capabilities);

				foreach (var item in cl.GetListing(null, FtpListOption.Recursive)) {
					FtpTrace.WriteLine(item);
				}
			}
		}

		
		[Trait("Category", Category_Code)]
		public void TestMODCOMP_PWD_Parser() {
			var response = "PWD = ~TNA=AMP,VNA=VOL03,FNA=U-ED-B2-USL";
			Match m;

			if ((m = Regex.Match(response, "PWD = (?<pwd>.*)")).Success) {
				FtpTrace.WriteLine("PWD: " + m.Groups["pwd"].Value);
			}
		}

		
		public void TestNameListing() {
			using (var cl = NewFtpClient()) {
				cl.ValidateCertificate += OnValidateCertificate;
				cl.DataConnectionType = FtpDataConnectionType.PASV;

				//cl.EncryptionMode = FtpEncryptionMode.Explicit;
				//cl.SocketPollInterval = 5000;
				cl.Connect();

				//FtpTrace.WriteLine("Sleeping for 10 seconds to force timeout.");
				//Thread.Sleep(10000);

				var items = cl.GetListing();
				foreach (var item in items) {
					//FtpTrace.WriteLine(item.FullName);
					//FtpTrace.WriteLine(item.Modified.Kind);
					//FtpTrace.WriteLine(item.Modified);
				}
			}
		}

		
		public void TestNameListingFTPS() {
			using (var cl = NewFtpClient()) {
				cl.ValidateCertificate += OnValidateCertificate;

				//cl.DataConnectionType = FtpDataConnectionType.PASV;
				cl.EncryptionMode = FtpEncryptionMode.Explicit;
				cl.Connect();

				//FtpTrace.WriteLine("Sleeping for 10 seconds to force timeout.");
				//Thread.Sleep(10000);

				foreach (var item in cl.GetListing()) {
					FtpTrace.WriteLine(item.FullName);

					//FtpTrace.WriteLine(item.Modified.Kind);
					//FtpTrace.WriteLine(item.Modified);
				}
			}
		}

#if !CORE14
		 // Beware: Completely ignores thrown exceptions - Doesn't actually test anything!
		public FtpClient Connect() {
			var threads = new List<Thread>();
			var cl = new FtpClient();

			cl.ValidateCertificate += OnValidateCertificate;

			//cl.EncryptionMode = FtpEncryptionMode.Explicit;

			for (var i = 0; i < 1; i++) {
				var count = i;

				var t = new Thread(new ThreadStart(delegate {
					cl.Credentials = new NetworkCredential(m_user, m_pass);
					cl.Host = m_host;
					cl.Connect();

					for (var j = 0; j < 10; j++) {
						cl.Execute("NOOP");
					}

					if (count % 2 == 0) {
						cl.Disconnect();
					}
				}));

				t.Start();
				threads.Add(t);
			}

			while (threads.Count > 0) {
				threads[0].Join();
				threads.RemoveAt(0);
			}

			return cl;
		}

		public void Upload(FtpClient cl) {
			var root = @"..\..\..";
			var threads = new List<Thread>();

			foreach (var s in Directory.GetFiles(root, "*", SearchOption.AllDirectories)) {
				var file = s;

				if (file.Contains(@"\.git")) {
					continue;
				}

				var t = new Thread(new ThreadStart(delegate { DoUpload(cl, root, file); }));

				t.Start();
				threads.Add(t);
			}

			while (threads.Count > 0) {
				threads[0].Join();
				threads.RemoveAt(0);
			}
		}
#endif

		public void DoUpload(FtpClient cl, string root, string s) {
			var type = FtpDataType.Binary;
			var path = Path.GetDirectoryName(s).Replace(root, "");
			var name = Path.GetFileName(s);

			if (Path.GetExtension(s).ToLower() == ".cs" || Path.GetExtension(s).ToLower() == ".txt") {
				type = FtpDataType.ASCII;
			}

			if (!cl.DirectoryExists(path)) {
				cl.CreateDirectory(path, true);
			}
			else if (cl.FileExists(string.Format("{0}/{1}", path, name))) {
				cl.DeleteFile(string.Format("{0}/{1}", path, name));
			}

			using (
				Stream istream = new FileStream(s, FileMode.Open, FileAccess.Read),
				ostream = cl.OpenWrite(s.Replace(root, ""), type)) {
				var buf = new byte[8192];
				var read = 0;

				while ((read = istream.Read(buf, 0, buf.Length)) > 0) {
					ostream.Write(buf, 0, read);
				}

				/*if (cl.HashAlgorithms != FtpHashAlgorithm.NONE) {
					Debug.Assert(cl.GetHash(s.Replace(root, "")).Verify(s), "The computed hashes don't match!");
				}*/
			}

			/*if (!cl.GetHash(s.Replace(root, "")).Verify(s))
				throw new Exception("Hashes didn't match!");*/
		}

#if !CORE14
		public void Download(FtpClient cl) {
			var threads = new List<Thread>();

			Download(threads, cl, "/");

			while (threads.Count > 0) {
				threads[0].Join();

				lock (threads) {
					threads.RemoveAt(0);
				}
			}
		}

		public void Download(List<Thread> threads, FtpClient cl, string path) {
			foreach (var item in cl.GetListing(path)) {
				if (item.Type == FtpObjectType.Directory) {
					Download(threads, cl, item.FullName);
				}
				else if (item.Type == FtpObjectType.File) {
					var file = item.FullName;

					var t = new Thread(new ThreadStart(delegate { DoDownload(cl, file); }));

					t.Start();

					lock (threads) {
						threads.Add(t);
					}
				}
			}
		}
#endif

		public void DoDownload(FtpClient cl, string file) {
			using (var s = cl.OpenRead(file)) {
				var buf = new byte[8192];

				while (s.Read(buf, 0, buf.Length) > 0) {
					;
				}
			}
		}

		public void Delete(FtpClient cl) {
			DeleteDirectory(cl, "/");
		}

		public void DeleteDirectory(FtpClient cl, string path) {
			foreach (var item in cl.GetListing(path)) {
				if (item.Type == FtpObjectType.File) {
					cl.DeleteFile(item.FullName);
				}
				else if (item.Type == FtpObjectType.Directory) {
					DeleteDirectory(cl, item.FullName);
					cl.DeleteDirectory(item.FullName);
				}
			}
		}

		
		public void TestUTF8() {
			// the following file name was reported in the discussions as having
			// problems:
			// https://netftp.codeplex.com/discussions/445090
			var filename = "Verbundmörtel Zubehör + Technische Daten DE.pdf";

			using (var cl = NewFtpClient()) {
				cl.DataConnectionType = FtpDataConnectionType.PASV;
				cl.InternetProtocolVersions = FtpIpVersion.ANY;

				using (var ostream = cl.OpenWrite(filename))
				using (var writer = new StreamWriter(ostream)) {
					writer.WriteLine(filename);
				}
			}
		}

		
		public void TestUploadDownloadFile() {
			using (var cl = NewFtpClient()) {
				cl.Connect();

				// 100 K file
				cl.UploadFile(@"D:\Github\FluentFTP\README.md", "/public_html/temp/README.md");
				cl.DownloadFile(@"D:\Github\FluentFTP\README2.md", "/public_html/temp/README.md");

				/*
				// 10 M file
				cl.UploadFile(@"D:\Drivers\mb_driver_intel_irst_6series.exe", "/public_html/temp/big.txt");
				cl.Rename("/public_html/temp/big.txt", "/public_html/temp/big2.txt");
				cl.DownloadFile(@"D:\Drivers\mb_driver_intel_irst_6series_2.exe", "/public_html/temp/big2.txt");
				*/
			}
		}

#if ASYNC
		
		public async Task TestUploadDownloadFileAsync() {
			using (var cl = NewFtpClient()) {
				// 100 K file
				await cl.UploadFileAsync(@"D:\Github\FluentFTP\README.md", "/public_html/temp/README.md");
				await cl.DownloadFileAsync(@"D:\Github\FluentFTP\README2.md", "/public_html/temp/README.md");

				/*
				// 10 M file
				await cl.UploadFileAsync(@"D:\Drivers\mb_driver_intel_irst_6series.exe", "/public_html/temp/big.txt");
				await cl.RenameAsync("/public_html/temp/big.txt", "/public_html/temp/big2.txt");
				await cl.DownloadFileAsync(@"D:\Drivers\mb_driver_intel_irst_6series_2.exe", "/public_html/temp/big2.txt");
				*/
			}
		}
#endif

		
		public void TestUploadDownloadFile_UTF() {
			using (var cl = NewFtpClient()) {
				// 100 K file
				cl.UploadFile(@"D:\Tests\Caffè.jpg", "/public_html/temp/Caffè.jpg");
				cl.DownloadFile(@"D:\Tests\Caffè2.jpg", "/public_html/temp/Caffè.jpg");

				/*
				// 10 M file
				cl.UploadFile(@"D:\Drivers\mb_driver_intel_irst_6series.exe", "/public_html/temp/big.txt");
				cl.Rename("/public_html/temp/big.txt", "/public_html/temp/big2.txt");
				cl.DownloadFile(@"D:\Drivers\mb_driver_intel_irst_6series_2.exe", "/public_html/temp/big2.txt");
				*/
			}
		}

		
		public void TestUploadDownloadFile_ANSI() {
			using (var cl = NewFtpClient()) {
				cl.Encoding = Encoding.GetEncoding(1252);

				// 100 K file
				var rpath = "/public_html/temp/Caffè.jpg";
				cl.UploadFile(@"D:\Tests\Caffè.jpg", rpath);
				cl.DownloadFile(@"D:\Tests\Caffè2.jpg", rpath);

				/*
				// 10 M file
				cl.UploadFile(@"D:\Drivers\mb_driver_intel_irst_6series.exe", "/public_html/temp/big.txt");
				cl.Rename("/public_html/temp/big.txt", "/public_html/temp/big2.txt");
				cl.DownloadFile(@"D:\Drivers\mb_driver_intel_irst_6series_2.exe", "/public_html/temp/big2.txt");
				*/
			}
		}

		
		public void TestUploadDownloadManyFiles() {
			using (var cl = NewFtpClient()) {
				cl.EnableThreadSafeDataConnections = false;
				cl.Connect();

				// 100 K file
				for (var i = 0; i < 3; i++) {
					FtpTrace.WriteLine(" ------------- UPLOAD " + i + " ------------------");
					cl.UploadFile(@"D:\Drivers\mb_driver_intel_bootdisk_irst_64_6series.exe", "/public_html/temp/small.txt");
				}

				// 100 K file
				for (var i = 0; i < 3; i++) {
					FtpTrace.WriteLine(" ------------- DOWNLOAD " + i + " ------------------");
					cl.DownloadFile(@"D:\Drivers\test\file" + i + ".exe", "/public_html/temp/small.txt");
				}

				FtpTrace.WriteLine(" ------------- ALL DONE! ------------------");
			}
		}

#if ASYNC
		
		public async Task TestUploadDownloadManyFilesAsync() {
			using (var cl = NewFtpClient()) {
				cl.EnableThreadSafeDataConnections = false;
				await cl.ConnectAsync();

				// 100 K file
				for (var i = 0; i < 3; i++) {
					FtpTrace.WriteLine(" ------------- UPLOAD " + i + " ------------------");
					await cl.UploadFileAsync(@"D:\Drivers\mb_driver_intel_bootdisk_irst_64_6series.exe", "/public_html/temp/small.txt");
				}

				// 100 K file
				for (var i = 0; i < 3; i++) {
					FtpTrace.WriteLine(" ------------- DOWNLOAD " + i + " ------------------");
					await cl.DownloadFileAsync(@"D:\Drivers\test\file" + i + ".exe", "/public_html/temp/small.txt");
				}

				FtpTrace.WriteLine(" ------------- ALL DONE! ------------------");
			}
		}
#endif

		
		public void TestUploadDownloadManyFiles2() {
			using (var cl = NewFtpClient()) {
				cl.EnableThreadSafeDataConnections = false;
				cl.Connect();

				// upload many
				cl.UploadFiles(new[] {@"D:\Drivers\test\file0.exe", @"D:\Drivers\test\file1.exe", @"D:\Drivers\test\file2.exe", @"D:\Drivers\test\file3.exe", @"D:\Drivers\test\file4.exe"}, "/public_html/temp/", FtpRemoteExists.Skip);

				// download many
				cl.DownloadFiles(@"D:\Drivers\test\", new[] {@"/public_html/temp/file0.exe", @"/public_html/temp/file1.exe", @"/public_html/temp/file2.exe", @"/public_html/temp/file3.exe", @"/public_html/temp/file4.exe"}, FtpLocalExists.Resume);

				FtpTrace.WriteLine(" ------------- ALL DONE! ------------------");

				cl.Dispose();
			}
		}

#if ASYNC
		
		public async Task TestUploadDownloadManyFiles2Async() {
			using (var cl = NewFtpClient()) {
				cl.EnableThreadSafeDataConnections = false;
				await cl.ConnectAsync();

				// upload many
				await cl.UploadFilesAsync(new[] {@"D:\Drivers\test\file0.exe", @"D:\Drivers\test\file1.exe", @"D:\Drivers\test\file2.exe", @"D:\Drivers\test\file3.exe", @"D:\Drivers\test\file4.exe"}, "/public_html/temp/", createRemoteDir: false);

				// download many
				await cl.DownloadFilesAsync(@"D:\Drivers\test\", new[] {@"/public_html/temp/file0.exe", @"/public_html/temp/file1.exe", @"/public_html/temp/file2.exe", @"/public_html/temp/file3.exe", @"/public_html/temp/file4.exe"}, FtpLocalExists.Resume);

				FtpTrace.WriteLine(" ------------- ALL DONE! ------------------");

				cl.Dispose();
			}
		}
#endif

		
		public void TestUploadDownloadZeroLenFile() {
			using (var cl = NewFtpClient()) {
				// 0 KB file
				cl.UploadFile(@"D:\zerolen.txt", "/public_html/temp/zerolen.txt");
				cl.DownloadFile(@"D:\zerolen2.txt", "/public_html/temp/zerolen.txt");
			}
		}

		
		public void TestListSpacedPath() {
			using (var cl = NewFtpClient()) {
				cl.EncryptionMode = FtpEncryptionMode.Explicit;
				cl.ValidateCertificate += OnValidateCertificate;

				foreach (var i in cl.GetListing("/public_html/temp/spaced folder/")) {
					FtpTrace.WriteLine(i.FullName);
				}
			}
		}

		
		public void TestFilePermissions() {
			using (var cl = NewFtpClient()) {
				foreach (var i in cl.GetListing("/public_html/temp/")) {
					FtpTrace.WriteLine(i.Name + " - " + i.Chmod);
				}

				var o = cl.GetFilePermissions("/public_html/temp/file3.exe");
				var o2 = cl.GetFilePermissions("/public_html/temp/README.md");

				cl.SetFilePermissions("/public_html/temp/file3.exe", 646);

				var o22 = cl.GetChmod("/public_html/temp/file3.exe");
			}
		}

		
		public void TestFileExists() {
			using (var cl = NewFtpClient()) {
				var f1_yes = cl.FileExists("/public_html");
				var f2_yes = cl.FileExists("/public_html/temp");
				var f3_yes = cl.FileExists("/public_html/temp/");
				var f3_no = cl.FileExists("/public_html/tempa/");
				var f4_yes = cl.FileExists("/public_html/temp/README.md");
				var f4_no = cl.FileExists("/public_html/temp/README");
				var f5_yes = cl.FileExists("/public_html/temp/Caffè.jpg");
				var f5_no = cl.FileExists("/public_html/temp/Caffèoo.jpg");

				cl.SetWorkingDirectory("/public_html/");

				var z_f2_yes = cl.FileExists("temp");
				var z_f3_yes = cl.FileExists("temp/");
				var z_f3_no = cl.FileExists("tempa/");
				var z_f4_yes = cl.FileExists("temp/README.md");
				var z_f4_no = cl.FileExists("temp/README");
				var z_f5_yes = cl.FileExists("temp/Caffè.jpg");
				var z_f5_no = cl.FileExists("temp/Caffèoo.jpg");
			}
		}

		
		public void TestDeleteDirectory() {
			using (var cl = NewFtpClient()) {
				cl.DeleteDirectory("/public_html/temp/otherdir/");
				cl.DeleteDirectory("/public_html/temp/spaced folder/");
			}
		}

		
		public void TestMoveFiles() {
			using (var cl = NewFtpClient()) {
				cl.MoveFile("/public_html/temp/README.md", "/public_html/temp/README_moved.md");

				cl.MoveDirectory("/public_html/temp/dir/", "/public_html/temp/dir_moved/");
			}
		}

		
		public void TestAutoDetect() {
			using (var cl = NewFtpClient_Tele2SpeedTest()) {
				var profiles = cl.AutoDetect(false);
				if (profiles.Count > 0) {
					var code = profiles[0].ToCode();
				}
			}
		}

		
		public void TestAutoConnect() {
			using (var cl = NewFtpClient_Tele2SpeedTest()) {
				var profile = cl.AutoConnect();
				if (profile != null) {
					var code = profile.ToCode();
					Console.WriteLine(code);
				}
			}
			/*using (var cl = NewFtpClient_Inacessible()) {
				var profile = cl.AutoConnect();
			}
			using (var cl = NewFtpClient_Inacessible2()) {
				var profile = cl.AutoConnect();
			}*/
		}

		
		public void TestSocksProxy()
		{
			using (var cl = new FtpClientSocks5Proxy(new FtpProxyProfile()
			{
				ProxyCredentials = null,
				ProxyHost = m_proxy_host,
				ProxyPort = m_proxy_port
			})
			{
				Host = m_host,
				Credentials = new NetworkCredential(m_user, m_pass)
			})
			{
				var items = cl.GetListing("/");
				Console.WriteLine(items.Length);
			}
		}

#if ASYNC
		
		public async void TestAutoConnectAsync() {
			using (var cl = NewFtpClient_Tele2SpeedTest()) {
				var profile = await cl.AutoConnectAsync();
				if (profile != null) {
					var code = profile.ToCode();
					Console.WriteLine(code);
				}
			}
			/*using (var cl = NewFtpClient_Inacessible()) {
				var profile = await cl.AutoConnectAsync();
			}
			using (var cl = NewFtpClient_Inacessible2()) {
				var profile = await cl.AutoConnectAsync();
			}*/
		}
#endif

		
		public void TestQuickDownloadFilePublic() {
			using (var cl = NewFtpClient()) {
				cl.Connect();


				//cl.QuickTransferLimit = 100000000; // 100 MB limit
				var sw = StartTimer();
				cl.DownloadFile(@"D:\Temp\10MB.zip", "10MB.zip");
				cl.DownloadFile(@"D:\Temp\10MB.zip", "10MB.zip");
				StopTimer(sw, "Downloading with Quick Transfer");



				//cl.QuickTransferLimit = 0; // disabled
				var sw2 = StartTimer();
				cl.DownloadFile(@"D:\Temp\10MB.zip", "10MB.zip");
				cl.DownloadFile(@"D:\Temp\10MB.zip", "10MB.zip");
				StopTimer(sw2, "Downloading with Filestream");


			}

		}

		
		public void TestQuickDownloadFilePublic2() {
			using (var cl = NewFtpClient()) {
				cl.Connect();


				//cl.QuickTransferLimit = 100000000; // 100 MB limit
				var sw = StartTimer();
				cl.DownloadFile(@"D:\Temp\100KB.zip", "100KB.zip");
				cl.DownloadFile(@"D:\Temp\100KB.zip", "100KB.zip");
				cl.DownloadFile(@"D:\Temp\100KB.zip", "100KB.zip");
				cl.DownloadFile(@"D:\Temp\100KB.zip", "100KB.zip");
				cl.DownloadFile(@"D:\Temp\100KB.zip", "100KB.zip");
				StopTimer(sw, "Downloading with Quick Transfer");



				//cl.QuickTransferLimit = 0; // disabled
				var sw2 = StartTimer();
				cl.DownloadFile(@"D:\Temp\100KB.zip", "100KB.zip");
				cl.DownloadFile(@"D:\Temp\100KB.zip", "100KB.zip");
				cl.DownloadFile(@"D:\Temp\100KB.zip", "100KB.zip");
				cl.DownloadFile(@"D:\Temp\100KB.zip", "100KB.zip");
				cl.DownloadFile(@"D:\Temp\100KB.zip", "100KB.zip");
				StopTimer(sw2, "Downloading with Filestream");


			}

		}

		
		public void TestUploadDownloadFilePublic() {
			using (var cl = NewFtpClient()) {
				cl.Connect();

				cl.DownloadFile(@"D:\Temp\10MB.zip", "10MB.zip");
				cl.DownloadFile(@"D:\Temp\1KB.zip", "1KB.zip");

				cl.UploadFile(@"D:\Github\FluentFTP\README.md", "/upload/README.md");

				cl.UploadFile(@"D:\Github\FluentFTP\.github\contributors.png", "/upload/contributors.png");

			}

		}

		private Stopwatch StartTimer() {
			var sw = new Stopwatch();
			sw.Start();
			return sw;
		}

		private void StopTimer(Stopwatch sw, string action) {
			sw.Stop();

			FtpTrace.WriteLine("");
			FtpTrace.WriteLine("---------------------------------------------------");
			FtpTrace.WriteLine("! " + action + " took " + ((double)sw.ElapsedMilliseconds / 1000) + " seconds");
			FtpTrace.WriteLine("---------------------------------------------------");

		}

	}
}
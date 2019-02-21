using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using FluentFTP;
using System.Threading;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
#if !NOASYNC
using System.Threading.Tasks;
#endif
using System.IO.Compression;
using System.Text;
using FluentFTP.Proxy;
using System.Security.Authentication;
using Xunit;

namespace Tests
{
	public class Tests
	{
		private const string Category_Code = "Code";
		private const string Category_PublicFTP = "PublicFTP";
		private const string Category_CustomFTP = "CustomerFTP";

		// SET THESE BEFORE RUNNING ANY TESTS!
		const string m_host = "";
		const string m_user = "";
		const string m_pass = "";

		private static readonly int[] connectionTypes = new int[] {
			(int) FtpDataConnectionType.EPSV,
			(int) FtpDataConnectionType.EPRT,
			(int) FtpDataConnectionType.PASV,
			(int) FtpDataConnectionType.PORT
		};

		#region Helpers

		private void OnValidateCertificate(FtpClient control, FtpSslValidationEventArgs e) => e.Accept = true;

		private static FtpClient NewFtpClient(string host = m_host, string username = m_user, string password = m_pass)
			=> new FtpClient(host, new NetworkCredential(username, password));

		private static FtpClient NewFtpClient_NetBsd() => NewFtpClient("ftp.netbsd.org", "ftp", "ftp");

		private static FtpClient NewFtpClient_Tele2SpeedTest() => new FtpClient("ftp://speedtest.tele2.net/");

		#endregion

		//[Fact]
		public void TestListPathWithHttp11Proxy()
		{
			using (FtpClient cl = new FtpClientHttp11Proxy(new ProxyInfo { Host = "127.0.0.1", Port = 3128, })) // Credential = new NetworkCredential() 
			{
				FtpTrace.WriteLine("FTPClient::ConnectionType = '" + cl.ConnectionType + "'");

				cl.Credentials = new NetworkCredential(m_user, m_pass);
				cl.Host = m_host;
				cl.ValidateCertificate += OnValidateCertificate;
				cl.DataConnectionType = FtpDataConnectionType.PASV;
				cl.Connect();

				foreach (FtpListItem item in cl.GetListing(null, FtpListOption.SizeModify | FtpListOption.ForceNameList))
				{
					FtpTrace.WriteLine(item.Modified.Kind);
					FtpTrace.WriteLine(item.Modified);
				}
			}
		}

		//[Fact]
		public void TestListPath()
		{
			using (FtpClient cl = NewFtpClient())
			{
				cl.EncryptionMode = FtpEncryptionMode.None;

				cl.GetListing();
				FtpTrace.WriteLine("Path listing succeeded");
				cl.GetListing(null, FtpListOption.NoPath);
				FtpTrace.WriteLine("No path listing succeeded");
			}
		}

		//[Fact]
		public void TestCheckCapabilities()
		{
			using (FtpClient cl = NewFtpClient())
			{
				cl.CheckCapabilities = false;
				Debug.Assert(cl.HasFeature(FtpCapability.NONE), "Excepted FTP capabilities to be NONE.");
			}
		}

#if !NOASYNC
		//[Fact]
		public async Task TestListPathAsync()
        {
            using (FtpClient cl = NewFtpClient())
            {
                cl.EncryptionMode = FtpEncryptionMode.None;

                await cl.GetListingAsync();
                FtpTrace.WriteLine("Path listing succeeded");
                await cl.GetListingAsync(null, FtpListOption.NoPath);
                FtpTrace.WriteLine("No path listing succeeded");
            }
        }
#endif

		//[Fact]
		public void StreamResponses()
		{
			using (FtpClient cl = NewFtpClient())
			{
				cl.EncryptionMode = FtpEncryptionMode.None;
				cl.ValidateCertificate += OnValidateCertificate;

				using (FtpDataStream s = (FtpDataStream)cl.OpenWrite("test.txt"))
				{
					FtpReply r = s.CommandStatus;

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

#if !NOASYNC
		//[Fact]
		public async Task StreamResponsesAsync()
        {
            using (FtpClient cl = NewFtpClient())
            {
                cl.EncryptionMode = FtpEncryptionMode.None;
                cl.ValidateCertificate += OnValidateCertificate;

                using (FtpDataStream s = (FtpDataStream)await cl.OpenWriteAsync("test.txt"))
                {
                    FtpReply r = s.CommandStatus;

                    FtpTrace.WriteLine("");
                    FtpTrace.WriteLine("Response to STOR:");
                    FtpTrace.WriteLine("Code: "+ r.Code);
                    FtpTrace.WriteLine("Message: "+ r.Message);
                    FtpTrace.WriteLine("Informational: "+ r.InfoMessages);

                    r = s.Close();
                    FtpTrace.WriteLine("");
                    FtpTrace.WriteLine("Response after close:");
                    FtpTrace.WriteLine("Code: "+ r.Code);
                    FtpTrace.WriteLine("Message: "+ r.Message);
                    FtpTrace.WriteLine("Informational: "+ r.InfoMessages);
                }
            }
        }
#endif

		//[Fact]
		public void TestUnixListing()
		{
			using (FtpClient cl = NewFtpClient())
			{
				if (!cl.FileExists("test.txt"))
				{
					using (Stream s = cl.OpenWrite("test.txt"))
					{
					}
				}

				foreach (FtpListItem i in cl.GetListing(null, FtpListOption.ForceList))
				{
					FtpTrace.WriteLine(i);
				}
			}
		}

		[Fact, Trait("Category", Category_Code)]
		public void TestFtpPath()
		{
			string path = "/home/sigurdhj/errors/16.05.2014/asdasd/asd asd asd aa asd/Kooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooo::asdasd";

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

		//[Fact]
		public void TestGetObjectInfo()
		{
			using (FtpClient client = NewFtpClient())
			{
				FtpTrace.WriteLine(client.GetObjectInfo("/public_html/temp/README.md"));
			}
		}

#if !NOASYNC && !CORE
		//[Fact]
        public async Task TestGetObjectInfoAsync()
        {
            using (FtpClient cl = NewFtpClient())
            {
                cl.Encoding = Encoding.UTF8;

                var item = await cl.GetObjectInfoAsync("/Examples/OpenRead.cs");
                FtpTrace.WriteLine(item.ToString());
            }
        }
#endif

		//[Fact]
		public void TestManualEncoding()
		{
			using (FtpClient cl = NewFtpClient())
			{
				cl.Encoding = Encoding.UTF8;

				using (Stream s = cl.OpenWrite("test.txt"))
				{
				}
			}
		}

		//[Fact]
		public void TestServer()
		{
			using (FtpClient cl = NewFtpClient())
			{
				cl.EncryptionMode = FtpEncryptionMode.Explicit;
				cl.ValidateCertificate += OnValidateCertificate;

				foreach (FtpListItem i in cl.GetListing("/"))
				{
					FtpTrace.WriteLine(i.FullName);
				}
			}
		}

		private void TestServerDownload(FtpClient client, string path)
		{
			foreach (FtpListItem i in client.GetListing(path))
			{
				switch (i.Type)
				{
					case FtpFileSystemObjectType.Directory:
						TestServerDownload(client, i.FullName);
						break;
					case FtpFileSystemObjectType.File:
						using (Stream s = client.OpenRead(i.FullName))
						{
							byte[] b = new byte[8192];
							int read = 0;
							long total = 0;

							try
							{
								while ((read = s.Read(b, 0, b.Length)) > 0)
								{
									total += read;

									Console.Write("\r{0}/{1} {2:p}          ",
										total, s.Length, (double)total / (double)s.Length);
								}

								Console.Write("\r{0}/{1} {2:p}       ",
										total, s.Length, (double)total / (double)s.Length);
							}
							finally
							{
								FtpTrace.WriteLine("");
							}
						}
						break;
				}
			}
		}

#if !NOASYNC
        private async Task TestServerDownloadAsync(FtpClient client, string path)
        {
            foreach (FtpListItem i in await client.GetListingAsync(path))
            {
                switch (i.Type)
                {
                    case FtpFileSystemObjectType.Directory:
                        await TestServerDownloadAsync(client, i.FullName);
                        break;
                    case FtpFileSystemObjectType.File:
                        using (Stream s = await client.OpenReadAsync(i.FullName))
                        {
                            byte[] b = new byte[8192];
                            int read = 0;
                            long total = 0;

                            try
                            {
                                while ((read = await s.ReadAsync(b, 0, b.Length)) > 0)
                                {
                                    total += read;

                                    Console.Write("\r{0}/{1} {2:p}          ",
                                        total, s.Length, (double)total / (double)s.Length);
                                }

                                Console.Write("\r{0}/{1} {2:p}       ",
                                        total, s.Length, (double)total / (double)s.Length);
                            }
                            finally
                            {
                                FtpTrace.WriteLine("");
                            }
                        }
                        break;
                }
            }
        }
#endif

#if !CORE14
		[Fact, Trait("Category", Category_PublicFTP)]
		public void TestDisposeWithMultipleThreads()
		{
			using (FtpClient cl = NewFtpClient_NetBsd())
			{
				Thread t1 = new Thread(() =>
				{
					cl.GetListing();
				});

				Thread t2 = new Thread(() =>
				{
					cl.Dispose();
				});

				t1.Start();
				Thread.Sleep(2000);
				t2.Start();

				t1.Join();
				t2.Join();
			}
		}
#endif

		[Fact, Trait("Category", Category_Code)]
		public void TestConnectionFailure()
		{
			try
			{
				using (FtpClient cl = NewFtpClient(host: "somefakehost"))
				{
					cl.ConnectTimeout = 5000;
					cl.Connect();
				}
			}
			catch (System.Net.Sockets.SocketException) { } // Expecting this
#if !NOASYNC
			catch (AggregateException ex) when (ex.InnerException is System.Net.Sockets.SocketException) { }
#endif
		}

		[Fact, Trait("Category", Category_PublicFTP)]
		public void TestNetBSDServer()
		{
			using (FtpClient client = NewFtpClient_NetBsd())
			{
				foreach (FtpListItem item in client.GetListing(null,
					FtpListOption.ForceList | FtpListOption.Modify | FtpListOption.DerefLinks))
				{
					FtpTrace.WriteLine(item);

					if (item.Type == FtpFileSystemObjectType.Link && item.LinkObject != null)
						FtpTrace.WriteLine(item.LinkObject);
				}
			}
		}

		//[Fact]
		public void TestGetListing()
		{
			using (FtpClient client = NewFtpClient())
			{
				client.Connect();
				foreach (FtpListItem i in client.GetListing("/public_html/temp/", FtpListOption.ForceList | FtpListOption.Recursive))
				{
					//FtpTrace.WriteLine(i);
				}
			}
		}

#if !CORE
		//[Fact]
		public void TestGetListingCCC()
		{
			using (FtpClient client = NewFtpClient())
			{
				client.EncryptionMode = FtpEncryptionMode.Explicit;
				client.PlainTextEncryption = true;
				client.SslProtocols = SslProtocols.Tls;
				client.ValidateCertificate += OnValidateCertificate;
				client.Connect();

				foreach (FtpListItem i in client.GetListing("/public_html/temp/", FtpListOption.ForceList | FtpListOption.Recursive))
				{
					//FtpTrace.WriteLine(i);
				}

				// 100 K file
				client.UploadFile(@"D:\Github\hgupta\FluentFTP\README.md", "/public_html/temp/README.md");
				client.DownloadFile(@"D:\Github\hgupta\FluentFTP\README2.md", "/public_html/temp/README.md");
			}
		}
#endif

		//[Fact]
		public void TestGetMachineListing()
		{
			using (FtpClient client = NewFtpClient())
			{
				client.ListingParser = FtpParser.Machine;
				client.Connect();
				foreach (FtpListItem i in client.GetListing("/public_html/temp/", FtpListOption.Recursive))
				{
					//FtpTrace.WriteLine(i);
				}
			}
		}

		//[Fact]
		public void TestFileZillaKick()
		{
			using (FtpClient cl = NewFtpClient())
			{
				cl.EnableThreadSafeDataConnections = false;

				if (cl.FileExists("TestFile.txt"))
					cl.DeleteFile("TestFile.txt");

				try
				{
					Stream s = cl.OpenWrite("TestFile.txt");
					for (int i = 0; true; i++)
					{
						s.WriteByte((byte)i);
#if CORE14
						Task.Delay(100).Wait();
#else
						Thread.Sleep(100);
#endif
					}

					//s.Close();
				}
				catch (FtpCommandException ex)
				{
					FtpTrace.WriteLine("Exception caught!");
					FtpTrace.WriteLine(ex.ToString());
				}
			}
		}

		[Fact, Trait("Category", Category_Code)]
		public void TestUnixListParser()
		{
			FtpListParser parser = new FtpListParser(new FtpClient());
			parser.Init("UNIX");
			//parser.parser = FtpParser.Legacy;

			string[] sample = new string[] {
				"drwxr-xr-x   7  user1 user1       512 Sep 27  2011 .",
				"drwxr-xr-x  31 user1  user1      1024 Sep 27  2011 ..",
				"lrwxrwxrwx   1 user1  user1      9 Sep 27  2011 data.0000 -> data.6460",
				"drwxr-xr-x  10 user1  user1      512 Jun 29  2012 data.6460",
				"lrwxrwxrwx   1 user1 user1       8 Sep 27  2011 sys.0000 -> sys.6460",
				"drwxr-xr-x 133 user1  user1     4096 Jun 25 16:26 sys.6460"
			};

			foreach (string s in sample)
			{
				FtpListItem item = parser.ParseSingleLine("/", s, 0, false);

				if (item != null)
					FtpTrace.WriteLine(item);
			}
		}

		[Fact, Trait("Category", Category_Code)]
		public void TestIISParser()
		{
			FtpListParser parser = new FtpListParser(new FtpClient());
			parser.Init("WINDOWS");
			//parser.parser = FtpParser.Legacy;

			string[] sample = new string[] {
				"03-07-13  10:02AM                  901 File01.xml",
				"03-07-13  10:03AM                  921 File02.xml",
				"03-07-13  10:04AM                  904 File03.xml",
				"03-07-13  10:04AM                  912 File04.xml",
				"03-08-13  11:10AM                  912 File05.xml",
				"03-15-13  02:38PM                  912 File06.xml",
				"03-07-13  10:16AM                  909 File07.xml",
				"03-07-13  10:16AM                  899 File08.xml",
				"03-08-13  10:22AM                  904 File09.xml",
				"03-25-13  07:27AM                  895 File10.xml",
				"03-08-13  10:22AM                 6199 File11.txt",
				"03-25-13  07:22AM                31444 File12.txt",
				"03-25-13  07:24AM                24537 File13.txt"
			};

			foreach (string s in sample)
			{
				FtpListItem item = parser.ParseSingleLine("/", s, 0, false);

				if (item != null)
				{
					FtpTrace.WriteLine(item);
				}
			}
		}

		[Fact, Trait("Category", Category_Code)]
		public void TestOpenVMSParser()
		{
			FtpListParser parser = new FtpListParser(new FtpClient());
			parser.Init("VMS");

			string[] sample = new string[] {
				"411_4114.TXT;1             11  21-MAR-2012 15:17 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
				"ACT_CC_NAME_4114.TXT;1    30  21-MAR-2012 15:17 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
				"ACT_CC_NUM_4114.TXT;1     30  21-MAR-2012 15:17 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
				"ACT_CELL_NAME_4114.TXT;1 113  21-MAR-2012 15:17 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
				"ACT_CELL_NUM_4114.TXT;1  113  21-MAR-2012 15:17 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
				"AGCY_BUDG_4114.TXT;1      63  21-MAR-2012 15:17 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
				"CELL_SUMM_4114.TXT;1     125  21-MAR-2012 15:17 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
				"CELL_SUMM_CHART_4114.PDF;2 95  21-MAR-2012 10:58 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
				"DET_4114.TXT;1          17472  21-MAR-2012 15:17 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
				"DET_4114_000.TXT;1        777  21-MAR-2012 15:18 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
				"DET_4114_001.TXT;1        254  21-MAR-2012 15:18 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
				"DET_4114_003.TXT;1         21  21-MAR-2012 15:18 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
				"DET_4114_006.TXT;1         22  21-MAR-2012 15:18 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
				"DET_4114_101.TXT;1        431  21-MAR-2012 15:18 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
				"DET_4114_121.TXT;1       2459  21-MAR-2012 15:18 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
				"DET_4114_124.TXT;1       4610  21-MAR-2012 15:18 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
				"DET_4114_200.TXT;1        936  21-MAR-2012 15:18 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)",
				"TEL_4114.TXT;1           1178  21-MAR-2012 15:19 [TBMS,TBMS_BOSS] (RWED,RWED,,RE)"
			};

			foreach (string s in sample)
			{
				FtpListItem item = parser.ParseSingleLine("disk$user520:[4114.2012.Jan]", s, 0, false);

				if (item != null)
				{
					FtpTrace.WriteLine(item);
				}
			}
		}

		//[Fact]
		public void TestDirectoryWithDots()
		{
			using (FtpClient cl = NewFtpClient())
			{
				cl.Connect();
				// FTP server set to timeout after 5 seconds.
				//Thread.Sleep(6000);

				cl.GetListing("Test.Directory", FtpListOption.ForceList);
				cl.SetWorkingDirectory("Test.Directory");
				cl.GetListing(null, FtpListOption.ForceList);
			}
		}

		//[Fact]
		public void TestDispose()
		{
			using (FtpClient cl = NewFtpClient())
			{
				cl.Connect();
				// FTP server set to timeout after 5 seconds.
				//Thread.Sleep(6000);

				foreach (FtpListItem item in cl.GetListing())
				{

				}
			}
		}

		//[Fact]
		public void TestHash()
		{
			using (FtpClient cl = NewFtpClient())
			{
				cl.Connect();

				FtpTrace.WriteLine("Supported HASH algorithms: " + cl.HashAlgorithms);
				FtpTrace.WriteLine("Current HASH algorithm: " + cl.GetHashAlgorithm());

				foreach (FtpHashAlgorithm alg in Enum.GetValues(typeof(FtpHashAlgorithm)))
				{
					if (alg != FtpHashAlgorithm.NONE && cl.HashAlgorithms.HasFlag(alg))
					{
						FtpHash hash = null;

						cl.SetHashAlgorithm(alg);
						hash = cl.GetHash("LICENSE.TXT");

						if (hash.IsValid)
						{
							Debug.Assert(hash.Verify(@"C:\FTPTEST\LICENSE.TXT"), "The computed hash didn't match or the hash object was invalid!");
						}
					}
				}
			}
		}

#if !NOASYNC
		//[Fact]
        public async Task TestHashAsync()
        {
            using (FtpClient cl = NewFtpClient())
            {
                await cl.ConnectAsync();

                FtpTrace.WriteLine("Supported HASH algorithms: "+ cl.HashAlgorithms);
                FtpTrace.WriteLine("Current HASH algorithm: "+ await cl.GetHashAlgorithmAsync());

                foreach (FtpHashAlgorithm alg in Enum.GetValues(typeof(FtpHashAlgorithm)))
                {
                    if (alg != FtpHashAlgorithm.NONE && cl.HashAlgorithms.HasFlag(alg))
                    {
                        FtpHash hash = null;

                        await cl.SetHashAlgorithmAsync(alg);
                        hash = await cl.GetHashAsync("LICENSE.TXT");

                        if (hash.IsValid)
                        {
                            Debug.Assert(hash.Verify(@"C:\FTPTEST\LICENSE.TXT"), "The computed hash didn't match or the hash object was invalid!");
                        }
                    }
                }
            }
        }
#endif

		//[Fact]
		public void TestReset()
		{
			using (FtpClient cl = NewFtpClient())
			{
				cl.Connect();

				using (Stream istream = cl.OpenRead("LICENSE.TXT", 10))
				{
				}
			}
		}

		[Fact, Trait("Category", Category_PublicFTP)]
		public void GetPublicFTPServerListing()
		{
			using (FtpClient cl = NewFtpClient_Tele2SpeedTest())
			{
				cl.Connect();

				FtpTrace.WriteLine(cl.Capabilities);

				foreach (FtpListItem item in cl.GetListing(null, FtpListOption.Recursive))
				{
					FtpTrace.WriteLine(item);
				}
			}
		}

		[Fact, Trait("Category", Category_Code)]
		public void TestMODCOMP_PWD_Parser()
		{
			string response = "PWD = ~TNA=AMP,VNA=VOL03,FNA=U-ED-B2-USL";
			Match m;

			if ((m = Regex.Match(response, "PWD = (?<pwd>.*)")).Success)
				FtpTrace.WriteLine("PWD: " + m.Groups["pwd"].Value);
		}

		//[Fact]
		public void TestNameListing()
		{
			using (FtpClient cl = NewFtpClient())
			{
				cl.ValidateCertificate += OnValidateCertificate;
				cl.DataConnectionType = FtpDataConnectionType.PASV;
				//cl.EncryptionMode = FtpEncryptionMode.Explicit;
				//cl.SocketPollInterval = 5000;
				cl.Connect();

				//FtpTrace.WriteLine("Sleeping for 10 seconds to force timeout.");
				//Thread.Sleep(10000);

				var items = cl.GetListing();
				foreach (FtpListItem item in items)
				{
					//FtpTrace.WriteLine(item.FullName);
					//FtpTrace.WriteLine(item.Modified.Kind);
					//FtpTrace.WriteLine(item.Modified);
				}
			}
		}

		//[Fact]
		public void TestNameListingFTPS()
		{
			using (FtpClient cl = NewFtpClient())
			{
				cl.ValidateCertificate += OnValidateCertificate;
				//cl.DataConnectionType = FtpDataConnectionType.PASV;
				cl.EncryptionMode = FtpEncryptionMode.Explicit;
				cl.Connect();

				//FtpTrace.WriteLine("Sleeping for 10 seconds to force timeout.");
				//Thread.Sleep(10000);

				foreach (FtpListItem item in cl.GetListing())
				{
					FtpTrace.WriteLine(item.FullName);
					//FtpTrace.WriteLine(item.Modified.Kind);
					//FtpTrace.WriteLine(item.Modified);
				}
			}
		}

#if !CORE14
		//[Fact] // Beware: Completely ignores thrown exceptions - Doesn't actually test anything!
		public FtpClient Connect()
		{
			List<Thread> threads = new List<Thread>();
			FtpClient cl = new FtpClient();

			cl.ValidateCertificate += OnValidateCertificate;
			//cl.EncryptionMode = FtpEncryptionMode.Explicit;

			for (int i = 0; i < 1; i++)
			{
				int count = i;

				Thread t = new Thread(new ThreadStart(delegate ()
				{
					cl.Credentials = new NetworkCredential(m_user, m_pass);
					cl.Host = m_host;
					cl.Connect();

					for (int j = 0; j < 10; j++)
						cl.Execute("NOOP");

					if (count % 2 == 0)
						cl.Disconnect();
				}));

				t.Start();
				threads.Add(t);
			}

			while (threads.Count > 0)
			{
				threads[0].Join();
				threads.RemoveAt(0);
			}

			return cl;
		}

		public void Upload(FtpClient cl)
		{
			string root = @"..\..\..";
			List<Thread> threads = new List<Thread>();

			foreach (string s in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
			{
				string file = s;

				if (file.Contains(@"\.git"))
					continue;

				Thread t = new Thread(new ThreadStart(delegate ()
				{
					DoUpload(cl, root, file);
				}));

				t.Start();
				threads.Add(t);
			}

			while (threads.Count > 0)
			{
				threads[0].Join();
				threads.RemoveAt(0);
			}
		}
#endif

		public void DoUpload(FtpClient cl, string root, string s)
		{
			FtpDataType type = FtpDataType.Binary;
			string path = Path.GetDirectoryName(s).Replace(root, "");
			string name = Path.GetFileName(s);

			if (Path.GetExtension(s).ToLower() == ".cs" || Path.GetExtension(s).ToLower() == ".txt")
				type = FtpDataType.ASCII;

			if (!cl.DirectoryExists(path))
				cl.CreateDirectory(path, true);
			else if (cl.FileExists(string.Format("{0}/{1}", path, name)))
				cl.DeleteFile(string.Format("{0}/{1}", path, name));

			using (
				Stream istream = new FileStream(s, FileMode.Open, FileAccess.Read),
						ostream = cl.OpenWrite(s.Replace(root, ""), type))
			{
				byte[] buf = new byte[8192];
				int read = 0;

				while ((read = istream.Read(buf, 0, buf.Length)) > 0)
				{
					ostream.Write(buf, 0, read);
				}

				if (cl.HashAlgorithms != FtpHashAlgorithm.NONE)
				{
					Debug.Assert(cl.GetHash(s.Replace(root, "")).Verify(s), "The computed hashes don't match!");
				}
			}

			/*if (!cl.GetHash(s.Replace(root, "")).Verify(s))
				throw new Exception("Hashes didn't match!");*/
		}

#if !CORE14
		public void Download(FtpClient cl)
		{
			List<Thread> threads = new List<Thread>();

			Download(threads, cl, "/");

			while (threads.Count > 0)
			{
				threads[0].Join();

				lock (threads)
				{
					threads.RemoveAt(0);
				}
			}
		}

		public void Download(List<Thread> threads, FtpClient cl, string path)
		{
			foreach (FtpListItem item in cl.GetListing(path))
			{
				if (item.Type == FtpFileSystemObjectType.Directory)
					Download(threads, cl, item.FullName);
				else if (item.Type == FtpFileSystemObjectType.File)
				{
					string file = item.FullName;

					Thread t = new Thread(new ThreadStart(delegate ()
					{
						DoDownload(cl, file);
					}));

					t.Start();

					lock (threads)
					{
						threads.Add(t);
					}
				}
			}
		}
#endif

		public void DoDownload(FtpClient cl, string file)
		{
			using (Stream s = cl.OpenRead(file))
			{
				byte[] buf = new byte[8192];

				while (s.Read(buf, 0, buf.Length) > 0) ;
			}
		}

		public void Delete(FtpClient cl)
		{
			DeleteDirectory(cl, "/");
		}

		public void DeleteDirectory(FtpClient cl, string path)
		{
			foreach (FtpListItem item in cl.GetListing(path))
			{
				if (item.Type == FtpFileSystemObjectType.File)
				{
					cl.DeleteFile(item.FullName);
				}
				else if (item.Type == FtpFileSystemObjectType.Directory)
				{
					DeleteDirectory(cl, item.FullName);
					cl.DeleteDirectory(item.FullName);
				}
			}
		}

		//[Fact]
		public void TestUTF8()
		{
			// the following file name was reported in the discussions as having
			// problems:
			// https://netftp.codeplex.com/discussions/445090
			string filename = "Verbundmörtel Zubehör + Technische Daten DE.pdf";

			using (FtpClient cl = NewFtpClient())
			{
				cl.DataConnectionType = FtpDataConnectionType.PASV;
				cl.InternetProtocolVersions = FtpIpVersion.ANY;

				using (Stream ostream = cl.OpenWrite(filename))
				using (StreamWriter writer = new StreamWriter(ostream))
				{
					writer.WriteLine(filename);
				}
			}
		}

		//[Fact]
		public void TestUploadDownloadFile()
		{
			using (FtpClient cl = NewFtpClient())
			{
				cl.Connect();

				// 100 K file
				cl.UploadFile(@"D:\Github\hgupta\FluentFTP\README.md", "/public_html/temp/README.md");
				cl.DownloadFile(@"D:\Github\hgupta\FluentFTP\README2.md", "/public_html/temp/README.md");

				/*
				// 10 M file
				cl.UploadFile(@"D:\Drivers\mb_driver_intel_irst_6series.exe", "/public_html/temp/big.txt");
				cl.Rename("/public_html/temp/big.txt", "/public_html/temp/big2.txt");
				cl.DownloadFile(@"D:\Drivers\mb_driver_intel_irst_6series_2.exe", "/public_html/temp/big2.txt");
				*/
			}
		}

#if !NOASYNC
		//[Fact]
        public async Task TestUploadDownloadFileAsync()
        {
            using (FtpClient cl = NewFtpClient())
            {
                // 100 K file
                await cl.UploadFileAsync(@"D:\Github\hgupta\FluentFTP\README.md", "/public_html/temp/README.md");
                await cl.DownloadFileAsync(@"D:\Github\hgupta\FluentFTP\README2.md", "/public_html/temp/README.md");

                /*
                // 10 M file
                await cl.UploadFileAsync(@"D:\Drivers\mb_driver_intel_irst_6series.exe", "/public_html/temp/big.txt");
                await cl.RenameAsync("/public_html/temp/big.txt", "/public_html/temp/big2.txt");
                await cl.DownloadFileAsync(@"D:\Drivers\mb_driver_intel_irst_6series_2.exe", "/public_html/temp/big2.txt");
                */
            }
        }
#endif

		//[Fact]
		public void TestUploadDownloadFile_UTF()
		{
			using (FtpClient cl = NewFtpClient())
			{
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

		//[Fact]
		public void TestUploadDownloadFile_ANSI()
		{
			using (FtpClient cl = NewFtpClient())
			{
				cl.Encoding = Encoding.GetEncoding(1252);

				// 100 K file
				string rpath = "/public_html/temp/Caffè.jpg";
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

		//[Fact]
		public void TestUploadDownloadManyFiles()
		{
			using (FtpClient cl = NewFtpClient())
			{
				cl.EnableThreadSafeDataConnections = false;
				cl.Connect();

				// 100 K file
				for (int i = 0; i < 3; i++)
				{
					FtpTrace.WriteLine(" ------------- UPLOAD " + i + " ------------------");
					cl.UploadFile(@"D:\Drivers\mb_driver_intel_bootdisk_irst_64_6series.exe", "/public_html/temp/small.txt");
				}

				// 100 K file
				for (int i = 0; i < 3; i++)
				{
					FtpTrace.WriteLine(" ------------- DOWNLOAD " + i + " ------------------");
					cl.DownloadFile(@"D:\Drivers\test\file" + i + ".exe", "/public_html/temp/small.txt");
				}

				FtpTrace.WriteLine(" ------------- ALL DONE! ------------------");
			}
		}

#if !NOASYNC
		//[Fact]
        public async Task TestUploadDownloadManyFilesAsync()
        {
            using (FtpClient cl = NewFtpClient())
            {
                cl.EnableThreadSafeDataConnections = false;
                await cl.ConnectAsync();

                // 100 K file
                for (int i = 0; i < 3; i++)
                {
                    FtpTrace.WriteLine(" ------------- UPLOAD " + i + " ------------------");
                    await cl.UploadFileAsync(@"D:\Drivers\mb_driver_intel_bootdisk_irst_64_6series.exe", "/public_html/temp/small.txt");
                }

                // 100 K file
                for (int i = 0; i < 3; i++)
                {
                    FtpTrace.WriteLine(" ------------- DOWNLOAD " + i + " ------------------");
                    await cl.DownloadFileAsync(@"D:\Drivers\test\file" + i + ".exe", "/public_html/temp/small.txt");
                }

                FtpTrace.WriteLine(" ------------- ALL DONE! ------------------");
            }
        }
#endif

		//[Fact]
		public void TestUploadDownloadManyFiles2()
		{
			using (FtpClient cl = NewFtpClient())
			{
				cl.EnableThreadSafeDataConnections = false;
				cl.Connect();

				// upload many
				cl.UploadFiles(new string[] { @"D:\Drivers\test\file0.exe", @"D:\Drivers\test\file1.exe", @"D:\Drivers\test\file2.exe", @"D:\Drivers\test\file3.exe", @"D:\Drivers\test\file4.exe" }, "/public_html/temp/", FtpExists.Skip);

				// download many
				cl.DownloadFiles(@"D:\Drivers\test\", new string[] { @"/public_html/temp/file0.exe", @"/public_html/temp/file1.exe", @"/public_html/temp/file2.exe", @"/public_html/temp/file3.exe", @"/public_html/temp/file4.exe" }, FtpLocalExists.Append);

				FtpTrace.WriteLine(" ------------- ALL DONE! ------------------");

				cl.Dispose();
			}
		}

#if !NOASYNC
		//[Fact]
        public async Task TestUploadDownloadManyFiles2Async()
        {
            using (FtpClient cl = NewFtpClient())
            {
                cl.EnableThreadSafeDataConnections = false;
                await cl.ConnectAsync();

                // upload many
                await cl.UploadFilesAsync(new string[] { @"D:\Drivers\test\file0.exe", @"D:\Drivers\test\file1.exe", @"D:\Drivers\test\file2.exe", @"D:\Drivers\test\file3.exe", @"D:\Drivers\test\file4.exe" }, "/public_html/temp/", createRemoteDir: false);

                // download many
                await cl.DownloadFilesAsync(@"D:\Drivers\test\", new string[] { @"/public_html/temp/file0.exe", @"/public_html/temp/file1.exe", @"/public_html/temp/file2.exe", @"/public_html/temp/file3.exe", @"/public_html/temp/file4.exe" }, FtpLocalExists.Append);

                FtpTrace.WriteLine(" ------------- ALL DONE! ------------------");

                cl.Dispose();
            }
        }
#endif

		//[Fact]
		public void TestUploadDownloadZeroLenFile()
		{
			using (FtpClient cl = NewFtpClient())
			{
				// 0 KB file
				cl.UploadFile(@"D:\zerolen.txt", "/public_html/temp/zerolen.txt");
				cl.DownloadFile(@"D:\zerolen2.txt", "/public_html/temp/zerolen.txt");
			}
		}

		//[Fact]
		public void TestListSpacedPath()
		{
			using (FtpClient cl = NewFtpClient())
			{
				cl.EncryptionMode = FtpEncryptionMode.Explicit;
				cl.ValidateCertificate += OnValidateCertificate;

				foreach (FtpListItem i in cl.GetListing("/public_html/temp/spaced folder/"))
				{
					FtpTrace.WriteLine(i.FullName);
				}
			}
		}

		//[Fact]
		public void TestFilePermissions()
		{
			using (FtpClient cl = NewFtpClient())
			{
				foreach (FtpListItem i in cl.GetListing("/public_html/temp/"))
				{
					FtpTrace.WriteLine(i.Name + " - " + i.Chmod);
				}

				FtpListItem o = cl.GetFilePermissions("/public_html/temp/file3.exe");
				FtpListItem o2 = cl.GetFilePermissions("/public_html/temp/README.md");

				cl.SetFilePermissions("/public_html/temp/file3.exe", 646);

				int o22 = cl.GetChmod("/public_html/temp/file3.exe");
			}
		}

		//[Fact]
		public void TestFileExists()
		{
			using (FtpClient cl = NewFtpClient())
			{
				bool f1_yes = cl.FileExists("/public_html");
				bool f2_yes = cl.FileExists("/public_html/temp");
				bool f3_yes = cl.FileExists("/public_html/temp/");
				bool f3_no = cl.FileExists("/public_html/tempa/");
				bool f4_yes = cl.FileExists("/public_html/temp/README.md");
				bool f4_no = cl.FileExists("/public_html/temp/README");
				bool f5_yes = cl.FileExists("/public_html/temp/Caffè.jpg");
				bool f5_no = cl.FileExists("/public_html/temp/Caffèoo.jpg");

				cl.SetWorkingDirectory("/public_html/");

				bool z_f2_yes = cl.FileExists("temp");
				bool z_f3_yes = cl.FileExists("temp/");
				bool z_f3_no = cl.FileExists("tempa/");
				bool z_f4_yes = cl.FileExists("temp/README.md");
				bool z_f4_no = cl.FileExists("temp/README");
				bool z_f5_yes = cl.FileExists("temp/Caffè.jpg");
				bool z_f5_no = cl.FileExists("temp/Caffèoo.jpg");
			}
		}

		//[Fact]
		public void TestDeleteDirectory()
		{
			using (FtpClient cl = NewFtpClient())
			{
				cl.DeleteDirectory("/public_html/temp/otherdir/");
				cl.DeleteDirectory("/public_html/temp/spaced folder/");
			}
		}

		//[Fact]
		public void TestMoveFiles()
		{
			using (FtpClient cl = NewFtpClient())
			{
				cl.MoveFile("/public_html/temp/README.md", "/public_html/temp/README_moved.md");

				cl.MoveDirectory("/public_html/temp/dir/", "/public_html/temp/dir_moved/");
			}
		}
	}
}

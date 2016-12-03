using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using FluentFTP;
using System.Threading;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Text;
using FluentFTP.Proxy;

namespace Tests {
    /// <summary>
    /// Torture test
    /// </summary>
    class Program {

		//static readonly string m_host = "127.0.0.1";
		static readonly string m_host = "127.0.0.1";
        static readonly string m_user = "anonymous";
        static readonly string m_pass = "";

        static void Main(string[] args) {
            FtpTrace.FlushOnWrite = true;
            FtpTrace.AddListener(new ConsoleTraceListener());

            try {
                /*foreach (int i in new int[] {
                     (int)FtpDataConnectionType.EPSV,
                     (int)FtpDataConnectionType.EPRT,
                     (int)FtpDataConnectionType.PASV,
                     (int)FtpDataConnectionType.PORT
                 }) {
                    using (FtpClient cl = new FtpClient()) {
                        cl.Credentials = new NetworkCredential(m_user, m_pass);
                        cl.Host = m_host;
                        cl.EncryptionMode = FtpEncryptionMode.None;
                        cl.ValidateCertificate += new FtpSslValidation(cl_ValidateCertificate);
                        cl.DataConnectionType = (FtpDataConnectionType)i;
                        //cl.Encoding = System.Text.Encoding.Default;
                        cl.Connect();
                        Upload(cl);
                        Download(cl);
                        Delete(cl);
                    }
                }*/

                //StreamResponses();

                //TestServer();

                //TestManualEncoding();

                //TestServer();

                //TestDisposeWithMultipleThreads();

                //TestMODCOMP_PWD_Parser();
                //TestDispose();
                //TestHash();

                TestNameListing();
                //TestOpenVMSParser();
                // TestIISParser();
                //GetMicrosoftFTPListing();
                //TestReset();
                //TestUTF8();

                //TestDirectoryWithDots();

                //TestUnixListParser();

                // TestFileZillaKick();

                //TestUnixList();
                //TestNetBSDServer();

                // TestConnectionFailure();

                //TestGetObjectInfo();

                //TestFtpPath();

                //TestUnixListing();

                //TestListPath();

                //TestListPathWithHttp11Proxy();
            }
            catch (Exception ex) {
                Console.WriteLine(ex.ToString());
            }

            Console.WriteLine("--DONE--");
            Console.ReadKey();
        }

        static void TestListPathWithHttp11Proxy()
        {
            using (FtpClient cl = new FtpClientHttp11Proxy(new ProxyInfo { Host = "127.0.0.1", Port = 3128, })) // Credential = new NetworkCredential() 
            {
                Console.WriteLine("FTPClient::ConnectionType = '" + cl.ConnectionType + "'");

                cl.Credentials = new NetworkCredential(m_user, m_pass);
                cl.Host = m_host;
                cl.ValidateCertificate += OnValidateCertificate;
                cl.DataConnectionType = FtpDataConnectionType.PASV;
                cl.Connect();

                foreach (FtpListItem item in cl.GetListing(null, FtpListOption.SizeModify | FtpListOption.ForceNameList))
                {
                    Console.WriteLine(item.Modified.Kind);
                    Console.WriteLine(item.Modified);
                }
            }
        }

        static void TestListPath() {
            using(FtpClient cl = new FtpClient()) {
                cl.Credentials = new NetworkCredential(m_user, m_pass);
                cl.Host = m_host;
                cl.EncryptionMode = FtpEncryptionMode.None;

                cl.GetListing();
                Console.WriteLine("Path listing succeeded");
                cl.GetListing(null, FtpListOption.NoPath);
                Console.WriteLine("No path listing succeeded");
            }
        }

        static void StreamResponses() {
            using (FtpClient cl = new FtpClient()) {
                cl.Credentials = new NetworkCredential(m_user, m_pass);
                cl.Host = m_host;
                cl.EncryptionMode = FtpEncryptionMode.None;
                cl.ValidateCertificate += new FtpSslValidation(delegate(FtpClient control, FtpSslValidationEventArgs e) {
                    e.Accept = true;
                });

                using (FtpDataStream s = (FtpDataStream)cl.OpenWrite("test.txt")) {
                    FtpReply r = s.CommandStatus;

                    Console.WriteLine();
                    Console.WriteLine("Response to STOR:");
                    Console.WriteLine("Code: {0}", r.Code);
                    Console.WriteLine("Message: {0}", r.Message);
                    Console.WriteLine("Informational: {0}", r.InfoMessages);

                    r = s.Close();
                    Console.WriteLine();
                    Console.WriteLine("Response after close:");
                    Console.WriteLine("Code: {0}", r.Code);
                    Console.WriteLine("Message: {0}", r.Message);
                    Console.WriteLine("Informational: {0}", r.InfoMessages);
                }
            }
        }

        static void TestUnixListing() {
            using (FtpClient cl = new FtpClient()) {
				cl.Host = m_host;
                cl.Credentials = new NetworkCredential("ftptest", "ftptest");

                if (!cl.FileExists("test.txt")) {
                    using (Stream s = cl.OpenWrite("test.txt")) {
                        s.Close();
                    }
                }

                foreach (FtpListItem i in cl.GetListing(null, FtpListOption.ForceList)) {
                    Console.WriteLine(i);
                }
            }
        }

        static void TestFtpPath() {
            string path = "/home/sigurdhj/errors/16.05.2014/asdasd/asd asd asd aa asd/Kooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooo::asdasd";

            Console.WriteLine(path.GetFtpDirectoryName());
            Console.WriteLine("./foobar".GetFtpDirectoryName());
            Console.WriteLine("foobar".GetFtpDirectoryName());
            Console.WriteLine(path.GetFtpFileName());
            Console.WriteLine("/foo/bar".GetFtpFileName());
            Console.WriteLine("./foo/bar".GetFtpFileName());
            Console.WriteLine("./bar".GetFtpFileName());
            Console.WriteLine("/bar".GetFtpFileName());
            Console.WriteLine("bar".GetFtpFileName());
        }

        static void TestGetObjectInfo() {
            using (FtpClient cl = new FtpClient()) {
                FtpListItem item;

                cl.Host = m_host;
                cl.Credentials = new NetworkCredential("ftptest", "ftptest");
                cl.Encoding = Encoding.Default;

                item = cl.GetObjectInfo("/Examples/OpenRead.cs");
                Console.WriteLine(item.ToString());
            }
        }

		static void TestManualEncoding() {
			using (FtpClient cl = new FtpClient()) {
                cl.Host = m_host;
                cl.Credentials = new NetworkCredential("ftptest", "ftptest");
                cl.Encoding = Encoding.Default;

                using (Stream s = cl.OpenWrite("test.txt")) {
                    s.Close();
                }
            }
        }

        static void TestServer() {
            using (FtpClient cl = new FtpClient()) {
                cl.Host = m_host;
                cl.Credentials = new NetworkCredential("ftptest", "ftptest");
                cl.EncryptionMode = FtpEncryptionMode.Explicit;
				cl.ValidateCertificate += (control, e) => {
                    e.Accept = true;
                };

                foreach (FtpListItem i in cl.GetListing("/")) {
                    Console.WriteLine(i.FullName);
                }
            }
        }

        static void TestServerDownload(FtpClient client, string path) {
            foreach (FtpListItem i in client.GetListing(path)) {
                switch (i.Type) {
                    case FtpFileSystemObjectType.Directory:
                        TestServerDownload(client, i.FullName);
                        break;
                    case FtpFileSystemObjectType.File:
                        using (Stream s = client.OpenRead(i.FullName)) {
                            byte[] b = new byte[8192];
                            int read = 0;
                            long total = 0;

                            try {
                                while ((read = s.Read(b, 0, b.Length)) > 0) {
                                    total += read;

                                    Console.Write("\r{0}/{1} {2:p}          ",
                                        total, s.Length, (double)total / (double)s.Length);
                                }

                                Console.Write("\r{0}/{1} {2:p}       ",
                                        total, s.Length, (double)total / (double)s.Length);
                            }
                            finally {
                                Console.WriteLine();
                            }
                        }
                        break;
                }
            }
        }

        static void cl_ValidateCertificate(FtpClient control, FtpSslValidationEventArgs e) {
            e.Accept = true;
        }

        static void TestDisposeWithMultipleThreads() {
            using (FtpClient cl = new FtpClient()) {
                cl.Host = "ftp.netbsd.org";
                cl.Credentials = new NetworkCredential("ftp", "ftp");

                Thread t1 = new Thread(() => {
                    cl.GetListing();
                });

                Thread t2 = new Thread(() => {
                    cl.Dispose();
                });

                t1.Start();
                Thread.Sleep(500);
                t2.Start();

                t1.Join();
                t2.Join();
            }
        }

        static void TestConnectionFailure() {
            try {
                using (FtpClient cl = new FtpClient()) {
                    cl.Credentials = new NetworkCredential("ftp", "ftp");
                    cl.Host = "somefakehost";
                    cl.ConnectTimeout = 5000;
                    cl.Connect();
                }
            }
            catch (Exception e) {
                Console.WriteLine("Caught connection faillure: {0}", e.Message);
            }
        }

        static void TestNetBSDServer() {
            using (FtpClient client = new FtpClient()) {
                client.Credentials = new NetworkCredential("ftp", "ftp");
                client.Host = "ftp.netbsd.org";

                foreach (FtpListItem item in client.GetListing(null,
                    FtpListOption.ForceList | FtpListOption.Modify | FtpListOption.DerefLinks)) {
                    Console.WriteLine(item);

                    if (item.Type == FtpFileSystemObjectType.Link && item.LinkObject != null)
                        Console.WriteLine(item.LinkObject);
                }
            }
        }

        static void TestUnixList() {
            using (FtpClient client = new FtpClient()) {
                client.Credentials = new NetworkCredential("ftptest", "ftptest");
                client.Host = m_host;
                foreach (FtpListItem i in client.GetListing(null, FtpListOption.ForceList | FtpListOption.Recursive)) {
                    Console.WriteLine(i.FullName);
                }
            }
        }

        static void TestFileZillaKick() {
            using (FtpClient cl = new FtpClient()) {
                cl.Host = "localhost";
                cl.Credentials = new NetworkCredential("ftptest", "ftptest");
                cl.EnableThreadSafeDataConnections = false;

                if (cl.FileExists("TestFile.txt"))
                    cl.DeleteFile("TestFile.txt");

                try {
                    Stream s = cl.OpenWrite("TestFile.txt");
                    for (int i = 0; true; i++) {
                        s.WriteByte((byte)i);
                        Thread.Sleep(100);
                    }

                    //s.Close();
                }
                catch (FtpCommandException ex) {
                    Console.WriteLine("Exception caught!");
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        static void TestUnixListParser() {
            string[] sample = new string[] {
                "drwxr-xr-x   7  user1 user1       512 Sep 27  2011 .",
                "drwxr-xr-x  31 user1  user1      1024 Sep 27  2011 ..",
                "lrwxrwxrwx   1 user1  user1      9 Sep 27  2011 data.0000 -> data.6460",
                "drwxr-xr-x  10 user1  user1      512 Jun 29  2012 data.6460",
                "lrwxrwxrwx   1 user1 user1       8 Sep 27  2011 sys.0000 -> sys.6460",
                "drwxr-xr-x 133 user1  user1     4096 Jun 25 16:26 sys.6460"
            };

            foreach (string s in sample) {
                FtpListItem item = FtpListItem.Parse("/", s, FtpCapability.NONE);

                if (item != null)
                    Console.WriteLine(item);
            }
        }

        static void TestDirectoryWithDots() {
            using (FtpClient cl = new FtpClient()) {
                cl.Credentials = new NetworkCredential("ftptest", "ftptest");
                cl.Host = "localhost";
                cl.Connect();
                // FTP server set to timeout after 5 seconds.
                //Thread.Sleep(6000);

                cl.GetListing("Test.Directory", FtpListOption.ForceList);
                cl.SetWorkingDirectory("Test.Directory");
                cl.GetListing(null, FtpListOption.ForceList);
            }
        }

        static void TestDispose() {
            using (FtpClient cl = new FtpClient()) {
                cl.Credentials = new NetworkCredential("ftptest", "ftptest");
                cl.Host = "localhost";
                cl.Connect();
                // FTP server set to timeout after 5 seconds.
                //Thread.Sleep(6000);

                foreach (FtpListItem item in cl.GetListing()) {

                }
            }
        }

        static void TestHash() {
            using (FtpClient cl = new FtpClient()) {
                cl.Credentials = new NetworkCredential("ftptest", "ftptest");
                cl.Host = "localhost";
                cl.Connect();

                Console.WriteLine("Supported HASH algorithms: {0}", cl.HashAlgorithms);
                Console.WriteLine("Current HASH algorithm: {0}", cl.GetHashAlgorithm());

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
            }
        }

        static void TestReset() {
            using (FtpClient cl = new FtpClient()) {
                cl.Credentials = new NetworkCredential("ftptest", "ftptest");
                cl.Host = "localhost";
                cl.Connect();

                using (Stream istream = cl.OpenRead("LICENSE.TXT", 10)) {
                    istream.Close();
                }
            }
        }

        static void GetMicrosoftFTPListing() {
            using (FtpClient cl = new FtpClient()) {
                cl.Credentials = new NetworkCredential("ftp", "ftp");
                cl.Host = "ftp.microsoft.com";
                cl.Connect();

                Console.WriteLine(cl.Capabilities);

                foreach (FtpListItem item in cl.GetListing(null, FtpListOption.Modify)) {
                    Console.WriteLine(item.Modified);
                }
            }
        }

        static void TestIISParser() {
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

            foreach (string s in sample) {
                FtpListItem item = FtpListItem.Parse("/", s, 0);

                if (item != null) {
                    Console.WriteLine(item.Modified);
                    //Console.WriteLine(item);
                }
            }
        }

        static void TestOpenVMSParser() {
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

            foreach (string s in sample) {
                FtpListItem item = FtpListItem.Parse("disk$user520:[4114.2012.Jan]", s, 0);

                if (item != null) {
                    Console.WriteLine(item.Modified);
                    //Console.WriteLine(item);
                }
            }
        }

        static void TestMODCOMP_PWD_Parser() {
            string response = "PWD = ~TNA=AMP,VNA=VOL03,FNA=U-ED-B2-USL";
            Match m;

            if ((m = Regex.Match(response, "PWD = (?<pwd>.*)")).Success)
                Console.WriteLine("PWD: {0}", m.Groups["pwd"].Value);
        }

        static void TestNameListing() {
            using (FtpClient cl = new FtpClient()) {
                //cl.Credentials = new NetworkCredential(m_user, m_pass);
				cl.Host = m_host;
                cl.ValidateCertificate += OnValidateCertificate;
                cl.DataConnectionType = FtpDataConnectionType.PASV;
                //cl.EncryptionMode = FtpEncryptionMode.Explicit;
                //cl.SocketPollInterval = 5000;
                cl.Connect();

                //Console.WriteLine("Sleeping for 10 seconds to force timeout.");
                //Thread.Sleep(10000);

                foreach (FtpListItem item in cl.GetListing()) {
					//Console.WriteLine(item.FullName);
                    //Console.WriteLine(item.Modified.Kind);
                    //Console.WriteLine(item.Modified);
                }
            }
        }

        static FtpClient Connect() {
            List<Thread> threads = new List<Thread>();
            FtpClient cl = new FtpClient();

            cl.ValidateCertificate += OnValidateCertificate;
            //cl.EncryptionMode = FtpEncryptionMode.Explicit;

            for (int i = 0; i < 10; i++) {
                int count = i;

                Thread t = new Thread(new ThreadStart(delegate() {
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

            while (threads.Count > 0) {
                threads[0].Join();
                threads.RemoveAt(0);
            }

            return cl;
        }

        static void OnValidateCertificate(FtpClient control, FtpSslValidationEventArgs e) {
            e.Accept = true;
        }

        static void Upload(FtpClient cl) {
            string root = @"..\..\..";
            List<Thread> threads = new List<Thread>();

            foreach (string s in Directory.GetFiles(root, "*", SearchOption.AllDirectories)) {
                string file = s;

                if (file.Contains(@"\.git"))
                    continue;

                Thread t = new Thread(new ThreadStart(delegate() {
                    DoUpload(cl, root, file);
                }));

                t.Start();
                threads.Add(t);
            }

            while (threads.Count > 0) {
                threads[0].Join();
                threads.RemoveAt(0);
            }
        }

        static void DoUpload(FtpClient cl, string root, string s) {
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
                        ostream = cl.OpenWrite(s.Replace(root, ""), type)) {
                byte[] buf = new byte[8192];
                int read = 0;

                try {
                    while ((read = istream.Read(buf, 0, buf.Length)) > 0) {
                        ostream.Write(buf, 0, read);
                    }
                }
                finally {
                    ostream.Close();
                    istream.Close();
                }

                if (cl.HashAlgorithms != FtpHashAlgorithm.NONE) {
                    Debug.Assert(cl.GetHash(s.Replace(root, "")).Verify(s), "The computed hashes don't match!");
                }
            }

            /*if (!cl.GetHash(s.Replace(root, "")).Verify(s))
                throw new Exception("Hashes didn't match!");*/
        }

        static void Download(FtpClient cl) {
            List<Thread> threads = new List<Thread>();

            Download(threads, cl, "/");

            while (threads.Count > 0) {
                threads[0].Join();

                lock (threads) {
                    threads.RemoveAt(0);
                }
            }
        }

        static void Download(List<Thread> threads, FtpClient cl, string path) {
            foreach (FtpListItem item in cl.GetListing(path)) {
                if (item.Type == FtpFileSystemObjectType.Directory)
                    Download(threads, cl, item.FullName);
                else if (item.Type == FtpFileSystemObjectType.File) {
                    string file = item.FullName;

                    Thread t = new Thread(new ThreadStart(delegate() {
                        DoDownload(cl, file);
                    }));

                    t.Start();

                    lock (threads) {
                        threads.Add(t);
                    }
                }
            }
        }

        static void DoDownload(FtpClient cl, string file) {
            using (Stream s = cl.OpenRead(file)) {
                byte[] buf = new byte[8192];

                try {
                    while (s.Read(buf, 0, buf.Length) > 0) ;
                }
                finally {
                    s.Close();
                }
            }
        }

        static void Delete(FtpClient cl) {
            DeleteDirectory(cl, "/");
        }

        static void DeleteDirectory(FtpClient cl, string path) {
            foreach (FtpListItem item in cl.GetListing(path)) {
                if (item.Type == FtpFileSystemObjectType.File) {
                    cl.DeleteFile(item.FullName);
                }
                else if (item.Type == FtpFileSystemObjectType.Directory) {
                    DeleteDirectory(cl, item.FullName);
                    cl.DeleteDirectory(item.FullName);
                }
            }
        }

        static void TestUTF8() {
            // the following file name was reported in the discussions as having
            // problems:
            // https://netftp.codeplex.com/discussions/445090
            string filename = "Verbundmörtel Zubehör + Technische Daten DE.pdf";

            using (FtpClient cl = new FtpClient()) {
                cl.Host = "localhost";
                cl.Credentials = new NetworkCredential("ftptest", "ftptest");
                cl.DataConnectionType = FtpDataConnectionType.PASV;
                cl.InternetProtocolVersions = FtpIpVersion.ANY;

                using (Stream ostream = cl.OpenWrite(filename)) {
                    StreamWriter writer = new StreamWriter(filename);
                    writer.WriteLine(filename);
                    writer.Close();
                }
            }
        }
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.FtpClient;
using System.Threading;
using System.Collections.Generic;

namespace Tests {
    /// <summary>
    /// Torture test
    /// </summary>
    class Program {
        static readonly string m_host = "localhost";
        static readonly string m_user = "ftptest";
        static readonly string m_pass = "ftptest";

        static void Main(string[] args) {
            Debug.Listeners.Add(new ConsoleTraceListener());

            try {
                foreach (int i in new int[] {
                    (int)FtpDataConnectionType.EPSV,
                    (int)FtpDataConnectionType.EPRT,
                    (int)FtpDataConnectionType.PASV,
                    (int)FtpDataConnectionType.PORT
                }) {
                    using (FtpClient cl = Connect()) {
                        cl.DataConnectionType = (FtpDataConnectionType)i;
                        Upload(cl);
                        Download(cl);
                        Delete(cl);
                    }
                }

                //TestNameListing();
                //TestOpenVMSParser();
            }
            catch (Exception ex) {
                Console.WriteLine(ex.ToString());
            }

            Console.WriteLine("--DONE--");
            Console.ReadKey();
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
                    Console.WriteLine(item);
                }
            }
        }

        static void TestNameListing() {
            using (FtpClient cl = new FtpClient()) {
                cl.Credentials = new NetworkCredential(m_user, m_pass);
                cl.Host = m_host;
                cl.Connect();

                foreach (FtpListItem item in cl.GetListing(null, FtpListOption.SizeModify | FtpListOption.ForceList)) {
                    Console.WriteLine(item);
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
            string path = Path.GetDirectoryName(s).Replace(root, "");
            string name = Path.GetFileName(s);

            if (!cl.DirectoryExists(path))
                cl.CreateDirectory(path, true);
            else if (cl.FileExists(string.Format("{0}/{1}", path, name)))
                cl.DeleteFile(string.Format("{0}/{1}", path, name));

            using (
                Stream istream = new FileStream(s, FileMode.Open, FileAccess.Read),
                        ostream = cl.OpenWrite(s.Replace(root, ""))) {
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
            }
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
    }
}

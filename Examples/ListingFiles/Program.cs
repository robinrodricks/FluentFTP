using System;
using System.Net.FtpClient;

namespace ListingFiles {
    class Program {
        static void Main(string[] args) {
            try {
                using (FtpClient cl = new FtpClient("ftp", "ftp", "ftp.mozilla.org")) {
                    cl.FtpLogStream = Console.OpenStandardOutput();
                    cl.FtpLogFlushOnWrite = true;

                    // example using GetListing()
                    foreach (FtpListItem item in cl.GetListing("/pub")) {
                        // if the server used LIST to get a file listing
                        // the modify date probably isn't accurate so lets
                        // pull an accurate one with MDTM. most servers
                        // that I have encountered do not support MDTM on
                        // directories but that may not always be true.
                        if (!cl.HasCapability(FtpCapability.MLSD) && cl.HasCapability(FtpCapability.MDTM)) {
                            DateTime modify = cl.GetLastWriteTime(string.Format("/pub/{0}", item.Name));

                            if (modify != DateTime.MinValue) {
                                item.Modify = modify;
                            }
                        }

                        Console.WriteLine(item.ToString());
                    }

                    // example using FtpFileSystemObject derivatives
                    using (FtpDirectory dir = new FtpDirectory(cl, "/pub")) {
                        foreach (FtpDirectory d in dir.Directories) {
                            Console.WriteLine("{0} {1} {2}", d.Name, d.Length, d.LastWriteTime);
                        }

                        foreach (FtpFile f in dir.Files) {
                            Console.WriteLine("{0} {1} {2}", f.Name, f.Length, f.LastWriteTime);
                        }
                    }
                }
            }
            catch (Exception ex) {
                Console.Error.WriteLine(ex.Message);
            }

            Console.WriteLine("-- PRESS ANY KEY TO CLOSE --");
            Console.ReadKey();
        }
    }
}

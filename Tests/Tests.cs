using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Net.FtpClient.Tests {
    [TestClass]
    public class Tests {
        [TestMethod]
        public void RunTests() {
            UploadFiles();
            DownloadFiles();
            DeleteFiles();
        }

        void UploadFiles() {
            using (FtpClient cl = new FtpClient(Config.FtpUser, Config.FtpPass, Config.FtpServer, Config.FtpPort)) {
                cl.FtpLogStream = Config.OpenTransactionLog();
                //cl.SslMode = FtpSslMode.None;
                cl.TransferProgress += new FtpTransferProgress(OnTransferProgress);
                cl.InvalidCertificate += new FtpInvalidCertificate(OnInvalidCertificate);
                UploadDirectory(cl, Config.FtpSourceCode, "/");
            }
        }

        void UploadDirectory(FtpClient cl, string path, string remotepath) {
            if (!cl.DirectoryExists(remotepath)) {
                cl.CreateDirectory(remotepath);
            }

            foreach (string f in System.IO.Directory.GetFiles(path, "*.cs")) {
                string remote = string.Format("{0}/{1}", remotepath, System.IO.Path.GetFileName(f));

                if (cl.FileExists(remote)) {
                    cl.RemoveFile(remote);
                }

                cl.Upload(f, remote);
            }

            foreach (string d in System.IO.Directory.GetDirectories(path)) {
                if (System.IO.Path.GetDirectoryName(d) != "Tests") {
                    UploadDirectory(cl, d, string.Format("{0}/{1}", remotepath, System.IO.Path.GetFileName(d)));
                }
            }
        }

        void DownloadFiles() {
            string root = @"C:\FTPTest";

            using (FtpClient cl = new FtpClient(Config.FtpUser, Config.FtpPass, Config.FtpServer, Config.FtpPort)) {
                cl.FtpLogStream = Config.OpenTransactionLog();
                cl.TransferProgress += new FtpTransferProgress(OnTransferProgress);
                cl.InvalidCertificate += new FtpInvalidCertificate(OnInvalidCertificate);
                DownloadDirectory(cl, root, cl.CurrentDirectory);
            }

            System.IO.Directory.Delete(root, true);
        }

        void DownloadDirectory(FtpClient cl, string root, FtpDirectory path) {
            if (!System.IO.Directory.Exists(root)) {
                System.IO.Directory.CreateDirectory(root);
            }

            foreach (FtpFile f in path.Files.ToArray()) {
                string local = string.Format(@"{0}\{1}", root, f.Name);

                if (System.IO.File.Exists(local)) {
                    System.IO.File.Delete(local);
                }

                cl.Download(f.FullName, local);
            }

            foreach (FtpDirectory d in path.Directories.ToArray()) {
                DownloadDirectory(cl, string.Format(@"{0}\{1}", root, d.Name), d);
            }
        }

        void DeleteFiles() {
            using (FtpClient cl = new FtpClient(Config.FtpUser, Config.FtpPass, Config.FtpServer, Config.FtpPort)) {
                cl.FtpLogStream = Config.OpenTransactionLog();
                cl.TransferProgress += new FtpTransferProgress(OnTransferProgress);
                cl.InvalidCertificate += new FtpInvalidCertificate(OnInvalidCertificate);
                DeleteFiles(cl.CurrentDirectory);
            }
        }

        void DeleteFiles(FtpDirectory path) {
            foreach (FtpFile f in path.Files.ToArray()) {
                f.Delete();
            }

            foreach (FtpDirectory d in path.Directories.ToArray()) {
                DeleteFiles(d);
                d.Delete();
            }
        }

        void OnInvalidCertificate(FtpChannel c, InvalidCertificateInfo e) {
            e.Ignore = true;
        }

        void OnTransferProgress(FtpTransferInfo e) {
            if (e.TransferType == FtpTransferType.Download) {
                System.Diagnostics.Debug.Write("Download: ");
            }
            else {
                System.Diagnostics.Debug.Write("Upload: ");
            }

            System.Diagnostics.Debug.WriteLine(string.Format("{0} {1}/{2} {3}%",
                e.FileName, e.Transferred, e.Length, e.Percentage));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.FtpClient;
using System.IO;

namespace ReleaseTests {
	class Program {
		static void RecursiveDownload(FtpDirectory dir, string local) {
			if (!Directory.Exists(local)) {
				Directory.CreateDirectory(local);
			}

			foreach (FtpFile f in dir.Files) {
				f.Download(string.Format("{0}\\{1}", local, f.Name));
			}

			foreach (FtpDirectory d in dir.Directories) {
				RecursiveDownload(d, string.Format("{0}\\{1}", local, d.Name));
			}
		}

		static void RecursiveUpload(FtpDirectory remote, DirectoryInfo local) {
			foreach (FileInfo f in local.GetFiles()) {
				FtpFile ff = new FtpFile(remote.Client, string.Format("{0}/{1}", remote.FullName, f.Name));

				if (!ff.Exists) {
					ff.Upload(f.FullName);
				}
			}

			foreach (DirectoryInfo d in local.GetDirectories()) {
				if (!remote.DirectoryExists(d.Name)) {
					remote.CreateDirectory(d.Name);
				}

				RecursiveUpload(new FtpDirectory(remote.Client,
					string.Format("{0}/{1}", remote.FullName, d.Name)), d);
			}
		}

		static void RecursiveDelete(FtpDirectory dir) {
			foreach (FtpFile f in dir.Files) {
				Console.WriteLine("X: {0}", f.FullName);
				f.Delete();
			}

			foreach (FtpDirectory d in dir.Directories) {
				RecursiveDelete(d);
				Console.WriteLine("X: {0}", d.FullName);
				d.Delete();
			}
		}

		static void Main(string[] args) {
			try {
				using (FtpClient cl = new FtpClient() {
					Server = "localhost", Username = "test", Password = "test",
					SslMode = FtpSslMode.Implicit, Port = 990, 
					DefaultDataMode = FtpDataMode.Passive
				}) {
					try {
						cl.TransferProgress += new FtpTransferProgress(cl_TransferProgress);
						cl.InvalidCertificate += new FtpInvalidCertificate(cl_InvalidCertificate);

						RecursiveDownload(cl.CurrentDirectory, "c:\\temp");
						cl.Disconnect();
						RecursiveDelete(cl.CurrentDirectory);
						cl.Disconnect();
						RecursiveUpload(cl.CurrentDirectory, new DirectoryInfo("c:\\temp"));
					}
					catch (FtpInvalidCertificateException ex) {
						Console.WriteLine(ex.Message);
						Console.ReadKey();
					}
				}

				Console.WriteLine("Done");
				Console.ReadKey();
			}
			catch (Exception ex) {
				Console.WriteLine(ex.Message);
				Console.ReadKey();
			}
		}

		static void cl_InvalidCertificate(FtpChannel c, InvalidCertificateInfo e) {
			//Console.Error.WriteLine("Invalid SSL certification from {0}: {1}", 
			//	c.RemoteEndPoint.ToString(), e.SslPolicyErrors);
			e.Ignore = true;
		}

		static void cl_TransferProgress(FtpTransferInfo e) {
			Console.Write("\r{0}: {1} {2}/{3} {4}/s {5}%   ",
				e.TransferType == FtpTransferType.Upload ? "U" : "D",
				Path.GetFileName(e.FileName), e.Transferred, e.Length,
				e.BytesPerSecond, e.Percentage);

			if (e.Complete) {
				Console.WriteLine();
			}
		}
	}
}

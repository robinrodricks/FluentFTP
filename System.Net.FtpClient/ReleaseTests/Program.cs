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
				try {
					Directory.CreateDirectory(local);
				}
				catch (Exception ex) {
					Console.WriteLine("E: {0}", ex.Message);
					return;
				}
			}

			foreach (FtpFile f in dir.Files) {
				try {
					f.Download(string.Format("{0}\\{1}", local, f.Name));
				}
				catch (Exception e) {
					Console.WriteLine("E: {0}", e.Message);
				}
			}

			foreach (FtpDirectory d in dir.Directories) {
				try {
					RecursiveDownload(d, string.Format("{0}\\{1}", local, d.Name));
				}
				catch (Exception e) {
					Console.WriteLine("E: {0}", e.Message);
				}
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
					SslMode = FtpSslMode.None, Port = 21, 
					DefaultDataMode = FtpDataMode.Passive
				}) {
					try {
						cl.TransferProgress += new FtpTransferProgress(cl_TransferProgress);
						cl.InvalidCertificate += new FtpInvalidCertificate(cl_InvalidCertificate);

						RecursiveDownload(cl.CurrentDirectory, "c:\\temp");
						RecursiveDelete(cl.CurrentDirectory);
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

			// force an abort on some donwloads
			// to see how the code handles.
			//if (e.Transferred > 8 * 1024)
			//	e.Cancel = true;

			if (e.Complete) {
				Console.WriteLine();
			}
		}
	}
}

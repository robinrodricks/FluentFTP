using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.FtpClient;
using System.IO;

namespace ReleaseTests {
	class Program {
		static void DownloadFile(FtpFile f, string local) {
			long size = f.Length, total = 0;
			int read = 0;

			using (FtpDataChannel chan = f.OpenRead()) {
				FileStream fs = new FileStream(local, FileMode.OpenOrCreate, FileAccess.Write);

				try {
					byte[] buf = new byte[chan.RecieveBufferSize];

					while ((read = chan.Read(buf, 0, buf.Length)) > 0) {
						fs.Write(buf, 0, read);

						total += read;

						Console.Write("\rD: {0} {1}/{2} {3:p}",
							f.Name, total, size, ((double)total / (double)size));
					}
				}
				finally {
					fs.Close();
					Console.WriteLine();
				}
			}
		}

		static void RecursiveDownload(FtpDirectory dir, string local) {
			if (!Directory.Exists(local)) {
				Directory.CreateDirectory(local);
			}

			foreach (FtpFile f in dir.Files) {
				DownloadFile(f, string.Format("{0}\\{1}", local, f.Name));
			}

			foreach (FtpDirectory d in dir.Directories) {
				RecursiveDownload(d, string.Format("{0}\\{1}", local, d.Name));
			}
		}

		static void UploadFile(FtpDirectory remote, FileInfo local) {
			long size = local.Length, total = 0;
			int read = 0;

			using (FtpDataChannel chan = remote.Client.OpenWrite(string.Format("{0}/{1}", remote.FullName, local.Name))) {
				FileStream fs = new FileStream(local.FullName, FileMode.Open, FileAccess.Read);

				try {
					byte[] buf = new byte[chan.RecieveBufferSize];

					while ((read = fs.Read(buf, 0, buf.Length)) > 0) {
						chan.Write(buf, 0, read);

						total += read;

						Console.Write("\rU: {0} {1}/{2} {3:p}",
							local.Name, total, size, ((double)total / (double)size));
					}
				}
				finally {
					fs.Close();
					Console.WriteLine();
				}
			}
		}

		static void RecursiveUpload(FtpDirectory remote, DirectoryInfo local) {
			foreach (FileInfo f in local.GetFiles()) {
				if (!remote.FileExists(f.Name)) {
					UploadFile(remote, f);
				}
			}

			foreach (DirectoryInfo d in local.GetDirectories()) {
				if (!remote.DirectoryExists(d.Name)) {
					remote.CreateDirectory(d.Name);
				}

				RecursiveUpload(new FtpDirectory(remote.Client, string.Format("{0}/{1}", remote.FullName, d.Name)), d);
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
			using (FtpClient cl = new FtpClient("test", "test", "localhost")) {
				cl.IgnoreInvalidSslCertificates = true;

				RecursiveDownload(cl.CurrentDirectory, "c:\\temp");
				RecursiveDelete(cl.CurrentDirectory);
				RecursiveUpload(cl.CurrentDirectory, new DirectoryInfo("c:\\temp"));
			}
		}
	}
}

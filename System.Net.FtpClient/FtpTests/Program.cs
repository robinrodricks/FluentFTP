using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.FtpClient;

namespace FtpTests {
	class Program {
		static void ListDirectory(FtpDirectory dir) {
			Console.WriteLine(dir.FullName);

			foreach (FtpFile f in dir.Files) {
				Console.WriteLine(f.FullName);
			}

			foreach (FtpDirectory d in dir.Directories) {
				ListDirectory(d);
			}
		}

		static void Main(string[] args) {
			try {
				//Examples.Download.DownloadFile();

				using (FtpClient cmd = new FtpClient()) {
					cmd.Username = "test";
					cmd.Password = "test";
					cmd.Server = "127.0.0.1";
					cmd.IgnoreInvalidSslCertificates = true;
					cmd.UseSsl = false;
					cmd.DefaultDataMode = FtpDataMode.Active;

					ListDirectory(cmd.CurrentDirectory);
				}
			}
			catch (Exception ex) {
				Console.WriteLine(ex.ToString());
			}

			Console.WriteLine("TEST DONE");
			Console.ReadKey();
		}
	}
}
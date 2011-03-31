using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.FtpClient;

namespace ftpcli {
	class _main : IDisposable {
		FtpClient FtpClient = new FtpClient();

		string Prompt {
			get {
				if (this.FtpClient.Connected) {
					return string.Format("[{0}:{1}] ", this.FtpClient.Server,
						this.FtpClient.CurrentDirectory.FullName);
				}

				return "[disconnected] ";
			}
		}

		private void Download(string[] args) {
			foreach (string s in args) {
				try {
					byte[] buf = new byte[4096];
					long read = 0;
					long total = 0;
					long size = this.FtpClient.GetFileSize(s);

					using (FtpDataChannel dc = this.FtpClient.OpenRead(s)) {
						while ((read = dc.Read(buf, 0, buf.Length)) > 0) {
							total += read;

							Console.Write("\rDownloading {0} {1}/{2} {3:p}",
								s, total, size, ((double)total / (double)size));
						}

						Console.WriteLine();
					}
				}
				catch (Exception ex) {
					WriteError(ex);
				}
			}
		}

		private void List(string[] args) {
			try {
				if (args != null && args.Length > 0) {
					foreach (string path in args) {
						Console.WriteLine("{0}:", path);

						foreach (string s in this.FtpClient.GetRawListing(path, FtpListType.LIST)) {
							Console.WriteLine(s);
						}
					}
				}
				else {
					foreach (string s in this.FtpClient.GetRawListing(this.FtpClient.CurrentDirectory.FullName, FtpListType.LIST)) {
						Console.WriteLine(s);
					}
				}
			}
			catch (Exception ex) {
				WriteError(ex);
			}
		}

		private void Connect(string server) {
			string buf;

			try {
				Console.Write("Username: ");
				buf = Console.ReadLine();

				if (buf == null || buf.Length < 1) {
					Console.WriteLine("Aborting");
					return;
				}

				this.FtpClient.Username = buf;

				Console.Write("Password: ");
				buf = Console.ReadLine();

				if (buf == null || buf.Length < 1) {
					Console.WriteLine("Aborting");
					return;
				}

				this.FtpClient.Password = buf;
				this.FtpClient.Server = server;

				if (this.FtpClient.Connected) {
					this.FtpClient.Disconnect();
				}

				this.FtpClient.Connect();
			}
			catch (Exception ex) {
				WriteError(ex);
			}
		}

		public void Run() {
			string input;

			Console.Write(this.Prompt);
			while ((input = Console.ReadLine()) != null) {
				string cmd = input.Trim();
				string[] args = null;

				for (int i = 0; i < input.Length; i++) {
					if (input[i] == ' ') {
						cmd = input.Substring(0, i);
						args = input.Substring(i + 1).Split(' ');
						break;
					}
				}

				switch (cmd) {
					case "connect":
					case "open":
						if (args.Length > 0) {
							this.Connect(args[0]);
						}
						else {
							Console.Error.WriteLine("You must specify a server.");
						}
						break;
					case "ls":
					case "list":
					case "dir":
						List(args);
						break;
					case "cd":
						if (args != null) {
							try {
								this.FtpClient.SetWorkingDirectory(args[0]);
							}
							catch (Exception ex) {
								WriteError(ex);
							}
						}
						break;
					case "active":
						this.FtpClient.DefaultDataMode = FtpDataMode.Active;
						Console.WriteLine("Using active mode transfers...");
						break;
					case "passive":
						this.FtpClient.DefaultDataMode = FtpDataMode.Passive;
						Console.WriteLine("Using passive mode transfers....");
						break;
					case "get":
						this.Download(args);
						break;
					case "close":
					case "disconnect":
						try {
							this.FtpClient.Disconnect();
						}
						catch (Exception ex) {
							WriteError(ex);
						}
						break;
					case "exit":
					case "quit":
						return;
					default:
						Console.Error.WriteLine("Unknown input: {0}", input);
						break;
				}

				Console.Write(this.Prompt);
			}
		}

		public void Dispose() {
			this.FtpClient.Dispose();
		}

		public _main(string[] args) {
			this.FtpClient.IgnoreInvalidSslCertificates = true;
			this.FtpClient.ResponseReceived += new ResponseReceived(FtpClient_ResponseReceived);
			// init command line args if there are any
		}

		void FtpClient_ResponseReceived(string message) {
			Console.WriteLine(message);
		}

		static void WriteError(Exception ex) {
#if DEBUG
			Console.WriteLine(ex.ToString());
#else
			Console.WriteLine(ex.Message);
#endif
		}

		static int Main(string[] args) {
			_main m = new _main(args);

			try {
				m.Run();
				return 0;
			}
			catch (Exception ex) {
				WriteError(ex);

#if DEBUG
				Console.WriteLine("Press any key to close...");
				Console.ReadKey();
#endif

				return 1;
			}
		}
	}
}

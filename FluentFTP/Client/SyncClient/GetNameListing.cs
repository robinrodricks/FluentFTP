using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using FluentFTP.Exceptions;
using FluentFTP.Helpers;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using FluentFTP.Client.Modules;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Returns a file/directory listing using the NLST command.
		/// </summary>
		/// <returns>A string array of file and directory names if any were returned.</returns>
		public string[] GetNameListing() {
			return GetNameListing(null);
		}

		/// <summary>
		/// Returns a file/directory listing using the NLST command.
		/// </summary>
		/// <param name="path">The path of the directory to list</param>
		/// <returns>A string array of file and directory names if any were returned.</returns>
		public string[] GetNameListing(string path) {

			// FIX : #768 NullOrEmpty is valid, means "use working directory".
			if (!string.IsNullOrEmpty(path)) {
				path = path.GetFtpPath();
			}

			LogFunc(nameof(GetNameListing), new object[] { path });

			var listing = new List<string>();

			path = GetAbsolutePath(path);

			lock (m_lock) {

				// always get the file listing in binary to avoid character translation issues with ASCII.
				SetDataTypeNoLock(ListingDataType);

				// read in raw listing
				try {
					using (var stream = OpenDataStream("NLST " + path, 0)) {
						LogLine(FtpTraceLevel.Verbose, "+---------------------------------------+");
						string line;

						try {
							while ((line = stream.ReadLine(Encoding)) != null) {
								listing.Add(line);
								LogLine(FtpTraceLevel.Verbose, "Listing:  " + line);
							}
						}
						finally {
							stream.Close();
						}
						LogLine(FtpTraceLevel.Verbose, "+---------------------------------------+");
					}
				}
				catch (FtpMissingSocketException) {
					// Some FTP server does not send any response when listing an empty directory
					// and the connection fails because no communication socket is provided by the server
				}
				catch (FtpCommandException ftpEx) {
					// Some FTP servers throw 550 for empty folders. Absorb these.
					if (ftpEx.CompletionCode == null || !ftpEx.CompletionCode.StartsWith("550")) {
						throw ftpEx;
					}
				}
				catch (IOException) {
					// Some FTP servers forcibly close the connection, we absorb these errors
				}

			}
			return listing.ToArray();
		}

	}
}

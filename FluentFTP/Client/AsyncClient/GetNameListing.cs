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
using System.Security.Authentication;

namespace FluentFTP {
	public partial class AsyncFtpClient {

		/// <summary>
		/// Returns a file/directory listing using the NLST command asynchronously
		/// </summary>
		/// <param name="path">The path of the directory to list</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns>An array of file and directory names if any were returned.</returns>
		public async Task<string[]> GetNameListing(string path, CancellationToken token = default(CancellationToken)) {

			// FIX : #768 NullOrEmpty is valid, means "use working directory".
			if (!string.IsNullOrEmpty(path)) {
				path = path.GetFtpPath();
			}

			LogFunction(nameof(GetNameListing), new object[] { path });

			var listing = new List<string>();

			path = await GetAbsolutePathAsync(path, token);

			// always get the file listing in binary to avoid character translation issues with ASCII.
			await SetDataTypeNoLockAsync(Config.ListingDataType, token);

			// read in raw listing
			try {
				await using (FtpDataStream stream = await OpenDataStreamAsync("NLST " + path, 0, token)) {
					Log(FtpTraceLevel.Verbose, "+---------------------------------------+");
					string line;

					try {
						while ((line = await stream.ReadLineAsync(Encoding, token)) != null) {
							listing.Add(line);
							Log(FtpTraceLevel.Verbose, "Listing:  " + line);
						}
					}
					finally {
						await stream.CloseAsync(token);
					}
					Log(FtpTraceLevel.Verbose, "+---------------------------------------+");
				}
			}
			catch (AuthenticationException) {
				FtpReply reply = await ((IInternalFtpClient)this).GetReplyInternal(token, "NLST " + path, false, -1); // no exhaustNoop, but non-blocking
				if (!reply.Success) {
					throw new FtpCommandException(reply);
				}
				throw;
			}
			catch (FtpMissingSocketException) {
				// Some FTP server does not send any response when listing an empty directory
				// and the connection fails because no communication socket is provided by the server
			}
			catch (FtpCommandException ftpEx) {
				// Some FTP servers throw 450 or 550 for empty folders. Absorb these.
				if (ftpEx.CompletionCode == null ||
					(!ftpEx.CompletionCode.StartsWith("450") && !ftpEx.CompletionCode.StartsWith("550"))) {
					throw ftpEx;
				}
			}
			catch (IOException) {
				// Some FTP servers forcibly close the connection, we absorb these errors
			}

			return listing.ToArray();
		}

		/// <summary>
		/// Returns a file/directory listing using the NLST command asynchronously
		/// </summary>
		/// <returns>An array of file and directory names if any were returned.</returns>
		public Task<string[]> GetNameListing(CancellationToken token = default(CancellationToken)) {
			return GetNameListing(null, token);
		}

	}
}

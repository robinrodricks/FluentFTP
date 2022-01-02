using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Security.Authentication;
using FluentFTP;
using FluentFTP.Servers;
#if (CORE || NETFX)
using System.Threading;
#endif
#if ASYNC
using System.Threading.Tasks;
#endif

namespace FluentFTP.Servers.Handlers {

	/// <summary>
	/// Server-specific handling for IBMzOSFTP servers
	/// </summary>
	public class IBMzOSFtpServer : FtpBaseServer {

		/// <summary>
		/// Return the FtpServer enum value corresponding to your server, or Unknown if its a custom implementation.
		/// </summary>
		public override FtpServer ToEnum() {
			return FtpServer.IBMzOSFTP;
		}

		/// <summary>
		/// Return true if your server is detected by the given FTP server welcome message.
		/// </summary>
		public override bool DetectByWelcome(string message) {

			// Detect IBM z/OS server
			// Welcome message: "220-FTPD1 IBM FTP CS V2R3 at mysite.gov, 16:51:54 on 2019-12-12."
			if (message.Contains("IBM FTP CS")) {
				return true;
			}

			return false;
		}

		/// <summary>
		/// Return the default file listing parser to be used with your FTP server.
		/// </summary>
		public override FtpParser GetParser() {
			return FtpParser.IBMzOS;
		}

		/// <summary>
		/// Perform server-specific post-connection commands here.
		/// Return true if you executed a server-specific command.
		/// </summary>
		public override void AfterConnected(FtpClient client) {
			FtpReply reply;
			if (!(reply = client.Execute("SITE DATASETMODE")).Success) {
				throw new FtpCommandException(reply);
			}
			if (!(reply = client.Execute("SITE QUOTESOVERRIDE")).Success) {
				throw new FtpCommandException(reply);
			}
		}

#if ASYNC
		/// <summary>
		/// Perform server-specific post-connection commands here.
		/// Return true if you executed a server-specific command.
		/// </summary>
		public override async Task AfterConnectedAsync(FtpClient client, CancellationToken token) {
			FtpReply reply;
			if (!(reply = await client.ExecuteAsync("SITE DATASETMODE", token)).Success) {
				throw new FtpCommandException(reply);
			}
			if (!(reply = await client.ExecuteAsync("SITE QUOTESOVERRIDE", token)).Success) {
				throw new FtpCommandException(reply);
			}
		}
#endif

		#region Get z/OS File Size

		public override bool IsCustomFileSize() {
			return true;
		}

		/// <summary>
		/// Get z/OS file size
		/// </summary>
		/// <param name="path">The full path of th file whose size you want to retrieve</param>
		/// <remarks>
		/// Make sure you are in the right realm (z/OS or HFS) before doing this
		/// </remarks>
		/// <returns>The size of the file</returns>
		public override long GetFileSize(FtpClient client, string path) {

			// prevent automatic parser detection switching to unix on HFS paths
			client.ListingParser = FtpParser.IBMzOS;

			// get metadata of the file
			FtpListItem[] entries = client.GetListing(path);

			// no entries or more than one: path is NOT for a single dataset or file
			if (entries.Length != 1) { return -1; }

			// if the path is for a SINGLE dataset or file, there will be only one entry
			FtpListItem entry = entries[0];

			// z/OS list parser will have determined that size
			return entry.Size;
		}

#if ASYNC
		/// <summary>
		/// Get z/OS file size
		/// </summary>
		/// <param name="path">The full path of th file whose size you want to retrieve</param>
		/// <remarks>
		/// Make sure you are in the right realm (z/OS or HFS) before doing this
		/// </remarks>
		/// <returns>The size of the file</returns>
		public override async Task<long> GetFileSizeAsync(FtpClient client, string path, CancellationToken token) {

			// prevent automatic parser detection switching to unix on HFS paths
			client.ListingParser = FtpParser.IBMzOS;

			// get metadata of the file
			FtpListItem[] entries = await client.GetListingAsync(path, token);

			// no entries or more than one: path is NOT for a single dataset or file
			if (entries.Length != 1) return -1;

			// if the path is for a SINGLE dataset or file, there will be only one entry
			FtpListItem entry = entries[0];

			// z/OS list parser will have determined that size
			return entry.Size;
		}
#endif
		#endregion

		/// <summary>
		/// Check if the given path is a root directory on your FTP server.
		/// If you are unsure, return false.
		/// </summary>
		public override bool IsRoot(FtpClient client, string path) {

			// If it is not a "/" root, it could perhaps be a z/OS root (like 'SYS1.')
			// Note: If on z/OS you have somehow managed to CWD "over" th top, i.e.
			// PWD returns "''" - you would need to CWD to some HLQ that only you can
			// imagine. There is no way to list the available top level HLQs.
			if (path.Split('.').Length - 1 == 1) {
				return true;
			}
			return false;
		}
	}
}


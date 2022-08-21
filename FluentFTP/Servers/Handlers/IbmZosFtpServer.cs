using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Security.Authentication;
using FluentFTP;
using FluentFTP.Servers;
#if (CORE || NETFX)
using System.Threading;
using FluentFTP.Helpers;
#endif
#if ASYNC
using System.Threading.Tasks;
#endif

namespace FluentFTP.Servers.Handlers {

	/// <summary>
	/// Server-specific handling for IBMzOSFTP servers
	/// </summary>
	internal class IBMzOSFtpServer : FtpBaseServer {

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


		public override bool IsCustomFileSize() {
			return true;
		}

		/// <summary>
		/// Get z/OS file size
		/// </summary>
		/// <param name="path">The full path of the file whose size you want to retrieve</param>
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
		/// <param name="path">The full path of the file whose size you want to retrieve</param>
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

		/// <summary>
		/// Check if the given path is a root directory on your FTP server.
		/// If you are unsure, return false.
		/// </summary>
		public override bool IsRoot(FtpClient client, string path) {

			// Note: If on z/OS you have somehow managed to CWD "over" the top, i.e.
			// PWD returns "''", it is also root - you would need to CWD to some HLQ
			// that only you can imagine. There is no way to list the available top
			// level HLQs.

			// z/OS HFS root
			if (path == "/") { 
				return true;
			}

			// z/OS HFS some path
			if (path.StartsWith("/")) {
				return false;
			}

			// z/OS HLQ (like 'SYS1.' or '')
			if (path.Trim('\'').TrimEnd('.').Split('.').Length <= 1) { 
				return true;
			}

			// all others
			return false;
		}

		/// <summary>
		/// Skip reporting a parser error
		/// </summary>
		public override bool SkipParserErrorReport() {
			return true;
		}

		/// <summary>
		/// Always read to End of stream on a download
		/// </summary>
		public override bool AlwaysReadToEnd(string remotePath)	{
			return true;
		}

		public override bool IsCustomGetAbsolutePath() {
			return true;
		}

		/// <summary>
		/// Perform server-specific path modification here.
		/// Return the absolute path.
		/// </summary>
		public override string GetAbsolutePath(FtpClient client, string path) {

			if (path == null || path.Trim().Length == 0) {
				path = client.GetWorkingDirectory();
				return path;
			}

			if (path.StartsWith("/") || path.StartsWith("\'")) {
				return path;
			}

			var pwd = client.GetWorkingDirectory();

			if (pwd.StartsWith("/")) {
				if (pwd.Equals("/")) {
					path = (pwd + path).GetFtpPath();
				}
				else {
					path = (pwd + "/" + path).GetFtpPath();
				}
				return path;
			}

			if (pwd.StartsWith("\'")) {
				if (pwd.Equals("`\'\'")) {
					path = ("\'" + path + "\'").GetFtpPath();
				}
				else {
					path = (pwd.TrimEnd('\'') + path + "\'").GetFtpPath();
				}
				return path;
			}

			return path;
		}

#if ASYNC
		/// <summary>
		/// Perform server-specific path modification here.
		/// Return the absolute path.
		/// </summary>
		public override async Task<string> GetAbsolutePathAsync(FtpClient client, string path, CancellationToken token)	{

			if (path == null || path.Trim().Length == 0) {
				path = await client.GetWorkingDirectoryAsync(token);
				return path;
			}

			if (path.StartsWith("/") || path.StartsWith("\'")) {
				return path;
			}

			var pwd = await client.GetWorkingDirectoryAsync(token);

			if (pwd.StartsWith("/")) {
				if (pwd.Equals("/")) { 
					path = (pwd + path).GetFtpPath();
				}
				else {
					path = (pwd + "/" + path).GetFtpPath();
				}
				return path;
			}

			if (pwd.StartsWith("\'")) {
				if (pwd.Equals("`\'\'")) {
					path = ("\'" + path + "\'").GetFtpPath();
				}
				else {
					path = (pwd.TrimEnd('\'') + path + "\'").GetFtpPath();
				}
				return path;
			}

			return path;
		}
#endif
	}
}


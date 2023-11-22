using System;
using FluentFTP.Helpers;
using System.Threading;
using FluentFTP.Client.Modules;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class AsyncFtpClient {

		/// <summary>
		/// Checks if a file exists on the server asynchronously.
		/// </summary>
		/// <param name="path">The full or relative path to the file</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns>True if the file exists, false otherwise</returns>
		public async Task<bool> FileExists(string path, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(path));
			}

			path = path.GetFtpPath();

			LogFunction(nameof(FileExists), new object[] { path });

			path = await GetAbsolutePathAsync(path, token);

			// since FTP does not include a specific command to check if a file exists
			// here we check if file exists by attempting to get its filesize (SIZE)
			if (HasFeature(FtpCapability.SIZE) && (ServerHandler == null || (ServerHandler != null && !ServerHandler.DontUseSizeEvenIfCapable(path)))) {
				// Fix #328: get filesize in ASCII or Binary mode as required by server
				FtpSizeReply sizeReply = new FtpSizeReply();
				await GetFileSizeInternal(path, -1, token, sizeReply);

				// handle known errors to the SIZE command
				var sizeKnownError = CheckFileExistsBySize(sizeReply);
				if (sizeKnownError.HasValue) {
					return sizeKnownError.Value;
				}
			}

			// check if file exists by attempting to get its date modified (MDTM)
			if (HasFeature(FtpCapability.MDTM) && (ServerHandler == null || (ServerHandler != null && !ServerHandler.DontUseMdtmEvenIfCapable(path)))) {
				FtpReply reply = await Execute("MDTM " + path, token);
				var ch = reply.Code[0];
				if (ch == '2') {
					return true;
				}

				if (ch == '5' && reply.Message.ContainsAnyCI(ServerStringModule.fileNotFound)) {
					return false;
				}
			}

			// check if file exists by getting a name listing (NLST)

			bool? handledByCustom = null;

			if (ServerHandler != null && ServerHandler.IsCustomFileExists()) {
				handledByCustom = await ServerHandler.FileExistsAsync(this, path, token);
			}

			if (handledByCustom != null) {
				return (bool)handledByCustom;
			}
			else {
				var fileList = await GetNameListing(path.GetFtpDirectoryName(), token);
				return FileListings.FileExistsInNameListing(fileList, path);
			}
		}

	}
}
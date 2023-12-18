using System;
using FluentFTP.Helpers;
using System.Threading;
using FluentFTP.Client.Modules;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Checks if a file exists on the server.
		/// </summary>
		/// <param name="path">The full or relative path to the file</param>
		/// <returns>True if the file exists</returns>
		public bool FileExists(string path) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(path));
			}

			path = path.GetFtpPath();

			LogFunction(nameof(FileExists), new object[] { path });

			path = GetAbsolutePath(path);

			// since FTP does not include a specific command to check if a file exists
			// here we check if file exists by attempting to get its filesize (SIZE)
			if (HasFeature(FtpCapability.SIZE) && (ServerHandler == null || (ServerHandler != null && !ServerHandler.DontUseSizeEvenIfCapable(path)))) {
				// Fix #328: get filesize in ASCII or Binary mode as required by server
				var sizeReply = new FtpSizeReply();
				GetFileSizeInternal(path, sizeReply, -1);

				// handle known errors to the SIZE command
				var sizeKnownError = CheckFileExistsBySize(sizeReply);
				if (sizeKnownError.HasValue) {
					return sizeKnownError.Value;
				}
			}

			// check if file exists by attempting to get its date modified (MDTM)
			if (HasFeature(FtpCapability.MDTM) && (ServerHandler == null || (ServerHandler != null && !ServerHandler.DontUseMdtmEvenIfCapable(path)))) {
				var reply = Execute("MDTM " + path);
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
				handledByCustom = ServerHandler.FileExists(this, path);
			}

			if (handledByCustom != null) {
				return (bool)handledByCustom;
			}
			else {
				var fileList = GetNameListing(path.GetFtpDirectoryName());
				return FileListings.FileExistsInNameListing(fileList, path);
			}
		}

	}
}
using FluentFTP.Helpers;

namespace FluentFTP.Client.BaseClient {
	public partial class BaseFtpClient {

		/// <summary>
		/// Check if the file is cleared to be uploaded, taking its existence/filesize and existsMode options into account.
		/// </summary>
		protected bool CanUploadFile(FtpResult result, FtpListItem[] remoteListing, FtpRemoteExists existsMode, out FtpRemoteExists existsModeToUse) {

			// check if the file already exists on the server
			existsModeToUse = existsMode;
			var fileExists = FileListings.FileExistsInListing(remoteListing, result.RemotePath);

			// if we want to skip uploaded files and the file already exists, mark its skipped
			if (existsMode == FtpRemoteExists.Skip && fileExists) {

				LogWithPrefix(FtpTraceLevel.Info, "Skipped file that already exists: " + result.LocalPath);

				result.IsSuccess = true;
				result.IsSkipped = true;
				return false;
			}

			// in any mode if the file does not exist, mark that exists check is not required
			if (!fileExists) {
				existsModeToUse = existsMode == FtpRemoteExists.Resume ? FtpRemoteExists.ResumeNoCheck : FtpRemoteExists.NoCheck;
			}
			return true;
		}
	}
}

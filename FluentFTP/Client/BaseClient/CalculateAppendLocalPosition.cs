using FluentFTP.Helpers;

namespace FluentFTP.Client.BaseClient {

	/// <summary>
	/// The base object for FtpClient, AsyncFtpClient
	/// </summary>
	public partial class BaseFtpClient {

		/// <summary>
		/// Calculate the position from which to append
		/// </summary>
		/// <param name="remotePath"></param>
		/// <param name="existsMode"></param>
		/// <param name="remotePosition"></param>
		/// <returns></returns>
		protected long CalculateAppendLocalPosition(string remotePath, FtpRemoteExists existsMode, long remotePosition) {

			long localPosition = 0;

			// resume - start the local file from the same position as the remote file
			if (existsMode == FtpRemoteExists.Resume || existsMode == FtpRemoteExists.ResumeNoCheck) {
				localPosition = remotePosition;
			}

			// append to end - start from the beginning of the local file
			else if (existsMode == FtpRemoteExists.AddToEnd || existsMode == FtpRemoteExists.AddToEndNoCheck) {
				localPosition = 0;
			}

			return localPosition;
		}
	}
}

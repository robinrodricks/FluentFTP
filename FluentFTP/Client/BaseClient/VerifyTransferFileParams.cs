using System;
using FluentFTP.Exceptions;
using FluentFTP.Helpers;

namespace FluentFTP.Client.BaseClient {
	public partial class BaseFtpClient {

		/// <summary>
		/// Verify that the client is usable
		/// </summary>
		/// <param name="sourcePath"></param>
		/// <param name="remoteClient"></param>
		/// <param name="remotePath"></param>
		/// <param name="existsMode"></param>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="FtpException"></exception>
		protected void VerifyTransferFileParams(string sourcePath, BaseFtpClient remoteClient, string remotePath, FtpRemoteExists existsMode) {
			if (remoteClient is null) {
				throw new ArgumentNullException(nameof(remoteClient), "Destination FXP FtpClient cannot be null!");
			}

			if (sourcePath.IsBlank()) {
				throw new ArgumentNullException(nameof(sourcePath), "FtpListItem must be specified!");
			}

			if (remotePath.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(remotePath));
			}

			if (!remoteClient.IsConnected) {
				throw new FtpException("The connection must be open before a transfer between servers can be initiated");
			}

			if (!this.IsConnected) {
				throw new FtpException("The source FXP FtpClient must be open and connected before a transfer between servers can be initiated");
			}

			if (existsMode is FtpRemoteExists.AddToEnd or FtpRemoteExists.AddToEndNoCheck) {
				throw new ArgumentException("FXP file transfer does not currently support AddToEnd or AddToEndNoCheck modes. Use another value for existsMode.", nameof(existsMode));
			}
		}



	}
}
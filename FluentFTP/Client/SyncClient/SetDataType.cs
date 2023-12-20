using FluentFTP.Exceptions;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class FtpClient {

		/// Sets the data type of information sent over the data stream
		/// <exception cref="FtpCommandException">Thrown when a FTP Command error condition occurs.</exception>
		/// <exception cref="FtpException">Thrown when a FTP error condition occurs.</exception>
		/// <param name="type">ASCII/Binary.</param>
		/// <remarks>This method doesn't do any locking to prevent recursive lock scenarios.  Callers must do their own locking.</remarks>
		protected void SetDataType(FtpDataType type) {
			// FIX : #291 only change the data type if different
			if (Status.CurrentDataType != type) {
				FtpReply reply;
				switch (type) {
					case FtpDataType.ASCII:
						if (!(reply = Execute("TYPE A")).Success) {
							throw new FtpCommandException(reply);
						}

						break;

					case FtpDataType.Binary:
						if (!(reply = Execute("TYPE I")).Success) {
							throw new FtpCommandException(reply);
						}

						break;

					default:
						throw new FtpException("Unsupported data type: " + type.ToString());
				}

				Status.CurrentDataType = type;
			}
		}

	}
}
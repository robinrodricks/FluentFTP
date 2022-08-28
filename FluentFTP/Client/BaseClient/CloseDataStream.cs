using System;
namespace FluentFTP.Client.BaseClient {

	public partial class BaseFtpClient {

		/// <summary>
		/// Disconnects a data stream
		/// </summary>
		/// <param name="stream">The data stream to close</param>
		FtpReply IInternalFtpClient.CloseDataStreamInternal(FtpDataStream stream) {
			LogFunc("CloseDataStream");

			var reply = new FtpReply();

			if (stream == null) {
				throw new ArgumentException("The data stream parameter was null");
			}

			lock (m_lock) {
				try {
					if (IsConnected) {
						// if the command that required the data connection was
						// not successful then there will be no reply from
						// the server, however if the command was successful
						// the server will send a reply when the data connection
						// is closed.
						if (stream.CommandStatus.Type == FtpResponseType.PositivePreliminary) {
							if (!(reply = GetReplyInternal()).Success) {
								throw new FtpCommandException(reply);
							}
						}
					}
				}
				finally {
					// if this is a clone of the original control
					// connection we should Dispose()
					if (IsClone) {
						((IInternalFtpClient)this).DisconnectInternal();
						Dispose();
					}
				}

			}

			return reply;
		}


	}
}

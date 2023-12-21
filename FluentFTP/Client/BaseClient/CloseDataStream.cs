using FluentFTP.Exceptions;
using System;
namespace FluentFTP.Client.BaseClient {

	public partial class BaseFtpClient {

		/// <summary>
		/// Disconnects a data stream
		/// </summary>
		/// <param name="stream">The data stream to close</param>
		FtpReply IInternalFtpClient.CloseDataStreamInternal(FtpDataStream stream) {
			LogFunction("CloseDataStream");

			var reply = new FtpReply();

			if (stream == null) {
				throw new ArgumentException("The data stream parameter was null");
			}

			try {
				if (IsConnected) {
					// if the command that required the data connection was
					// not successful then there will be no reply from
					// the server, however if the command was successful
					// the server will send a reply when the data connection
					// is closed.
					if (stream.CommandStatus.Type == FtpResponseType.PositivePreliminary) {
						if (!(reply = ((IInternalFtpClient)this).GetReplyInternal(LastCommandExecuted)).Success) {
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

			return reply;
		}


	}
}

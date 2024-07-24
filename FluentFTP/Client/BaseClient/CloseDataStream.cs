using FluentFTP.Exceptions;

using System;
using System.Threading;
using System.Threading.Tasks;

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
					// Because the data connection was closed, if the command that required the data
					// connection was not successful then there will be no reply from the server,
					// however if the command was successful the server will send a reply .
					if (stream.CommandStatus.Type == FtpResponseType.PositivePreliminary) {
						if (!(reply = ((IInternalFtpClient)this).GetReplyInternal(LastCommandExecuted)).Success) {
							throw new FtpCommandException(reply);
						}
					}
				}
			}
			finally {
				// if this is a clone of the original control connection we should Dispose() the entire client
				if (IsClone) {
					((IInternalFtpClient)this).DisconnectInternal();
					((IInternalFtpClient)this).DisposeInternal();
				}
			}

			return reply;
		}

		/// <summary>
		/// Disconnects a data stream
		/// </summary>
		/// <param name="stream">The data stream to close</param>
		async Task<FtpReply> IInternalFtpClient.CloseDataStreamInternal(FtpDataStream stream, CancellationToken token) {
			LogFunction("CloseDataStream");

			var reply = new FtpReply();

			if (stream == null) {
				throw new ArgumentException("The data stream parameter was null");
			}

			try {
				if (IsConnected) {
					// Because the data connection was closed, if the command that required the data
					// connection was not successful then there will be no reply from the server,
					// however if the command was successful the server will send a reply .
					if (stream.CommandStatus.Type == FtpResponseType.PositivePreliminary) {
						if (!(reply = await ((IInternalFtpClient)this).GetReplyInternal(token, LastCommandExecuted)).Success) {
							throw new FtpCommandException(reply);
						}
					}
				}
			}
			finally {
				// if this is a clone of the original control connection we should Dispose() the entire client
				if (IsClone) {
					await ((IInternalFtpClient)this).DisconnectInternal(token);
					await ((IInternalFtpClient)this).DisposeInternal(token);
				}
			}

			return reply;
		}

	}
}

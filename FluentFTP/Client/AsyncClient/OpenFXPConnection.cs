using FluentFTP.Exceptions;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class AsyncFtpClient {

		/// <summary>
		/// Opens a FXP PASV connection between the source FTP Server and the destination FTP Server
		/// </summary>
		/// <param name="remoteClient">Valid FTP connection to the destination FTP Server</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <param name="progress">Provide an implementation of IProgress to track download progress.</param>
		/// <returns>A data stream ready to be used</returns>
		protected async Task<FtpFxpSessionAsync> OpenPassiveFXPConnectionAsync(AsyncFtpClient remoteClient, bool progress, CancellationToken token) {
			FtpReply reply, reply2;
			Match m;
			AsyncFtpClient sourceClient = this;
			AsyncFtpClient destinationClient = remoteClient;
			AsyncFtpClient progressClient = null;

			// create a new connection to the target FTP server to track progress
			// if progress tracking is enabled during this FXP transfer
			if (progress) {
				progressClient = (AsyncFtpClient)remoteClient.Clone();
				progressClient.Status.AutoDispose = true;
				progressClient.Status.CopyFrom(remoteClient.Status);
				await progressClient.Connect(token);
				await progressClient.SetWorkingDirectory(await remoteClient.GetWorkingDirectory(token), token);
			}

			await sourceClient.SetDataTypeAsync(sourceClient.Config.FXPDataType, token);
			await destinationClient.SetDataTypeAsync(destinationClient.Config.FXPDataType, token);

			// send PASV/CPSV commands to destination FTP server to get passive port to be used from source FTP server
			// first try with PASV - commonly supported by all servers
			if (!(reply = await destinationClient.Execute("PASV", token)).Success) {

				// then try with CPSV - known to be supported by glFTPd server
				// FIXES #666 - glFTPd server - 435 Failed TLS negotiation on data channel
				if (!(reply2 = await destinationClient.Execute("CPSV", token)).Success) {
					throw new FtpCommandException(reply);
				}
				else {

					// use the CPSV response and extract the port from it
					reply = reply2;
				}
			}

			// extract port from response
			m = Regex.Match(reply.Message, @"(?<quad1>[0-9]+)," + @"(?<quad2>[0-9]+)," + @"(?<quad3>[0-9]+)," + @"(?<quad4>[0-9]+)," + @"(?<port1>[0-9]+)," + @"(?<port2>[0-9]+)");

			if (!m.Success || m.Groups.Count != 7) {
				throw new FtpException("Malformed PASV response: " + reply.Message);
			}

			// Instruct source server to open a connection to the destination Server

			if (!(reply = await sourceClient.Execute($"PORT {m.Value}", token)).Success) {
				throw new FtpCommandException(reply);
			}

			// the FXP session stores the active connections used for this FXP transfer
			return new FtpFxpSessionAsync {
				SourceServer = sourceClient,
				TargetServer = destinationClient,
				ProgressServer = progressClient,
			};
		}

	}
}

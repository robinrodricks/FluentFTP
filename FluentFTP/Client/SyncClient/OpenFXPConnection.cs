using FluentFTP.Exceptions;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Opens a FXP PASV connection between the source FTP Server and the destination FTP Server
		/// </summary>
		/// <param name="remoteClient">FtpClient instance of the destination FTP Server</param>
		/// <param name="progress">Provide an implementation of IProgress to track download progress.</param>
		/// <returns>A data stream ready to be used</returns>
		protected FtpFxpSession OpenPassiveFXPConnection(FtpClient remoteClient, bool progress) {
			FtpReply reply, reply2;
			Match m;
			FtpClient sourceClient = this;
			FtpClient destinationClient = remoteClient;
			FtpClient progressClient = null;

			// create a new connection to the target FTP server to track progress
			// if progress tracking is enabled during this FXP transfer
			if (progress) {
				progressClient = (FtpClient)remoteClient.Clone();
				progressClient.Status.AutoDispose = true;
				progressClient.Status.CopyFrom(remoteClient.Status);
				progressClient.Connect();
				progressClient.SetWorkingDirectory(remoteClient.GetWorkingDirectory());
			}

			sourceClient.SetDataType(sourceClient.Config.FXPDataType);
			destinationClient.SetDataType(destinationClient.Config.FXPDataType);

			// send PASV/CPSV commands to destination FTP server to get passive port to be used from source FTP server
			// first try with PASV - commonly supported by all servers
			if (!(reply = destinationClient.Execute("PASV")).Success) {

				// then try with CPSV - known to be supported by glFTPd server
				// FIXES #666 - glFTPd server - 435 Failed TLS negotiation on data channel
				if (!(reply2 = destinationClient.Execute("CPSV")).Success) {
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

			if (!(reply = sourceClient.Execute($"PORT {m.Value}")).Success) {
				throw new FtpCommandException(reply);
			}

			// the FXP session stores the active connections used for this FXP transfer
			return new FtpFxpSession {
				SourceServer = sourceClient,
				TargetServer = destinationClient,
				ProgressServer = progressClient,
			};
		}

	}
}

using System;
using System.IO;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Linq;
using System.Net;
using FluentFTP.Helpers;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class AsyncFtpClient {

#if ASYNC
		// TODO: add example
		/// <summary>
		/// Retrieves a reply from the server. Do not execute this method
		/// unless you are sure that a reply has been sent, i.e., you
		/// executed a command. Doing so will cause the code to hang
		/// indefinitely waiting for a server reply that is never coming.
		/// </summary>
		/// <returns>FtpReply representing the response from the server</returns>
		public async Task<FtpReply> GetReply(CancellationToken token) {
			var reply = new FtpReply();
			string buf;

			if (!IsConnected) {
				throw new InvalidOperationException("No connection to the server has been established.");
			}

			m_stream.ReadTimeout = m_readTimeout;
			while ((buf = await m_stream.ReadLineAsync(Encoding, token)) != null) {
				if (DecodeStringToReply(buf, ref reply)) {
					break;
				}
				reply.InfoMessages += buf + "\n";
			}

			reply = ProcessGetReply(reply);

			return reply;
		}
#endif

	}
}
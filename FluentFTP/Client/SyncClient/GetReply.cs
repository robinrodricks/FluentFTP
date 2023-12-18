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
	public partial class FtpClient {

		/// <summary>
		/// Retrieves a reply from the server.
		/// Support "normal" mode waiting for a command reply, subject to timeout exception
		/// and "exhaustNoop" mode, which waits for 10 seconds to collect out of band NOOP responses
		/// </summary>
		/// <returns>FtpReply representing the response from the server</returns>
		public FtpReply GetReply() {
			FtpReply ftpReply = new FtpReply();

			m_sema.Wait();
			try {
				 ftpReply = GetReplyInternal();
			}
			finally {
				m_sema.Release();
			}

			return ftpReply;
		}

	}
}
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
		/// <param name="exhaustNoop">Set to true to select the NOOP devouring mode</param>
		/// <param name="command">We are waiting for the response to which command?</param>
		/// <returns>FtpReply representing the response from the server</returns>
		public FtpReply GetReply(bool exhaustNoop = false, string command = null) {
			return GetReplyInternal(exhaustNoop, command);
		}

	}
}
using System.Threading.Tasks;
using System.Threading;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Retrieves a reply from the server.
		/// Support "normal" mode waiting for a command reply, subject to timeout exception
		/// and "exhaustNoop" mode, which waits for 10 seconds to collect out of band NOOP responses
		/// </summary>
		/// <returns>FtpReply representing the response from the server</returns>
		public FtpReply GetReply() {
			return ((IInternalFtpClient)this).GetReplyInternal(null, false, 0);
		}
	}
}
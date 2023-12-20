using FluentFTP.Exceptions;

using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class AsyncFtpClient {

		/// <summary>
		/// Disables UTF8 support and changes the Encoding property
		/// back to ASCII. If the server returns an error when trying
		/// to turn UTF8 off a FtpCommandException will be thrown.
		/// </summary>
		public async Task DisableUTF8(CancellationToken token = default(CancellationToken)) {
			FtpReply reply;

			reply = await Execute("OPTS UTF8 OFF", token);

			if (!reply.Success) {
				throw new FtpCommandException(reply);
			}

			m_textEncoding = Encoding.ASCII;
			m_textEncodingAutoUTF = false;
		}
	}
}

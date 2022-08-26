using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP.Client.BaseClient {
	public partial class BaseFtpClient {


		/// <summary>
		/// Data shouldn't be on the socket, if it is it probably means we've been disconnected.
		/// Read and discard whatever is there and optionally close the connection.
		/// Returns the stale data as text, if any, or null if none was found.
		/// </summary>
		/// <param name="closeStream">close the connection?</param>
		/// <param name="evenEncrypted">even read encrypted data?</param>
		/// <param name="traceData">trace data to logs?</param>
		protected string ReadStaleData(bool closeStream, bool evenEncrypted, bool traceData) {
			string staleData = null;
			if (m_stream != null && m_stream.SocketDataAvailable > 0) {
				if (traceData) {
					LogStatus(FtpTraceLevel.Info, "There is stale data on the socket, maybe our connection timed out or you did not call GetReply(). Re-connecting...");
				}

				if (m_stream.IsConnected && (!m_stream.IsEncrypted || evenEncrypted)) {
					var buf = new byte[m_stream.SocketDataAvailable];
					m_stream.RawSocketRead(buf);
					staleData = Encoding.GetString(buf).TrimEnd('\r', '\n');
					if (traceData) {
						LogStatus(FtpTraceLevel.Verbose, "The stale data was: " + staleData);
					}
					if (string.IsNullOrEmpty(staleData)) {
						closeStream = false;
					}
				}

				if (closeStream) {
					m_stream.Close();
				}
			}
			return staleData;
		}

#if ASYNC
		/// <summary>
		/// Data shouldn't be on the socket, if it is it probably means we've been disconnected.
		/// Read and discard whatever is there and optionally close the connection.
		/// Returns the stale data as text, if any, or null if none was found.
		/// </summary>
		/// <param name="closeStream">close the connection?</param>
		/// <param name="evenEncrypted">even read encrypted data?</param>
		/// <param name="traceData">trace data to logs?</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		protected async Task<string> ReadStaleDataAsync(bool closeStream, bool evenEncrypted, bool traceData, CancellationToken token) {
			string staleData = null;
			if (m_stream != null && m_stream.SocketDataAvailable > 0) {
				if (traceData) {
					LogStatus(FtpTraceLevel.Info, "There is stale data on the socket, maybe our connection timed out or you did not call GetReply(). Re-connecting...");
				}

				if (m_stream.IsConnected && (!m_stream.IsEncrypted || evenEncrypted)) {
					var buf = new byte[m_stream.SocketDataAvailable];
					await m_stream.RawSocketReadAsync(buf, token);
					staleData = Encoding.GetString(buf).TrimEnd('\r', '\n');
					if (traceData) {
						LogStatus(FtpTraceLevel.Verbose, "The stale data was: " + staleData);
					}
				}

				if (closeStream) {
					m_stream.Close();
				}
			}
			return staleData;
		}
#endif

	}
}

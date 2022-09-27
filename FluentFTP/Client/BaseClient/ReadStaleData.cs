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
		/// <param name="logData">copy stale data information to logs?</param>
		/// <param name="logFrom">for the log information</param>
		protected string ReadStaleData(bool closeStream, bool logData, string logFrom) {
			string staleData = null;

			if (logData) {
				LogWithPrefix(FtpTraceLevel.Verbose, "Checking for stale data: " + logFrom);
			}

			if (m_stream != null) {

				while (m_stream.SocketDataAvailable > 0) {
					if (logData) {
						LogWithPrefix(FtpTraceLevel.Info, "Socket has stale data");
					}
					byte[] buf = new byte[m_stream.SocketDataAvailable];
					if (m_stream.IsEncrypted) {
						m_stream.Read(buf, 0, buf.Length);
					}
					else {
						m_stream.RawSocketRead(buf);
					}
					staleData = Encoding.GetString(buf).TrimEnd('\0', '\r', '\n');
					if (logData) {
						LogWithPrefix(FtpTraceLevel.Verbose, "The stale data was: " + staleData);
					}
				}

				if (string.IsNullOrEmpty(staleData)) {
					closeStream = false;
				}

				if (closeStream) {
					LogWithPrefix(FtpTraceLevel.Info, "Closing stream because of stale data");
					m_stream.Close();
				}

			}

			return staleData;
		}

		/// <summary>
		/// Data shouldn't be on the socket, if it is it probably means we've been disconnected.
		/// Read and discard whatever is there and optionally close the connection.
		/// Returns the stale data as text, if any, or null if none was found.
		/// </summary>
		/// <param name="closeStream">close the connection?</param>
		/// <param name="logData">copy stale data information to logs?</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		protected async Task<string> ReadStaleDataAsync(bool closeStream, bool traceData, string logFrom, CancellationToken token) {
			string staleData = null;

			if (traceData) {
				LogWithPrefix(FtpTraceLevel.Verbose, "Checking for stale data: " + logFrom);
			}

			if (m_stream != null) {

				while (m_stream.SocketDataAvailable > 0) {
					if (traceData) {
						LogWithPrefix(FtpTraceLevel.Info, "Socket has stale data");
					}
					byte[] buf = new byte[m_stream.SocketDataAvailable];
					if (m_stream.IsEncrypted) {
						await m_stream.ReadAsync(buf, 0, buf.Length, token);
					}
					else {
						await m_stream.RawSocketReadAsync(buf, token);
					}
					staleData = Encoding.GetString(buf).TrimEnd('\0', '\r', '\n');
					if (traceData) {
						LogWithPrefix(FtpTraceLevel.Verbose, "The stale data was: " + staleData);
					}
				}

				if (string.IsNullOrEmpty(staleData)) {
					closeStream = false;
				}

				if (closeStream) {
					LogWithPrefix(FtpTraceLevel.Info, "Closing stream because of stale data");
					m_stream.Close();
				}

			}

			return staleData;
		}

	}
}

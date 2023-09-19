using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP.Client.BaseClient {
	public partial class BaseFtpClient {


		/// <summary>
		/// Data shouldn't be on the socket, if it is it probably means we've been disconnected.
		/// Read and discard whatever is there.
		/// Returns the stale data as text, if any, or null if none was found.
		/// </summary>
		/// <param name="logData">copy stale data information to logs?</param>
		/// <param name="logFrom">for the log information</param>
		protected string ReadStaleData(bool logData, string logFrom) {
			string staleData = null;

			if (m_stream != null) {

				if (m_stream.SocketDataAvailable > 0 && logData) {
					LogWithPrefix(FtpTraceLevel.Info, "Control connection has stale data - " + logFrom);
				}

				while (m_stream.SocketDataAvailable > 0) {
					byte[] buf = new byte[m_stream.SocketDataAvailable];
					if (m_stream.IsEncrypted) {
						m_stream.Read(buf, 0, buf.Length);
					}
					else {
						m_stream.RawSocketRead(buf);
					}
					staleData += Encoding.GetString(buf).TrimEnd('\0', '\r', '\n') + Environment.NewLine;
				}

				if (!string.IsNullOrEmpty(staleData) && logData) {
					LogWithPrefix(FtpTraceLevel.Verbose, "The stale data was: ");
					string[] staleLines = Regex.Split(staleData, Environment.NewLine);
					foreach (string staleLine in staleLines) {
						if (!string.IsNullOrWhiteSpace(staleLine)) {
							Log(FtpTraceLevel.Verbose, "Stale:    " + staleLine);
						}
					}
				}

				if (Status.IgnoreStaleData) {
					Status.IgnoreStaleData = false;
					LogWithPrefix(FtpTraceLevel.Verbose, "Stale data ignored");
					return null;
				}

			}

			return staleData;
		}

		/// <summary>
		/// Data shouldn't be on the socket, if it is it probably means we've been disconnected.
		/// Read and discard whatever is there.
		/// Returns the stale data as text, if any, or null if none was found.
		/// </summary>
		/// <param name="logData">copy stale data information to logs?</param>
		/// <param name="logFrom">called from where (text)</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		protected async Task<string> ReadStaleDataAsync(bool logData, string logFrom, CancellationToken token) {
			string staleData = null;

			if (m_stream != null) {

				if (m_stream.SocketDataAvailable > 0 && logData) {
					LogWithPrefix(FtpTraceLevel.Info, "Socket has stale data - " + logFrom);
				}

				while (m_stream.SocketDataAvailable > 0) {
					byte[] buf = new byte[m_stream.SocketDataAvailable];
					if (m_stream.IsEncrypted) {
						await m_stream.ReadAsync(buf, 0, buf.Length, token);
					}
					else {
						await m_stream.RawSocketReadAsync(buf, token);
					}
					staleData += Encoding.GetString(buf).TrimEnd('\0', '\r', '\n') + Environment.NewLine;
				}

				if (!string.IsNullOrEmpty(staleData) && logData) {
					LogWithPrefix(FtpTraceLevel.Verbose, "The stale data was: ");
					string[] staleLines = Regex.Split(staleData, Environment.NewLine);
					foreach (string staleLine in staleLines) {
						if (!string.IsNullOrWhiteSpace(staleLine)) {
							Log(FtpTraceLevel.Verbose, "Stale:    " + staleLine);
						}
					}
				}

			}

			if (Status.IgnoreStaleData) {
				Status.IgnoreStaleData = false;
				LogWithPrefix(FtpTraceLevel.Verbose, "Stale data ignored");
				return null;
			}

			return staleData;
		}

	}
}

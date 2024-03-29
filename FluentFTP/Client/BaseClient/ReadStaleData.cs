using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP.Client.BaseClient {
	public partial class BaseFtpClient {


		/// <summary>
		/// Data shouldn't be on the socket, if it is it probably means we've been disconnected.
		/// Read and discard whatever is there.
		/// Returns the stale data as text or string.empty, if any, or null if none was found.
		/// </summary>
		/// <param name="logFrom">for the log information</param>
		protected string ReadStaleData(string logFrom) {
			string staleData = null;
			int staleBytes = 0;

			if (m_stream != null) {

				if (Status.IgnoreStaleData) {
					Stopwatch sw = new Stopwatch();
					sw.Start();

					LogWithPrefix(FtpTraceLevel.Verbose, "Stale data wait - " + logFrom);

					do {
						if (m_stream.SocketDataAvailable > 0 || !m_stream.IsConnected) {
							break;
						}
					} while (true);

					if (m_stream.IsConnected) {
						LogWithPrefix(FtpTraceLevel.Verbose, "Stale data took " + sw.ElapsedMilliseconds + "(ms) to arrive");
					}
				}

				if (m_stream.SocketDataAvailable > 0) {
					LogWithPrefix(FtpTraceLevel.Info, "Control connection has stale data(" + m_stream.SocketDataAvailable + ") - " + logFrom);
				}

				while (m_stream.SocketDataAvailable > 0) {
					int nRcvBytes;
					byte[] buf = new byte[m_stream.SocketDataAvailable];
					if (m_stream.IsEncrypted) {
						nRcvBytes = m_stream.Read(buf, 0, buf.Length);
					}
					else {
						nRcvBytes = m_stream.RawSocketRead(buf);
					}
					// Even though SocketDataAvailable > 0, this is possible: nRcvBytes = 0
					// Prevent endless loop
					if (nRcvBytes <= 0) {
						if (string.IsNullOrEmpty(staleData)) {
							LogWithPrefix(FtpTraceLevel.Verbose, "Unable to retrieve stale data");
						}
						staleData = string.Empty; // Not NULL, i.e. we had stale data but it was empty
						break;
					}
					staleBytes += nRcvBytes;
					staleData += Encoding.GetString(buf).TrimEnd('\0', '\r', '\n') + Environment.NewLine;
				}

				if (!string.IsNullOrEmpty(staleData)) {
					LogWithPrefix(FtpTraceLevel.Verbose, "The stale data was (length = " + staleBytes + "): ");
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
				if (staleData != null) {
					LogWithPrefix(FtpTraceLevel.Verbose, "Stale data ignored");
					staleData = null;
				}
			}

			return staleData;
		}

		/// <summary>
		/// Data shouldn't be on the socket, if it is it probably means we've been disconnected.
		/// Read and discard whatever is there.
		/// Returns the stale data as text or string.empty, if any, or null if none was found.
		/// </summary>
		/// <param name="logFrom">called from where (text)</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		protected async Task<string> ReadStaleDataAsync(string logFrom, CancellationToken token) {
			string staleData = null;
			int staleBytes = 0;

			if (m_stream != null) {

				if (Status.IgnoreStaleData) {
					Stopwatch sw = new Stopwatch();
					sw.Start();

					LogWithPrefix(FtpTraceLevel.Verbose, "Stale data wait - " + logFrom);

					do {
						if (m_stream.SocketDataAvailable > 0 || !m_stream.IsConnected) {
							break;
						}
					} while (true);

					if (m_stream.IsConnected) {
						LogWithPrefix(FtpTraceLevel.Verbose, "Stale data took " + sw.ElapsedMilliseconds + "(ms) to arrive");
					}
				}

				if (m_stream.SocketDataAvailable > 0) {
					LogWithPrefix(FtpTraceLevel.Info, "Control connection has stale data(" + m_stream.SocketDataAvailable + ") - " + logFrom);
				}

				while (m_stream.SocketDataAvailable > 0) {
					int nRcvBytes;
					byte[] buf = new byte[m_stream.SocketDataAvailable];
					if (m_stream.IsEncrypted) {
						nRcvBytes = await m_stream.ReadAsync(buf, 0, buf.Length, token);
					}
					else {
						nRcvBytes = await m_stream.RawSocketReadAsync(buf, token);
					}
					// Even though SocketDataAvailable > 0, this is possible: nRcvBytes = 0
					// Prevent endless loop
					if (nRcvBytes <= 0) {
						if (string.IsNullOrEmpty(staleData)) {
							LogWithPrefix(FtpTraceLevel.Verbose, "Unable to retrieve stale data");
						}
						staleData = string.Empty; // Not NULL, i.e. we had stale data but it was empty
						break;
					}
					staleBytes += nRcvBytes;
					staleData += Encoding.GetString(buf).TrimEnd('\0', '\r', '\n') + Environment.NewLine;
				}

				if (!string.IsNullOrEmpty(staleData)) {
					LogWithPrefix(FtpTraceLevel.Verbose, "The stale data was (length = " + staleBytes + "): ");
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
				if (staleData != null) {
					LogWithPrefix(FtpTraceLevel.Verbose, "Stale data ignored");
					staleData = null;
				}
			}

			return staleData;
		}

	}
}

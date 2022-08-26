using System.Text;
using System.Collections.Generic;
using System.Net;
using FluentFTP.Proxy;
using FluentFTP.Rules;
using FluentFTP.Client.Modules;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP.Client.BaseClient {
	public partial class BaseFtpClient {

		/// <summary>
		/// Forcibly set the capabilities of your FTP server.
		/// By default capabilities are loaded automatically after calling Connect and you don't need to use this method.
		/// This is only for advanced use-cases.
		/// </summary>
		public void SetFeatures(List<FtpCapability> capabilities) {
			m_capabilities = capabilities;
		}

		/// <summary>
		/// Performs a bitwise and to check if the specified
		/// flag is set on the <see cref="Capabilities"/>  property.
		/// </summary>
		/// <param name="cap">The <see cref="FtpCapability"/> to check for</param>
		/// <returns>True if the feature was found, false otherwise</returns>
		public bool HasFeature(FtpCapability cap) {
			if (cap == FtpCapability.NONE && Capabilities.Count == 0) {
				return true;
			}

			return Capabilities.Contains(cap);
		}


		protected static string DecodeUrl(string url) {
			return WebUtility.UrlDecode(url);
		}

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
					if(string.IsNullOrEmpty(staleData)) {
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

		/// <summary>
		/// Checks if this FTP/FTPS connection is made through a proxy.
		/// </summary>
		public bool IsProxy() {
			return this is FtpClientProxy;
		}
		
		/// <summary>
		/// Returns true if the file passes all the rules
		/// </summary>
		protected bool FilePassesRules(FtpResult result, List<FtpRule> rules, bool useLocalPath, FtpListItem item = null) {
			if (rules != null && rules.Count > 0) {
				var passes = FtpRule.IsAllAllowed(rules, item ?? result.ToListItem(useLocalPath));
				if (!passes) {

					LogStatus(FtpTraceLevel.Info, "Skipped file due to rule: " + (useLocalPath ? result.LocalPath : result.RemotePath));

					// mark that the file was skipped due to a rule
					result.IsSkipped = true;
					result.IsSkippedByRule = true;

					// skip uploading the file
					return false;
				}
			}
			return true;
		}

	}
}

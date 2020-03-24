using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using FluentFTP.Proxy;
using FluentFTP.Servers;
using FluentFTP.Rules;
using SysSslProtocols = System.Security.Authentication.SslProtocols;
#if !CORE
using System.Web;
#endif
#if (CORE || NETFX)
using System.Threading;
#endif
#if ASYNC
using System.Threading.Tasks;

#endif

namespace FluentFTP {
	public partial class FtpClient : IDisposable {
		#region Utils

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

		/// <summary>
		/// Retrieves the delegate for the specified IAsyncResult and removes
		/// it from the m_asyncmethods collection if the operation is successful
		/// </summary>
		/// <typeparam name="T">Type of delegate to retrieve</typeparam>
		/// <param name="ar">The IAsyncResult to retrieve the delegate for</param>
		/// <returns>The delegate that generated the specified IAsyncResult</returns>
		protected T GetAsyncDelegate<T>(IAsyncResult ar) {
			T func;

			lock (m_asyncmethods) {
				if (m_isDisposed) {
					throw new ObjectDisposedException("This connection object has already been disposed.");
				}

				if (!m_asyncmethods.ContainsKey(ar)) {
					throw new InvalidOperationException("The specified IAsyncResult could not be located.");
				}

				if (!(m_asyncmethods[ar] is T)) {
#if CORE
					throw new InvalidCastException("The AsyncResult cannot be matched to the specified delegate. ");
#else
					var st = new StackTrace(1);

					throw new InvalidCastException("The AsyncResult cannot be matched to the specified delegate. " + "Are you sure you meant to call " + st.GetFrame(0).GetMethod().Name + " and not another method?"
					);
#endif
				}

				func = (T)m_asyncmethods[ar];
				m_asyncmethods.Remove(ar);
			}

			return func;
		}

		/// <summary>
		/// Ensure a relative path is absolute by appending the working dir
		/// </summary>
		private string GetAbsolutePath(string path) {

			if (path == null || path.Trim().Length == 0) {
				// if path not given, then use working dir
				var pwd = GetWorkingDirectory();
				if (pwd != null && pwd.Trim().Length > 0) {
					path = pwd;
				}
				else {
					path = "./";
				}

			}

			// FIX : #153 ensure this check works with unix & windows
			// FIX : #454 OpenVMS paths can be a single character
			else if (!path.StartsWith("/") && !(path.Length > 1 && path[1] == ':')) {

				// if its a server-specific absolute path then don't add base dir
				if (ServerHandler != null && ServerHandler.IsAbsolutePath(path)) {
					return path;
				}

				// if relative path given then add working dir to calc full path
				var pwd = GetWorkingDirectory();
				if (pwd != null && pwd.Trim().Length > 0) {
					if (path.StartsWith("./")) {
						path = path.Remove(0, 2);
					}

					path = (pwd + "/" + path).GetFtpPath();
				}
			}

			return path;
		}

#if ASYNC
		/// <summary>
		/// Ensure a relative path is absolute by appending the working dir
		/// </summary>
		private async Task<string> GetAbsolutePathAsync(string path, CancellationToken token) {

			if (path == null || path.Trim().Length == 0) {
				// if path not given, then use working dir
				string pwd = await GetWorkingDirectoryAsync(token);
				if (pwd != null && pwd.Trim().Length > 0) {
					path = pwd;
				}
				else {
					path = "./";
				}
			}

			// FIX : #153 ensure this check works with unix & windows
			// FIX : #454 OpenVMS paths can be a single character
			else if (!path.StartsWith("/") && !(path.Length > 1 && path[1] == ':')) {

				// if relative path given then add working dir to calc full path
				string pwd = await GetWorkingDirectoryAsync(token);
				if (pwd != null && pwd.Trim().Length > 0) {
					if (path.StartsWith("./")) {
						path = path.Remove(0, 2);
					}

					path = (pwd + "/" + path).GetFtpPath();
				}
			}

			return path;
		}
#endif

		private static string DecodeUrl(string url) {
#if CORE
			return WebUtility.UrlDecode(url);
#else
			return HttpUtility.UrlDecode(url);
#endif
		}

		/// <summary>
		/// Disables UTF8 support and changes the Encoding property
		/// back to ASCII. If the server returns an error when trying
		/// to turn UTF8 off a FtpCommandException will be thrown.
		/// </summary>
		public void DisableUTF8() {
			FtpReply reply;

#if !CORE14
			lock (m_lock) {
#endif
				if (!(reply = Execute("OPTS UTF8 OFF")).Success) {
					throw new FtpCommandException(reply);
				}

				m_textEncoding = Encoding.ASCII;
				m_textEncodingAutoUTF = false;
#if !CORE14
			}

#endif
		}

		/// <summary>
		/// Data shouldn't be on the socket, if it is it probably
		/// means we've been disconnected. Read and discard
		/// whatever is there and close the connection (optional).
		/// </summary>
		/// <param name="closeStream">close the connection?</param>
		/// <param name="evenEncrypted">even read encrypted data?</param>
		/// <param name="traceData">trace data to logs?</param>
		private void ReadStaleData(bool closeStream, bool evenEncrypted, bool traceData) {
			if (m_stream != null && m_stream.SocketDataAvailable > 0) {
				if (traceData) {
					LogStatus(FtpTraceLevel.Info, "There is stale data on the socket, maybe our connection timed out or you did not call GetReply(). Re-connecting...");
				}

				if (m_stream.IsConnected && (!m_stream.IsEncrypted || evenEncrypted)) {
					var buf = new byte[m_stream.SocketDataAvailable];
					m_stream.RawSocketRead(buf);
					if (traceData) {
						LogStatus(FtpTraceLevel.Verbose, "The stale data was: " + Encoding.GetString(buf).TrimEnd('\r', '\n'));
					}
				}

				if (closeStream) {
					m_stream.Close();
				}
			}
		}

#if ASYNC
		/// <summary>
		/// Data shouldn't be on the socket, if it is it probably
		/// means we've been disconnected. Read and discard
		/// whatever is there and close the connection (optional).
		/// </summary>
		/// <param name="closeStream">close the connection?</param>
		/// <param name="evenEncrypted">even read encrypted data?</param>
		/// <param name="traceData">trace data to logs?</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		private async Task ReadStaleDataAsync(bool closeStream, bool evenEncrypted, bool traceData, CancellationToken token) {
			if (m_stream != null && m_stream.SocketDataAvailable > 0) {
				if (traceData) {
					LogStatus(FtpTraceLevel.Info, "There is stale data on the socket, maybe our connection timed out or you did not call GetReply(). Re-connecting...");
				}

				if (m_stream.IsConnected && (!m_stream.IsEncrypted || evenEncrypted)) {
					var buf = new byte[m_stream.SocketDataAvailable];
					await m_stream.RawSocketReadAsync(buf, token);
					if (traceData) {
						LogStatus(FtpTraceLevel.Verbose, "The stale data was: " + Encoding.GetString(buf).TrimEnd('\r', '\n'));
					}
				}

				if (closeStream) {
					m_stream.Close();
				}
			}
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
		private bool FilePassesRules(FtpResult result, List<FtpRule> rules, bool useLocalPath, FtpListItem item = null) {
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
		

		#endregion

		#region Logging

		/// <summary>
		/// Add a custom listener here to get events every time a message is logged.
		/// </summary>
		public Action<FtpTraceLevel, string> OnLogEvent;

		/// <summary>
		/// Log a function call with relevant arguments
		/// </summary>
		/// <param name="function">The name of the API function</param>
		/// <param name="args">The args passed to the function</param>
		public void LogFunc(string function, object[] args = null) {
			// log to attached logger if given
			if (OnLogEvent != null) {
				OnLogEvent(FtpTraceLevel.Verbose, ">         " + function + "(" + args.ItemsToString().Join(", ") + ")");
			}

			// log to system
			FtpTrace.WriteFunc(function, args);
		}

		/// <summary>
		/// Log a message
		/// </summary>
		/// <param name="eventType">The type of tracing event</param>
		/// <param name="message">The message to write</param>
		public void LogLine(FtpTraceLevel eventType, string message) {
			// log to attached logger if given
			if (OnLogEvent != null) {
				OnLogEvent(eventType, message);
			}

			// log to system
			FtpTrace.WriteLine(eventType, message);
		}

		/// <summary>
		/// Log a message, adding an automatic prefix to the message based on the `eventType`
		/// </summary>
		/// <param name="eventType">The type of tracing event</param>
		/// <param name="message">The message to write</param>
		public void LogStatus(FtpTraceLevel eventType, string message) {
			// add prefix
			message = TraceLevelPrefix(eventType) + message;

			// log to attached logger if given
			if (OnLogEvent != null) {
				OnLogEvent(eventType, message);
			}

			// log to system
			FtpTrace.WriteLine(eventType, message);
		}

		private static string TraceLevelPrefix(FtpTraceLevel level) {
			switch (level) {
				case FtpTraceLevel.Verbose:
					return "Status:   ";

				case FtpTraceLevel.Info:
					return "Status:   ";

				case FtpTraceLevel.Warn:
					return "Warning:  ";

				case FtpTraceLevel.Error:
					return "Error:    ";
			}

			return "Status:   ";
		}

		#endregion
	}
}
#define TRACE
using System;
using System.Diagnostics;
using System.IO;

namespace FluentFTP.Helpers {
	/// <summary>
	/// Used for transaction logging and debug information.
	/// </summary>
	public static class FtpTrace {
#if !CORE
		private static volatile TraceSource m_traceSource = new TraceSource("FluentFTP") {
			Switch = new SourceSwitch("sourceSwitch", "Verbose") {Level = SourceLevels.All}
		};

		private static bool m_flushOnWrite = true;


		/// <summary>
		/// Should the trace listeners be flushed immediately after writing to them?
		/// </summary>
		public static bool FlushOnWrite {
			get => m_flushOnWrite;
			set => m_flushOnWrite = value;
		}

		private static bool m_prefix = false;

		/// <summary>
		/// Should the log entries be written with a prefix of "FluentFTP"?
		/// Useful if you have a single TraceListener shared across multiple libraries.
		/// </summary>
		public static bool LogPrefix {
			get => m_prefix;
			set => m_prefix = value;
		}


		/// <summary>
		/// Add a TraceListner to the collection. You can use one of the predefined
		/// TraceListeners in the System.Diagnostics namespace, such as ConsoleTraceListener
		/// for logging to the console, or you can write your own deriving from 
		/// System.Diagnostics.TraceListener.
		/// </summary>
		/// <param name="listener">The TraceListener to add to the collection</param>
		public static void AddListener(TraceListener listener) {
			lock (m_traceSource) {
				m_traceSource.Listeners.Add(listener);
			}
		}

		/// <summary>
		/// Remove the specified TraceListener from the collection
		/// </summary>
		/// <param name="listener">The TraceListener to remove from the collection.</param>
		public static void RemoveListener(TraceListener listener) {
			lock (m_traceSource) {
				m_traceSource.Listeners.Remove(listener);
			}
		}

#endif

		private static bool m_LogToConsole = false;

		/// <summary>
		/// Should FTP communication be logged to console?
		/// </summary>
		public static bool LogToConsole {
			get => m_LogToConsole;
			set => m_LogToConsole = value;
		}

		private static string m_LogToFile = null;

		/// <summary>
		/// Set this to a file path to append all FTP communication to it.
		/// </summary>
		public static string LogToFile {
			get => m_LogToFile;
			set => m_LogToFile = value;
		}

		private static bool m_functions = true;

		/// <summary>
		/// Should the function calls be logged in Verbose mode?
		/// </summary>
		public static bool LogFunctions {
			get => m_functions;
			set => m_functions = value;
		}

		private static bool m_IP = false;

		/// <summary>
		/// Should the FTP server IP addresses be included in the logs?
		/// </summary>
		public static bool LogIP {
			get => m_IP;
			set => m_IP = value;
		}

		private static bool m_username = false;

		/// <summary>
		/// Should the FTP usernames be included in the logs?
		/// </summary>
		public static bool LogUserName {
			get => m_username;
			set => m_username = value;
		}

		private static bool m_password = false;

		/// <summary>
		/// Should the FTP passwords be included in the logs?
		/// </summary>
		public static bool LogPassword {
			get => m_password;
			set => m_password = value;
		}

		private static bool m_tracing = true;

		/// <summary>
		/// Should we trace at all?
		/// </summary>
		public static bool EnableTracing {
			get => m_tracing;
			set => m_tracing = value;
		}

		/// <summary>
		/// Write to the TraceListeners
		/// </summary>
		/// <param name="message">The message to write</param>

		//[Obsolete("Use overloads with FtpTraceLevel")]
		public static void Write(string message) {
			Write(FtpTraceLevel.Verbose, message);
		}

		/// <summary>
		/// Write to the TraceListeners
		/// </summary>
		/// <param name="message">The message to write</param>

		//[Obsolete("Use overloads with FtpTraceLevel")]
		public static void WriteLine(object message) {
			Write(FtpTraceLevel.Verbose, message.ToString());
		}

		/// <summary>
		/// Write to the TraceListeners
		/// </summary>
		/// <param name="eventType">The type of tracing event</param>
		/// <param name="message">The message to write</param>
		public static void WriteLine(FtpTraceLevel eventType, object message) {
			Write(eventType, message.ToString());
		}

		/// <summary>
		/// Write to the TraceListeners, for the purpose of logging a API function call
		/// </summary>
		/// <param name="function">The name of the API function</param>
		/// <param name="args">The args passed to the function</param>
		public static void WriteFunc(string function, object[] args = null) {
			if (m_functions) {
				Write(FtpTraceLevel.Verbose, "");
				Write(FtpTraceLevel.Verbose, "# " + function + "(" + args.ItemsToString().Join(", ") + ")");
			}
		}


		/// <summary>
		/// Write to the TraceListeners
		/// </summary>
		/// <param name="eventType">The type of tracing event</param>
		/// <param name="message">A formattable string to write</param>
		public static void Write(FtpTraceLevel eventType, string message) {
			if (!EnableTracing) {
				return;
			}

#if DEBUG
			Debug.WriteLine(message);
#else
			if (m_LogToConsole) {
				Console.WriteLine(message);
			}

			if (m_LogToFile != null) {
				File.AppendAllText(m_LogToFile, message + "\n");
			}
#endif

#if !CORE

			if (m_prefix) {
				// if prefix is wanted then use TraceEvent()
				m_traceSource.TraceEvent(TraceLevelTranslation(eventType), 0, message);
			}
			else {
				// if prefix is NOT wanted then write manually
				EmitEvent(m_traceSource, TraceLevelTranslation(eventType), message);
			}

			if (m_flushOnWrite) {
				m_traceSource.Flush();
			}

#endif
		}


#if !CORE

		private static TraceEventType TraceLevelTranslation(FtpTraceLevel level) {
			switch (level) {
				case FtpTraceLevel.Verbose:
					return TraceEventType.Verbose;

				case FtpTraceLevel.Info:
					return TraceEventType.Information;

				case FtpTraceLevel.Warn:
					return TraceEventType.Warning;

				case FtpTraceLevel.Error:
					return TraceEventType.Error;

				default:
					return TraceEventType.Verbose;
			}
		}

		private static object traceSync = new object();

		private static void EmitEvent(TraceSource traceSource, TraceEventType eventType, string message) {
			try {
				lock (traceSync) {
					if (traceSource.Switch.ShouldTrace(eventType)) {
						foreach (TraceListener listener in traceSource.Listeners) {
							try {
								listener.WriteLine(message);
								listener.Flush();
							}
							catch {
							}
						}
					}
				}
			}
			catch {
			}
		}

#endif
	}
}
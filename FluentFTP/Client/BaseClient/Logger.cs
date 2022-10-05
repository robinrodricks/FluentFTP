using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using FluentFTP.Helpers;
using Microsoft.Extensions.Logging;

namespace FluentFTP.Client.BaseClient {
	public partial class BaseFtpClient {

		/// <summary>
		/// Log the version of the running assembly
		/// </summary>
		protected void LogVersion() {

			string applicationVersion = Assembly.GetAssembly(MethodBase.GetCurrentMethod().DeclaringType).GetName().Version.ToString();
			LogWithPrefix(FtpTraceLevel.Verbose, "FluentFTP " + applicationVersion);

		}

		/// <summary>
		/// Log a function call with relevant arguments
		/// </summary>
		/// <param name="function">The name of the API function</param>
		/// <param name="args">The args passed to the function</param>
		protected void LogFunction(string function, object[] args = null) {

			var fullMessage = (">         " + function + "(" + args.ItemsToString().Join(", ") + ")");

			// log to modern logger if given
			m_logger?.LogInformation(fullMessage);

			// log to legacy logger if given
			m_legacyLogger?.Invoke(FtpTraceLevel.Verbose, fullMessage);

			// log to system
			LogToDebugOrConsole("");
			LogToDebugOrConsole("# " + function + "(" + args.ItemsToString().Join(", ") + ")");

		}

		/// <summary>
		/// Log a message
		/// </summary>
		/// <param name="eventType">The type of tracing event</param>
		/// <param name="message">The message to write</param>
		protected void Log(FtpTraceLevel eventType, string message) {

			// log to modern logger if given
			if (m_logger != null) {
				LogToLogger(eventType, message);
			}

			// log to legacy logger if given
			m_legacyLogger?.Invoke(eventType, message);

			// log to system
			LogToDebugOrConsole(message);
		}

		/// <summary>
		/// Log a message, adding an automatic prefix to the message based on the `eventType`
		/// </summary>
		/// <param name="eventType">The type of tracing event</param>
		/// <param name="message">The message to write</param>
		protected void LogWithPrefix(FtpTraceLevel eventType, string message) {

			var fullMessage = GetLogPrefix(eventType) + message;

			// log to attached logger if given
			if (m_logger != null) {
				LogToLogger(eventType, fullMessage);
			}

			// log to legacy logger if given
			m_legacyLogger?.Invoke(eventType, fullMessage);

			// log to system
			LogToDebugOrConsole(fullMessage);
		}

		/// <summary>
		/// Log a message to the attached logger.
		/// </summary>
		private void LogToLogger(FtpTraceLevel eventType, string message) {
			switch (eventType) {
				case FtpTraceLevel.Verbose:
					m_logger.LogDebug(message);
					break;

				case FtpTraceLevel.Info:
					m_logger.LogInformation(message);
					break;

				case FtpTraceLevel.Warn:
					m_logger.LogWarning(message);
					break;

				case FtpTraceLevel.Error:
					m_logger.LogError(message);
					break;
			}
		}

		/// <summary>
		/// Log a message to the debug output and console.
		/// </summary>
		protected void LogToDebugOrConsole(string message) {
#if DEBUG
			Debug.WriteLine(message);
#endif
			if (Config.LogToConsole) {
				Console.WriteLine(message);
			}
		}

		protected static string GetLogPrefix(FtpTraceLevel eventType) {
			switch (eventType) {
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

		/// <summary>
		/// To allow for external connected classes to use the attached logger.
		/// </summary>
		void IInternalFtpClient.LogLine(FtpTraceLevel eventType, string message) {
			this.Log(eventType, message);
		}

		/// <summary>
		/// To allow for external connected classes to use the attached logger.
		/// </summary>
		void IInternalFtpClient.LogStatus(FtpTraceLevel eventType, string message) {
			this.LogWithPrefix(eventType, message);
		}

	}
}
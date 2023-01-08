using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using FluentFTP.Helpers;
using FluentFTP.Helpers.Logging;

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
			m_logger?.Log(FtpTraceLevel.Info, fullMessage);

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
			m_logger?.Log(eventType, message);

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
		/// <param name="exception">An optional exeption</param>
		/// <param name="exNewLine">Write an optional exeption on a new line</param>
		protected void LogWithPrefix(FtpTraceLevel eventType, string message, Exception exception = null, bool exNewLine = false) {
			// log to attached logger if given
			m_logger?.Log(eventType, message, exception);

			var fullMessage = eventType.GetLogPrefix() + message + (exception is not null ? (exNewLine ? Environment.NewLine + eventType.GetLogPrefix() : " : ") + exception.Message : null);

			// log to legacy logger if given
			m_legacyLogger?.Invoke(eventType, fullMessage);

			// log to system
			LogToDebugOrConsole(fullMessage);
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

		/// <summary>
		/// To allow for external connected classes to use the attached logger.
		/// </summary>
		void IInternalFtpClient.LogLine(FtpTraceLevel eventType, string message) {
			this.Log(eventType, message);
		}

		/// <summary>
		/// To allow for external connected classes to use the attached logger.
		/// </summary>
		void IInternalFtpClient.LogStatus(FtpTraceLevel eventType, string message, Exception exception, bool exNewLine) {
			this.LogWithPrefix(eventType, message, exception, exNewLine);
		}

	}
}
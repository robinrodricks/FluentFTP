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
			if (AnyLoggingIsEnabled()) {
				string applicationVersion = Assembly.GetAssembly(MethodBase.GetCurrentMethod().DeclaringType).GetName().Version.ToString();
				string target;
#if NET20
				target = ".NET Framework 2.0";
#elif NET35
				target = ".NET Framework 3.5";
#elif NET40
				target = ".NET Framework 4.0";
#elif NET45
				target = ".NET Framework 4.5";
#elif NET451
				target = ".NET Framework 4.5.1";
#elif NET452
				target = ".NET Framework 4.5.2";
#elif NET46
				target = ".NET Framework 4.6";
#elif NET461
				target = ".NET Framework 4.6.1";
#elif NET462
				target = ".NET Framework 4.6.2";
#elif NET47
				target = ".NET Framework 4.7";
#elif NET471
				target = ".NET Framework 4.7.1";
#elif NET472
				target = ".NET Framework 4.7.2";
#elif NET48
				target = ".NET Framework 4.8";
#elif NET48_OR_GREATER
				target = ".NET Framework 4.8+";
#elif NETSTANDARD1_0
				target = ".NET Standard 1.0";
#elif NETSTANDARD1_1
				target = ".NET Standard 1.1";
#elif NETSTANDARD1_2
				target = ".NET Standard 1.2";
#elif NETSTANDARD1_3
				target = ".NET Standard 1.3";
#elif NETSTANDARD1_4
				target = ".NET Standard 1.4";
#elif NETSTANDARD1_5
				target = ".NET Standard 1.5";
#elif NETSTANDARD1_6
				target = ".NET Standard 1.6";
#elif NETSTANDARD2_0
				target = ".NET Standard 2.0";
#elif NETSTANDARD2_1
				target = ".NET Standard 2.1";
#elif NETSTANDARD2_1_OR_GREATER
				target = ".NET Standard 2.1+";
#elif NETCOREAPP1_0
				target = ".NET Core 1.0";
#elif NETCOREAPP1_1
				target = ".NET Core 1.1";
#elif NETCOREAPP2_0
				target = ".NET Core 2.0";
#elif NETCOREAPP2_1
				target = ".NET Core 2.1";
#elif NETCOREAPP2_2
				target = ".NET Core 2.2";
#elif NETCOREAPP3_0
				target = ".NET Core 3.0";
#elif NETCOREAPP3_1
				target = ".NET Core 3.1";
#elif NET5_0
				target = ".NET 5.0";
#elif NET6_0
				target = ".NET 6.0";
#elif NET7_0
				target = ".NET 7.0";
#elif NET8_0
				target = ".NET 8.0";
#elif NET8_0_OR_GREATER
				target = ".NET 8.0+";
#else
				target = "Unknown";
#endif
				LogWithPrefix(FtpTraceLevel.Verbose, "FluentFTP " + applicationVersion + "(" + target + ") " + this.ClientType);
			}
		}

		/// <summary>
		/// Log a function call with relevant arguments
		/// </summary>
		/// <param name="function">The name of the API function</param>
		/// <param name="args">The args passed to the function</param>
		protected void LogFunction(string function, object args) {
			if (AnyLoggingIsEnabled()) {
				var funcCallString = function + "(" + args.ObjectToString() + ")";

				var fullMessage = ">         " + funcCallString;

				// log to modern logger if given
				m_logger?.Log(FtpTraceLevel.Info, fullMessage);

				// log to legacy logger if given
				m_legacyLogger?.Invoke(FtpTraceLevel.Verbose, fullMessage);

				// log to system
				LogToDebugOrConsole("");
				LogToDebugOrConsole("# " + funcCallString);
			}
		}

		/// <summary>
		/// Log a function call with relevant arguments
		/// </summary>
		/// <param name="function">The name of the API function</param>
		/// <param name="args">The args passed to the function</param>
		protected void LogFunction(string function, object[] args = null) {
			if (AnyLoggingIsEnabled()) {
				var funcCallString = function + "(" + args.ItemsToString().Join(", ") + ")";

				var fullMessage = ">         " + funcCallString;

				// log to modern logger if given
				m_logger?.Log(FtpTraceLevel.Info, fullMessage);

				// log to legacy logger if given
				m_legacyLogger?.Invoke(FtpTraceLevel.Verbose, fullMessage);

				// log to system
				LogToDebugOrConsole("");
				LogToDebugOrConsole("# " + funcCallString);
			}
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
			if (AnyLoggingIsEnabled()) {
				// log to attached logger if given
				m_logger?.Log(eventType, message, exception);

				var fullMessage = eventType.GetLogPrefix() + message + (exception is not null ? (exNewLine ? Environment.NewLine + eventType.GetLogPrefix() : " : ") + exception.Message : null);

				// log to legacy logger if given
				m_legacyLogger?.Invoke(eventType, fullMessage);

				// log to system
				LogToDebugOrConsole(fullMessage);
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

		private bool AnyLoggingIsEnabled() {
#if DEBUG
			return true;
#else
			return m_logger is { } || m_legacyLogger is { } || Config.LogToConsole;
#endif
		}
	}
}
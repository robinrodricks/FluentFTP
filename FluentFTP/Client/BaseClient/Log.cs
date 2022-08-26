using System;
using System.Linq;
using FluentFTP.Helpers;

namespace FluentFTP.Client.BaseClient {
	public partial class BaseFtpClient {
		

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

		protected static string TraceLevelPrefix(FtpTraceLevel level) {
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


	}
}

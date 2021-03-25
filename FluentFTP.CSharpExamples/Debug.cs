using System;
using System.Diagnostics;
using FluentFTP;

namespace Examples {
	/// <summary>
	/// Example for logging server transactions for use in debugging problems.
	/// </summary>
	internal static class DebugExample {

#if NETFX

		/// <summary>
		/// Log to a console window
		/// </summary>
		private static void LogToConsole() {
			FtpTrace.AddListener(new ConsoleTraceListener());

			// now use System.Net.FtpCLient as usual and the server transactions
			// will be written to the Console window.
		}

		/// <summary>
		/// Log to a text file
		/// </summary>
		private static void LogToFile() {
			FtpTrace.AddListener(new TextWriterTraceListener("log_file.txt"));

			// now use System.Net.FtpCLient as usual and the server transactions
			// will be written to the specified log file.
		}

		/// <summary>
		/// Custom trace listener class that can log the transaction
		/// however you want.
		/// </summary>
		private class CustomTraceListener : TraceListener {
			public override void Write(string message) {
				Console.Write(message);
			}

			public override void WriteLine(string message) {
				Console.WriteLine(message);
			}
		}

		/// <summary>
		/// Log to a custom TraceListener
		/// </summary>
		private static void LogToCustomListener() {
			FtpTrace.AddListener(new CustomTraceListener());
		}

		//Listeners can also be attached via app.config, web.config, or machine.config files
		/*
		 * <system.diagnostics>
		 *      <trace autoflush="true"></trace>
		 *      <sources>
		 *        <source name="FluentFTP">
		 *          <listeners>
		 *            <clear />
		 *            <!--Attach NLog Trace Listener -->
		 *            <add name="nlog" />
		 *            <!-- Attach a Console Listener -->
		 *            <add name="console />
		 *            <!-- Attach a File Trace Listener -->
		 *            <add name="file" />
		 *            <!-- Attach a Custom Listener -->
		 *            <add name="myLogger" />
		 *         </listeners>
		 *        </source>
		 *      </sources>
		 *      <sharedListeners>
		 *         <!--Define Console Listener -->
		 *         <add name="console" type="System.Diagnostics.ConsoleTraceListener" />
		 *         <!--Define File Listener -->
		 *         <add name="file" type="System.Diagnostics.TextWriterTraceListener
		 *          initializeData="outputFile.log">
		 *              <!--Only write errors -->
		 *              <filter type="System.Diagnostics.EventTypeFilter" initializeData="Error" />
		 *          </add>
		 *         <!--Define Custom Listener -->
		 *         <add name="custom" type="MyNamespace.MyCustomTraceListener />
		 *        <!-- Define NLog Logger -->
		 *        <add name="nlog" type="NLog.NLogTraceListener, NLog" />
		 *      </sharedListeners>
		 *    </system.diagnostics>
		 */

#endif

	}

}
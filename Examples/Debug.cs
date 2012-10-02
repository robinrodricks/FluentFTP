using System;
using System.Diagnostics;
using System.Net.FtpClient;

namespace Examples {
    /// <summary>
    /// Example for logging server transactions for use in debugging problems. In order
    /// for this feature to work System.Net.FtpClient needs to be built with #DEBUG defined.
    /// The code that logs to the Debug TraceListener is omitted from release builds.
    /// </summary>
    public static class DebugExample {
        /// <summary>
        /// Log to a console window
        /// </summary>
        static void LogToConsole() {
            Debug.Listeners.Add(new ConsoleTraceListener());

            // now use System.Net.FtpCLient as usual and the server transactions
            // will be written to the Console window.
        }

        /// <summary>
        /// Log to a text file
        /// </summary>
        static void LogToFile() {
            Debug.Listeners.Add(new TextWriterTraceListener("log_file.txt"));

            // now use System.Net.FtpCLient as usual and the server transactions
            // will be written to the Console window.
        }

        /// <summary>
        /// Custom trace listener class that can log the transaction
        /// however you want.
        /// </summary>
        class CustomTraceListener : TraceListener {
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
        static void LogToCustomListener() {
            Debug.Listeners.Add(new CustomTraceListener());
        }
    }
}

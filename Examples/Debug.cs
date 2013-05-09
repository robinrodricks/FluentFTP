using System;
using System.Diagnostics;
using System.Net.FtpClient;

namespace Examples {
    /// <summary>
    /// Example for logging server transactions for use in debugging problems. If DEBUG
    /// is defined this information is logged via System.Diagnostics.Debug.Write() as well 
    /// so you'll the same information in your Visual Studio Output window
    /// </summary>
    public static class DebugExample {
        /// <summary>
        /// Log to a console window
        /// </summary>
        static void LogToConsole() {
            FtpTrace.AddListener(new ConsoleTraceListener());

            // now use System.Net.FtpCLient as usual and the server transactions
            // will be written to the Console window.
        }

        /// <summary>
        /// Log to a text file
        /// </summary>
        static void LogToFile() {
            FtpTrace.AddListener(new TextWriterTraceListener("log_file.txt"));

            // now use System.Net.FtpCLient as usual and the server transactions
            // will be written to the specified log file.
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
            FtpTrace.AddListener(new CustomTraceListener());
        }
    }
}

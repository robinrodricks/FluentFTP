using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Net.FtpClient {
    /// <summary>
    /// Used for transaction logging and debug information.
    /// </summary>
    /// <example>The following example illustrates how to assist in debugging
    /// System.Net.FtpClient by getting a transaction log from the server.
    /// <code source="..\Examples\Debug.cs" lang="cs" />
    /// </example>
    public static class FtpTrace {
        static List<TraceListener> m_listeners = new List<TraceListener>();

        /// <summary>
        /// Add a TraceListner to the collection. You can use one of the predefined
        /// TraceListeners in the System.Diagnostics namespace, such as ConsoleTraceListener
        /// for logging to the console, or you can write your own deriving from 
        /// System.Diagnostics.TraceListener.
        /// </summary>
        /// <param name="listener">The TraceListener to add to the collection</param>
        public static void AddListener(TraceListener listener) {
            lock (m_listeners) {
                m_listeners.Add(listener);
            }
        }

        /// <summary>
        /// Remove the specified TraceListener from the collection
        /// </summary>
        /// <param name="listener">The TraceListener to remove from the collection.</param>
        public static void RemoveListener(TraceListener listener) {
            lock (m_listeners) {
                m_listeners.Remove(listener);
            }
        }

        /// <summary>
        /// Write to the TraceListeners.
        /// </summary>
        /// <param name="message">The message to write</param>
        /// <param name="args">Optional variables if using a format string similar to string.Format()</param>
        public static void Write(string message, params object[] args) {
            Write(string.Format(message, args));
        }

        /// <summary>
        /// Write to the TraceListeners
        /// </summary>
        /// <param name="message">The message to write</param>
        public static void Write(string message) {
            TraceListener[] listeners;

            lock (m_listeners) {
                listeners = m_listeners.ToArray();
            }

#if DEBUG
            Debug.Write(message);
#endif

            foreach (TraceListener t in listeners) {
                t.Write(message);
            }
        }

        /// <summary>
        /// Write to the TraceListeners.
        /// </summary>
        /// <param name="message">The message to write</param>
        /// <param name="args">Optional variables if using a format string similar to string.Format()</param>
        public static void WriteLine(string message, params object[] args) {
            Write(string.Format("{0}{1}", string.Format(message, args), Environment.NewLine));
        }

        /// <summary>
        /// Write to the TraceListeners
        /// </summary>
        /// <param name="message">The message to write</param>
        public static void WriteLine(string message) {
            Write(string.Format("{0}{1}", message, Environment.NewLine));
        }
    }
}

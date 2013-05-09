using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Net.FtpClient {
    /// <summary>
    /// Used for transaction logging and debug information.
    /// </summary>
    public static class FtpTrace {
        static List<TraceListener> m_listeners = new List<TraceListener>();

        /// <summary>
        /// Add a TraceListner to the collection. You can use one of the predefined
        /// TraceListeners in the System.Diagnostics namespace, such as ConsoleTraceListener
        /// for logging to the console, or you can write your own deriving from 
        /// System.Diagnostics.TraceListener.
        /// </summary>
        /// <param name="listener">The TraceListener to add to the collection</param>
        public static void Add(TraceListener listener) {
            lock (m_listeners) {
                m_listeners.Add(listener);
            }
        }

        /// <summary>
        /// Remove the specified TraceListener from the collection
        /// </summary>
        /// <param name="listener">The TraceListener to remove from the collection.</param>
        public static void Remove(TraceListener listener) {
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
#if DEBUG
            Debug.Write(string.Format(message, args));
#endif

            lock (m_listeners) {
                foreach (TraceListener t in m_listeners) {
                    t.Write(string.Format(message, args));
                }
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
    }
}

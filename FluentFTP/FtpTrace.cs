#define TRACE
using System;
using System.Diagnostics;

namespace FluentFTP {
	/// <summary>
	/// Used for transaction logging and debug information.
	/// </summary>
	/// <example>The following example illustrates how to assist in debugging
	/// FluentFTP by getting a transaction log from the server.
	/// <code source="..\Examples\Debug.cs" lang="cs" />
	/// </example>
	public static class FtpTrace {

#if !CORE
		//static List<TraceListener> m_listeners = new List<TraceListener>();
	    private static readonly TraceSource m_traceSource = new TraceSource("FluentFTP") {
	        Switch = new SourceSwitch("sourceSwitch", "Verbose") { Level = SourceLevels.All }
	    };

		static bool m_flushOnWrite = false;

		/// <summary>
		/// Gets or sets whether the trace listeners should be flushed or not
		/// after writing to them. Default value is false.
		/// </summary>
		public static bool FlushOnWrite {
			get {
				return m_flushOnWrite;
			}
			set {
				m_flushOnWrite = value;
			}
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

		/// <summary>
		/// Write to the TraceListeners
		/// </summary>
		/// <param name="message">The message to write</param>
		[Obsolete("Use overloads with FtpTraceLevel")]
		public static void Write(string message) {
		    Write(FtpTraceLevel.Debug, message);
		}

		/// <summary>
		/// Write to the TraceListeners
		/// </summary>
		/// <param name="message">The message to write</param>
        [Obsolete("Use overloads with FtpTraceLevel")]
		public static void WriteLine(object message) {
			Write(string.Concat(message, Environment.NewLine));
		}


	    /// <summary>
	    /// Write to the TraceListeners
	    /// </summary>
	    /// <param name="eventType">The type of tracing event</param>
	    /// <param name="message">A formattable string to write</param>
	    /// <param name="args">Arguments to insert into the formattable string</param>
	    public static void Write(FtpTraceLevel eventType, string message, params object[] args) {
	        string msg = string.Format(message, args);
#if CORE && DEBUG
		    Debug.Write(msg);
#elif !CORE
	        var diagTraceLvl = TraceLevelTranslation(eventType);
	        m_traceSource.TraceEvent(diagTraceLvl, 0, msg);
	        if (m_flushOnWrite)
	            m_traceSource.Flush();
#endif
	    }

	    /// <summary>
	    /// Write to the TraceListeners
	    /// </summary>
	    /// <param name="eventType">The type of tracing event</param>
	    /// <param name="message">A formattable string to write</param>
	    /// <param name="args">Arguments to insert into the formattable string</param>
	    public static void WriteLine(FtpTraceLevel eventType, string message, params object[] args) {
	        Write(eventType, string.Concat(message, Environment.NewLine), args);
	    }

#if !CORE

	    private static TraceEventType TraceLevelTranslation(FtpTraceLevel level) {
	        switch(level) {
	            case FtpTraceLevel.Debug:
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
#endif
	}
}
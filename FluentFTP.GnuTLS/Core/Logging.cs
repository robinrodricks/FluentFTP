using static FluentFTP.GnuTLS.GnuTlsInternalStream;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FluentFTP.GnuTLS.Enums;

namespace FluentFTP.GnuTLS.Core {
	internal class Logging {

		public static int logMaxLevel;
		public static GnuMessage logDebugInformation;
		public static Queue<string> logQueue;
		public static int logQueueMaxSize;

		private static GnuStreamLogCBFunc logCBFunc;

		// Not suppressable, level 0 - the GnuTls version message
		public static void Log(string msg) {
			Log(0, msg, true);
		}

		// Not suppressable, level 0 - messages from Utils - Check(...)
		// prior to exception throw
		public static void LogNoQueue(string msg) {
			Log(0, msg, false);
		}

		// Suppressable, level 1 - Debug messages from the GnuTls stream
		// Default: InteropFunction
		public static void LogGnuFunc(string msg) {
			LogGnuFunc(GnuMessage.InteropFunction, msg);
		}
		// General: Any type
		public static void LogGnuFunc(GnuMessage type, string msg) {
			if ((type & logDebugInformation) != 0 || msg.StartsWith("Error")) {
				Log(1, "Interop : " + msg, true);
			}
		}

		// Common log routine for "Interop" and "Internal" messages.
		// These are buffered regardless of loglevel settings.
		// Then they are filtered by loglevel and passed via callback
		// to the FluentFTP logging framework.
		// In case of an exception being thrown, the buffered messages
		// are re-issued prior to throw of the exception to aid in debugging.
		public static void Log(int lvl, string msg, bool q) {
			string s = lvl.ToString().PadRight(3) + " " + msg.TrimEnd(new char[] { '\n', '\r' });

			if (q) {
				if (logQueue.Count < logQueueMaxSize) {
					logQueue.Enqueue(s);
				}
				else {
					logQueue.Enqueue(s);
					logQueue.Dequeue();
				}
			}

			if (lvl > logMaxLevel) {
				return;
			}

			if (logCBFunc != null) {
				logCBFunc(s);
			}
		}

		// This "Log" overload is used by the GnuTLS.dll "internal" log callback
		// To suppress these messages in the overall FluentFTP log, use the
		// GnuTls config option "LogLevel" - to suppress them entirely, set
		// to 0.
		private static void Log(int lvl, IntPtr msg) {
			string s = Marshal.PtrToStringAnsi(msg);

			// Remove some unimportant messages
			if (lvl == 2 &&
				(s.StartsWith("Keeping ciphersuite") ||
				 s.StartsWith("Advertizing version"))) {
				return;
			}

			string logMsg = "Internal: " + s;
			//Static.Free(msg);
			Log(lvl, logMsg, true);
		}

		// This is the definition of the log call back that the GnuTLS.dll will call.
		// It is set up and defined in the main class FtpGnuStream
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void GnuTlsLogCBFunc(int level, IntPtr message);

		// Avoid garbage collection failure of this callback
		private static GnuTlsLogCBFunc gnuTlsLogCBFunc = Log;

		// Setup logging
		public static void InitLogging(GnuStreamLogCBFunc logCBFunc, int logMaxLevel, GnuMessage logDebugInformation, int logQueueMaxSize) {
			logQueue = new Queue<string>();

			Logging.logCBFunc = logCBFunc;
			Logging.logMaxLevel = logMaxLevel;
			Logging.logDebugInformation = logDebugInformation;
			Logging.logQueueMaxSize = logQueueMaxSize;

			GnuTls.GlobalSetLogFunction(gnuTlsLogCBFunc);
			GnuTls.GlobalSetLogLevel(99);
		}
	}
}

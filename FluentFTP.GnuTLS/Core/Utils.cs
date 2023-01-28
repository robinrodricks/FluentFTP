using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace FluentFTP.GnuTLS.Core {
	public class Utils {

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static string? GetCurrentMethod() {

			var st = new StackTrace();
			var sf = st.GetFrame(1);

			return sf.GetMethod().Name;
		}

		public static int Check(string? methodName, int result, params int[] allowed) {

			if (result >= 0) {
				return result;
			}

			if (allowed.Contains(result)) {
				return result;
			}

			GnuTlsException ex;

			if (!EC.ec.TryGetValue(result, out string errTxt)) errTxt = "Unknown error";

			ex = new GnuTlsException(methodName + " failed: (" + result + ") " + errTxt);
			ex.ExMethod = methodName;
			ex.ExResult = result;
			ex.ExMeaning = errTxt;

			Logging.LogNoQueue(ex.Message);

			Logging.LogNoQueue("Last " + Logging.logQueueMaxSize + " GnuTLS buffered debug messages:");

			foreach (string s in Logging.logQueue) {
				Logging.LogNoQueue(s);
			}

			throw ex;
		}
	}
}

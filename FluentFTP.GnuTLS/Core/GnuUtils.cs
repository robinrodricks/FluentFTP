using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace FluentFTP.GnuTLS.Core {
	internal class GnuUtils {

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static string GetCurrentMethod() {

			var st = new StackTrace();
			var sf = st.GetFrame(1);

			return "*" + sf.GetMethod().Name + "(...)";
		}

		public static int Check(string methodName, int result, params int[] resultsAllowed) {

			if (result >= 0) {
				return result;
			}

			if (resultsAllowed.Contains(result)) {
				return result;
			}

			// Consider also checking GnuTls.ErrorIsFatal(result)

			GnuTlsException ex;

			string errTxt = GnuTlsErrorText(result);

			ex = new GnuTlsException("Error   : " + methodName + " failed: (" + result + ") " + errTxt);
			ex.ExMethod = methodName;
			ex.ExResult = result;
			ex.ExMeaning = errTxt;

			Logging.LogNoQueue(ex.Message);

			Logging.LogNoQueue("Debug   : Last " + Logging.logQueueMaxSize + " GnuTLS buffered debug messages follow:");

			foreach (string s in Logging.logQueue) {
				Logging.LogNoQueue("Debug   : " + s);
			}

			throw ex;
		}

		public static string GnuTlsErrorText(int errorCode) {
			if (!EC.ec.TryGetValue(errorCode, out string errText)) errText = "Unknown error";
			return errText;
		}
	}
}

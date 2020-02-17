using System;
using System.Collections.Generic;
using System.Text;

namespace FluentFTP.Rules {
	/// <summary>
	/// Base class used for all FTP Rules. Extend this class to create custom rules.
	/// You only need to provide an implementation for IsAllowed, and add any custom arguments that you require.
	/// </summary>
	public class FtpRule {

		public FtpRule() {
		}

		/// <summary>
		/// Returns true if the object has passed this rules.
		/// </summary>
		public virtual bool IsAllowed(FtpListItem result) {
			return true;
		}

		/// <summary>
		/// Returns true if the object has passed all the rules.
		/// </summary>
		public static bool IsAllAllowed(List<FtpRule> rules, FtpListItem result) {
			foreach (var rule in rules) {
				if (!rule.IsAllowed(result)) {
					return false;
				}
			}
			return true;
		}

	}
}
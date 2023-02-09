using FluentFTP.GnuTLS.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace FluentFTP.GnuTLS.Priority {
	internal class PriorityBuilder {

		public static string Build(GnuSuite suite, IList<GnuOption> options, IList<GnuAdvanced> specials, GnuProfile profile) {
			var sb = new StringBuilder();

			sb.Append(PriorityConstants.Suites[(int)suite]);

			// add options based on the operator
			if (options != null) {
				foreach (var option in options) {
					sb.Append(':');
					sb.Append(option.Operator == GnuOperator.Include ? '+' : '-');
					sb.Append(PriorityConstants.Options[(int)option.Command]);
				}
			}

			// add special commands
			if (specials != null) {
				foreach (var special in specials) {
					sb.Append(":");
					sb.Append(PriorityConstants.Specials[(int)special]);
				}
			}

			// add profile
			if (profile != GnuProfile.None) {
				sb.Append(":");
				sb.Append(PriorityConstants.Profiles[(int)profile]);
			}

			return sb.ToString();
		}

	}
}

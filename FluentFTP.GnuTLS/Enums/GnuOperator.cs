using System;
using System.Collections.Generic;
using System.Text;

namespace FluentFTP.GnuTLS.Enums {
	public enum GnuOperator {

		/// <summary>
		/// Includes the given `Command` into the active configuration.
		/// </summary>
		Include,

		/// <summary>
		/// Excludes the given `Command` from the active configuration.
		/// </summary>
		Exclude

	}
}

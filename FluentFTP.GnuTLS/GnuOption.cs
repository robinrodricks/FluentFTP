using FluentFTP.GnuTLS.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace FluentFTP.GnuTLS {

	/// <summary>
	/// Either includes or excludes this `Command` from the protocol security suite, depending on the `Operator` given.
	/// </summary>
	public class GnuOption {

		public GnuOperator Operator;
		public GnuCommand Command;

		public GnuOption(GnuOperator op, GnuCommand cmd) {
			Operator = op;
			Command = cmd;
		}


	}
}

using System;
using System.Collections.Generic;
using System.Text;

namespace FluentFTP.GnuTLS.Enums {

	[Flags]
	public enum GnuMessage : ushort {
		None = 0,

		InteropFunction = 1,
		InteropMsg = 1 << 1,
		Handshake = 1 << 2,
		Alert = 1 << 3,
		Read = 1 << 4,
		Write = 1 << 5,
		ClientCertificateValidation = 1 << 6,
		ShowClientCertificateInfo = 1 << 7,
		ShowClientCertificatePEM = 1 << 8,
		X509 = 1 << 9,
		RAWPK = 1 << 10,

		All = unchecked((ushort)-1),
	}

}

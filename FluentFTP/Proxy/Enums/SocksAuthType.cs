using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FluentFTP.Proxy.Enums {
	internal enum SocksAuthType {
		NoAuthRequired = 0x00,
		GSSAPI = 0x01,
		UsernamePassword = 0x02,
		NoAcceptableMethods = 0xFF
	}
}

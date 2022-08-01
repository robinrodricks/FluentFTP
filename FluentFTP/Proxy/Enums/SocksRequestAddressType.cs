using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FluentFTP.Proxy.Enums {
	internal enum SocksRequestAddressType {
		Unknown = 0x00,
		IPv4 = 0x01,
		FQDN = 0x03,
		IPv6 = 0x04
	}
}

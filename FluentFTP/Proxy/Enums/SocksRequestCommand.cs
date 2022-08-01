using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FluentFTP.Proxy.Enums {
	internal enum SocksRequestCommand : byte {
		Connect = 0x01,
		Bind = 0x02,
		UdpAssociate = 0x03
	}
}

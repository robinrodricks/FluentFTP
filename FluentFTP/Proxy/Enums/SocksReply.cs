using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FluentFTP.Proxy.Enums {
	internal enum SocksReply {
		Succeeded = 0x00,
		GeneralSOCKSServerFailure = 0x01,
		NotAllowedByRuleset = 0x02,
		NetworkUnreachable = 0x03,
		HostUnreachable = 0x04,
		ConnectionRefused = 0x05,
		TTLExpired = 0x06,
		CommandNotSupported = 0x07,
		AddressTypeNotSupported = 0x08
	}
}

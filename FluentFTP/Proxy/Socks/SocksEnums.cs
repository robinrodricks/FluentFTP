public enum SocksAuthType
{
	NoAuthRequired = 0x00,
	GSSAPI = 0x01,
	UsernamePassword = 0x02,
	NoAcceptableMethods = 0xFF
}

public enum SocksReply
{
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

internal enum SocksRequestAddressType
{
	Unknown = 0x00,
	IPv4 = 0x01,
	FQDN = 0x03,
	IPv6 = 0x04
}

internal enum SocksRequestCommand : byte
{
	Connect = 0x01,
	Bind = 0x02,
	UdpAssociate = 0x03
}

internal enum SocksVersion
{
	Four = 0x04,
	Five = 0x05
}
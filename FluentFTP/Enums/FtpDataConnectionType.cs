using System;

namespace FluentFTP {
	/// <summary>
	/// Data connection type
	/// </summary>
	public enum FtpDataConnectionType {
		/// <summary>
		/// This type of data connection attempts to use the EPSV command
		/// and if the server does not support EPSV it falls back to the
		/// PASV command before giving up unless you are connected via IPv6
		/// in which case the PASV command is not supported.
		/// </summary>
		AutoPassive,

		/// <summary>
		/// Passive data connection. EPSV is a better
		/// option if it's supported. Passive connections
		/// connect to the IP address dictated by the server
		/// which may or may not be accessible by the client
		/// for example a server behind a NAT device may
		/// give an IP address on its local network that
		/// is inaccessible to the client. Please note that IPv6
		/// does not support this type data connection. If you
		/// ask for PASV and are connected via IPv6 EPSV will
		/// automatically be used in its place.
		/// </summary>
		PASV,

		/// <summary>
		/// Same as PASV except the host supplied by the server is ignored
		/// and the data connection is made to the same address that the control
		/// connection is connected to. This is useful in scenarios where the
		/// server supplies a private/non-routable network address in the
		/// PASV response. It's functionally identical to EPSV except some
		/// servers may not implement the EPSV command. Please note that IPv6
		/// does not support this type data connection. If you
		/// ask for PASV and are connected via IPv6 EPSV will
		/// automatically be used in its place.
		/// </summary>
		PASVEX,

		/// <summary>
		/// Extended passive data connection, recommended. Works
		/// the same as a PASV connection except the server
		/// does not dictate an IP address to connect to, instead
		/// the passive connection goes to the same address used
		/// in the control connection. This type of data connection
		/// supports IPv4 and IPv6.
		/// </summary>
		EPSV,

		/// <summary>
		/// This type of data connection attempts to use the EPRT command
		/// and if the server does not support EPRT it falls back to the
		/// PORT command before giving up unless you are connected via IPv6
		/// in which case the PORT command is not supported.
		/// </summary>
		AutoActive,

		/// <summary>
		/// Active data connection, not recommended unless
		/// you have a specific reason for using this type.
		/// Creates a listening socket on the client which
		/// requires firewall exceptions on the client system
		/// as well as client network when connecting to a
		/// server outside of the client's network. In addition
		/// the IP address of the interface used to connect to the
		/// server is the address the server is told to connect to
		/// which, if behind a NAT device, may be inaccessible to
		/// the server. This type of data connection is not supported
		/// by IPv6. If you specify PORT and are connected via IPv6
		/// EPRT will automatically be used instead.
		/// </summary>
		PORT,

		/// <summary>
		/// Extended active data connection, not recommended
		/// unless you have a specific reason for using this
		/// type. Creates a listening socket on the client
		/// which requires firewall exceptions on the client
		/// as well as client network when connecting to a 
		/// server outside of the client's network. The server
		/// connects to the IP address it sees the client coming
		/// from. This type of data connection supports IPv4 and IPv6.
		/// </summary>
		EPRT
	}
}
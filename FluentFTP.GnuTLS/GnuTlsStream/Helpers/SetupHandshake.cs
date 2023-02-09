using System;
using System.IO;
using FluentFTP.GnuTLS.Core;

namespace FluentFTP.GnuTLS {

	internal partial class GnuTlsInternalStream : Stream, IDisposable {

		private void SetupHandshake() {

			// Stangely, one reads that this also somehow influences maximum TLS session time
			Core.GnuTls.DbSetCacheExpiration(sess, 100000000);

			// Handle the different ways Config could pass a priority string to here
			if (priority == string.Empty) {
				// None given, so use GnuTLS default
				Core.GnuTls.SetDefaultPriority(sess);
			}
			else if (priority.StartsWith("+") || priority.StartsWith("-")) {
				// Add or subtract from default
				Core.GnuTls.SetDefaultPriorityAppend(sess, priority);
			}
			else {
				// Use verbatim
				Core.GnuTls.PrioritySetDirect(sess, priority);
			}

			// Bits for Diffie-Hellman prime
			Core.GnuTls.DhSetPrimeBits(sess, 1024);

			// Allocate and link credential object
			Core.GnuTls.CredentialsSet(cred, sess);

			// Application Layer Protocol Negotiation (ALPN)
			// (alway AFTER credential allocation and setup
			if (!string.IsNullOrEmpty(alpn)) {
				Core.GnuTls.AlpnSetProtocols(sess, alpn);
			}

			// Tell GnuTLS how to send and receive: Use already open socket
			Core.GnuTls.TransportSetInt(sess, (int)socket.Handle);

			// Set the timeout for the handshake process
			Core.GnuTls.HandshakeSetTimeout(sess, (uint)timeout);

			// Any client certificate for presentation to server?
			SetupClientCertificates();

		}

	}
}

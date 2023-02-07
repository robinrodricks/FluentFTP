using FluentFTP.GnuTLS.Core;

using System;
using System.IO;

namespace FluentFTP.GnuTLS {

	internal partial class GnuTlsStream : Stream, IDisposable {

		private void SetupHandshake() {

			// Stangely, one reads that this also somehow influences maximum TLS session time
			GnuTls.DbSetCacheExpiration(sess, 100000000);

			// Handle the different ways Config could pass a priority string to here
			if (priority == string.Empty) {
				// None given, so use GnuTLS default
				GnuTls.SetDefaultPriority(sess);
			}
			else if (priority.StartsWith("+") || priority.StartsWith("-")) {
				// Add or subtract from default
				GnuTls.SetDefaultPriorityAppend(sess, priority);
			}
			else {
				// Use verbatim
				GnuTls.PrioritySetDirect(sess, priority);
			}

			// Bits for Diffie-Hellman prime
			GnuTls.DhSetPrimeBits(sess, 1024);

			// Allocate and link credential object
			GnuTls.CredentialsSet(cred, sess);

			// Application Layer Protocol Negotiation (ALPN)
			// (alway AFTER credential allocation and setup
			if (!string.IsNullOrEmpty(alpn)) {
				GnuTls.AlpnSetProtocols(sess, alpn);
			}

			// Tell GnuTLS how to send and receive: Use already open socket
			GnuTls.TransportSetInt(sess, (int)socket.Handle);

			// Set the timeout for the handshake process
			GnuTls.HandshakeSetTimeout(sess, (uint)timeout);

			// Any client certificate for presentation to server?
			SetupClientCertificates();

		}

	}
}

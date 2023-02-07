using FluentFTP.GnuTLS.Core;

using System;
using System.IO;

namespace FluentFTP.GnuTLS {

	internal partial class GnuTlsStream : Stream, IDisposable {

		internal static void HandshakeHook(IntPtr session, uint description, uint post, uint incoming) {

			if (session == null) {
				return;
			}

			string action;

			// incoming  post
			// ==============
			//    1       0    received
			//    1       1    processed
			//
			//    0       0    about to send
			//    0       1    sent
			//

			if (incoming == 0) {
				// send
				action = post == 0 ? "about to send" : "sent";
			}
			else {
				// receive
				action = post == 0 ? "received" : "processed";
			}

			Logging.LogGnuFunc(LogDebugInformationMessagesT.Handshake, "Handshake " + action + " " + Enum.GetName(typeof(HandshakeDescriptionT), description));

			// Check for certain action/description combinations

			if (incoming != 0 && post != 0) { // receive processed") 

				//
				// TLS1.2 : If the session ticket extension is active, a session ticke may appear
				//          ProFTPd server will do this, for example
				//          One can forbid this by setting GNUTLS_NO_TICKETS_TLS12 on the init flags
				//          or by using %NO_TICKETS_TLS12 in the priority string in config
				// TLS1.3 : A session ticket appeared
				//
				if (description == (uint)HandshakeDescriptionT.GNUTLS_HANDSHAKE_NEW_SESSION_TICKET) {
					SessionFlagsT flags = Native.SessionGetFlags(session);
					if (flags.HasFlag(SessionFlagsT.GNUTLS_SFLAGS_SESSION_TICKET)) {
						Native.SessionGetData2(session, ref resumeDataTLS);
						Logging.LogGnuFunc(LogDebugInformationMessagesT.Handshake, "Retrieved session data with new session ticket");

						Native.SessionSetData(session, resumeDataTLS);
						//Native.GnuFree(resumeDataTLS.ptr);
					}
				}

			}

		}


	}
}

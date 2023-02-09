using System;

namespace FluentFTP.GnuTLS.Core {
	internal abstract class Session : IDisposable {

		public IntPtr ptr;

		protected Session(InitFlagsT flags) {
			string gcm = GnuUtils.GetCurrentMethod() + ":Session";
			Logging.LogGnuFunc(gcm);

			_ = GnuUtils.Check(gcm, GnuTls.gnutls_init(ref ptr, flags));
		}

		public void Dispose() {
			if (ptr != IntPtr.Zero) {
				string gcm = GnuUtils.GetCurrentMethod() + ":Session";
				Logging.LogGnuFunc(gcm);

				GnuTls.gnutls_deinit(ptr);
				ptr = IntPtr.Zero;
			}
		}
	}

	internal class ClientSession : Session {

		public ClientSession() : base(InitFlagsT.GNUTLS_CLIENT) {
		}
		public ClientSession(InitFlagsT flags) : base(InitFlagsT.GNUTLS_CLIENT | flags & ~InitFlagsT.GNUTLS_SERVER) {
		}
	}

	internal class ServerSession : Session {

		public ServerSession() : base(InitFlagsT.GNUTLS_SERVER) {
		}
		public ServerSession(InitFlagsT flags) : base(InitFlagsT.GNUTLS_SERVER | flags & ~InitFlagsT.GNUTLS_CLIENT) {
		}
	}
}

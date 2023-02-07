using System;

namespace FluentFTP.GnuTLS.Core {
	public abstract class Session : IDisposable {

		public IntPtr ptr;

		protected Session(InitFlagsT flags) {
			string gcm = Utils.GetCurrentMethod() + ":Session";
			Logging.LogGnuFunc(gcm);

			_ = Utils.Check(gcm, GnuTls.gnutls_init(ref ptr, flags));
		}

		public void Dispose() {
			if (this.ptr != IntPtr.Zero) {
				string gcm = Utils.GetCurrentMethod() + ":Session";
				Logging.LogGnuFunc(gcm);

				GnuTls.gnutls_deinit(ptr);
				this.ptr = IntPtr.Zero;
			}
		}
	}

	public class ClientSession : Session {

		public ClientSession() : base(InitFlagsT.GNUTLS_CLIENT) {
		}
		public ClientSession(InitFlagsT flags) : base(InitFlagsT.GNUTLS_CLIENT | (flags & ~InitFlagsT.GNUTLS_SERVER)) {
		}
	}

	public class ServerSession : Session {

		public ServerSession() : base(InitFlagsT.GNUTLS_SERVER) {
		}
		public ServerSession(InitFlagsT flags) : base(InitFlagsT.GNUTLS_SERVER | (flags & ~InitFlagsT.GNUTLS_CLIENT)) {
		}
	}
}

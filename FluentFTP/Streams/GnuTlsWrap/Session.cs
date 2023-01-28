using System;
using System.Runtime.InteropServices;

namespace GnuTlsWrap {
	public abstract class Session : IDisposable {

		public IntPtr ptr;

		protected Session(InitFlagsT flags) {
			string gcm = Utils.GetCurrentMethod() + ":Session";
			Logging.LogGnuFunc(gcm);

			_ = Utils.Check(gcm, gnutls_init(ref ptr, flags));
		}

		public void Dispose() {
			if (this.ptr != IntPtr.Zero) {
				string gcm = Utils.GetCurrentMethod() + ":Session";
				Logging.LogGnuFunc(gcm);

				gnutls_deinit(ptr);
				this.ptr = IntPtr.Zero;
			}
		}

		// G N U T L S API calls for session init / deinit

		// int gnutls_init (gnutls_session_t * session, unsigned int flags)
		[DllImport("Streams/GnuTlsWrap/Libs/libgnutls-30.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl, EntryPoint = "gnutls_init")]
		private static extern int gnutls_init(ref IntPtr session, InitFlagsT flags);

		// void gnutls_deinit (gnutls_session_t session)
		[DllImport("Streams/GnuTlsWrap/Libs/libgnutls-30.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl, EntryPoint = "gnutls_deinit")]
		private static extern void gnutls_deinit(IntPtr session);
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

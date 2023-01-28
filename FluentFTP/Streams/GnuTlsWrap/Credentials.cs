using System;
using System.Runtime.InteropServices;
using static System.Collections.Specialized.BitVector32;

namespace GnuTlsWrap {
	public abstract class Credentials : IDisposable {

		public IntPtr ptr;

		public CredentialsTypeT credentialsType;

		protected Credentials(CredentialsTypeT type) {
			credentialsType = type;
		}

		public void Dispose() {
		}
	}

	public class CertificateCredentials : Credentials, IDisposable {

		public CertificateCredentials() : base(CredentialsTypeT.GNUTLS_CRD_CERTIFICATE) {
			string gcm = Utils.GetCurrentMethod() + ":CertificateCredentials";
			Logging.LogGnuFunc(gcm);

			string errText = "CertificateCredentials()";
			_ = Utils.Check(errText + " : certificate_allocate_credentials", gnutls_certificate_allocate_credentials(ref this.ptr));
		}

		public void Dispose() {
			if (this.ptr != IntPtr.Zero) {
				string gcm = Utils.GetCurrentMethod() + ":CertificateCredentials";
				Logging.LogGnuFunc(gcm);

				gnutls_certificate_free_credentials(this.ptr);
				this.ptr = IntPtr.Zero;
			}
		}

		// G N U T L S API calls for certificate credentials init / deinit

		// int gnutls_certificate_allocate_credentials (gnutls_certificate_credentials_t * res)
		[DllImport("Streams/GnuTlsWrap/Libs/libgnutls-30.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl, EntryPoint = "gnutls_certificate_allocate_credentials")]
		private static extern int gnutls_certificate_allocate_credentials(ref IntPtr res);

		// void gnutls_certificate_free_credentials(gnutls_certificate_credentials_t sc)
		[DllImport("Streams/GnuTlsWrap/Libs/libgnutls-30.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl, EntryPoint = "gnutls_certificate_free_credentials")]
		private static extern void gnutls_certificate_free_credentials(IntPtr sc);
	}
}

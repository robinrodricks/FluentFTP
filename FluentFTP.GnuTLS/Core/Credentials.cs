using System;

namespace FluentFTP.GnuTLS.Core {
	internal abstract class Credentials : IDisposable {

		public IntPtr ptr;

		public CredentialsTypeT credentialsType;

		protected Credentials(CredentialsTypeT type) {
			credentialsType = type;
		}

		public void Dispose() {
		}
	}

	internal class CertificateCredentials : Credentials, IDisposable {

		public CertificateCredentials() : base(CredentialsTypeT.GNUTLS_CRD_CERTIFICATE) {
			string gcm = GnuUtils.GetCurrentMethod() + ":CertificateCredentials";
			Logging.LogGnuFunc(gcm);

			string errText = "CertificateCredentials()";
			_ = GnuUtils.Check(errText + " : certificate_allocate_credentials", GnuTls.gnutls_certificate_allocate_credentials(ref ptr));
		}

		public void Dispose() {
			if (ptr != IntPtr.Zero) {
				string gcm = GnuUtils.GetCurrentMethod() + ":CertificateCredentials";
				Logging.LogGnuFunc(gcm);

				GnuTls.gnutls_certificate_free_credentials(ptr);
				ptr = IntPtr.Zero;
			}
		}
	}
}

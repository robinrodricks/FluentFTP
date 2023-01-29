using System;

namespace FluentFTP.GnuTLS.Core {
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
			_ = Utils.Check(errText + " : certificate_allocate_credentials", Native.gnutls_certificate_allocate_credentials(ref this.ptr));
		}

		public void Dispose() {
			if (this.ptr != IntPtr.Zero) {
				string gcm = Utils.GetCurrentMethod() + ":CertificateCredentials";
				Logging.LogGnuFunc(gcm);

				Native.gnutls_certificate_free_credentials(this.ptr);
				this.ptr = IntPtr.Zero;
			}
		}
	}
}

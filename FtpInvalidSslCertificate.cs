using System;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.FtpClient {
	/// <summary>
	/// Invalid SSL certification event arguments
	/// </summary>
	public class InvalidCertificateInfo : EventArgs {
		bool _ignoreCertificate = false;
		/// <summary>
		/// Gets or sets a value indicating if the invalid certificate should be ignored.
		/// </summary>
		public bool Ignore {
			get { return _ignoreCertificate; }
			set { _ignoreCertificate = value; }
		}

		SslPolicyErrors _errs = SslPolicyErrors.None;
		/// <summary>
		/// The problems encountered with the certificate
		/// </summary>
		public SslPolicyErrors SslPolicyErrors {
			get { return _errs; }
			private set { _errs = value; }
		}

		X509Certificate _cert = null;
		/// <summary>
		/// The SSL certificate that failed verification
		/// </summary>
		public X509Certificate SslCertificate {
			get { return _cert; }
			private set { _cert = value; }
		}

		/// <summary>
		/// Iniatlizes the object
		/// </summary>
		/// <param name="c">The ftp channel this object is associated with. Could
		/// be FtpCommandChannel or FtpDataChannel</param>
		public InvalidCertificateInfo(FtpChannel c) {
			this.SslPolicyErrors = c.SslPolicyErrors;
			this.SslCertificate = c.SslCertificate;
		}
	}
}

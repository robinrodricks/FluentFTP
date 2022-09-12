using System;
using System.IO;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;

#if NET45
using System.Threading.Tasks;

#endif

namespace FluentFTP {
	/// <summary>
	/// Event is fired when a SSL certificate needs to be validated
	/// </summary>
	/// <param name="control">The control connection that triggered the event</param>
	/// <param name="e">Event args</param>
	public delegate void FtpSslValidation(FtpClient control, FtpSslValidationEventArgs e);

	/// <summary>
	/// Event args for the FtpSslValidationError delegate
	/// </summary>
	public class FtpSslValidationEventArgs : EventArgs {
		private X509Certificate m_certificate = null;

		/// <summary>
		/// The certificate to be validated
		/// </summary>
		public X509Certificate Certificate {
			get => m_certificate;
			set => m_certificate = value;
		}

		private X509Chain m_chain = null;

		/// <summary>
		/// The certificate chain
		/// </summary>
		public X509Chain Chain {
			get => m_chain;
			set => m_chain = value;
		}

		private SslPolicyErrors m_policyErrors = SslPolicyErrors.None;

		/// <summary>
		/// Validation errors, if any.
		/// </summary>
		public SslPolicyErrors PolicyErrors {
			get => m_policyErrors;
			set => m_policyErrors = value;
		}

		private bool m_accept = false;

		/// <summary>
		/// Gets or sets a value indicating if this certificate should be accepted. The default
		/// value is false. If the certificate is not accepted, an AuthenticationException will
		/// be thrown.
		/// </summary>
		public bool Accept {
			get => m_accept;
			set => m_accept = value;
		}
	}

}
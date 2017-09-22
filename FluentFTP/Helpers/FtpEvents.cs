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
	/// Event fired if a bad SSL certificate is encountered. This even is used internally; if you
	/// don't have a specific reason for using it you are probably looking for FtpSslValidation.
	/// </summary>
	/// <param name="stream"></param>
	/// <param name="e"></param>
	public delegate void FtpSocketStreamSslValidation(FtpSocketStream stream, FtpSslValidationEventArgs e);

	/// <summary>
	/// Event args for the FtpSslValidationError delegate
	/// </summary>
	public class FtpSslValidationEventArgs : EventArgs {
		X509Certificate m_certificate = null;
		/// <summary>
		/// The certificate to be validated
		/// </summary>
		public X509Certificate Certificate {
			get {
				return m_certificate;
			}
			set {
				m_certificate = value;
			}
		}

		X509Chain m_chain = null;
		/// <summary>
		/// The certificate chain
		/// </summary>
		public X509Chain Chain {
			get {
				return m_chain;
			}
			set {
				m_chain = value;
			}
		}

		SslPolicyErrors m_policyErrors = SslPolicyErrors.None;
		/// <summary>
		/// Validation errors, if any.
		/// </summary>
		public SslPolicyErrors PolicyErrors {
			get {
				return m_policyErrors;
			}
			set {
				m_policyErrors = value;
			}
		}

		bool m_accept = false;
		/// <summary>
		/// Gets or sets a value indicating if this certificate should be accepted. The default
		/// value is false. If the certificate is not accepted, an AuthenticationException will
		/// be thrown.
		/// </summary>
		public bool Accept {
			get {
				return m_accept;
			}
			set {
				m_accept = value;
			}
		}
	}

}
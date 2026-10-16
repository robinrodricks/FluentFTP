using System.Linq;

namespace FluentFTP.Client.BaseClient {

	public partial class BaseFtpClient {

		/// <summary>
		/// Catches the socket stream ssl validation event and fires the event handlers
		/// attached to this object for validating SSL certificates
		/// </summary>
		/// <param name="stream">The stream that fired the event</param>
		/// <param name="e">The event args used to validate the certificate</param>
		protected void FireValidateCertficate(FtpSocketStream stream, FtpSslValidationEventArgs e) {
			OnValidateCertficate(e);

			// remember the accepted control certificate so data connections can be held to it
			m_controlCertificate = e.Accept ? e.Certificate?.GetRawCertData() : null;
		}

		/// <summary>
		/// Validates the certificate presented on a data connection. The certificate accepted on the
		/// control connection is accepted again, any other certificate must pass validation on its own.
		/// </summary>
		/// <param name="e">The event args used to validate the certificate</param>
		internal void ValidateDataCertificate(FtpSslValidationEventArgs e) {
			var certificate = e.Certificate?.GetRawCertData();
			if (m_controlCertificate != null && certificate != null && certificate.SequenceEqual(m_controlCertificate)) {
				e.Accept = true;
				return;
			}

			OnValidateCertficate(e);
		}

		/// <summary>
		/// Fires the SSL validation event
		/// </summary>
		/// <param name="e">Event Args</param>
		protected void OnValidateCertficate(FtpSslValidationEventArgs e) {

			// automatically validate if ValidateAnyCertificate is set
			if (Config.ValidateAnyCertificate) {
				e.Accept = true;
				return;
			}

			// fallback to manual validation using the ValidateCertificate event
			m_ValidateCertificate?.Invoke(this, e);

		}

	}
}

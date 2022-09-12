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
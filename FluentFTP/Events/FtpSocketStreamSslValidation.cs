namespace FluentFTP {

	/// <summary>
	/// Event fired if a bad SSL certificate is encountered. This even is used internally; if you
	/// don't have a specific reason for using it you are probably looking for FtpSslValidation.
	/// </summary>
	/// <param name="stream"></param>
	/// <param name="e"></param>
	public delegate void FtpSocketStreamSslValidation(FtpSocketStream stream, FtpSslValidationEventArgs e);

}
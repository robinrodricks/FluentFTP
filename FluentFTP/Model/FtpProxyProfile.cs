using System.Net;

namespace FluentFTP {

	/// <summary>
	/// Connection profile for a proxy connection.
	/// </summary>
	public class FtpProxyProfile {

		/// <summary> 
		/// Proxy server host name. Mandatory.
		/// </summary>
		public string ProxyHost { get; set; }

		/// <summary> 
		/// Proxy server port. Mandatory.
		/// </summary>
		public int ProxyPort { get; set; }

		/// <summary> 
		/// Proxy server login credentials. Mandatory if your proxy needs authentication, leave it blank otherwise.
		/// </summary>
		public NetworkCredential ProxyCredentials { get; set; }

		/// <summary> 
		/// FTP server host name. Optional. You can either set it here or set `ftpClient.Host` later on.
		/// </summary>
		public string FtpHost { get; set; }

		/// <summary> 
		/// FTP server port. Optional. You can either set it here or set `ftpClient.Port` later on.
		/// </summary>
		public int FtpPort { get; set; }

		/// <summary> 
		/// FTP server login credentials. Optional. You can either set it here or set `ftpClient.Credentials` later on.
		/// </summary>
		public NetworkCredential FtpCredentials { get; set; }


	}
}
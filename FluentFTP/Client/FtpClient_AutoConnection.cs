using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using FluentFTP.Proxy;
using SysSslProtocols = System.Security.Authentication.SslProtocols;
using FluentFTP.Servers;
#if !CORE
using System.Web;
#endif
#if (CORE || NETFX)
using System.Threading;
#endif
#if ASYNC
using System.Threading.Tasks;

#endif

namespace FluentFTP {
	public partial class FtpClient : IDisposable {


		#region Auto Detect

		private static List<FtpEncryptionMode> autoConnectEncryption = new List<FtpEncryptionMode> {
			FtpEncryptionMode.None, FtpEncryptionMode.Implicit, FtpEncryptionMode.Explicit
		};

		private static List<SysSslProtocols> autoConnectProtocols = new List<SysSslProtocols> {
			SysSslProtocols.None,
#if ASYNC
			SysSslProtocols.Tls12, SysSslProtocols.Tls11, 
#endif
#if !ASYNC
			SysSslProtocols.Tls,
#endif
			SysSslProtocols.Ssl3, SysSslProtocols.Ssl2,
#if !CORE
			SysSslProtocols.Default,
#endif
		};

		private static List<FtpDataConnectionType> autoConnectData = new List<FtpDataConnectionType> {
			FtpDataConnectionType.PASV, FtpDataConnectionType.EPSV, FtpDataConnectionType.PORT, FtpDataConnectionType.EPRT, FtpDataConnectionType.PASVEX
		};

		private static List<Encoding> autoConnectEncoding = new List<Encoding> {
			Encoding.UTF8, Encoding.ASCII
		};

		/// <summary>
		/// Automatic FTP and FTPS connection negotiation.
		/// This method tries every possible combination of the FTP connection properties, and returns the list of successful connection profiles.
		/// You can configure it to stop after finding the first successful profile, or to collect all successful profiles.
		/// You can then generate code for the profile using the FtpProfile.ToCode method.
		/// If no successful profiles are found, a blank list is returned.
		/// </summary>
		/// <param name="firstOnly">Find all successful profiles (false) or stop after finding the first successful profile (true)?</param>
		/// <returns></returns>
		public List<FtpProfile> AutoDetect(bool firstOnly = true) {
			var results = new List<FtpProfile>();

#if !CORE14
			lock (m_lock) {
#endif
				LogFunc(nameof(AutoDetect), new object[] { firstOnly });

				if (IsDisposed) {
					throw new ObjectDisposedException("This FtpClient object has been disposed. It is no longer accessible.");
				}

				if (Host == null) {
					throw new FtpException("No host has been specified. Please set the 'Host' property before trying to auto connect.");
				}

				if (Credentials == null) {
					throw new FtpException("No username and password has been specified. Please set the 'Credentials' property before trying to auto connect.");
				}

				// get known working connection profile based on the host (if any)
				var knownProfile = FtpServerSpecificHandler.GetWorkingProfileFromHost(Host, Port);
				if (knownProfile != null) {
					results.Add(knownProfile);
					return results;
				}

				// try each encoding
				encoding:
				foreach (var encoding in autoConnectEncoding) {
					// try each encryption mode
					encryption:
					foreach (var encryption in autoConnectEncryption) {
						// try each SSL protocol
						protocol:
						foreach (var protocol in autoConnectProtocols) {
							// skip secure protocols if testing plain FTP
							if (encryption == FtpEncryptionMode.None && protocol != SysSslProtocols.None) {
								continue;
							}

							// try each data connection type
							dataType:
							foreach (var dataType in autoConnectData) {
								// clone this connection
								var conn = CloneConnection();

								// set basic props
								conn.Host = Host;
								conn.Port = Port;
								conn.Credentials = Credentials;

								// set rolled props
								conn.EncryptionMode = encryption;
								conn.SslProtocols = protocol;
								conn.DataConnectionType = dataType;
								conn.Encoding = encoding;

								// try to connect
								var connected = false;
								try {
									conn.Connect();
									connected = true;
									conn.Dispose();
								}
								catch (Exception ex) {
									conn.Dispose();
								}

								// if it worked, add the profile
								if (connected) {
									results.Add(new FtpProfile {
										Host = Host,
										Credentials = Credentials,
										Encryption = encryption,
										Protocols = protocol,
										DataConnection = dataType,
										Encoding = encoding
									});

									// stop if only 1 wanted
									if (firstOnly) {
										return results;
									}
								}
							}
						}
					}
				}


#if !CORE14
			}
#endif

			return results;
		}

		#endregion

		#region Auto Connect

		/// <summary>
		/// Connect to the given server profile.
		/// </summary>
		public void Connect(FtpProfile profile) {

			// copy over the profile properties to this instance
			LoadProfile(profile);

			// begin connection
			Connect();
		}

#if ASYNC
		/// <summary>
		/// Connect to the given server profile.
		/// </summary>
		public async Task ConnectAsync(FtpProfile profile, CancellationToken token = default(CancellationToken)) {

			// copy over the profile properties to this instance
			LoadProfile(profile);

			// begin connection
			await ConnectAsync(token);
		}
#endif

		private void LoadProfile(FtpProfile profile) {

			// verify args
			if (profile == null) {
				throw new ArgumentException("Required parameter is null or blank.", "profile");
			}
			if (profile.Host.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "profile.Host");
			}
			if (profile.Credentials == null) {
				throw new ArgumentException("Required parameter is null.", "profile.Credentials");
			}
			if (profile.Encoding == null) {
				throw new ArgumentException("Required parameter is null.", "profile.Encoding");
			}

			// copy over the profile properties to this instance
			Host = profile.Host;
			Credentials = profile.Credentials;
			EncryptionMode = profile.Encryption;
			SslProtocols = profile.Protocols;
			DataConnectionType = profile.DataConnection;
			Encoding = profile.Encoding;
			if (profile.Timeout != 0) {
				ConnectTimeout = profile.Timeout;
				ReadTimeout = profile.Timeout;
				DataConnectionConnectTimeout = profile.Timeout;
				DataConnectionReadTimeout = profile.Timeout;
			}
			if (SocketPollInterval != 0) {
				SocketPollInterval = profile.SocketPollInterval;
			}
			if (RetryAttempts != 0) {
				RetryAttempts = profile.RetryAttempts;
			}
		}

		/// <summary>
		/// Automatic FTP and FTPS connection negotiation.
		/// This method tries every possible combination of the FTP connection properties, and connects to the first successful profile.
		/// Returns the FtpProfile if the connection succeeded, or null if it failed.
		/// </summary>
		public FtpProfile AutoConnect() {
			LogFunc(nameof(AutoConnect));

			// detect the first available connection profile
			var results = AutoDetect();
			if (results.Count > 0) {
				var profile = results[0];

				// if we are using SSL, set a basic server acceptance function
				if (profile.Encryption != FtpEncryptionMode.None) {
					ValidateCertificate += new FtpSslValidation(delegate (FtpClient c, FtpSslValidationEventArgs e) {
						if (e.PolicyErrors != System.Net.Security.SslPolicyErrors.None) {
							e.Accept = false;
						}
						else {
							e.Accept = true;
						}
					});
				}

				// connect to the first found profile
				Connect(profile);

				// return the working profile
				return profile;
			}

			return null;
		}

#if ASYNC
		/// <summary>
		/// Automatic FTP and FTPS connection negotiation.
		/// This method tries every possible combination of the FTP connection properties, and connects to the first successful profile.
		/// Returns the FtpProfile if the connection succeeded, or null if it failed.
		/// </summary>
		public async Task<FtpProfile> AutoConnectAsync(CancellationToken token = default(CancellationToken)) {
			LogFunc(nameof(AutoConnectAsync));

			// detect the first available connection profile
			var results = AutoDetect();
			if (results.Count > 0) {
				var profile = results[0];

				// if we are using SSL, set a basic server acceptance function
				if (profile.Encryption != FtpEncryptionMode.None) {
					ValidateCertificate += new FtpSslValidation(delegate (FtpClient c, FtpSslValidationEventArgs e) {
						if (e.PolicyErrors != System.Net.Security.SslPolicyErrors.None) {
							e.Accept = false;
						}
						else {
							e.Accept = true;
						}
					});
				}

				// connect to the first found profile
				await ConnectAsync(profile, token);

				// return the working profile
				return profile;
			}

			return null;
		}
#endif

		#endregion


	}
}

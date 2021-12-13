using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using FluentFTP.Proxy;
using SysSslProtocols = System.Security.Authentication.SslProtocols;
using FluentFTP.Servers;
using FluentFTP.Helpers;
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
			FtpEncryptionMode.Auto, FtpEncryptionMode.None, FtpEncryptionMode.Implicit, 
		};

		private static List<SysSslProtocols> autoConnectProtocols = new List<SysSslProtocols> {
			//SysSslProtocols.None,
#if ASYNC
			SysSslProtocols.Tls12 | SysSslProtocols.Tls11, 
#endif
#if !ASYNC
			SysSslProtocols.Tls,
#endif
#if !CORE
			SysSslProtocols.Default,
#endif
		};

		private static List<FtpDataConnectionType> autoConnectData = new List<FtpDataConnectionType> {
			FtpDataConnectionType.PASV, FtpDataConnectionType.EPSV, FtpDataConnectionType.PORT, FtpDataConnectionType.EPRT, FtpDataConnectionType.PASVEX
		};

		/// <summary>
		/// Automatic FTP and FTPS connection negotiation.
		/// This method tries every possible combination of the FTP connection properties, and returns the list of successful connection profiles.
		/// You can configure it to stop after finding the first successful profile, or to collect all successful profiles.
		/// You can then generate code for the profile using the FtpProfile.ToCode method.
		/// If no successful profiles are found, a blank list is returned.
		/// </summary>
		/// <param name="firstOnly">Find all successful profiles (false) or stop after finding the first successful profile (true)</param>
		/// <param name="cloneConnection">Use a new cloned FtpClient for testing connection profiles (true) or use the source FtpClient (false)</param>
		/// <returns></returns>
		public List<FtpProfile> AutoDetect(bool firstOnly = true, bool cloneConnection = true) {
			var results = new List<FtpProfile>();

#if !CORE14
			lock (m_lock) {
#endif
				LogFunc(nameof(AutoDetect), new object[] { firstOnly, cloneConnection });
				ValidateAutoDetect();

				// get known working connection profile based on the host (if any)
				var knownProfile = FtpServerSpecificHandler.GetWorkingProfileFromHost(Host, Port);
				if (knownProfile != null) {
					results.Add(knownProfile);
					return results;
				}

				var blacklistedEncryptions = new List<FtpEncryptionMode>();
				bool resetPort = m_port == 0;

				// clone this connection or use this connection
				var conn = cloneConnection ? CloneConnection() : this;

				// copy basic props if cloned connection
				if (cloneConnection) {
					conn.Host = this.Host;
					conn.Port = this.Port;
					conn.Credentials = this.Credentials;
				}

				// disconnect if already connected
				if (conn.IsConnected) {
					conn.Disconnect();
				}

				// try each encryption mode
				foreach (var encryption in autoConnectEncryption) {

					// skip if FTPS was tried and failed
					if (blacklistedEncryptions.Contains(encryption)) {
						continue;
					}

					// try each SSL protocol
					foreach (var protocol in autoConnectProtocols) {

						// skip plain protocols if testing secure FTPS -- disabled because 'None' is recommended by Microsoft
						/*if (encryption != FtpEncryptionMode.None && protocol == SysSslProtocols.None) {
							continue;
						}*/

						// skip secure protocols if testing plain FTP
						if (encryption == FtpEncryptionMode.None && protocol != SysSslProtocols.None) {
							continue;
						}

						// reset port so it auto computes based on encryption type
						if (resetPort) {
							conn.Port = 0;
						}

						// set rolled props
						conn.EncryptionMode = encryption;
						conn.SslProtocols = protocol;
						conn.DataConnectionType = FtpDataConnectionType.AutoPassive;
						conn.Encoding = Encoding.UTF8;

						// try to connect
						var connected = false;
						var dataConn = FtpDataConnectionType.PASV;
						try {
							conn.Connect();
							connected = true;

							// get data connection once connected
							dataConn = AutoDataConnection(conn);

							// if non-cloned connection, we want to remain connected if it works
							if (cloneConnection) {
								conn.Disconnect();
							}
						}
						catch (Exception ex) {

#if !CORE14
							if (ex is AuthenticationException)
							{
								throw new FtpInvalidCertificateException();
							}
#endif

							// since the connection failed, disconnect and retry
							conn.Disconnect();

							// if server does not support FTPS no point trying encryption again
							if (IsFtpsFailure(blacklistedEncryptions, encryption, ex)) {
								goto SkipEncryptionMode;
							}

							// catch error "no such host is known" and hard abort
							if (IsPermanantConnectionFailure(ex)) {
								if (cloneConnection) {
									conn.Dispose();
								}

								// rethrow permanant failures so caller can be made aware of it
								throw;
							}
						}

						// if it worked
						if (connected) {

							// if connected by explicit FTPS failed, no point trying encryption again
							if (IsConnectedButFtpsFailure(blacklistedEncryptions, encryption, conn._ConnectionFTPSFailure)) {
							}

							results.Add(new FtpProfile {
								Host = Host,
								Credentials = Credentials,
								Encryption = blacklistedEncryptions.Contains(encryption) ? FtpEncryptionMode.None : encryption,
								Protocols = protocol,
								DataConnection = dataConn,
								Encoding = Encoding.UTF8,
								EncodingVerified = conn._ConnectionUTF8Success || conn.HasFeature(FtpCapability.UTF8)
							});

							// stop if only 1 wanted
							if (firstOnly) {
								goto Exit;
							}

						}
					}

					SkipEncryptionMode:
					var skip = true;

				}


				Exit:
				if (cloneConnection) {
					conn.Dispose();
				}
#if !CORE14
			}
#endif
			return results;
		}

#if ASYNC
		/// <summary>
		/// Automatic FTP and FTPS connection negotiation.
		/// This method tries every possible combination of the FTP connection properties, and returns the list of successful connection profiles.
		/// You can configure it to stop after finding the first successful profile, or to collect all successful profiles.
		/// You can then generate code for the profile using the FtpProfile.ToCode method.
		/// If no successful profiles are found, a blank list is returned.
		/// </summary>
		/// <param name="firstOnly">Find all successful profiles (false) or stop after finding the first successful profile (true)</param>
		/// <param name="cloneConnection">Use a new cloned FtpClient for testing connection profiles (true) or use the source FtpClient (false)</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns></returns>
		public async Task<List<FtpProfile>> AutoDetectAsync(bool firstOnly, bool cloneConnection = true, CancellationToken token = default(CancellationToken)) {
			var results = new List<FtpProfile>();

			LogFunc(nameof(AutoDetectAsync), new object[] { firstOnly, cloneConnection });
			ValidateAutoDetect();

			// get known working connection profile based on the host (if any)
			var knownProfile = FtpServerSpecificHandler.GetWorkingProfileFromHost(Host, Port);
			if (knownProfile != null) {
				results.Add(knownProfile);
				return results;
			}

			var blacklistedEncryptions = new List<FtpEncryptionMode>();
			bool resetPort = m_port == 0;

			// clone this connection or use this connection
			var conn = cloneConnection ? CloneConnection() : this;

			// copy basic props if cloned connection
			if (cloneConnection) {
				conn.Host = this.Host;
				conn.Port = this.Port;
				conn.Credentials = this.Credentials;
			}

			// disconnect if already connected
			if (conn.IsConnected) {
				await conn.DisconnectAsync(token);
			}

			// try each encryption mode
			foreach (var encryption in autoConnectEncryption) {

				// skip if FTPS was tried and failed
				if (blacklistedEncryptions.Contains(encryption)) {
					continue;
				}

				// try each SSL protocol
				foreach (var protocol in autoConnectProtocols) {

					// skip plain protocols if testing secure FTPS -- disabled because 'None' is recommended by Microsoft
					/*if (encryption != FtpEncryptionMode.None && protocol == SysSslProtocols.None) {
						continue;
					}*/

					// skip secure protocols if testing plain FTP
					if (encryption == FtpEncryptionMode.None && protocol != SysSslProtocols.None) {
						continue;
					}

					// reset port so it auto computes based on encryption type
					if (resetPort) {
						conn.Port = 0;
					}

					// set rolled props
					conn.EncryptionMode = encryption;
					conn.SslProtocols = protocol;
					conn.DataConnectionType = FtpDataConnectionType.AutoPassive;
					conn.Encoding = Encoding.UTF8;

					// try to connect
					var connected = false;
					var dataConn = FtpDataConnectionType.PASV;
					try {
						await conn.ConnectAsync(token);
						connected = true;

						// get data connection once connected
						dataConn = AutoDataConnection(conn);

						// if non-cloned connection, we want to remain connected if it works
						if (cloneConnection) {
							await conn.DisconnectAsync(token);
						}
					}
					catch (Exception ex) {

#if !CORE14
						if (ex is AuthenticationException)
						{
							throw new FtpInvalidCertificateException();
						}
#endif

						// since the connection failed, disconnect and retry
						await conn.DisconnectAsync(token);

						// if server does not support FTPS no point trying encryption again
						if (IsFtpsFailure(blacklistedEncryptions, encryption, ex)) {
							goto SkipEncryptionMode;
						}

						// catch error "no such host is known" and hard abort
						if (IsPermanantConnectionFailure(ex)) {
							if (cloneConnection) {
								conn.Dispose();
							}

							// rethrow permanant failures so caller can be made aware of it
							throw;
						}
					}

					// if it worked, add the profile
					if (connected) {

						// if connected by explicit FTPS failed, no point trying encryption again
						if (IsConnectedButFtpsFailure(blacklistedEncryptions, encryption, conn._ConnectionFTPSFailure)) {
						}

						results.Add(new FtpProfile {
							Host = Host,
							Credentials = Credentials,
							Encryption = encryption,
							Protocols = protocol,
							DataConnection = dataConn,
							Encoding = Encoding.UTF8,
							EncodingVerified = conn._ConnectionUTF8Success || conn.HasFeature(FtpCapability.UTF8)
						});

						// stop if only 1 wanted
						if (firstOnly) {
							goto Exit;
						}
					}
				}

				SkipEncryptionMode:
				var skip = true;

			}


			Exit:
			if (cloneConnection) {
				conn.Dispose();
			}

			return results;
		}
#endif

		private void ValidateAutoDetect() {
			if (IsDisposed) {
				throw new ObjectDisposedException("This FtpClient object has been disposed. It is no longer accessible.");
			}

			if (Host == null) {
				throw new FtpException("No host has been specified. Please set the 'Host' property before trying to auto connect.");
			}

			if (Credentials == null) {
				throw new FtpException("No username and password has been specified. Please set the 'Credentials' property before trying to auto connect.");
			}
		}

		private static bool IsFtpsFailure(List<FtpEncryptionMode> blacklistedEncryptions, FtpEncryptionMode encryption, Exception ex) {

			// catch error starting explicit FTPS and don't try any more secure connections
			if (encryption == FtpEncryptionMode.Auto || encryption == FtpEncryptionMode.Explicit) {
				if (ex is FtpSecurityNotAvailableException) {

					// ban explicit FTPS
					blacklistedEncryptions.Add(encryption);
					return true;
				}
			}

			// catch error starting implicit FTPS and don't try any more secure connections
			if (encryption == FtpEncryptionMode.Implicit) {
				if ((ex is SocketException && (ex as SocketException).SocketErrorCode == SocketError.ConnectionRefused)
					|| ex is TimeoutException) {

					// ban implicit FTPS
					blacklistedEncryptions.Add(encryption);
					return true;
				}
			}

			return false;
		}

		private static bool IsConnectedButFtpsFailure(List<FtpEncryptionMode> blacklistedEncryptions, FtpEncryptionMode encryption, bool failedFTPS) {

			// catch error starting explicit FTPS and don't try any more secure connections
			if (failedFTPS) {
				if (encryption == FtpEncryptionMode.Auto || encryption == FtpEncryptionMode.Explicit) {

					// ban explicit FTPS
					blacklistedEncryptions.Add(encryption);
					return true;
				}
			}

			return false;
		}

		private static bool IsPermanantConnectionFailure(Exception ex) {

			// catch error "no such host is known" and hard abort
			if (ex is SocketException && ((SocketException)ex).SocketErrorCode == SocketError.HostNotFound) {
				return true;
			}

			// catch error "timed out trying to connect" and hard abort
			if (ex is TimeoutException) {
				return true;
			}

			// catch authentication error and hard abort (see issue #697)
			if (ex is FtpAuthenticationException) {

				// only catch auth error if the credentials have been rejected by the server
				// because the error is also thrown if connection drops due to TLS or EncryptionMode
				// (see issue #700 for more details)
				var authError = ex as FtpAuthenticationException;
				if (authError.CompletionCode != null && authError.CompletionCode.StartsWith("530")) {
					return true;
				}
			}

			return false;
		}

		#endregion

		#region Auto Data Connection

		private FtpDataConnectionType AutoDataConnection(FtpClient conn) {

			// check socket protocol version
			if (conn.m_stream.LocalEndPoint.AddressFamily == AddressFamily.InterNetwork) {

				// IPV4
				return FtpDataConnectionType.PASV;

			}
			else {

				// IPV6
				// always use enhanced passive (enhanced PORT is not recommended and no other types support IPV6)
				return FtpDataConnectionType.EPSV;
			}
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

		/// <summary>
		/// Load the given connection profile and configure the FTP client instance accordingly.
		/// </summary>
		/// <param name="profile">Connection profile. Not modified.</param>
		public void LoadProfile(FtpProfile profile) {

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
		/// It will throw exceptions for permanent failures like invalid host or invalid credentials.
		/// </summary>
		public FtpProfile AutoConnect() {
			LogFunc(nameof(AutoConnect));

			// connect to the first available connection profile
			var results = AutoDetect(true, false);
			if (results.Count > 0) {
				var profile = results[0];

				// load the profile so final property selections are
				// loaded into the current connection
				LoadProfile(profile);

				// if we are using SSL, set a basic server acceptance function
				SetDefaultCertificateValidation(profile);

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
		/// It will throw exceptions for permanent failures like invalid host or invalid credentials.
		/// </summary>
		public async Task<FtpProfile> AutoConnectAsync(CancellationToken token = default(CancellationToken)) {
			LogFunc(nameof(AutoConnectAsync));

			// connect to the first available connection profile
			var results = await AutoDetectAsync(true, false, token);
			if (results.Count > 0) {
				var profile = results[0];

				// load the profile so final property selections are
				// loaded into the current connection
				LoadProfile(profile);

				// if we are using SSL, set a basic server acceptance function
				SetDefaultCertificateValidation(profile);

				// return the working profile
				return profile;
			}

			return null;
		}
#endif
		private void SetDefaultCertificateValidation(FtpProfile profile) {
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
		}

		#endregion


	}
}

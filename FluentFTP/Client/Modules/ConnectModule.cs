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
using FluentFTP.Helpers;
#if !CORE
using System.Web;
using FluentFTP.Client.Modules;
#endif
#if (CORE || NETFX)
using System.Threading;
using System.ComponentModel;
#endif
#if ASYNC
using System.Threading.Tasks;

#endif

namespace FluentFTP.Client.Modules {
	internal static class ConnectModule {

		private static List<FtpEncryptionMode> DefaultEncryptionPriority = new List<FtpEncryptionMode> {
			FtpEncryptionMode.Auto, FtpEncryptionMode.None, FtpEncryptionMode.Implicit,
		};

		private static List<SysSslProtocols> DefaultProtocolPriority = new List<SysSslProtocols> {
			//SysSslProtocols.None,
#if ASYNC
			SysSslProtocols.Tls12 | SysSslProtocols.Tls11,

			// fix #907: support TLS 1.3 in .NET 5+
#if NET50_OR_LATER
			SysSslProtocols.Tls13
#endif

#endif
#if !ASYNC
			SysSslProtocols.Tls,
#endif
#if !CORE
			SysSslProtocols.Default,
#endif
		};

		/// <summary>
		/// Automatic FTP and FTPS connection negotiation.
		/// This method tries every possible combination of the FTP connection properties, and returns the list of successful connection profiles.
		/// You can configure it to stop after finding the first successful profile, or to collect all successful profiles.
		/// You can then generate code for the profile using the FtpProfile.ToCode method.
		/// If no successful profiles are found, a blank list is returned.
		/// </summary>
		public static List<FtpProfile> AutoDetect(FtpClient client, bool firstOnly, bool cloneConnection) {
			var results = new List<FtpProfile>();

			// get known working connection profile based on the host (if any)
			List<FtpEncryptionMode> encryptionsToTry;
			var knownProfile = GetWorkingProfileFromHost(client.Host, out encryptionsToTry);

			var blacklistedEncryptions = new List<FtpEncryptionMode>();
			bool resetPort = (client.Port == 990 || client.Port == 21);

			// clone this connection or use this connection
			var conn = cloneConnection ? client.Clone() : client;

			// copy basic props if cloned connection
			if (cloneConnection) {
				conn.Host = client.Host;
				conn.Port = client.Port;
				conn.Credentials = client.Credentials;
			}

			// disconnect if already connected
			if (conn.IsConnected) {
				conn.Disconnect();
			}

			// try each encryption mode
			foreach (var encryption in encryptionsToTry) {

				// skip if FTPS was tried and failed
				if (blacklistedEncryptions.Contains(encryption)) {
					continue;
				}

				// try each SSL protocol
				bool tryTLS13 = false;
				foreach (var protocol in DefaultProtocolPriority) {

					// fix #907: support TLS 1.3 in .NET 5+
					// only try TLS 1.3 if required
#if NET50_OR_LATER
					if (protocol == SysSslProtocols.Tls13 && !tryTLS13) {
						continue;
					}
#endif

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

					// configure props
					ConfigureClient(conn, encryption, protocol, knownProfile);

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

						// unpack aggregate exception
#if NET50_OR_LATER
						if (ex is AggregateException) {
							ex = ((AggregateException)ex).InnerExceptions[0];
						}
#endif

						// since the connection failed, disconnect and retry
						conn.Disconnect();

						// fix #907: support TLS 1.3 in .NET 5+
						// if it is a protocol error, then jump to the next protocol
						if (IsProtocolFailure(ex)) {
#if NET50_OR_LATER
							if (protocol == SysSslProtocols.Tls13) {
								client.LogStatus(FtpTraceLevel.Info, "Failed to connect with TLS1.3"); ;
							}
							else {
								client.LogStatus(FtpTraceLevel.Info, "Failed to connect with TLS1.1/TLS1.2, trying TLS1.3"); ;
							}
#endif
							tryTLS13 = true;
							continue;
						}

#if !CORE14
						if (ex is AuthenticationException) {
							throw new FtpInvalidCertificateException((AuthenticationException)ex);
						}
#endif

						// if server does not support FTPS no point trying encryption again
						if (IsFtpsFailure(blacklistedEncryptions, encryption, ex)) {
							goto SkipEncryptionMode;
						}

						// catch error "no such host is known" and hard abort
						if (IsPermanantConnectionFailure(ex)) {
							if (cloneConnection) {
								conn.Dispose();
							}

							// rethrow permanent failures so caller can be made aware of it
							throw;
						}
					}

					// if it worked
					if (connected) {

						// if connected by explicit FTPS failed, no point trying encryption again
						if (IsConnectedButFtpsFailure(blacklistedEncryptions, encryption, conn.Status.ConnectionFTPSFailure)) {
						}

						// list the computed FtpProfile
						SaveResult(results, knownProfile, blacklistedEncryptions, conn, encryption, protocol, dataConn);

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


#if ASYNC
		/// <summary>
		/// Automatic FTP and FTPS connection negotiation.
		/// This method tries every possible combination of the FTP connection properties, and returns the list of successful connection profiles.
		/// You can configure it to stop after finding the first successful profile, or to collect all successful profiles.
		/// You can then generate code for the profile using the FtpProfile.ToCode method.
		/// If no successful profiles are found, a blank list is returned.
		/// </summary>
		public static async Task<List<FtpProfile>> AutoDetectAsync(FtpClient client, bool firstOnly, bool cloneConnection, CancellationToken token) {
			var results = new List<FtpProfile>();

			// get known working connection profile based on the host (if any)
			List<FtpEncryptionMode> encryptionsToTry;
			var knownProfile = GetWorkingProfileFromHost(client.Host, out encryptionsToTry);

			var blacklistedEncryptions = new List<FtpEncryptionMode>();
			bool resetPort = (client.Port == 990 || client.Port == 21);

			// clone this connection or use this connection
			var conn = cloneConnection ? client.Clone() : client;

			// copy basic props if cloned connection
			if (cloneConnection) {
				conn.Host = client.Host;
				conn.Port = client.Port;
				conn.Credentials = client.Credentials;
			}

			// disconnect if already connected
			if (conn.IsConnected) {
				await conn.DisconnectAsync(token);
			}

			// try each encryption mode
			foreach (var encryption in encryptionsToTry) {

				// skip if FTPS was tried and failed
				if (blacklistedEncryptions.Contains(encryption)) {
					continue;
				}

				// try each SSL protocol
				bool tryTLS13 = false;
				foreach (var protocol in DefaultProtocolPriority) {
					// fix #907: support TLS 1.3 in .NET 5+
					// only try TLS 1.3 if required
#if NET50_OR_LATER
					if (protocol == SysSslProtocols.Tls13 && !tryTLS13) {
						continue;
					}
#endif

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

					// configure props
					ConfigureClient(conn, encryption, protocol, knownProfile);

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

						// unpack aggregate exception
#if NET50_OR_LATER
						if (ex is AggregateException) {
							ex = ((AggregateException)ex).InnerExceptions[0];
						}
#endif

						// since the connection failed, disconnect and retry
						await conn.DisconnectAsync(token);

						// fix #907: support TLS 1.3 in .NET 5+
						// if it is a protocol error, then jump to the next protocol
						if (IsProtocolFailure(ex)) {
#if NET50_OR_LATER
							if (protocol == SysSslProtocols.Tls13) {
								client.LogStatus(FtpTraceLevel.Info, "Failed to connect with TLS1.3"); ;
							}
							else {
								client.LogStatus(FtpTraceLevel.Info, "Failed to connect with TLS1.1/TLS1.2, trying TLS1.3"); ;
							}
#endif
							tryTLS13 = true;
							continue;
						}

#if !CORE14
						if (ex is AuthenticationException) {
							throw new FtpInvalidCertificateException((AuthenticationException)ex);
						}
#endif

						// if server does not support FTPS no point trying encryption again
						if (IsFtpsFailure(blacklistedEncryptions, encryption, ex)) {
							goto SkipEncryptionMode;
						}

						// catch error "no such host is known" and hard abort
						if (IsPermanantConnectionFailure(ex)) {
							if (cloneConnection) {
								conn.Dispose();
							}

							// rethrow permanent failures so caller can be made aware of it
							throw;
						}
					}

					// if it worked
					if (connected) {

						// if connected by explicit FTPS failed, no point trying encryption again
						if (IsConnectedButFtpsFailure(blacklistedEncryptions, encryption, conn.Status.ConnectionFTPSFailure)) {
						}

						// list the computed FtpProfile
						SaveResult(results, knownProfile, blacklistedEncryptions, conn, encryption, protocol, dataConn);

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

		private static void SaveResult(List<FtpProfile> results, FtpProfile knownProfile, List<FtpEncryptionMode> blacklistedEncryptions, FtpClient conn, FtpEncryptionMode encryption, SysSslProtocols protocol, FtpDataConnectionType dataConn) {
			results.Add(new FtpProfile {
				Host = conn.Host,
				Credentials = conn.Credentials,
				Encryption = blacklistedEncryptions.Contains(encryption) ? FtpEncryptionMode.None : encryption,
				Protocols = protocol,
				DataConnection = dataConn,
				Encoding = Encoding.UTF8,
				EncodingVerified = conn.Status.ConnectionUTF8Success || conn.HasFeature(FtpCapability.UTF8),

				// FIX #901: Azure FTP connection
				// copy some props for known profile
				Timeout = knownProfile != null ? knownProfile.Timeout : 0,
				RetryAttempts = knownProfile != null ? knownProfile.RetryAttempts : 0,
				SocketPollInterval = knownProfile != null ? knownProfile.SocketPollInterval : 0,
			});
		}

		private static void ConfigureClient(FtpClient client, FtpEncryptionMode encryption, SysSslProtocols protocol, FtpProfile knownProfile) {

			// set rolled props
			client.EncryptionMode = encryption;
			client.SslProtocols = protocol;
			client.DataConnectionType = FtpDataConnectionType.AutoPassive;
			client.Encoding = Encoding.UTF8;

			// FIX #901: Azure FTP connection
			// copy some props for known profile
			if (knownProfile != null) {
				client.ConnectTimeout = knownProfile.Timeout;
				client.RetryAttempts = knownProfile.RetryAttempts;
				client.SocketPollInterval = knownProfile.SocketPollInterval;
			}

			// FIX #907: support TLS 1.3 in .NET 5+
			// only try TLS 1.3 if required
/*#if NET50_OR_LATER
			if (protocol == SysSslProtocols.Tls13) {
				client.StaleDataCheck = false;
			}
			else {
				client.StaleDataCheck = true;
			}
#endif*/
		}

		/// <summary>
		/// Check if the server refused to support one type of FTPS encryption, and if so blacklist that type of encryption.
		/// </summary>
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

		/// <summary>
		/// Check if its an auth failure or something permanent,
		/// so that we don't need to retry all the connection config combinations and can hard-abort the AutoConnect.
		/// </summary>
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


		private static FtpDataConnectionType AutoDataConnection(FtpClient conn) {

			// check socket protocol version
			if (conn.InternetProtocol == FtpIpVersion.IPv4) {

				// IPV4
				return FtpDataConnectionType.PASV;

			}
			else {

				// IPV6
				// always use enhanced passive (enhanced PORT is not recommended and no other types support IPV6)
				return FtpDataConnectionType.EPSV;
			}
		}

		/// <summary>
		/// Load the given connection profile and configure the FTP client instance accordingly.
		/// </summary>
		public static void LoadProfile(FtpClient client, FtpProfile profile) {

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
			client.Host = profile.Host;
			client.Credentials = profile.Credentials;
			client.EncryptionMode = profile.Encryption;
			client.SslProtocols = profile.Protocols;
			client.DataConnectionType = profile.DataConnection;
			client.Encoding = profile.Encoding;
			if (profile.Timeout != 0) {
				client.ConnectTimeout = profile.Timeout;
				client.ReadTimeout = profile.Timeout;
				client.DataConnectionConnectTimeout = profile.Timeout;
				client.DataConnectionReadTimeout = profile.Timeout;
			}
			if (client.SocketPollInterval != 0) {
				client.SocketPollInterval = profile.SocketPollInterval;
			}
			if (client.RetryAttempts != 0) {
				client.RetryAttempts = profile.RetryAttempts;
			}
		}

		/// <summary>
		/// Create a default ValidateCertificate handler that accepts valid certificates.
		/// </summary>
		public static void SetDefaultCertificateValidation(FtpClient client, FtpProfile profile) {
			if (profile.Encryption != FtpEncryptionMode.None) {
				//if (client.ValidateCertificate == null) {
					client.ValidateCertificate += new FtpSslValidation(delegate (FtpClient c, FtpSslValidationEventArgs e) {
						if (e.PolicyErrors != System.Net.Security.SslPolicyErrors.None) {
							e.Accept = false;
						}
						else {
							e.Accept = true;
						}
					});
				//}
			}
		}


		/// <summary>
		/// Return a known working connection profile from the host/port combination.
		/// </summary>
		public static FtpProfile GetWorkingProfileFromHost(string host, out List<FtpEncryptionMode> encryptionsToTry) {

			encryptionsToTry = DefaultEncryptionPriority.ShallowClone();

			// Azure App Services / Azure Websites
			if (host.IndexOf("azurewebsites.windows.net", StringComparison.OrdinalIgnoreCase) > -1) {

				encryptionsToTry = new List<FtpEncryptionMode> { FtpEncryptionMode.Implicit };

				return new FtpProfile {
					RetryAttempts = 5,
					SocketPollInterval = 1000,
					Timeout = 2000
				};

			}

			return null;
		}

		/// <summary>
		/// Check if the server requires TLS 1.3 protocol
		/// </summary>
		private static bool IsProtocolFailure(Exception ex) {
			var msg = "Authentication failed because the remote party sent a TLS alert: 'ProtocolVersion'";
			if (ex.Message.Contains(msg)) {
#if !NET50_OR_LATER
				throw new FtpProtocolUnsupportedException("Your server requires TLS 1.3 and your .NET version is too low to support it! Please upgrade your project to .NET 5+ in order to activate TLS 1.3.");
#endif
				return true;
			}

#if !CORE14
			if (ex is AuthenticationException &&
				((AuthenticationException)ex).InnerException != null &&
				((AuthenticationException)ex).InnerException.Message.ToLower().ContainsAny(ServerStringModule.failedTLS)) {
				return true;
			}
#endif

			return false;
		}

	}
}

using System;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Security.Authentication;
using SysSslProtocols = System.Security.Authentication.SslProtocols;
using FluentFTP.Helpers;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP.Client.BaseClient;
using System.Linq;
using FluentFTP.Exceptions;

namespace FluentFTP.Client.Modules {
	/// <summary>
	/// Class responsible for automatically detecting working FTP settings to connect to a target FTP server.
	/// </summary>
	internal static class ConnectModule {

		private static List<FtpEncryptionMode> DefaultEncryptionPriority = new List<FtpEncryptionMode> {
			FtpEncryptionMode.Auto, FtpEncryptionMode.None, FtpEncryptionMode.Implicit,
		};

		private static List<SysSslProtocols> DefaultProtocolPriority = new List<SysSslProtocols> {

			SysSslProtocols.Tls12 | SysSslProtocols.Tls11,

#if NETFRAMEWORK
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
			FtpClient conn = cloneConnection ? (FtpClient)client.Clone() : client;

			// copy basic props if cloned connection
			if (cloneConnection) {
				conn.Host = client.Host;
				conn.Port = client.Port;
				conn.Credentials = client.Credentials;
			}

			// disconnect if already connected
			if (conn.IsConnected) {
				((IInternalFtpClient)conn).DisconnectInternal();
			}

			// try each encryption mode
			foreach (var encryption in encryptionsToTry) {

				// skip if FTPS was tried and failed
				if (blacklistedEncryptions.Contains(encryption)) {
					continue;
				}

				// try each SSL protocol
				foreach (var protocol in DefaultProtocolPriority) {

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
							((IInternalFtpClient)conn).DisconnectInternal();
						}
					}
					catch (Exception ex) {

						// unpack aggregate exception
#if NET50_OR_LATER
						if (ex is AggregateException aex) {
							ex = aex.InnerExceptions[0];
						}
#endif
						// since the connection failed, disconnect and retry
						((IInternalFtpClient)conn).DisconnectInternal();

						// if server does not support FTPS no point trying encryption again
						if (IsFtpsFailure(blacklistedEncryptions, encryption, ex)) {
							goto SkipEncryptionMode;
						}

						// check if permanent failures and hard abort
						var permaEx = IsPermanentConnectionFailure(ex);
						if (permaEx != null) {
							if (cloneConnection) {
								conn.Dispose();
							}

							// rethrow permanent failures so caller can be made aware of it
							throw permaEx;
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
				;

			}


		Exit:
			if (cloneConnection) {
				conn.Dispose();
			}
			return results;
		}

		/// <summary>
		/// Automatic FTP and FTPS connection negotiation.
		/// This method tries every possible combination of the FTP connection properties, and returns the list of successful connection profiles.
		/// You can configure it to stop after finding the first successful profile, or to collect all successful profiles.
		/// You can then generate code for the profile using the FtpProfile.ToCode method.
		/// If no successful profiles are found, a blank list is returned.
		/// </summary>
		public static async Task<List<FtpProfile>> AutoDetectAsync(AsyncFtpClient client, bool firstOnly, bool cloneConnection, CancellationToken token) {
			var results = new List<FtpProfile>();

			// get known working connection profile based on the host (if any)
			List<FtpEncryptionMode> encryptionsToTry;
			var knownProfile = GetWorkingProfileFromHost(client.Host, out encryptionsToTry);

			var blacklistedEncryptions = new List<FtpEncryptionMode>();
			bool resetPort = (client.Port == 990 || client.Port == 21);

			// clone this connection or use this connection
			AsyncFtpClient conn = cloneConnection ? (AsyncFtpClient)client.Clone() : client;

			// copy basic props if cloned connection
			if (cloneConnection) {
				conn.Host = client.Host;
				conn.Port = client.Port;
				conn.Credentials = client.Credentials;
			}

			// disconnect if already connected
			if (conn.IsConnected) {
				await conn.Disconnect(token);
			}

			// try each encryption mode
			foreach (var encryption in encryptionsToTry) {

				// skip if FTPS was tried and failed
				if (blacklistedEncryptions.Contains(encryption)) {
					continue;
				}

				// try each SSL protocol
				foreach (var protocol in DefaultProtocolPriority) {

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
						await conn.Connect(token);
						connected = true;

						// get data connection once connected
						dataConn = AutoDataConnection(conn);

						// if non-cloned connection, we want to remain connected if it works
						if (cloneConnection) {
							await conn.Disconnect(token);
						}
					}
					catch (Exception ex) {

						// unpack aggregate exception
#if NET50_OR_LATER
						if (ex is AggregateException aex) {
							ex = aex.InnerExceptions[0];
						}
#endif

						// since the connection failed, disconnect and retry
						await conn.Disconnect(token);

						// if server does not support FTPS no point trying encryption again
						if (IsFtpsFailure(blacklistedEncryptions, encryption, ex)) {
							goto SkipEncryptionMode;
						}

						// check if permanent failures and hard abort
						var permaEx = IsPermanentConnectionFailure(ex);
						if (permaEx != null) {
							if (cloneConnection) {
								conn.Dispose();
							}

							// rethrow permanent failures so caller can be made aware of it
							throw permaEx;
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
				;

			}


		Exit:
			if (cloneConnection) {
				conn.Dispose();
			}

			return results;
		}

		private static void SaveResult(List<FtpProfile> results, FtpProfile knownProfile, List<FtpEncryptionMode> blacklistedEncryptions, BaseFtpClient conn, FtpEncryptionMode encryption, SysSslProtocols protocol, FtpDataConnectionType dataConn) {
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

		private static void ConfigureClient(BaseFtpClient client, FtpEncryptionMode encryption, SysSslProtocols protocol, FtpProfile knownProfile) {

			// override some props
			client.Config.EncryptionMode = encryption;
			client.Config.SslProtocols = protocol;
			client.Config.DataConnectionType = FtpDataConnectionType.AutoPassive;
			client.Encoding = Encoding.UTF8;

			// FIX #901: Azure FTP connection
			// copy some props for known profile
			if (knownProfile != null) {
				client.Config.ConnectTimeout = knownProfile.Timeout;
				client.Config.RetryAttempts = knownProfile.RetryAttempts;
				client.Config.SocketPollInterval = knownProfile.SocketPollInterval;
			}
		}

		/// <summary>
		/// Check if the server refused to support one type of FTPS encryption, and if so blacklist that type of encryption.
		/// </summary>
		private static bool IsFtpsFailure(List<FtpEncryptionMode> blacklistedEncryptions, FtpEncryptionMode encryption, Exception ex) {

			// catch error starting explicit FTPS and don't try any more secure connections
			if (encryption is FtpEncryptionMode.Auto or FtpEncryptionMode.Explicit) {
				if (ex is FtpSecurityNotAvailableException) {

					// ban explicit FTPS
					blacklistedEncryptions.Add(encryption);
					return true;
				}
			}

			// catch error starting implicit FTPS and don't try any more secure connections
			if (encryption == FtpEncryptionMode.Implicit) {
				if (ex is SocketException { SocketErrorCode: SocketError.ConnectionRefused } or TimeoutException) {

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
				if (encryption is FtpEncryptionMode.Auto or FtpEncryptionMode.Explicit) {

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
		/// Return the exception if it is a hard failure, or null if no issue is found.
		/// </summary>
		private static Exception IsPermanentConnectionFailure(Exception ex) {

			// Authentication related failures

			// catch unsupported protocol failure
			var msg = "Authentication failed because the remote party sent a TLS alert: 'ProtocolVersion'";
			if (ex.Message.Contains(msg) ||
				(ex is AuthenticationException authEx &&
					authEx.InnerException != null &&
					authEx.InnerException.Message.ToLower().ContainsAny(ServerStringModule.failedTLS))) {

				return new FtpProtocolUnsupportedException("Your server requires TLS 1.3 and FluentFTP does not currently support TLS 1.3 due to poor .NET support for this protocol.");
			}

			// catch credential related authentication failure (see issue #697)
			if (ex is FtpAuthenticationException credEx) {

				// only catch auth error if the credentials have been rejected by the server
				// because the error is also thrown if connection drops due to TLS or EncryptionMode
				// (see issue #700 for more details)
				if (credEx.CompletionCode != null && credEx.CompletionCode.StartsWith("530")) {
					return ex;
				}
			}

			// generic permanent authentication failure leftover: probably wrong FTPS certificate
			if (ex is AuthenticationException certEx) {
				return new FtpInvalidCertificateException(certEx);
			}

			// Network related failures

			// catch error "no such host is known" and hard abort
			if (ex is SocketException { SocketErrorCode: SocketError.HostNotFound }) {
				return ex;
			}

			// catch error "timed out trying to connect" and hard abort
			if (ex is TimeoutException) {
				return ex;
			}

			return null;
		}


		private static FtpDataConnectionType AutoDataConnection(BaseFtpClient conn) {

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
		public static void LoadProfile(BaseFtpClient client, FtpProfile profile) {

			// verify args
			if (profile == null) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(profile));
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
			client.Config.EncryptionMode = profile.Encryption;
			client.Config.SslProtocols = profile.Protocols;
			client.Config.DataConnectionType = profile.DataConnection;
			client.Encoding = profile.Encoding;
			if (profile.Timeout != 0) {
				client.Config.ConnectTimeout = profile.Timeout;
				client.Config.ReadTimeout = profile.Timeout;
				client.Config.DataConnectionConnectTimeout = profile.Timeout;
				client.Config.DataConnectionReadTimeout = profile.Timeout;
			}
			if (client.Config.SocketPollInterval != 0) {
				client.Config.SocketPollInterval = profile.SocketPollInterval;
			}
			if (client.Config.RetryAttempts != 0) {
				client.Config.RetryAttempts = profile.RetryAttempts;
			}
		}

		/// <summary>
		/// Create a default ValidateCertificate handler that accepts valid certificates.
		/// </summary>
		public static void SetDefaultCertificateValidation(BaseFtpClient client, FtpProfile profile) {
			if (profile.Encryption != FtpEncryptionMode.None && client.ValidateCertificateHandlerExists == false) {
				client.ValidateCertificate += new FtpSslValidation(delegate (BaseFtpClient c, FtpSslValidationEventArgs e) {
					if (e.PolicyErrors != System.Net.Security.SslPolicyErrors.None) {
						e.Accept = false;
					}
					else {
						e.Accept = true;
					}
				});
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
		/// Modify the `Status.InCriticalSequence` flag based on the FTP command sent, by checking against a list of known critical commands.
		/// A critical sequence will not be interrupted by an automatic reconnect.
		/// </summary>
		public static void CheckCriticalSequence(BaseFtpClient client, string cmd) {
			var cmdFirstWord = cmd.Split(new char[] { ' ' })[0];

			if (cmdFirstWord.EqualsAny(ServerStringModule.criticalStartingCommands)) {
				client.Status.InCriticalSequence = true;
				return;
			}

			if (cmdFirstWord.EqualsAny(ServerStringModule.criticalTerminatingCommands)) {
				client.Status.InCriticalSequence = false;
				return;
			}
		}

	}
}

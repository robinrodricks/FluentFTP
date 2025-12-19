using System;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;

namespace Examples {
	internal static class ConnectFtpsLegacyRsaExample {

		/// <summary>
		/// Example: Connect to a legacy FTPS server that only supports RSA key-exchange ciphers.
		///
		/// Some legacy FTPS servers (especially on Linux) only negotiate RSA key-exchange TLS ciphers
		/// (e.g., TLS_RSA_WITH_AES_256_GCM_SHA384) and fail when .NET on Linux offers only ECDHE ciphers.
		///
		/// This example shows how to configure FluentFTP to work with such servers by customizing
		/// the SSL client authentication options to include RSA cipher suites.
		///
		/// Security Note: Enabling RSA key-exchange ciphers reduces security (no forward secrecy).
		/// Only use this for interop with legacy FTPS servers that cannot be upgraded.
		/// </summary>
		public static void ConnectFtpsLegacyRsa() {
			using (var conn = new FtpClient("localhost", "username", "password")) {
				// Set encryption mode to Explicit FTPS (AUTH TLS)
				conn.Config.EncryptionMode = FtpEncryptionMode.Explicit;

				// Configure SSL client authentication options to include RSA cipher suites
				// This is required for legacy servers that only support RSA key-exchange
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
				conn.ConfigureAuthentication += (client, e) => {
#if NET5_0_OR_GREATER
					// Set cipher suites policy to include RSA key-exchange ciphers
					// CipherSuitesPolicy is only available in .NET 5.0+
					e.Options.CipherSuitesPolicy = new CipherSuitesPolicy(new[] {
						TlsCipherSuite.TLS_RSA_WITH_AES_256_GCM_SHA384,
						TlsCipherSuite.TLS_RSA_WITH_AES_128_GCM_SHA256,
						TlsCipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA256,
						TlsCipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA256,
						TlsCipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA,
						TlsCipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA,
					});
#endif
				};
#endif
				// Optionally: Skip certificate validation for testing (not recommended for production)
				// conn.Config.ValidateAnyCertificate = true;
				try {
					// Connect to the server
					conn.Connect();
				}
				catch (Exception e) {
					Console.WriteLine(e);
					throw;
				}


				// Now you can use the FTP client normally
				Console.WriteLine("Connected successfully!");
				Console.WriteLine($"Current directory: {conn.GetWorkingDirectory()}");
			}
		}

		/// <summary>
		/// Example: Connect to a legacy FTPS server that only supports RSA key-exchange ciphers (Async version).
		/// </summary>
		public static async Task ConnectFtpsLegacyRsaAsync() {
			var token = new CancellationToken();
			using (var conn = new AsyncFtpClient("localhost", "username", "password")) {

				// Set encryption mode to Explicit FTPS (AUTH TLS)
				conn.Config.EncryptionMode = FtpEncryptionMode.Explicit;

				// Configure SSL client authentication options to include RSA cipher suites
				// This is required for legacy servers that only support RSA key-exchange
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
				conn.ConfigureAuthentication += (client, e) => {
#if NET5_0_OR_GREATER
					// Set cipher suites policy to include RSA key-exchange ciphers
					// CipherSuitesPolicy is only available in .NET 5.0+
					e.Options.CipherSuitesPolicy = new CipherSuitesPolicy(new[] {
						TlsCipherSuite.TLS_RSA_WITH_AES_256_GCM_SHA384,
						TlsCipherSuite.TLS_RSA_WITH_AES_128_GCM_SHA256,
						TlsCipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA256,
						TlsCipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA256,
						TlsCipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA,
						TlsCipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA,
					});
#endif
				};
#endif

				// Optionally: Skip certificate validation for testing (not recommended for production)
				// conn.Config.ValidateAnyCertificate = true;

				// Connect to the server
				await conn.Connect(token);

				// Now you can use the FTP client normally
				Console.WriteLine("Connected successfully!");
				Console.WriteLine($"Current directory: {await conn.GetWorkingDirectory(token)}");
			}
		}

		/// <summary>
		/// Example: Minimal configuration for legacy RSA-only servers.
		/// This is a simplified version that only includes the most common RSA cipher suites.
		/// </summary>
		public static void ConnectFtpsLegacyRsaMinimal() {
			using (var conn = new FtpClient("localhost", "username", "password")) {
				conn.Config.EncryptionMode = FtpEncryptionMode.Explicit;

#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
				conn.ConfigureAuthentication += (client, e) => {
#if NET5_0_OR_GREATER
					// Minimal set: Only the most common RSA cipher suites
					e.Options.CipherSuitesPolicy = new CipherSuitesPolicy(new[] {
						TlsCipherSuite.TLS_RSA_WITH_AES_256_GCM_SHA384,
						TlsCipherSuite.TLS_RSA_WITH_AES_128_GCM_SHA256,
					});
#endif
				};
#endif
				// Optionally: Skip certificate validation for testing (not recommended for production)
				// conn.Config.ValidateAnyCertificate = true;
				conn.Connect();
			}
		}

	}

}

using FluentFTP.Exceptions;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class AsyncFtpClient {

		/// <summary>
		/// Performs a login on the server. This method is overridable so
		/// that the login procedure can be changed to support, for example,
		/// a FTP proxy.
		/// </summary>
		protected virtual async Task Authenticate(CancellationToken token) {
			await Authenticate(Credentials.UserName, Credentials.Password, Credentials.Domain, token);
		}

		/// <summary>
		/// Performs a login on the server. This method is overridable so
		/// that the login procedure can be changed to support, for example,
		/// a FTP proxy.
		/// </summary>
		/// <exception cref="FtpAuthenticationException">On authentication failures</exception>
		/// <remarks>
		/// To handle authentication failures without retries, catch FtpAuthenticationException.
		/// </remarks>
		protected virtual async Task Authenticate(string userName, string password, string account, CancellationToken token) {

			// mark that we are not authenticated
			m_IsAuthenticated = false;
			// send the USER command along with the FTP username
			FtpReply reply = await Execute("USER " + userName, token);

			// check the reply to the USER command
			if (!reply.Success) {
				throw new FtpAuthenticationException(reply);
			}

			// if it was accepted
			else if (reply.Type == FtpResponseType.PositiveIntermediate) {

				// send the PASS command along with the FTP password
				reply = await Execute("PASS " + password, token);

				// fix for #620: some servers send multiple responses that must be read and decoded,
				// otherwise the connection is aborted and remade and it goes into an infinite loop
				var staleData = await ReadStaleDataAsync("in authentication", token);
				if (staleData != null) {
					var staleReply = new FtpReply();
					if (DecodeStringToReply(staleData, ref staleReply) && !staleReply.Success) {
						throw new FtpAuthenticationException(staleReply);
					}
				}

				// check the first reply to the PASS command
				if (!reply.Success) {
					throw new FtpAuthenticationException(reply);
				}

				// only possible 3** here is `332 Need account for login`
				if (reply.Type == FtpResponseType.PositiveIntermediate) {
					reply = await Execute("ACCT " + account, token);

					if (!reply.Success) {
						throw new FtpAuthenticationException(reply);
					}
					else {
						m_IsAuthenticated = true;
					}
				}
				else if (reply.Type == FtpResponseType.PositiveCompletion) {
					m_IsAuthenticated = true;
				}
			}
		}

	}
}

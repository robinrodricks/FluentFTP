using FluentFTP.Exceptions;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Performs a login on the server. This method is overridable so
		/// that the login procedure can be changed to support, for example,
		/// a FTP proxy.
		/// </summary>
		protected virtual void Authenticate() {
			Authenticate(Credentials.UserName, Credentials.Password, Credentials.Domain);
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
		protected virtual void Authenticate(string userName, string password, string account) {

			// mark that we are not authenticated
			m_IsAuthenticated = false;

			// send the USER command along with the FTP username
			FtpReply reply = Execute("USER " + userName);

			// check the reply to the USER command
			if (!reply.Success) {
				throw new FtpAuthenticationException(reply);
			}

			// if it was accepted
			else if (reply.Type == FtpResponseType.PositiveIntermediate) {

				// send the PASS command along with the FTP password
				reply = Execute("PASS " + password);

				// fix for #620: some servers send multiple responses that must be read and decoded,
				// otherwise the connection is aborted and remade and it goes into an infinite loop
				var staleData = ReadStaleData("in authentication");
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
					reply = Execute("ACCT " + account);

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

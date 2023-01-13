using FluentFTP.Helpers;
using System;
using System.IO;
using System.Text.RegularExpressions;
using FluentFTP.Client.BaseClient;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP.Exceptions;

namespace FluentFTP.Proxy.AsyncProxy {
	/// <summary> A FTP client with a HTTP 1.1 proxy implementation. </summary>
	public class AsyncFtpClientHttp11Proxy : AsyncFtpClientProxy {
		/// <summary> A FTP client with a HTTP 1.1 proxy implementation </summary>
		/// <param name="proxy">Proxy information</param>
		public AsyncFtpClientHttp11Proxy(FtpProxyProfile proxy)
			: base(proxy) {
			ConnectionType = "HTTP 1.1 Proxy";
		}

		/// <summary> Redefine the first dialog: HTTP Frame for the HTTP 1.1 Proxy </summary>
		protected override async Task HandshakeAsync(CancellationToken token = default) {
			var proxyConnectionReply = await GetReply(token);
			if (!proxyConnectionReply.Success) {
				throw new FtpException("Can't connect " + Host + " via proxy " + Proxy.ProxyHost + ".\nMessage : " +
									   proxyConnectionReply.ErrorMessage);
			}

			// TO TEST: if we are able to detect the actual FTP server software from this reply
			HandshakeReply = proxyConnectionReply;
		}

		/// <summary>
		/// Creates a new instance of this class. Useful in FTP proxy classes.
		/// </summary>
		protected override BaseFtpClient Create() {
			return new AsyncFtpClientHttp11Proxy(Proxy);
		}

		/// <summary>
		/// Connects to the server using an existing <see cref="FtpSocketStream"/>
		/// </summary>
		/// <param name="stream">The existing socket stream</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		protected override Task ConnectAsync(FtpSocketStream stream, CancellationToken token) {
			return ConnectAsync(stream, Host, Port, FtpIpVersion.ANY, token);
		}

		/// <summary>
		/// Connects to the server using an existing <see cref="FtpSocketStream"/>
		/// </summary>
		/// <param name="stream">The existing socket stream</param>
		/// <param name="host">Host name</param>
		/// <param name="port">Port number</param>
		/// <param name="ipVersions">IP version to use</param>
		/// <param name="token">IP version to use</param>
		protected override async Task ConnectAsync(FtpSocketStream stream, string host, int port, FtpIpVersion ipVersions, CancellationToken token) {
			await base.ConnectAsync(stream, token);

			var writer = new StreamWriter(stream);
			await writer.WriteLineAsync(string.Format("CONNECT {0}:{1} HTTP/1.1", host, port));
			await writer.WriteLineAsync(string.Format("Host: {0}:{1}", host, port));
			if (Proxy.ProxyCredentials != null) {
				var credentialsHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(Proxy.ProxyCredentials.UserName + ":" + Proxy.ProxyCredentials.Password));
				await writer.WriteLineAsync("Proxy-Authorization: Basic " + credentialsHash);
			}

			await writer.WriteLineAsync("User-Agent: custom-ftp-client");
			await writer.WriteLineAsync();
			await writer.FlushAsync();

			await ProxyHandshakeAsync(stream, token);
		}

		private async Task ProxyHandshakeAsync(FtpSocketStream stream, CancellationToken token = default) {
			var proxyConnectionReply = await GetProxyReplyAsync(stream, token);
			if (!proxyConnectionReply.Success) {
				throw new FtpException("Can't connect " + Host + " via proxy " + Proxy.ProxyHost + ".\nMessage : " + proxyConnectionReply.ErrorMessage);
			}
		}

		private async Task<FtpReply> GetProxyReplyAsync(FtpSocketStream stream, CancellationToken token = default) {
			var reply = new FtpReply();
			string buf;

			if (!IsConnected) {
				throw new InvalidOperationException("No connection to the server has been established.");
			}

			stream.ReadTimeout = Config.ReadTimeout;
			while ((buf = await stream.ReadLineAsync(Encoding, token)) != null) {
				Match m;

				Log(FtpTraceLevel.Info, buf);

				if ((m = Regex.Match(buf, @"^HTTP/.*\s(?<code>[0-9]{3}) (?<message>.*)$")).Success) {
					reply.Code = m.Groups["code"].Value;
					reply.Message = m.Groups["message"].Value;
					break;
				}

				reply.InfoMessages += buf + "\n";
			}

			// fixes #84 (missing bytes when downloading/uploading files through proxy)
			while ((buf = await stream.ReadLineAsync(Encoding, token)) != null) {
				Log(FtpTraceLevel.Info, buf);

				if (Strings.IsNullOrWhiteSpace(buf)) {
					break;
				}

				reply.InfoMessages += buf + "\n";
			}

			return reply;
		}

	}
}
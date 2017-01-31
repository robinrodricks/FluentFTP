using System;
using System.IO;
using System.Text.RegularExpressions;

namespace FluentFTP.Proxy {
	/// <summary> A FTP client with a HTTP 1.1 proxy implementation. </summary>
	public class FtpClientHttp11Proxy : FtpClientProxy {
		/// <summary> A FTP client with a HTTP 1.1 proxy implementation </summary>
		/// <param name="proxy">Proxy information</param>
		public FtpClientHttp11Proxy(ProxyInfo proxy)
			: base(proxy) {
			ConnectionType = "HTTP 1.1 Proxy";
		}

		/// <summary> Redefine the first dialog: HTTP Frame for the HTTP 1.1 Proxy </summary>
		protected override void Handshake() {
			var proxyConnectionReply = GetReply();
			if (!proxyConnectionReply.Success)
				throw new FtpException("Can't connect " + Host + " via proxy " + Proxy.Host + ".\nMessage : " +
										proxyConnectionReply.ErrorMessage);
		}

		protected override FtpClient Create() {
			return new FtpClientHttp11Proxy(Proxy);
		}

		protected override void Connect(FtpSocketStream stream) {
			Connect(stream, Host, Port, FtpIpVersion.ANY);
		}

		protected override void Connect(FtpSocketStream stream, string host, int port, FtpIpVersion ipVersions) {
			base.Connect(stream);

			var writer = new StreamWriter(stream);
			writer.WriteLine("CONNECT {0}:{1} HTTP/1.1", host, port);
			writer.WriteLine("Host: {0}:{1}", host, port);
			if (Proxy.Credentials != null) {
				var credentialsHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(string.Format("{0}:{1}", Proxy.Credentials.UserName, Proxy.Credentials.Password)));
				writer.WriteLine("Proxy-Authorization: Basic {0}", credentialsHash);
			}
			writer.WriteLine("User-Agent: custom-ftp-client");
			writer.WriteLine();
			writer.Flush();

			ProxyHandshake(stream);
		}

		private void ProxyHandshake(FtpSocketStream stream) {
			var proxyConnectionReply = GetProxyReply(stream);
			if (!proxyConnectionReply.Success)
				throw new FtpException("Can't connect " + Host + " via proxy " + Proxy.Host + ".\nMessage : " + proxyConnectionReply.ErrorMessage);
		}

		private FtpReply GetProxyReply(FtpSocketStream stream) {

			FtpReply reply = new FtpReply();
			string buf;

			lock (Lock) {
				if (!IsConnected)
					throw new InvalidOperationException("No connection to the server has been established.");

				stream.ReadTimeout = ReadTimeout;
				while ((buf = stream.ReadLine(Encoding)) != null) {
					Match m;

					FtpTrace.WriteLine(buf);

					if ((m = Regex.Match(buf, @"^HTTP/.*\s(?<code>[0-9]{3}) (?<message>.*)$")).Success) {
						reply.Code = m.Groups["code"].Value;
						reply.Message = m.Groups["message"].Value;
						break;
					}

					reply.InfoMessages += string.Format("{0}\n", buf);
				}
			}

			return reply;
		}
	}
}
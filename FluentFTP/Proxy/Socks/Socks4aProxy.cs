using FluentFTP.Exceptions;
using FluentFTP.Proxy.Enums;
using System.Text;

#if ASYNC
using System.Threading.Tasks;
#endif

namespace FluentFTP.Proxy.Socks {
	/// <summary>
	///     This class is not reusable.
	///     You have to create a new instance for each connection / attempt.
	/// </summary>
	internal class Socks4aProxy : Socks4Proxy {
		private readonly byte[] _buffer;
		private readonly string _destinationHost;
		private readonly int _destinationPort;
		private readonly FtpSocketStream _socketStream;

		public Socks4aProxy(string destinationHost, int destinationPort, FtpSocketStream socketStream)
			: base(destinationHost, destinationPort, socketStream) {
			_buffer = new byte[512];
			_destinationHost = destinationHost;
			_destinationPort = destinationPort;
			_socketStream = socketStream;
		}

		public override void Connect() {
			// The client connects to the server,
			// and sends a version identifier / method selection message.
			byte[] destIp = { 0, 0, 0, 1 };
			byte[] destPort = GetDestinationPortBytes(_destinationPort);
			byte[] hostBytes = ASCIIEncoding.ASCII.GetBytes(_destinationHost);

			var methodsBuffer = new byte[10 + hostBytes.Length];
			methodsBuffer[0] = (byte)SocksVersion.V4; // VER
			methodsBuffer[1] = 0x01; // NMETHODS
			destPort.CopyTo(methodsBuffer, 2);
			destIp.CopyTo(methodsBuffer, 4);
			methodsBuffer[8] = 0x00;  // null (byte with all zeros) terminator
			hostBytes.CopyTo(methodsBuffer, 9);  // copy the host name to the request byte array
			methodsBuffer[9 + hostBytes.Length] = 0x00;  // null (byte with all zeros) terminator

			_socketStream.Write(methodsBuffer, 0, methodsBuffer.Length);

			// The server selects from one of the methods given in METHODS,
			// and sends a METHOD selection message:
			var receivedBytes = _socketStream.Read(_buffer, 0, 2);
			if (receivedBytes != 2) {
				_socketStream.Close();
				throw new FtpProxyException($"Negotiation Response had an invalid length of {receivedBytes}");
			}
			if (_buffer[1] != 90) {
				_socketStream.Close();
				if (_buffer[1] == 91) {
					throw new FtpProxyException("Request rejected or failed");
				}
				if (_buffer[1] == 92) {
					throw new FtpProxyException("Request rejected becuase SOCKS server cannot connect to identd on the client");
				}
				if (_buffer[1] == 93) {
					throw new FtpProxyException("Request rejected because the client program and identd report different user-ids");
				}
				throw new FtpProxyException($"Unknown error with code {_buffer[1]}");
			}
		}

#if ASYNC
		public override async Task ConnectAsync() {
			// The client connects to the server,
			// and sends a version identifier / method selection message.
			byte[] destIp = { 0, 0, 0, 1 };
			byte[] destPort = GetDestinationPortBytes(_destinationPort);
			byte[] hostBytes = ASCIIEncoding.ASCII.GetBytes(_destinationHost);

			var methodsBuffer = new byte[14];
			methodsBuffer[0] = (byte)SocksVersion.V4; // VER
			methodsBuffer[1] = 0x01; // NMETHODS
			destPort.CopyTo(methodsBuffer, 2);
			destIp.CopyTo(methodsBuffer, 4);
			methodsBuffer[8] = 0x00;  // null (byte with all zeros) terminator
			hostBytes.CopyTo(methodsBuffer, 9);  // copy the host name to the request byte array
			methodsBuffer[13] = 0x00;  // null (byte with all zeros) terminator

			await _socketStream.WriteAsync(methodsBuffer, 0, methodsBuffer.Length);

			// The server selects from one of the methods given in METHODS,
			// and sends a METHOD selection message:
			var receivedBytes = await _socketStream.ReadAsync(_buffer, 0, 2);
			if (receivedBytes != 2) {
				_socketStream.Close();
				throw new FtpProxyException($"Negotiation Response had an invalid length of {receivedBytes}");
			}
			if (_buffer[1] != 90) {
				_socketStream.Close();
				if (_buffer[1] == 91) {
					throw new FtpProxyException("Request rejected or failed");
				}
				if (_buffer[1] == 92) {
					throw new FtpProxyException("Request rejected becuase SOCKS server cannot connect to identd on the client");
				}
				if (_buffer[1] == 93) {
					throw new FtpProxyException("Request rejected because the client program and identd report different user-ids");
				}
				throw new FtpProxyException($"Unknown error with code {_buffer[1]}");
			}
		}
#endif
	}
}
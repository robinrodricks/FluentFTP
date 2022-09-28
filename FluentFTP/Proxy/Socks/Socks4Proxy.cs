using FluentFTP.Exceptions;
using FluentFTP.Proxy.Enums;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP.Proxy.Socks {
	/// <summary>
	///     This class is not reusable.
	///     You have to create a new instance for each connection / attempt.
	/// </summary>
	internal class Socks4Proxy {
		private readonly byte[] _buffer;
		private readonly string _destinationHost;
		private readonly int _destinationPort;
		private readonly FtpSocketStream _socketStream;

		public Socks4Proxy(string destinationHost, int destinationPort, FtpSocketStream socketStream) {
			_buffer = new byte[512];
			_destinationHost = destinationHost;
			_destinationPort = destinationPort;
			_socketStream = socketStream;
		}


		/// <summary>
		/// Translate the host name or IP address to a byte array.
		/// </summary>
		/// <param name="destinationHost">Host name or IP address.</param>
		/// <returns>Byte array representing IP address in bytes.</returns>
		protected byte[] GetIPAddressBytes(string destinationHost) {
			IPAddress ipAddr = null;

			//  if the address doesn't parse then try to resolve with dns
			if (!IPAddress.TryParse(destinationHost, out ipAddr)) {
				throw new ArgumentException(String.Format("An error occurred while attempting to parse the host IP address {0}.", destinationHost));
			}

			// return address bytes
			return ipAddr.GetAddressBytes();
		}

		/// <summary>
		/// Translate the destination port value to a byte array.
		/// </summary>
		/// <param name="value">Destination port.</param>
		/// <returns>Byte array representing an 16 bit port number as two bytes.</returns>
		protected byte[] GetDestinationPortBytes(int value) {
			byte[] array = new byte[2];
			array[0] = Convert.ToByte(value / 256);
			array[1] = Convert.ToByte(value % 256);
			return array;
		}

		public virtual void Connect() {
			// The client connects to the server,
			// and sends a version identifier / method selection message.
			byte[] destIp = GetIPAddressBytes(_destinationHost);
			byte[] destPort = GetDestinationPortBytes(_destinationPort);

			var methodsBuffer = new byte[9];
			methodsBuffer[0] = (byte)SocksVersion.V4; // VER
			methodsBuffer[1] = 0x01; // NMETHODS
			destPort.CopyTo(methodsBuffer, 2);
			destIp.CopyTo(methodsBuffer, 4);
			methodsBuffer[8] = 0x00;  // null (byte with all zeros) terminator

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

		public virtual async Task ConnectAsync(CancellationToken cancellationToken) {
			// The client connects to the server,
			// and sends a version identifier / method selection message.
			byte[] destIp = GetIPAddressBytes(_destinationHost);
			byte[] destPort = GetDestinationPortBytes(_destinationPort);

			var methodsBuffer = new byte[9];
			methodsBuffer[0] = (byte)SocksVersion.V4; // VER
			methodsBuffer[1] = 0x01; // NMETHODS
			destPort.CopyTo(methodsBuffer, 2);
			destIp.CopyTo(methodsBuffer, 4);
			methodsBuffer[8] = 0x00;  // null (byte with all zeros) terminator

			await _socketStream.WriteAsync(methodsBuffer, 0, methodsBuffer.Length, cancellationToken);

			// The server selects from one of the methods given in METHODS,
			// and sends a METHOD selection message:
			var receivedBytes = await _socketStream.ReadAsync(_buffer, 0, 2, cancellationToken);
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

	}
}
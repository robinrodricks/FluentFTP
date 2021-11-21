using System;
using System.Net;
using System.Text;

#if ASYNC
using System.Threading.Tasks;
#endif

namespace FluentFTP.Proxy.Socks
{
	/// <summary>
	///     This class is not reusable.
	///     You have to create a new instance for each connection / attempt.
	/// </summary>
	public class SocksProxy
	{
		private readonly byte[] _buffer;
		private readonly string _destinationHost;
		private readonly int _destinationPort;
		private readonly FtpSocketStream _socketStream;
		private SocksAuthType? _authType;

		public SocksProxy(string destinationHost, int destinationPort, FtpSocketStream socketStream)
		{
			_buffer = new byte[512];
			_destinationHost = destinationHost;
			_destinationPort = destinationPort;
			_socketStream = socketStream;
		}

		public void Negotiate()
		{
			// The client connects to the server,
			// and sends a version identifier / method selection message.
			var methodsBuffer = new byte[]
			{
				(byte)SocksVersion.Five, // VER
				0x01, // NMETHODS
				(byte)SocksAuthType.NoAuthRequired // Methods
			};

			_socketStream.Write(methodsBuffer, 0, methodsBuffer.Length);

			// The server selects from one of the methods given in METHODS,
			// and sends a METHOD selection message:
			var receivedBytes = _socketStream.Read(_buffer, 0, 2);
			if (receivedBytes != 2)
			{
				_socketStream.Close();
				throw new SocksProxyException($"Negotiation Response had an invalid length of {receivedBytes}");
			}

			_authType = (SocksAuthType)_buffer[1];
		}

		public void Authenticate()
		{
			AuthenticateInternal();
		}

		public void Connect()
		{
			var requestBuffer = GetConnectRequest();
			_socketStream.Write(requestBuffer, 0, requestBuffer.Length);

			SocksReply reply;
			
			// The server evaluates the request, and returns a reply.
			// - First we read VER, REP, RSV & ATYP
			var received = _socketStream.Read(_buffer, 0, 4);
			if (received != 4)
			{
				if (received >= 2)
				{
					reply = (SocksReply)_buffer[1];
					HandleProxyCommandError(reply);
				}

				_socketStream.Close();
				throw new SocksProxyException($"Connect Reply has Invalid Length {received}. Expecting 4.");
			}

			// - Now we check if the reply was positive.
			reply = (SocksReply)_buffer[1];

			if (reply != SocksReply.Succeeded)
			{
				HandleProxyCommandError(reply);
			}

			// - Consume rest of the SOCKS5 protocol so the next read will give application data.
			var atyp = (SocksRequestAddressType)_buffer[3];
			int atypSize;
			int read;

			switch (atyp)
			{
				case SocksRequestAddressType.IPv4:
					atypSize = 6;
					read = _socketStream.Read(_buffer, 0, atypSize);
					break;
				case SocksRequestAddressType.IPv6:
					atypSize = 18;
					read = _socketStream.Read(_buffer, 0, atypSize);
					break;
				case SocksRequestAddressType.FQDN:
					atypSize = 1;
					_socketStream.Read(_buffer, 0, atypSize);
					atypSize = _buffer[0] + 2;
					read = _socketStream.Read(_buffer, 0, atypSize);
					break;
				default:
					_socketStream.Close();
					throw new SocksProxyException("Unknown Socks Request Address Type", new ArgumentOutOfRangeException());
			}

			if (read != atypSize)
			{
				_socketStream.Close();
				throw new SocksProxyException($"Unexpected Response size from Request Type Data. Expected {atypSize} received {read}");
			}
		}


		private void AuthenticateInternal()
		{
			if (!_authType.HasValue)
			{
				_socketStream.Close();
				throw new SocksProxyException("Invalid Auth Type Declared, see inner exception for details.", new ArgumentException("No SOCKS5 auth method has been set."));
			}

			// The client and server then enter a method-specific sub-negotiation.
			switch (_authType.Value)
			{
				case SocksAuthType.NoAuthRequired:
					break;

				case SocksAuthType.GSSAPI:
					_socketStream.Close();
					throw new SocksProxyException("Invalid Auth Type Declared, see inner exception for details.", new NotSupportedException("GSSAPI is not implemented."));

				case SocksAuthType.UsernamePassword:
					_socketStream.Close();
					throw new SocksProxyException("Invalid Auth Type Declared, see inner exception for details.",
						new NotSupportedException("UsernamePassword is not implemented."));

				// If the selected METHOD is X'FF', none of the methods listed by the
				// client are acceptable, and the client MUST close the connection
				case SocksAuthType.NoAcceptableMethods:
					_socketStream.Close();
					throw new SocksProxyException("Invalid Auth Type Declared, see inner exception for details.",
						new MissingMethodException("METHOD is X'FF' No Client requested methods are acceptable. Closing the connection."));

				default:
					_socketStream.Close();
					throw new SocksProxyException("Invalid Auth Type Declared, see inner exception for details.",
						new ArgumentOutOfRangeException());
			}
		}

		private byte[] GetConnectRequest()
		{
			// Once the method-dependent sub negotiation has completed,
			// the client sends the request details.
			bool issHostname = !IPAddress.TryParse(_destinationHost, out var ip);

			var dstAddress = issHostname
				? Encoding.ASCII.GetBytes(_destinationHost)
				: ip.GetAddressBytes();

			var requestBuffer = issHostname
				? new byte[7 + dstAddress.Length]
				: new byte[6 + dstAddress.Length];

			requestBuffer[0] = (byte)SocksVersion.Five;
			requestBuffer[1] = (byte)SocksRequestCommand.Connect;

			if (issHostname)
			{
				requestBuffer[3] = (byte)SocksRequestAddressType.FQDN;
				requestBuffer[4] = (byte)dstAddress.Length;

				for (var i = 0; i < dstAddress.Length; i++)
				{
					requestBuffer[5 + i] = dstAddress[i];
				}

				requestBuffer[5 + dstAddress.Length] = (byte)(_destinationPort >> 8);
				requestBuffer[6 + dstAddress.Length] = (byte)_destinationPort;
			}
			else
			{
				requestBuffer[3] = dstAddress.Length == 4
					? (byte)SocksRequestAddressType.IPv4
					: (byte)SocksRequestAddressType.IPv6;

				for (var i = 0; i < dstAddress.Length; i++)
				{
					requestBuffer[4 + i] = dstAddress[i];
				}

				requestBuffer[4 + dstAddress.Length] = (byte)(_destinationPort >> 8);
				requestBuffer[5 + dstAddress.Length] = (byte)_destinationPort;
			}

			return requestBuffer;
		}

#if ASYNC
		public async Task NegotiateAsync()
		{
			// The client connects to the server,
			// and sends a version identifier / method selection message.
			var methodsBuffer = new byte[]
			{
				(byte)SocksVersion.Five, // VER
				0x01, // NMETHODS
				(byte)SocksAuthType.NoAuthRequired // Methods
			};

			await _socketStream.WriteAsync(methodsBuffer, 0, methodsBuffer.Length);

			// The server selects from one of the methods given in METHODS,
			// and sends a METHOD selection message:
			var receivedBytes = await _socketStream.ReadAsync(_buffer, 0, 2);
			if (receivedBytes != 2)
			{
				_socketStream.Close();
				throw new SocksProxyException($"Negotiation Response had an invalid length of {receivedBytes}");
			}

			_authType = (SocksAuthType)_buffer[1];
		}

		public Task AuthenticateAsync()
		{
			AuthenticateInternal();
			return Task.FromResult(0);
		}

		public async Task ConnectAsync()
		{
			var requestBuffer = GetConnectRequest();
			await _socketStream.WriteAsync(requestBuffer, 0, requestBuffer.Length);

			SocksReply reply;

			// The server evaluates the request, and returns a reply.
			// - First we read VER, REP, RSV & ATYP
			var received = await _socketStream.ReadAsync(_buffer, 0, 4);
			if (received != 4)
			{
				if (received >= 2)
				{
					reply = (SocksReply)_buffer[1];
					HandleProxyCommandError(reply);
				}

				_socketStream.Close();
				throw new SocksProxyException($"Connect Reply has Invalid Length {received}. Expecting 4.");
			}

			// - Now we check if the reply was positive.
			reply = (SocksReply)_buffer[1];

			if (reply != SocksReply.Succeeded)
			{
				HandleProxyCommandError(reply);
			}

			// - Consume rest of the SOCKS5 protocol so the next read will give application data.
			var atyp = (SocksRequestAddressType)_buffer[3];
			int atypSize;
			int read;

			switch (atyp)
			{
				case SocksRequestAddressType.IPv4:
					atypSize = 6;
					read = await _socketStream.ReadAsync(_buffer, 0, atypSize);
					break;
				case SocksRequestAddressType.IPv6:
					atypSize = 18;
					read = await _socketStream.ReadAsync(_buffer, 0, atypSize);
					break;
				case SocksRequestAddressType.FQDN:
					atypSize = 1;
					await _socketStream.ReadAsync(_buffer, 0, atypSize);

					atypSize = _buffer[0] + 2;
					read = await _socketStream.ReadAsync(_buffer, 0, atypSize);
					break;
				default:
					_socketStream.Close();
					throw new SocksProxyException("Unknown Socks Request Address Type", new ArgumentOutOfRangeException());
			}

			if (read != atypSize)
			{
				_socketStream.Close();
				throw new SocksProxyException($"Unexpected Response size from Request Type Data. Expected {atypSize} received {read}");
			}
		}
#endif
		private void HandleProxyCommandError(SocksReply replyCode)
		{
			string proxyErrorText;
			switch (replyCode)
			{
				case SocksReply.GeneralSOCKSServerFailure:
					proxyErrorText = "a general socks destination failure occurred";
					break;
				case SocksReply.NotAllowedByRuleset:
					proxyErrorText = "the connection is not allowed by proxy destination rule set";
					break;
				case SocksReply.NetworkUnreachable:
					proxyErrorText = "the network was unreachable";
					break;
				case SocksReply.HostUnreachable:
					proxyErrorText = "the host was unreachable";
					break;
				case SocksReply.ConnectionRefused:
					proxyErrorText = "the connection was refused by the remote network";
					break;
				case SocksReply.TTLExpired:
					proxyErrorText = "the time to live (TTL) has expired";
					break;
				case SocksReply.CommandNotSupported:
					proxyErrorText = "the command issued by the proxy client is not supported by the proxy destination";
					break;
				case SocksReply.AddressTypeNotSupported:
					proxyErrorText = "the address type specified is not supported";
					break;
				default:
					proxyErrorText = $"an unknown SOCKS reply with the code value '{replyCode}' was received";
					break;
			}

			_socketStream.Close();
			throw new SocksProxyException($"Proxy error: {proxyErrorText} for destination host {_destinationHost} port number {_destinationPort}.");
		}
	}
}
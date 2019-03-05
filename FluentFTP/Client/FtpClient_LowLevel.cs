using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Globalization;
using System.Security.Authentication;
using System.Net;
using FluentFTP.Proxy;
#if !CORE
using System.Web;
#endif

#if (CORE || NETFX)
using System.Threading;
#endif
#if ASYNC
using System.Threading.Tasks;
#endif

namespace FluentFTP {
	
	public partial class FtpClient : IDisposable {

		#region Active/Passive Streams

		/// <summary>
		/// Opens the specified type of passive data stream
		/// </summary>
		/// <param name="type">Type of passive data stream to open</param>
		/// <param name="command">The command to execute that requires a data stream</param>
		/// <param name="restart">Restart location in bytes for file transfer</param>
		/// <returns>A data stream ready to be used</returns>
		FtpDataStream OpenPassiveDataStream(FtpDataConnectionType type, string command, long restart) {

			this.LogFunc("OpenPassiveDataStream", new object[] { type, command, restart });

			FtpDataStream stream = null;
			FtpReply reply;
			Match m;
			string host = null;
			int port = 0;

			if (m_stream == null)
				throw new InvalidOperationException("The control connection stream is null! Generally this means there is no connection to the server. Cannot open a passive data stream.");

			if (type == FtpDataConnectionType.EPSV || type == FtpDataConnectionType.AutoPassive) {
				if (!(reply = Execute("EPSV")).Success) {
					// if we're connected with IPv4 and data channel type is AutoPassive then fallback to IPv4
					if ((reply.Type == FtpResponseType.TransientNegativeCompletion || reply.Type == FtpResponseType.PermanentNegativeCompletion) && type == FtpDataConnectionType.AutoPassive && m_stream != null && m_stream.LocalEndPoint.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
						return OpenPassiveDataStream(FtpDataConnectionType.PASV, command, restart);
					throw new FtpCommandException(reply);
				}

				m = Regex.Match(reply.Message, @"\(\|\|\|(?<port>\d+)\|\)");
				if (!m.Success) {
					throw new FtpException("Failed to get the EPSV port from: " + reply.Message);
				}

				host = m_host;
				port = int.Parse(m.Groups["port"].Value);
			} else {
				if (m_stream.LocalEndPoint.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
					throw new FtpException("Only IPv4 is supported by the PASV command. Use EPSV instead.");

				if (!(reply = Execute("PASV")).Success)
					throw new FtpCommandException(reply);

				m = Regex.Match(reply.Message, @"(?<quad1>\d+)," + @"(?<quad2>\d+)," + @"(?<quad3>\d+)," + @"(?<quad4>\d+)," + @"(?<port1>\d+)," + @"(?<port2>\d+)");

				if (!m.Success || m.Groups.Count != 7)
					throw new FtpException(("Malformed PASV response: " + reply.Message));

				// PASVEX mode ignores the host supplied in the PASV response
				if (type == FtpDataConnectionType.PASVEX)
					host = m_host;
				else
					host = (m.Groups["quad1"].Value + "." + m.Groups["quad2"].Value + "." + m.Groups["quad3"].Value + "." + m.Groups["quad4"].Value);

				port = (int.Parse(m.Groups["port1"].Value) << 8) + int.Parse(m.Groups["port2"].Value);

				//use host ip if server advertises a non-routeable IP
				m = Regex.Match(host, @"(^10\.)|(^172\.1[6-9]\.)|(^172\\.2[0-9]\.)|(^172\.3[0-1]\.)|(^192\.168\.)|(^127\.0\.0\.1)|(^0\.0\.0\.0)");

				if (m.Success) {
					host = m_host;
				}
			}

			stream = new FtpDataStream(this);
			stream.Client = this;
			stream.ConnectTimeout = DataConnectionConnectTimeout;
			stream.ReadTimeout = DataConnectionReadTimeout;
			Connect(stream, host, port, InternetProtocolVersions);
			stream.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.KeepAlive, m_keepAlive);

			if (restart > 0) {
				if (!(reply = Execute("REST " + restart)).Success)
					throw new FtpCommandException(reply);
			}

			if (!(reply = Execute(command)).Success) {
				stream.Close();
				if (command.StartsWith("NLST ") && reply.Code == "550" && reply.Message == "No files found.") {
					//workaround for ftpd which responses "550 No files found." when folder exists but is empty
				} else {
					throw new FtpCommandException(reply);
				}
			}

			// the command status is used to determine
			// if a reply needs to be read from the server
			// when the stream is closed so always set it
			// otherwise things can get out of sync.
			stream.CommandStatus = reply;

#if !NO_SSL
			// this needs to take place after the command is executed
			if (m_dataConnectionEncryption && m_encryptionmode != FtpEncryptionMode.None) {
				stream.ActivateEncryption(m_host,
					this.ClientCertificates.Count > 0 ? this.ClientCertificates : null,
					m_SslProtocols);
			}
#endif

			return stream;
		}

#if ASYNC
        /// <summary>
        /// Opens the specified type of passive data stream
        /// </summary>
        /// <param name="type">Type of passive data stream to open</param>
        /// <param name="command">The command to execute that requires a data stream</param>
        /// <param name="restart">Restart location in bytes for file transfer</param>
        /// <returns>A data stream ready to be used</returns>
        async Task<FtpDataStream> OpenPassiveDataStreamAsync(FtpDataConnectionType type, string command, long restart, CancellationToken token = default(CancellationToken))
        {

            this.LogFunc(nameof(OpenPassiveDataStreamAsync), new object[] { type, command, restart });

            FtpDataStream stream = null;
            FtpReply reply;
            Match m;
            string host = null;
            int port = 0;

            if (m_stream == null)
                throw new InvalidOperationException("The control connection stream is null! Generally this means there is no connection to the server. Cannot open a passive data stream.");

            if (type == FtpDataConnectionType.EPSV || type == FtpDataConnectionType.AutoPassive)
            {
                if (!(reply = await ExecuteAsync("EPSV", token)).Success)
                {
                    // if we're connected with IPv4 and data channel type is AutoPassive then fallback to IPv4
                    if ((reply.Type == FtpResponseType.TransientNegativeCompletion || reply.Type == FtpResponseType.PermanentNegativeCompletion) && type == FtpDataConnectionType.AutoPassive && m_stream != null && m_stream.LocalEndPoint.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        return await OpenPassiveDataStreamAsync(FtpDataConnectionType.PASV, command, restart, token);
                    throw new FtpCommandException(reply);
                }

                m = Regex.Match(reply.Message, @"\(\|\|\|(?<port>\d+)\|\)");
                if (!m.Success)
                {
                    throw new FtpException("Failed to get the EPSV port from: " + reply.Message);
                }

                host = m_host;
                port = int.Parse(m.Groups["port"].Value);
            }
            else
            {
                if (m_stream.LocalEndPoint.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                    throw new FtpException("Only IPv4 is supported by the PASV command. Use EPSV instead.");

                if (!(reply = await ExecuteAsync("PASV", token)).Success)
                    throw new FtpCommandException(reply);

                m = Regex.Match(reply.Message, @"(?<quad1>\d+)," + @"(?<quad2>\d+)," + @"(?<quad3>\d+)," + @"(?<quad4>\d+)," + @"(?<port1>\d+)," + @"(?<port2>\d+)");

                if (!m.Success || m.Groups.Count != 7)
                    throw new FtpException(("Malformed PASV response: " + reply.Message));

                // PASVEX mode ignores the host supplied in the PASV response
                if (type == FtpDataConnectionType.PASVEX)
                    host = m_host;
                else
                    host = (m.Groups["quad1"].Value + "." + m.Groups["quad2"].Value + "." + m.Groups["quad3"].Value + "." + m.Groups["quad4"].Value);

                port = (int.Parse(m.Groups["port1"].Value) << 8) + int.Parse(m.Groups["port2"].Value);

				//use host ip if server advertises a non-routeable IP
				m = Regex.Match(host,@"(^10\.)|(^172\.1[6-9]\.)|(^172\\.2[0-9]\.)|(^172\.3[0-1]\.)|(^192\.168\.)|(^127\.0\.0\.1)|(^0\.0\.0\.0)");

				if(m.Success){			
					host = m_host;
				}
            }

            stream = new FtpDataStream(this);
			stream.Client = this;
            stream.ConnectTimeout = DataConnectionConnectTimeout;
            stream.ReadTimeout = DataConnectionReadTimeout;
            await ConnectAsync(stream, host, port, InternetProtocolVersions, token);
            stream.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.KeepAlive, m_keepAlive);

            if (restart > 0)
            {
                if (!(reply = await ExecuteAsync("REST " + restart, token)).Success)
                    throw new FtpCommandException(reply);
            }

            if (!(reply = await ExecuteAsync(command, token)).Success)
            {
                stream.Close();
                throw new FtpCommandException(reply);
            }

            // the command status is used to determine
            // if a reply needs to be read from the server
            // when the stream is closed so always set it
            // otherwise things can get out of sync.
            stream.CommandStatus = reply;

#if !NO_SSL
            // this needs to take place after the command is executed
            if (m_dataConnectionEncryption && m_encryptionmode != FtpEncryptionMode.None)
            {
                await stream.ActivateEncryptionAsync(m_host,
                    this.ClientCertificates.Count > 0 ? this.ClientCertificates : null,
                    m_SslProtocols);
            }
#endif

            return stream;
        }
#endif

		/// <summary>
		/// Returns the ip address to be sent to the server for the active connection
		/// </summary>
		/// <param name="ip"></param>
		/// <returns></returns>
		string GetLocalAddress(IPAddress ip) {
			// Use resolver
			if (m_AddressResolver != null) {
				return m_Address ?? (m_Address = m_AddressResolver());
			}

			// Use supplied ip
			return ip.ToString();
		}

		/// <summary>
		/// Opens the specified type of active data stream
		/// </summary>
		/// <param name="type">Type of passive data stream to open</param>
		/// <param name="command">The command to execute that requires a data stream</param>
		/// <param name="restart">Restart location in bytes for file transfer</param>
		/// <returns>A data stream ready to be used</returns>
		FtpDataStream OpenActiveDataStream(FtpDataConnectionType type, string command, long restart) {

			this.LogFunc("OpenActiveDataStream", new object[] { type, command, restart });

			FtpDataStream stream = new FtpDataStream(this);
			stream.Client = this;
			FtpReply reply;
#if !CORE
			IAsyncResult ar;
#endif

			if (m_stream == null)
				throw new InvalidOperationException("The control connection stream is null! Generally this means there is no connection to the server. Cannot open an active data stream.");

			if (m_ActivePorts == null || !m_ActivePorts.Any()) {
				// Use random port
				stream.Listen(m_stream.LocalEndPoint.Address, 0);
			} else {
				var success = false;
				// Use one of the specified ports
				foreach (var port in m_ActivePorts) {
					try {
						stream.Listen(m_stream.LocalEndPoint.Address, port);
						success = true;
						break;
					} catch (SocketException se) {
#if NETFX
						// Already in use
						if (se.ErrorCode != 10048)
							throw;
#else
                        throw;
#endif
					}
				}

				// No usable port found
				if (!success)
					throw new Exception("No valid active data port available!");
			}
#if !CORE
			ar = stream.BeginAccept(null, null);
#endif

			if (type == FtpDataConnectionType.EPRT || type == FtpDataConnectionType.AutoActive) {
				int ipver = 0;

				switch (stream.LocalEndPoint.AddressFamily) {
					case System.Net.Sockets.AddressFamily.InterNetwork:
						ipver = 1; // IPv4
						break;
					case System.Net.Sockets.AddressFamily.InterNetworkV6:
						ipver = 2; // IPv6
						break;
					default:
						throw new InvalidOperationException("The IP protocol being used is not supported.");
				}

				if (!(reply = Execute("EPRT |" + ipver + "|" + GetLocalAddress(stream.LocalEndPoint.Address) + "|" + stream.LocalEndPoint.Port + "|")).Success) {

					// if we're connected with IPv4 and the data channel type is AutoActive then try to fall back to the PORT command
					if (reply.Type == FtpResponseType.PermanentNegativeCompletion && type == FtpDataConnectionType.AutoActive && m_stream != null && m_stream.LocalEndPoint.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) {
						stream.ControlConnection = null; // we don't want this failed EPRT attempt to close our control connection when the stream is closed so clear out the reference.
						stream.Close();
						return OpenActiveDataStream(FtpDataConnectionType.PORT, command, restart);
					} else {
						stream.Close();
						throw new FtpCommandException(reply);
					}
				}
			} else {
				if (m_stream.LocalEndPoint.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
					throw new FtpException("Only IPv4 is supported by the PORT command. Use EPRT instead.");

				if (!(reply = Execute("PORT " +
						GetLocalAddress(stream.LocalEndPoint.Address).Replace('.', ',') + "," +
						stream.LocalEndPoint.Port / 256 + "," +
						stream.LocalEndPoint.Port % 256)).Success) {
					stream.Close();
					throw new FtpCommandException(reply);
				}
			}

			if (restart > 0) {
				if (!(reply = Execute("REST " + restart)).Success)
					throw new FtpCommandException(reply);
			}

			if (!(reply = Execute(command)).Success) {
				stream.Close();
				throw new FtpCommandException(reply);
			}

			// the command status is used to determine
			// if a reply needs to be read from the server
			// when the stream is closed so always set it
			// otherwise things can get out of sync.
			stream.CommandStatus = reply;

#if CORE
			stream.AcceptAsync().Wait();
#else
			ar.AsyncWaitHandle.WaitOne(m_dataConnectionConnectTimeout);
			if (!ar.IsCompleted) {
				stream.Close();
				throw new TimeoutException("Timed out waiting for the server to connect to the active data socket.");
			}

			stream.EndAccept(ar);
#endif

#if !NO_SSL
			if (m_dataConnectionEncryption && m_encryptionmode != FtpEncryptionMode.None) {
				stream.ActivateEncryption(m_host,
					this.ClientCertificates.Count > 0 ? this.ClientCertificates : null,
					m_SslProtocols);
			}
#endif

			stream.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.KeepAlive, m_keepAlive);
			stream.ReadTimeout = m_dataConnectionReadTimeout;

			return stream;
		}

#if ASYNC
		/// <summary>
		/// Opens the specified type of active data stream
		/// </summary>
		/// <param name="type">Type of passive data stream to open</param>
		/// <param name="command">The command to execute that requires a data stream</param>
		/// <param name="restart">Restart location in bytes for file transfer</param>
		/// <param name="token">Cancellation Token</param>
		/// <returns>A data stream ready to be used</returns>
		async Task<FtpDataStream> OpenActiveDataStreamAsync(FtpDataConnectionType type, string command, long restart, CancellationToken token = default(CancellationToken))
        {

            this.LogFunc(nameof(OpenActiveDataStreamAsync), new object[] { type, command, restart });

            FtpDataStream stream = new FtpDataStream(this);
			stream.Client = this;
            FtpReply reply;

            if (m_stream == null)
                throw new InvalidOperationException("The control connection stream is null! Generally this means there is no connection to the server. Cannot open an active data stream.");

            if (m_ActivePorts == null || !m_ActivePorts.Any())
            {
                // Use random port
                stream.Listen(m_stream.LocalEndPoint.Address, 0);
            }
            else
            {
                var success = false;
                // Use one of the specified ports
                foreach (var port in m_ActivePorts)
                {
                    try
                    {
                        stream.Listen(m_stream.LocalEndPoint.Address, port);
                        success = true;
	                    break;
                    }
                    catch (SocketException se)
                    {
#if NETFX
                        // Already in use
                        if (se.ErrorCode != 10048)
                            throw;
#else
                        throw;
#endif
                    }
                }

                // No usable port found
                if (!success)
                    throw new Exception("No valid active data port available!");
            }

            var result = stream.AcceptAsync();

            if (type == FtpDataConnectionType.EPRT || type == FtpDataConnectionType.AutoActive)
            {
                int ipver = 0;

                switch (stream.LocalEndPoint.AddressFamily)
                {
                    case System.Net.Sockets.AddressFamily.InterNetwork:
                        ipver = 1; // IPv4
                        break;
                    case System.Net.Sockets.AddressFamily.InterNetworkV6:
                        ipver = 2; // IPv6
                        break;
                    default:
                        throw new InvalidOperationException("The IP protocol being used is not supported.");
                }

                if (!(reply = await ExecuteAsync("EPRT |" + ipver + "|" + GetLocalAddress(stream.LocalEndPoint.Address) + "|" + stream.LocalEndPoint.Port + "|", token)).Success)
                {

                    // if we're connected with IPv4 and the data channel type is AutoActive then try to fall back to the PORT command
                    if (reply.Type == FtpResponseType.PermanentNegativeCompletion && type == FtpDataConnectionType.AutoActive && m_stream != null && m_stream.LocalEndPoint.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        stream.ControlConnection = null; // we don't want this failed EPRT attempt to close our control connection when the stream is closed so clear out the reference.
                        stream.Close();
                        return await OpenActiveDataStreamAsync(FtpDataConnectionType.PORT, command, restart, token);
                    }
                    else
                    {
                        stream.Close();
                        throw new FtpCommandException(reply);
                    }
                }
            }
            else
            {
                if (m_stream.LocalEndPoint.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                    throw new FtpException("Only IPv4 is supported by the PORT command. Use EPRT instead.");

                if (!(reply = await ExecuteAsync("PORT " +
                        GetLocalAddress(stream.LocalEndPoint.Address).Replace('.', ',') + "," +
                        stream.LocalEndPoint.Port / 256 + "," +
                        stream.LocalEndPoint.Port % 256, token)).Success)
                {
                    stream.Close();
                    throw new FtpCommandException(reply);
                }
            }

            if (restart > 0)
            {
                if (!(reply = await ExecuteAsync("REST " + restart, token)).Success)
                    throw new FtpCommandException(reply);
            }

            if (!(reply = await ExecuteAsync(command, token)).Success)
            {
                stream.Close();
                throw new FtpCommandException(reply);
            }

            // the command status is used to determine
            // if a reply needs to be read from the server
            // when the stream is closed so always set it
            // otherwise things can get out of sync.
            stream.CommandStatus = reply;

            await result;

#if !NO_SSL
            if (m_dataConnectionEncryption && m_encryptionmode != FtpEncryptionMode.None)
            {
                await stream.ActivateEncryptionAsync(m_host,
                    this.ClientCertificates.Count > 0 ? this.ClientCertificates : null,
                    m_SslProtocols);
            }
#endif

            stream.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.KeepAlive, m_keepAlive);
            stream.ReadTimeout = m_dataConnectionReadTimeout;

            return stream;
        }
#endif

		/// <summary>
		/// Opens a data stream.
		/// </summary>
		/// <param name='command'>The command to execute that requires a data stream</param>
		/// <param name="restart">Restart location in bytes for file transfer</param>
		/// <returns>The data stream.</returns>
		FtpDataStream OpenDataStream(string command, long restart) {

			FtpDataConnectionType type = m_dataConnectionType;
			FtpDataStream stream = null;

#if !CORE14
			lock (m_lock) {
#endif
				if (!IsConnected)
					Connect();

				// The PORT and PASV commands do not work with IPv6 so
				// if either one of those types are set change them
				// to EPSV or EPRT appropriately.
				if (m_stream.LocalEndPoint.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6) {
					switch (type) {
						case FtpDataConnectionType.PORT:
							type = FtpDataConnectionType.EPRT;
							this.LogLine(FtpTraceLevel.Info, "Changed data connection type to EPRT because we are connected with IPv6.");
							break;
						case FtpDataConnectionType.PASV:
						case FtpDataConnectionType.PASVEX:
							type = FtpDataConnectionType.EPSV;
							this.LogLine(FtpTraceLevel.Info, "Changed data connection type to EPSV because we are connected with IPv6.");
							break;
					}
				}

				switch (type) {
					case FtpDataConnectionType.AutoPassive:
					case FtpDataConnectionType.EPSV:
					case FtpDataConnectionType.PASV:
					case FtpDataConnectionType.PASVEX:
						stream = OpenPassiveDataStream(type, command, restart);
						break;
					case FtpDataConnectionType.AutoActive:
					case FtpDataConnectionType.EPRT:
					case FtpDataConnectionType.PORT:
						stream = OpenActiveDataStream(type, command, restart);
						break;
				}

				if (stream == null)
					throw new InvalidOperationException("The specified data channel type is not implemented.");
#if !CORE14
			}
#endif

			return stream;
		}

#if ASYNC
		/// <summary>
		/// Opens a data stream.
		/// </summary>
		/// <param name='command'>The command to execute that requires a data stream</param>
		/// <param name="restart">Restart location in bytes for file transfer</param>
		/// <param name="token">Cancellation Token</param>
		/// <returns>The data stream.</returns>
		async Task<FtpDataStream> OpenDataStreamAsync(string command, long restart, CancellationToken token = default(CancellationToken))
        {

            FtpDataConnectionType type = m_dataConnectionType;
            FtpDataStream stream = null;

            if (!IsConnected)
                await ConnectAsync(token);

            // The PORT and PASV commands do not work with IPv6 so
            // if either one of those types are set change them
            // to EPSV or EPRT appropriately.
            if (m_stream.LocalEndPoint.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                switch (type)
                {
                    case FtpDataConnectionType.PORT:
                        type = FtpDataConnectionType.EPRT;
                        this.LogLine(FtpTraceLevel.Info, "Changed data connection type to EPRT because we are connected with IPv6.");
                        break;
                    case FtpDataConnectionType.PASV:
                    case FtpDataConnectionType.PASVEX:
                        type = FtpDataConnectionType.EPSV;
                        this.LogLine(FtpTraceLevel.Info, "Changed data connection type to EPSV because we are connected with IPv6.");
                        break;
                }
            }

            switch (type)
            {
                case FtpDataConnectionType.AutoPassive:
                case FtpDataConnectionType.EPSV:
                case FtpDataConnectionType.PASV:
                case FtpDataConnectionType.PASVEX:
                    stream = await OpenPassiveDataStreamAsync(type, command, restart, token);
                    break;
                case FtpDataConnectionType.AutoActive:
                case FtpDataConnectionType.EPRT:
                case FtpDataConnectionType.PORT:
                    stream = await OpenActiveDataStreamAsync(type, command, restart, token);
                    break;
            }

            if (stream == null)
                throw new InvalidOperationException("The specified data channel type is not implemented.");

            return stream;
        }
#endif

		/// <summary>
		/// Disconnects a data stream
		/// </summary>
		/// <param name="stream">The data stream to close</param>
		internal FtpReply CloseDataStream(FtpDataStream stream) {

			this.LogFunc("CloseDataStream");

			FtpReply reply = new FtpReply();

			if (stream == null)
				throw new ArgumentException("The data stream parameter was null");

#if !CORE14
			lock (m_lock) {
#endif
				try {
					if (IsConnected) {
						// if the command that required the data connection was
						// not successful then there will be no reply from
						// the server, however if the command was successful
						// the server will send a reply when the data connection
						// is closed.
						if (stream.CommandStatus.Type == FtpResponseType.PositivePreliminary) {
							if (!(reply = GetReply()).Success) {
								throw new FtpCommandException(reply);
							}
						}
					}
				} finally {
					// if this is a clone of the original control
					// connection we should Dispose()
					if (IsClone) {
						Disconnect();
						Dispose();
					}
				}
#if !CORE14
			}
#endif

			return reply;
		}

		#endregion

		#region Open Read

		/// <summary>
		/// Opens the specified file for reading
		/// </summary>
		/// <param name="path">The full or relative path of the file</param>
		/// <returns>A stream for reading the file on the server</returns>
		/// <example><code source="..\Examples\OpenRead.cs" lang="cs" /></example>
		public Stream OpenRead(string path) {
			return OpenRead(path, FtpDataType.Binary, 0, true);
		}

		/// <summary>
		/// Opens the specified file for reading
		/// </summary>
		/// <param name="path">The full or relative path of the file</param>
		/// <param name="type">ASCII/Binary</param>
		/// <returns>A stream for reading the file on the server</returns>
		/// <example><code source="..\Examples\OpenRead.cs" lang="cs" /></example>
		public Stream OpenRead(string path, FtpDataType type) {
			return OpenRead(path, type, 0, true);
		}

		/// <summary>
		/// Opens the specified file for reading
		/// </summary>
		/// <param name="path">The full or relative path of the file</param>
		/// <param name="type">ASCII/Binary</param>
		/// <param name="checkIfFileExists">Only set this to false if you are SURE that the file does not exist. If true, it reads the file size and saves it into the stream length.</param>
		/// <returns>A stream for reading the file on the server</returns>
		/// <example><code source="..\Examples\OpenRead.cs" lang="cs" /></example>
		public Stream OpenRead(string path, FtpDataType type, bool checkIfFileExists) {
			return OpenRead(path, type, 0, checkIfFileExists);
		}

		/// <summary>
		/// Opens the specified file for reading
		/// </summary>
		/// <param name="path">The full or relative path of the file</param>
		/// <param name="type">ASCII/Binary</param>
		/// <param name="restart">Resume location</param>
		/// <returns>A stream for reading the file on the server</returns>
		/// <example><code source="..\Examples\OpenRead.cs" lang="cs" /></example>
		public virtual Stream OpenRead(string path, FtpDataType type, long restart) {
			return OpenRead(path, type, restart, true);
		}

		/// <summary>
		/// Opens the specified file for reading
		/// </summary>
		/// <param name="path">The full or relative path of the file</param>
		/// <param name="restart">Resume location</param>
		/// <returns>A stream for reading the file on the server</returns>
		/// <example><code source="..\Examples\OpenRead.cs" lang="cs" /></example>
		public Stream OpenRead(string path, long restart) {
			return OpenRead(path, FtpDataType.Binary, restart, true);
		}

		/// <summary>
		/// Opens the specified file for reading
		/// </summary>
		/// <param name="path">The full or relative path of the file</param>
		/// <param name="restart">Resume location</param>
		/// <param name="checkIfFileExists">Only set this to false if you are SURE that the file does not exist. If true, it reads the file size and saves it into the stream length.</param>
		/// <returns>A stream for reading the file on the server</returns>
		/// <example><code source="..\Examples\OpenRead.cs" lang="cs" /></example>
		public Stream OpenRead(string path, long restart, bool checkIfFileExists) {
			return OpenRead(path, FtpDataType.Binary, restart, checkIfFileExists);
		}

		/// <summary>
		/// Opens the specified file for reading
		/// </summary>
		/// <param name="path">The full or relative path of the file</param>
		/// <param name="type">ASCII/Binary</param>
		/// <param name="restart">Resume location</param>
		/// <param name="checkIfFileExists">Only set this to false if you are SURE that the file does not exist. If true, it reads the file size and saves it into the stream length.</param>
		/// <returns>A stream for reading the file on the server</returns>
		/// <example><code source="..\Examples\OpenRead.cs" lang="cs" /></example>
		public virtual Stream OpenRead(string path, FtpDataType type, long restart, bool checkIfFileExists) {

			// verify args
			if (path.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "path");

			this.LogFunc("OpenRead", new object[] { path, type, restart });

			FtpClient client = null;
			FtpDataStream stream = null;
			long length = 0;

#if !CORE14
			lock (m_lock) {
#endif
				this.SetDataType(type);
				if (m_threadSafeDataChannels) {
					client = CloneConnection();
					client.Connect();
					client.SetWorkingDirectory(GetWorkingDirectory());
				} else {
					client = this;
				}

				length = checkIfFileExists ? client.GetFileSize(path) : 0;
				stream = client.OpenDataStream(("RETR " + path.GetFtpPath()), restart);
#if !CORE14
			}
#endif

			if (stream != null) {
				if (length > 0)
					stream.SetLength(length);

				if (restart > 0)
					stream.SetPosition(restart);
			}

			return stream;
		}

#if !CORE
		/// <summary>
		/// Begins an asynchronous operation to open the specified file for reading
		/// </summary>
		/// <param name="path">The full or relative path of the file</param>
		/// <param name="callback">Async Callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginOpenRead.cs" lang="cs" /></example>
		public IAsyncResult BeginOpenRead(string path, AsyncCallback callback, object state) {
			return BeginOpenRead(path, FtpDataType.Binary, 0, callback, state);
		}

		/// <summary>
		/// Opens the specified file for reading
		/// </summary>
		/// <param name="path">The full or relative path of the file</param>
		/// <param name="type">ASCII/Binary</param>
		/// <param name="callback">Async Callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginOpenRead.cs" lang="cs" /></example>
		public IAsyncResult BeginOpenRead(string path, FtpDataType type, AsyncCallback callback, object state) {
			return BeginOpenRead(path, type, 0, callback, state);
		}

		/// <summary>
		/// Begins an asynchronous operation to open the specified file for reading
		/// </summary>
		/// <param name="path">The full or relative path of the file</param>
		/// <param name="restart">Resume location</param>
		/// <param name="callback">Async Callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginOpenRead.cs" lang="cs" /></example>
		public IAsyncResult BeginOpenRead(string path, long restart, AsyncCallback callback, object state) {
			return BeginOpenRead(path, FtpDataType.Binary, restart, callback, state);
		}

		delegate Stream AsyncOpenRead(string path, FtpDataType type, long restart);

		/// <summary>
		/// Begins an asynchronous operation to open the specified file for reading
		/// </summary>
		/// <param name="path">The full or relative path of the file</param>
		/// <param name="type">ASCII/Binary</param>
		/// <param name="restart">Resume location</param>
		/// <param name="callback">Async Callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginOpenRead.cs" lang="cs" /></example>
		public IAsyncResult BeginOpenRead(string path, FtpDataType type, long restart, AsyncCallback callback, object state) {
			AsyncOpenRead func;
			IAsyncResult ar;

			lock (m_asyncmethods) {
				ar = (func = new AsyncOpenRead(OpenRead)).BeginInvoke(path, type, restart, callback, state);
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends a call to <see cref="o:BeginOpenRead"/>
		/// </summary>
		/// <param name="ar"><see cref="IAsyncResult"/> returned from <see cref="o:BeginOpenRead"/></param>
		/// <returns>A readable stream of the remote file</returns>
		/// <example><code source="..\Examples\BeginOpenRead.cs" lang="cs" /></example>
		public Stream EndOpenRead(IAsyncResult ar) {
			return GetAsyncDelegate<AsyncOpenRead>(ar).EndInvoke(ar);
		}

#endif
#if ASYNC
		/// <summary>
		/// Opens the specified file for reading asynchronously
		/// </summary>
		/// <param name="path">The full or relative path of the file</param>
		/// <param name="type">ASCII/Binary</param>
		/// <param name="restart">Resume location</param>
		/// <param name="checkIfFileExists">Only set this to false if you are SURE that the file does not exist. If true, it reads the file size and saves it into the stream length.</param>
		/// <param name="token">Cancellation Token</param>
		/// <returns>A stream for reading the file on the server</returns>
		public virtual async Task<Stream> OpenReadAsync(string path, FtpDataType type, long restart, bool checkIfFileExists, CancellationToken token = default(CancellationToken))
		{
			// verify args
			if (path.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "path");

			this.LogFunc(nameof(OpenReadAsync), new object[] { path, type, restart });

			FtpClient client = null;
			FtpDataStream stream = null;
			long length = 0;

			if (m_threadSafeDataChannels)
			{
				client = CloneConnection();
				await client.ConnectAsync(token);
				await client.SetWorkingDirectoryAsync(await GetWorkingDirectoryAsync(token), token);
			}
			else
			{
				client = this;
			}

			await client.SetDataTypeAsync(type, token);
			length = checkIfFileExists ? await client.GetFileSizeAsync(path, token) : 0;
			stream = await client.OpenDataStreamAsync(("RETR " + path.GetFtpPath()), restart, token);

			if (stream != null)
			{
				if (length > 0)
					stream.SetLength(length);

				if (restart > 0)
					stream.SetPosition(restart);
			}

			return stream;
		}

		/// <summary>
		/// Opens the specified file for reading asynchronously
		/// </summary>
		/// <param name="path">The full or relative path of the file</param>
		/// <param name="type">ASCII/Binary</param>
		/// <param name="restart">Resume location</param>
		/// <param name="token">Cancellation Token</param>
		/// <returns>A readable stream of the remote file</returns>
		public Task<Stream> OpenReadAsync(string path, FtpDataType type, long restart, CancellationToken token = default(CancellationToken)) {
			return OpenReadAsync(path, type, restart, true, token);
		}

		/// <summary>
		/// Opens the specified file for reading asynchronously
		/// </summary>
		/// <param name="path">The full or relative path of the file</param>
		/// <param name="type">ASCII/Binary</param>
		/// <param name="token">Cancellation Token</param>
		/// <returns>A readable stream of the remote file</returns>
		public Task<Stream> OpenReadAsync(string path, FtpDataType type, CancellationToken token = default(CancellationToken)) {
			return OpenReadAsync(path, type, 0, true, token);
		}

		/// <summary>
		/// Opens the specified file for reading asynchronously
		/// </summary>
		/// <param name="path">The full or relative path of the file</param>
		/// <param name="restart">Resume location</param>
		/// <param name="token">Cancellation Token</param>
		/// <returns>A readable stream of the remote file</returns>
		public Task<Stream> OpenReadAsync(string path, long restart, CancellationToken token = default(CancellationToken)) {
			return OpenReadAsync(path, FtpDataType.Binary, restart, true, token);
		}

		/// <summary>
		/// Opens the specified file for reading asynchronously
		/// </summary>
		/// <param name="path">The full or relative path of the file</param>
		/// <param name="token">Cancellation Token</param>
		/// <param name="token">Cancellation Token</param>
		/// <returns>A readable stream of the remote file</returns>
		public Task<Stream> OpenReadAsync(string path, CancellationToken token = default(CancellationToken)) {
			return OpenReadAsync(path, FtpDataType.Binary, 0, true, token);
		}
#endif

		#endregion

		#region Open Write

		/// <summary>
		/// Opens the specified file for writing. Please call GetReply() after you have successfully transfered the file to read the "OK" command sent by the server and prevent stale data on the socket.
		/// </summary>
		/// <param name="path">Full or relative path of the file</param>
		/// <returns>A stream for writing to the file on the server</returns>
		/// <example><code source="..\Examples\OpenWrite.cs" lang="cs" /></example>
		public Stream OpenWrite(string path) {
			return OpenWrite(path, FtpDataType.Binary, true);
		}

		/// <summary>
		/// Opens the specified file for writing. Please call GetReply() after you have successfully transfered the file to read the "OK" command sent by the server and prevent stale data on the socket.
		/// </summary>
		/// <param name="path">Full or relative path of the file</param>
		/// <param name="type">ASCII/Binary</param>
		/// <returns>A stream for writing to the file on the server</returns>
		/// <example><code source="..\Examples\OpenWrite.cs" lang="cs" /></example>
		public virtual Stream OpenWrite(string path, FtpDataType type) {
			return OpenWrite(path, type, true);
		}

		/// <summary>
		/// Opens the specified file for writing. Please call GetReply() after you have successfully transfered the file to read the "OK" command sent by the server and prevent stale data on the socket.
		/// </summary>
		/// <param name="path">Full or relative path of the file</param>
		/// <param name="type">ASCII/Binary</param>
		/// <param name="checkIfFileExists">Only set this to false if you are SURE that the file does not exist. If true, it reads the file size and saves it into the stream length.</param>
		/// <returns>A stream for writing to the file on the server</returns>
		/// <example><code source="..\Examples\OpenWrite.cs" lang="cs" /></example>
		public virtual Stream OpenWrite(string path, FtpDataType type, bool checkIfFileExists) {

			// verify args
			if (path.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "path");

			this.LogFunc("OpenWrite", new object[] { path, type });

			FtpClient client = null;
			FtpDataStream stream = null;
			long length = 0;

#if !CORE14
			lock (m_lock) {
#endif
				if (m_threadSafeDataChannels) {
					client = CloneConnection();
					client.Connect();
					client.SetWorkingDirectory(GetWorkingDirectory());
				} else {
					client = this;
				}

				client.SetDataType(type);
				length = checkIfFileExists ? client.GetFileSize(path) : 0;
				stream = client.OpenDataStream(("STOR " + path.GetFtpPath()), 0);

				if (length > 0 && stream != null)
					stream.SetLength(length);
#if !CORE14
			}
#endif

			return stream;
		}

#if !CORE
		/// <summary>
		/// Begins an asynchronous operation to open the specified file for writing
		/// </summary>
		/// <param name="path">Full or relative path of the file</param>
		/// <param name="callback">Async callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginOpenWrite.cs" lang="cs" /></example>
		public IAsyncResult BeginOpenWrite(string path, AsyncCallback callback, object state) {
			return BeginOpenWrite(path, FtpDataType.Binary, callback, state);
		}

		delegate Stream AsyncOpenWrite(string path, FtpDataType type);

		/// <summary>
		/// Begins an asynchronous operation to open the specified file for writing
		/// </summary>
		/// <param name="path">Full or relative path of the file</param>
		/// <param name="type">ASCII/Binary</param>
		/// <param name="callback">Async callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginOpenWrite.cs" lang="cs" /></example>
		public IAsyncResult BeginOpenWrite(string path, FtpDataType type, AsyncCallback callback, object state) {
			AsyncOpenWrite func;
			IAsyncResult ar;

			lock (m_asyncmethods) {
				ar = (func = new AsyncOpenWrite(OpenWrite)).BeginInvoke(path, type, callback, state);
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends a call to <see cref="o:BeginOpenWrite"/>
		/// </summary>
		/// <param name="ar"><see cref="IAsyncResult"/> returned from <see cref="o:BeginOpenWrite"/></param>
		/// <returns>A writable stream</returns>
		/// <example><code source="..\Examples\BeginOpenWrite.cs" lang="cs" /></example>
		public Stream EndOpenWrite(IAsyncResult ar) {
			return GetAsyncDelegate<AsyncOpenWrite>(ar).EndInvoke(ar);
		}

#endif
#if ASYNC
		/// <summary>
		/// Opens the specified file for writing. Please call GetReply() after you have successfully transfered the file to read the "OK" command sent by the server and prevent stale data on the socket.
		/// </summary>
		/// <param name="path">Full or relative path of the file</param>
		/// <param name="type">ASCII/Binary</param>
		/// <param name="checkIfFileExists">Only set this to false if you are SURE that the file does not exist. If true, it reads the file size and saves it into the stream length.</param>
		/// <param name="token"><param name="token">Cancellation Token</param>
		/// <returns>A stream for writing to the file on the server</returns>
		public virtual async Task<Stream> OpenWriteAsync(string path, FtpDataType type, bool checkIfFileExists, CancellationToken token = default(CancellationToken))
		{
			// verify args
			if (path.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "path");

			this.LogFunc(nameof(OpenWriteAsync), new object[] { path, type });

			FtpClient client = null;
			FtpDataStream stream = null;
			long length = 0;

			if (m_threadSafeDataChannels)
			{
				client = CloneConnection();
				await client.ConnectAsync(token);
				await client.SetWorkingDirectoryAsync(await GetWorkingDirectoryAsync(token), token);
			}
			else
			{
				client = this;
			}

			await client.SetDataTypeAsync(type, token);
			length = checkIfFileExists ? await client.GetFileSizeAsync(path, token) : 0;
			stream = await client.OpenDataStreamAsync(("STOR " + path.GetFtpPath()), 0, token);

			if (length > 0 && stream != null)
				stream.SetLength(length);

			return stream;
		}

		/// <summary>
		/// Opens the specified file for writing. Please call GetReply() after you have successfully transfered the file to read the "OK" command sent by the server and prevent stale data on the socket. asynchronously
		/// </summary>
		/// <param name="path">Full or relative path of the file</param>
		/// <param name="type">ASCII/Binary</param>
		/// <param name="token">Cancellation Token</param>
		/// <returns>A stream for writing to the file on the server</returns>
		public Task<Stream> OpenWriteAsync(string path, FtpDataType type, CancellationToken token = default(CancellationToken)) {
			return OpenWriteAsync(path, type, true, token);
		}

		/// <summary>
		/// Opens the specified file for writing. Please call GetReply() after you have successfully transfered the file to read the "OK" command sent by the server and prevent stale data on the socket. asynchronously
		/// </summary>
		/// <param name="path">Full or relative path of the file</param>
		/// <param name="token">Cancellation Token</param>
		/// <param name="token">Cancellation Token</param>
		/// <returns>A stream for writing to the file on the server</returns>
		public Task<Stream> OpenWriteAsync(string path, CancellationToken token = default(CancellationToken)) {
			return OpenWriteAsync(path, FtpDataType.Binary, true, token);
		}
#endif

		#endregion

		#region Open Append

		/// <summary>
		/// Opens the specified file for appending. Please call GetReply() after you have successfully transfered the file to read the "OK" command sent by the server and prevent stale data on the socket.
		/// </summary>
		/// <param name="path">The full or relative path to the file to be opened</param>
		/// <returns>A stream for writing to the file on the server</returns>
		/// <example><code source="..\Examples\OpenAppend.cs" lang="cs" /></example>
		public Stream OpenAppend(string path) {
			return OpenAppend(path, FtpDataType.Binary, true);
		}

		/// <summary>
		/// Opens the specified file for appending. Please call GetReply() after you have successfully transfered the file to read the "OK" command sent by the server and prevent stale data on the socket.
		/// </summary>
		/// <param name="path">The full or relative path to the file to be opened</param>
		/// <param name="type">ASCII/Binary</param>
		/// <param name="checkIfFileExists">Only set this to false if you are SURE that the file does not exist. If true, it reads the file size and saves it into the stream length.</param>
		/// <returns>A stream for writing to the file on the server</returns>
		/// <example><code source="..\Examples\OpenAppend.cs" lang="cs" /></example>
		public virtual Stream OpenAppend(string path, FtpDataType type) {
			return OpenAppend(path, type, true);
		}

		/// <summary>
		/// Opens the specified file for appending. Please call GetReply() after you have successfully transfered the file to read the "OK" command sent by the server and prevent stale data on the socket.
		/// </summary>
		/// <param name="path">The full or relative path to the file to be opened</param>
		/// <param name="type">ASCII/Binary</param>
		/// <param name="checkIfFileExists">Only set this to false if you are SURE that the file does not exist. If true, it reads the file size and saves it into the stream length.</param>
		/// <returns>A stream for writing to the file on the server</returns>
		/// <example><code source="..\Examples\OpenAppend.cs" lang="cs" /></example>
		public virtual Stream OpenAppend(string path, FtpDataType type, bool checkIfFileExists) {

			// verify args
			if (path.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "path");

			this.LogFunc("OpenAppend", new object[] { path, type });

			FtpClient client = null;
			FtpDataStream stream = null;
			long length = 0;

#if !CORE14
			lock (m_lock) {
#endif
				if (m_threadSafeDataChannels) {
					client = CloneConnection();
					client.Connect();
					client.SetWorkingDirectory(GetWorkingDirectory());
				} else {
					client = this;
				}

				client.SetDataType(type);
				length = checkIfFileExists ? client.GetFileSize(path) : 0;
				stream = client.OpenDataStream(("APPE " + path.GetFtpPath()), 0);

				if (length > 0 && stream != null) {
					stream.SetLength(length);
					stream.SetPosition(length);
				}
#if !CORE14
			}
#endif

			return stream;
		}

#if !CORE
		/// <summary>
		/// Begins an asynchronous operation to open the specified file for appending
		/// </summary>
		/// <param name="path">Full or relative path of the file</param>
		/// <param name="callback">Async callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginOpenAppend.cs" lang="cs" /></example>
		public IAsyncResult BeginOpenAppend(string path, AsyncCallback callback, object state) {
			return BeginOpenAppend(path, FtpDataType.Binary, callback, state);
		}

		delegate Stream AsyncOpenAppend(string path, FtpDataType type);

		/// <summary>
		/// Begins an asynchronous operation to open the specified file for appending
		/// </summary>
		/// <param name="path">Full or relative path of the file</param>
		/// <param name="type">ASCII/Binary</param>
		/// <param name="callback">Async callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		/// <example><code source="..\Examples\BeginOpenAppend.cs" lang="cs" /></example>
		public IAsyncResult BeginOpenAppend(string path, FtpDataType type, AsyncCallback callback, object state) {
			IAsyncResult ar;
			AsyncOpenAppend func;

			lock (m_asyncmethods) {
				ar = (func = new AsyncOpenAppend(OpenAppend)).BeginInvoke(path, type, callback, state);
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends a call to <see cref="o:BeginOpenAppend"/>
		/// </summary>
		/// <param name="ar"><see cref="IAsyncResult"/> returned from <see cref="o:BeginOpenAppend"/></param>
		/// <returns>A writable stream</returns>
		/// <example><code source="..\Examples\BeginOpenAppend.cs" lang="cs" /></example>
		public Stream EndOpenAppend(IAsyncResult ar) {
			return GetAsyncDelegate<AsyncOpenAppend>(ar).EndInvoke(ar);
		}

#endif
#if ASYNC
		/// <summary>
		/// Opens the specified file to be appended asynchronously
		/// </summary>
		/// <param name="path">Full or relative path of the file</param>
		/// <param name="type">ASCII/Binary</param>
		/// <param name="checkIfFileExists">Only set this to false if you are SURE that the file does not exist. If true, it reads the file size and saves it into the stream length.</param>
		/// <param name="token">Cancellation Token</param>
		/// <returns>A stream for writing to the file on the server</returns>
		public virtual async Task<Stream> OpenAppendAsync(string path, FtpDataType type, bool checkIfFileExists, CancellationToken token = default(CancellationToken))
		{
			// verify args
			if (path.IsBlank())
				throw new ArgumentException("Required parameter is null or blank.", "path");

			this.LogFunc(nameof(OpenAppendAsync), new object[] { path, type });

			FtpClient client = null;
			FtpDataStream stream = null;
			long length = 0;


			if (m_threadSafeDataChannels)
			{
				client = CloneConnection();
				await client.ConnectAsync(token);
				await client.SetWorkingDirectoryAsync(await GetWorkingDirectoryAsync(token), token);
			}
			else
			{
				client = this;
			}

			await client.SetDataTypeAsync(type, token);
			length = checkIfFileExists ? await client.GetFileSizeAsync(path, token) : 0;
			stream = await client.OpenDataStreamAsync(("APPE " + path.GetFtpPath()), 0, token);

			if (length > 0 && stream != null)
			{
				stream.SetLength(length);
				stream.SetPosition(length);
			}

			return stream;
		}

		/// <summary>
		/// Opens the specified file to be appended asynchronously
		/// </summary>
		/// <param name="path">Full or relative path of the file</param>
		/// <param name="type">ASCII/Binary</param>
		/// <param name="token">Cancellation Token</param>
		/// <returns>A stream for writing to the file on the server</returns>
		public Task<Stream> OpenAppendAsync(string path, FtpDataType type, CancellationToken token = default(CancellationToken)) {
			return OpenAppendAsync(path, type, true, token);
		}

		/// <summary>
		/// Opens the specified file to be appended asynchronously
		/// </summary>
		/// <param name="path">Full or relative path of the file</param>
		/// <param name="token">Cancellation Token</param>
		/// <returns>A stream for writing to the file on the server</returns>
		public Task<Stream> OpenAppendAsync(string path, CancellationToken token = default(CancellationToken)) {
			return OpenAppendAsync(path, FtpDataType.Binary, true, token);
		}
#endif

		#endregion

		#region Set Data Type

		protected bool ForceSetDataType = false;

		/// <summary>
		/// Sets the data type of information sent over the data stream
		/// </summary>
		/// <param name="type">ASCII/Binary</param>
		protected void SetDataType(FtpDataType type) {
#if !CORE14
			lock (m_lock) {
#endif
				// FIX : #291 only change the data type if different
				if (CurrentDataType != type || ForceSetDataType) {

					// FIX : #318 always set the type when we create a new connection
					ForceSetDataType = false;

					this.SetDataTypeInternal(type);
				}
#if !CORE14
			}
#endif

			CurrentDataType = type;

		}

		/// <summary>Internal method that handles actually setting the data type.</summary>
		/// <exception cref="FtpCommandException">Thrown when a FTP Command error condition occurs.</exception>
		/// <exception cref="FtpException">Thrown when a FTP error condition occurs.</exception>
		/// <param name="type">ASCII/Binary.</param>
		/// <remarks>This method doesn't do any locking to prevent recursive lock scenarios.  Callers must do their own locking.</remarks>
		private void SetDataTypeInternal(FtpDataType type) {
			FtpReply reply;
			switch (type) {
				case FtpDataType.ASCII:
					if (!(reply = Execute("TYPE A")).Success)
						throw new FtpCommandException(reply);
					/*if (!(reply = Execute("STRU R")).Success)
						this.LogLine(reply.Message);*/
					break;
				case FtpDataType.Binary:
					if (!(reply = Execute("TYPE I")).Success)
						throw new FtpCommandException(reply);
					/*if (!(reply = Execute("STRU F")).Success)
						this.LogLine(reply.Message);*/
					break;
				default:
					throw new FtpException("Unsupported data type: " + type.ToString());
			}
		}

#if !CORE
		delegate void AsyncSetDataType(FtpDataType type);

		/// <summary>
		/// Begins an asynchronous operation to set the data type of information sent over the data stream
		/// </summary>
		/// <param name="type">ASCII/Binary</param>
		/// <param name="callback">Async callback</param>
		/// <param name="state">State object</param>
		/// <returns>IAsyncResult</returns>
		protected IAsyncResult BeginSetDataType(FtpDataType type, AsyncCallback callback, object state) {
			IAsyncResult ar;
			AsyncSetDataType func;

			lock (m_asyncmethods) {
				ar = (func = new AsyncSetDataType(SetDataType)).BeginInvoke(type, callback, state);
				m_asyncmethods.Add(ar, func);
			}

			return ar;
		}

		/// <summary>
		/// Ends a call to <see cref="BeginSetDataType"/>
		/// </summary>
		/// <param name="ar">IAsyncResult returned from <see cref="BeginSetDataType"/></param>
		protected void EndSetDataType(IAsyncResult ar) {
			GetAsyncDelegate<AsyncSetDataType>(ar).EndInvoke(ar);
		}

#endif
#if ASYNC
		/// <summary>
		/// Sets the data type of information sent over the data stream asynchronously
		/// </summary>
		/// <param name="type">ASCII/Binary</param>
		/// <param name="token">Cancellation Token</param>
		protected async Task SetDataTypeAsync(FtpDataType type, CancellationToken token = default(CancellationToken)) {
			// FIX : #291 only change the data type if different
			if (CurrentDataType == type)
				return;
			
			FtpReply reply;
			switch (type)
			{
				case FtpDataType.ASCII:
					if (!(reply = await ExecuteAsync("TYPE A", token)).Success)
						throw new FtpCommandException(reply);
					break;
				case FtpDataType.Binary:
					if (!(reply = await ExecuteAsync("TYPE I", token)).Success)
						throw new FtpCommandException(reply);
					break;
				default:
					throw new FtpException("Unsupported data type: " + type.ToString());
			}

			CurrentDataType = type;
		}
#endif
		#endregion

	}
}
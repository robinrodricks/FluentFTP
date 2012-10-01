using System;
using System.IO;
using System.Text.RegularExpressions;

namespace System.Net.FtpClient {
    /// <summary>
    /// FtpDataStream object setup for passive mode transfers
    /// </summary>
	public class FtpPassiveStream : FtpDataStream {
        /// <summary>
        /// Executes the sepcified command on the control connection
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
		public override FtpReply Execute(string command) {
			// if we're already connected we need to close
			// and reset ourselves
			if(this.Socket.Connected) {
				this.Close();
			}

            // On servers that advertise PRET (DrFTPD), the PRET command
            // must be executed before a passive connection is opened.
            if (this.ControlConnection.HasCapability(FtpCapability.PRET)) {
                FtpReply reply;

                if (!(reply = this.ControlConnection.Execute("PRET {0}", command)).Success) {
                    throw new FtpCommandException(reply);
                }
            }

			if(!this.Socket.Connected) {
				this.Open();
			}

			try {
				this.ControlConnection.LockControlConnection();
				this.CommandReply = this.ControlConnection.Execute(command);
			}
			finally {
				this.ControlConnection.UnlockControlConnection();
			}

            return this.CommandReply;
		}

        /// <summary>
        /// Verify is a special ip address is routable, if not we should
        /// use the server address.
        /// </summary>
        /// <param name="host">The ip address to test.</param>
        /// <returns>true if routable false otherwise.</returns>
        private bool IsRootableAddress(string host) {

            IPAddress address = null;
            if (IPAddress.TryParse(host, out address)) {
                string parsedAddress = address.ToString();
                if (address.AddressFamily == Sockets.AddressFamily.InterNetwork) {
                    if (address == IPAddress.Loopback) {
                        return false;
                    }

                    if (parsedAddress.StartsWith("10.") || parsedAddress.StartsWith("192.168") || parsedAddress.StartsWith("169.254")) {
                        return false;
                    }

                    if (parsedAddress.StartsWith("172")) {
                        string[] splittedAddress = parsedAddress.Split(new char[] { '.' });
                        if (splittedAddress.Length < 4 || (Convert.ToInt32(splittedAddress[1]) >= 16 && Convert.ToInt32(splittedAddress[1]) <= 31)) {
                            return false;
                        }
                    }
                }
                else {
                    if (address.Equals(IPAddress.IPv6Any)) {
                        return false;
                    }

                    if (address.Equals(IPAddress.IPv6Loopback)) {
                        return false;
                    }

                    if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal) {
                        return false;
                    }

                    // IPv4 mapped
                    string mapped = "::ffff:";
                    if (parsedAddress.StartsWith(mapped)) {
                        string ipv4 = parsedAddress.Substring(mapped.Length, parsedAddress.Length - mapped.Length);
                            if (string.IsNullOrEmpty(ipv4)) {
                                return false;
                            }

                            return IsRootableAddress(ipv4);
                    }
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Open the specified type of passive stream
        /// </summary>
        /// <param name="type"></param>
		protected override void Open(FtpDataChannelType type) {
            FtpReply reply;
			Match m = null;
			string host = null;
			int port = 0;

			try {
				this.ControlConnection.LockControlConnection();

                IPAddress serverAddress = IPAddress.Parse(this.ControlConnection.Server);

                // if the data channel type is AutoPassive then check the
                // server capabilities for EPSV and decide which command
                // to use
                if (type == FtpDataChannelType.AutoPassive) {
                    if (this.ControlConnection.HasCapability(FtpCapability.EPSV) || serverAddress.AddressFamily == Sockets.AddressFamily.InterNetworkV6)
                        type = FtpDataChannelType.ExtendedPassive;
                    else
                        type = FtpDataChannelType.Passive;
                }
                
                if (serverAddress.AddressFamily == Sockets.AddressFamily.InterNetworkV6 && type != FtpDataChannelType.ExtendedPassive)
                    type = FtpDataChannelType.ExtendedPassive;

				switch(type) {
					case FtpDataChannelType.ExtendedPassive:
						reply = this.ControlConnection.Execute("EPSV");
                        break;
					case FtpDataChannelType.Passive:
						reply = this.ControlConnection.Execute("PASV");
						break;
					default:
						throw new Exception("Passive streams do not support " + type.ToString());
				}

				if(!reply.Success) {
					throw new FtpCommandException(reply);
				}

				if(type == FtpDataChannelType.Passive) {
					m = Regex.Match(reply.Message,
						"([0-9]+),([0-9]+),([0-9]+),([0-9]+),([0-9]+),([0-9]+)");

					if(!m.Success || m.Groups.Count != 7) {
						throw new FtpException(string.Format("Malformed PASV response: {0}", reply.Message));
					}

					host = string.Format("{0}.{1}.{2}.{3}", m.Groups[1].Value,
						m.Groups[2].Value, m.Groups[3].Value, m.Groups[4].Value);

                    if (!IsRootableAddress(host)) {
                        host = this.ControlConnection.Server;
                    }

					port = (int.Parse(m.Groups[5].Value) << 8) + int.Parse(m.Groups[6].Value);
				}
				else if(type == FtpDataChannelType.ExtendedPassive) {
					// according to RFC 2428, EPSV response must be exactly the
					// the same as EPRT response except the first two fields MUST BE blank
					// so that leaves us with (|||port_here|)
					m = Regex.Match(reply.Message, @"\(\|\|\|(\d+)\|\)");
					if(!m.Success) {
						throw new FtpException("Failed to get the EPSV port from: " + reply.Message);
					}

					host = this.ControlConnection.Server;
					port = int.Parse(m.Groups[1].Value);
				}

				//this.Socket.Connect(host, port);
                IAsyncResult ar = this.Socket.BeginConnect(host, port, null, null);
                if (this.ControlConnection != null)
                    ar.AsyncWaitHandle.WaitOne(this.ControlConnection.DataChannelConnectionTimeout);
                else
                    ar.AsyncWaitHandle.WaitOne(-1);

                if (!ar.IsCompleted)
                    throw new TimeoutException("Timed out try to connect to the server's data channel.");

                this.Socket.EndConnect(ar);
			}
			finally {
				this.ControlConnection.UnlockControlConnection();
			}
		}

		/// <summary>
		/// Initalizes a new instance of passive data stream
		/// </summary>
		/// <param name="chan"></param>
		public FtpPassiveStream(FtpControlConnection chan)
			: base() {
			if(chan == null) {
				throw new ArgumentNullException("chan");
			}

			this.ControlConnection = chan;
		}
	}
}

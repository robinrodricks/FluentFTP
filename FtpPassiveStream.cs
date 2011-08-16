using System;
using System.IO;
using System.Text.RegularExpressions;

namespace System.Net.FtpClient {
	public class FtpPassiveStream : FtpDataStream {
		public override bool Execute(string command) {
			// if we're already connected we need to close
			// and reset ourselves
			if(this.Socket.Connected) {
				this.Close();
			}

			if(!this.Socket.Connected) {
				this.Open();
			}

			try {
				this.ControlConnection.LockControlConnection();
				return this.ControlConnection.Execute(command);
			}
			finally {
				this.ControlConnection.UnlockControlConnection();
			}
		}

		protected override void Open(FtpDataChannelType type) {
			Match m = null;
			string host = null;
			int port = 0;

			try {
				this.ControlConnection.LockControlConnection();

				switch(type) {
					case FtpDataChannelType.ExtendedPassive:
						this.ControlConnection.Execute("EPSV");
						if(this.ControlConnection.ResponseType == FtpResponseType.PermanentNegativeCompletion) {
							// fall back to PASV if EPSV fails
							this.ControlConnection.RemoveCapability(FtpCapability.EPSV);
							this.ControlConnection.RemoveCapability(FtpCapability.EPRT);
							this.ControlConnection.Execute("PASV");
							type = FtpDataChannelType.Passive;
						}
						break;
					case FtpDataChannelType.Passive:
						this.ControlConnection.Execute("PASV");
						break;
					default:
						throw new Exception("Passive streams do not support " + type.ToString());
				}

				if(!this.ControlConnection.ResponseStatus) {
					throw new FtpCommandException(this.ControlConnection);
				}

				if(type == FtpDataChannelType.Passive) {
					m = Regex.Match(this.ControlConnection.ResponseMessage,
						"([0-9]+),([0-9]+),([0-9]+),([0-9]+),([0-9]+),([0-9]+)");

					if(!m.Success || m.Groups.Count != 7) {
						throw new FtpException(string.Format("Malformed PASV response: {0}", this.ControlConnection.ResponseMessage));
					}

					host = string.Format("{0}.{1}.{2}.{3}", m.Groups[1].Value,
						m.Groups[2].Value, m.Groups[3].Value, m.Groups[4].Value);
					port = (int.Parse(m.Groups[5].Value) << 8) + int.Parse(m.Groups[6].Value);
				}
				else if(type == FtpDataChannelType.ExtendedPassive) {
					// according to RFC 2428, EPSV response must be exactly the
					// the same as EPRT response except the first two fields MUST BE blank
					// so that leaves us with (|||port_here|)
					m = Regex.Match(this.ControlConnection.ResponseMessage, @"\(\|\|\|(\d+)\|\)");
					if(!m.Success) {
						throw new FtpException("Failed to get the EPSV port from: " + this.ControlConnection.ResponseMessage);
					}

					host = this.ControlConnection.Server;
					port = int.Parse(m.Groups[1].Value);
				}

				this.Socket.Connect(host, port);
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

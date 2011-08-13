using System;
using System.Net;
using System.Net.Sockets;

namespace System.Net.FtpClient {
	public class FtpActiveStream : FtpDataStream {
		public override bool Execute(string command) {
			if(this.Socket.Connected) {
                if (this.DataMode == FtpDataMode.Stream) {
                    throw new FtpException("A command has already been executed on this data stream. You must create a new stream.");
                }
                else {
                    return this.CommandChannel.Execute(command);
                }
			}

			this.Open();
			this.CommandChannel.Execute(command);

			if(this.CommandChannel.ResponseStatus) {
				this.Accept();
			}

			return this.CommandChannel.ResponseStatus;
		}

		protected void Accept() {
			Socket s = this.Socket.Accept();

			this.Socket.Close();
			this.Socket = null;
			this.Socket = s;
		}

		protected override void Open(FtpDataChannelType type) {
			string ipaddress = null;
			int port = 0;

			this.Socket.Bind(new IPEndPoint(((IPEndPoint)this.CommandChannel.LocalEndPoint).Address, 0));
			//this.Socket.Bind(new IPEndPoint(IPAddress.Any, 0));
			this.Socket.Listen(1);

			ipaddress = ((IPEndPoint)this.Socket.LocalEndPoint).Address.ToString();
			port = ((IPEndPoint)this.Socket.LocalEndPoint).Port;

			switch(type) {
				case FtpDataChannelType.ExtendedActive:
					this.CommandChannel.Execute("EPRT |1|{0}|{1}|", ipaddress, port);
					break;
				case FtpDataChannelType.Active:
					this.CommandChannel.Execute("PORT {0},{1},{2}", 
						ipaddress.Replace(".", ","), port / 256, port % 256);
					break;
				default:
					throw new Exception("Active streams do not support " + type.ToString());
			}

			if(!this.CommandChannel.ResponseStatus) {
				throw new FtpException(this.CommandChannel.ResponseMessage);
			}
		}

		public FtpActiveStream(FtpCommandChannel chan, FtpDataMode mode)
			: base() {
			if(chan == null) {
				throw new ArgumentNullException("chan");
			}
			this.CommandChannel = chan;
            this.DataMode = mode;
		}
	}
}

using System;
using System.Net;
using System.Net.Sockets;

namespace System.Net.FtpClient {
    /// <summary>
    /// FtpDataStream setup for active mode transfers
    /// </summary>
    public class FtpActiveStream : FtpDataStream {
        /// <summary>
        /// Executes the specified command on the control connection
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public override FtpReply Execute(string command) {
            // if we're already connected we need
            // to reset ourselves and start over
            if (this.Socket.Connected) {
                this.Close();
            }

            if (!this.Socket.Connected) {
                this.Open();
            }

            try {

                this.ControlConnection.LockControlConnection();
                this.CommandReply = this.ControlConnection.Execute(command);

                if (this.CommandReply.Success && !this.Socket.Connected) {
                    this.Accept();
                }
            }
            finally {
                this.ControlConnection.UnlockControlConnection();
            }

            return this.CommandReply;
        }

        /// <summary>
        /// Accepts the incomming connection
        /// </summary>
        protected void Accept() {
            //this.Socket = this.Socket.Accept();
            IAsyncResult ar = this.Socket.BeginAccept(null, null);
            if (this.ControlConnection != null)
                ar.AsyncWaitHandle.WaitOne(this.ControlConnection.DataChannelConnectionTimeout);
            else
                ar.AsyncWaitHandle.WaitOne(-1);

            if (!ar.IsCompleted)
                throw new TimeoutException("Timed out waiting for the server to connect to the data channel.");

            this.Socket = this.Socket.EndAccept(ar);
        }

        /// <summary>
        /// Opens the specified type of active data stream
        /// </summary>
        /// <param name="type"></param>
        protected override void Open(FtpDataChannelType type) {
            FtpReply reply;
            string ipaddress = null;
            int port = 0;

            this.Socket.Bind(new IPEndPoint(((IPEndPoint)this.ControlConnection.LocalEndPoint).Address, 0));
            this.Socket.Listen(1);

            ipaddress = ((IPEndPoint)this.Socket.LocalEndPoint).Address.ToString();
            port = ((IPEndPoint)this.Socket.LocalEndPoint).Port;

            try {
                this.ControlConnection.LockControlConnection();

                IPAddress serverAddress = IPAddress.Parse(this.ControlConnection.Server);

                // if the data channel type is AutoActive then check the
                // server capabilities for EPRT and decide which command
                // to use
                if (type == FtpDataChannelType.AutoActive) {
                    if (this.ControlConnection.HasCapability(FtpCapability.EPRT) || serverAddress.AddressFamily == AddressFamily.InterNetworkV6)
                        type = FtpDataChannelType.ExtendedActive;
                    else
                        type = FtpDataChannelType.Active;
                }

                if (serverAddress.AddressFamily == AddressFamily.InterNetworkV6 && type != FtpDataChannelType.ExtendedActive)
                    type = FtpDataChannelType.ExtendedActive;

                switch (type) {
                    case FtpDataChannelType.ExtendedActive:
                        if (serverAddress.AddressFamily == AddressFamily.InterNetworkV6)
                            reply = this.ControlConnection.Execute("EPRT |2|{0}|{1}|", ipaddress, port);    
                        else
                            reply = this.ControlConnection.Execute("EPRT |1|{0}|{1}|", ipaddress, port);
                        break;
                    case FtpDataChannelType.Active:
                        reply = this.ControlConnection.Execute("PORT {0},{1},{2}",
                            ipaddress.Replace(".", ","), port / 256, port % 256);
                        break;
                    default:
                        throw new Exception("Active streams do not support " + type.ToString());
                }

                if (!reply.Success) {
                    throw new FtpCommandException(reply);
                }
            }
            finally {
                this.ControlConnection.UnlockControlConnection();
            }
        }

        /// <summary>
        /// Initalizes a new instance of an active ftp data stream
        /// </summary>
        /// <param name="chan"></param>
        public FtpActiveStream(FtpControlConnection chan)
            : base() {
            if (chan == null) {
                throw new ArgumentNullException("chan");
            }
            this.ControlConnection = chan;
        }
    }
}

using System;
using System.IO;
using System.Text.RegularExpressions;

namespace System.Net.FtpClient {
    public class FtpPassiveStream : FtpDataStream {
        public override bool Execute(string command) {
            // if we're already connected we need to close
            // and reset ourselves
            if (this.Socket.Connected) {
                this.Close();
            }

            if (!this.Socket.Connected) {
                this.Open();
            }

            try {
                this.CommandChannel.LockCommandChannel();
                return this.CommandChannel.Execute(command);
            }
            finally {
                this.CommandChannel.UnlockCommandChannel();
            }
        }

        protected override void Open(FtpDataChannelType type) {
            Match m = null;
            string host = null;
            int port = 0;

            try {
                this.CommandChannel.LockCommandChannel();

                switch (type) {
                    case FtpDataChannelType.ExtendedPassive:
                        this.CommandChannel.Execute("EPSV");
                        break;
                    case FtpDataChannelType.Passive:
                        this.CommandChannel.Execute("PASV");
                        break;
                    default:
                        throw new Exception("Passive streams do not support " + type.ToString());
                }

                if (!this.CommandChannel.ResponseStatus) {
                    // if using epsv, fall back to pasv in the
                    // event the epsv command wasn't acccepted
                    if (type == FtpDataChannelType.ExtendedPassive && this.CommandChannel.ResponseType == FtpResponseType.PermanentNegativeCompletion) {
                        this.CommandChannel.RemoveCapability(FtpCapability.EPSV);
                        this.CommandChannel.RemoveCapability(FtpCapability.EPRT);
                        this.Open(FtpDataChannelType.Passive);
                        return;
                    }

                    throw new FtpException(this.CommandChannel.ResponseMessage);
                }

                if (type == FtpDataChannelType.Passive) {
                    m = Regex.Match(this.CommandChannel.ResponseMessage,
                        "([0-9]+),([0-9]+),([0-9]+),([0-9]+),([0-9]+),([0-9]+)");

                    if (!m.Success || m.Groups.Count != 7) {
                        throw new FtpException(string.Format("Malformed PASV response: {0}", this.CommandChannel.ResponseMessage));
                    }

                    host = string.Format("{0}.{1}.{2}.{3}", m.Groups[1].Value,
                        m.Groups[2].Value, m.Groups[3].Value, m.Groups[4].Value);
                    port = (int.Parse(m.Groups[5].Value) << 8) + int.Parse(m.Groups[6].Value);
                }
                else if (type == FtpDataChannelType.ExtendedPassive) {
                    // according to RFC 2428, EPSV response must be exactly the
                    // the same as EPRT response except the first two fields MUST BE blank
                    // so that leaves us with (|||port_here|)
                    m = Regex.Match(this.CommandChannel.ResponseMessage, @"\(\|\|\|(\d+)\|\)");
                    if (!m.Success) {
                        throw new FtpException("Failed to get the EPSV port from: " + this.CommandChannel.ResponseMessage);
                    }

                    host = this.CommandChannel.Server;
                    port = int.Parse(m.Groups[1].Value);
                }

                this.Socket.Connect(host, port);
            }
            finally {
                this.CommandChannel.UnlockCommandChannel();
            }
        }

        public FtpPassiveStream(FtpCommandChannel chan)
            : base() {
            if (chan == null) {
                throw new ArgumentNullException("chan");
            }

            this.CommandChannel = chan;
        }
    }
}

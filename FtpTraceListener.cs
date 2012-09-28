using System;
using System.Diagnostics;
using System.IO;

namespace System.Net.FtpClient {
    /// <summary>
    /// Adds a trace lister for System.Net.FtpClient output 
    /// </summary>
    internal class FtpTraceListener : TraceListener, IDisposable {
        Stream _ostream = null;
        public Stream OutputStream {
            get { return _ostream; }
            set { _ostream = value; this.Writer = null; }
        }

        StreamWriter _writer = null;
        protected StreamWriter Writer {
            get {
                if (_writer == null && this.OutputStream != null) {
                    _writer = new StreamWriter(this.OutputStream, System.Text.Encoding.Default);
                }

                return _writer;
            }
            private set {
                _writer = value;
            }
        }

        bool _flushOnWrite = false;
        public bool FlushOnWrite {
            get { return _flushOnWrite; }
            set { _flushOnWrite = value; }
        }

        public override void Write(string message) {
#if DEBUG
            Debug.WriteLine(message);
#endif

            if (this.Writer != null && this.Writer.BaseStream != null && this.Writer.BaseStream.CanWrite) {
                lock (this.Writer) {
                    this.Writer.Write(message);

                    if (this.FlushOnWrite) {
                        this.Writer.Flush();
                    }
                }
            }
        }

        public override void WriteLine(string message) {
#if DEBUG
            Debug.WriteLine(message);
#endif

            if (this.Writer != null && this.Writer.BaseStream != null && this.Writer.BaseStream.CanWrite) {
                lock (this.Writer) {
                    this.Writer.WriteLine(message);

                    if (this.FlushOnWrite) {
                        this.Writer.Flush();
                    }
                }
            }
        }

        public new void Dispose() {
            if (this.Writer != null) {
                this.Writer.Dispose();
                this.Writer = null;
            }

            this.OutputStream = null;
            base.Dispose();
        }

        public FtpTraceListener()
            : base("FtpTraceListener") {
        }
    }
}

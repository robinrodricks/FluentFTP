using System;

namespace System.Net.FtpClient {
    /*
        Block Header

            +----------------+----------------+----------------+
            | Descriptor     |    Byte Count                   |
            |         8 bits |                      16 bits    |
            +----------------+----------------+----------------+
            

         The descriptor codes are indicated by bit flags in the
         descriptor byte.  Four codes have been assigned, where each
         code number is the decimal value of the corresponding bit in
         the byte.

            Code     Meaning
            
             128     End of data block is EOR
              64     End of data block is EOF
              32     Suspected errors in data block
              16     Data block is a restart marker

         With this encoding, more than one descriptor coded condition
         may exist for a particular block.  As many bits as necessary
         may be flagged.

         The restart marker is embedded in the data stream as an
         integral number of 8-bit bytes representing printable
         characters in the language being used over the control
         connection (e.g., default--NVT-ASCII).  <SP> (Space, in the
         appropriate language) must not be used WITHIN a restart marker.

         For example, to transmit a six-character marker, the following
         would be sent:

            +--------+--------+--------+
            |Descrptr|  Byte count     |
            |code= 16|             = 6 |
            +--------+--------+--------+

            +--------+--------+--------+
            | Marker | Marker | Marker |
            | 8 bits | 8 bits | 8 bits |
            +--------+--------+--------+

            +--------+--------+--------+
            | Marker | Marker | Marker |
            | 8 bits | 8 bits | 8 bits |
            +--------+--------+--------+
     */
    [Flags]
    public enum FtpBlockDescriptor : short {
        EndOfRecord = 128,
        EndOfFile = 64,
        Errors = 32,
        Restart = 16,
        Empty = 0
    }

    public class FtpBlockHeader {
        private FtpBlockDescriptor _desc = FtpBlockDescriptor.Empty;
        public FtpBlockDescriptor Descriptor {
            get { return _desc; }
            private set { _desc = value; }
        }

        public bool IsEndOfFile {
            get {
                return ((this.Descriptor & FtpBlockDescriptor.EndOfFile) == FtpBlockDescriptor.EndOfFile);
            }
        }

        public bool IsEndOfRecord {
            get {
                return ((this.Descriptor & FtpBlockDescriptor.EndOfRecord) == FtpBlockDescriptor.EndOfRecord);
            }
        }

        public bool HasErrors {
            get {
                return ((this.Descriptor & FtpBlockDescriptor.Errors) == FtpBlockDescriptor.Errors);
            }
        }

        public bool IsBlockFinished {
            get { return this.Length == this.TotalRead; }
        }

        int _length = 0;
        public int Length {
            get { return _length; }
            private set { _length = value; }
        }

        int _read = 0;
        public int TotalRead {
            get { return _read; }
            set { _read = value; }
        }

        public override string ToString() {
            return string.Format("Descriptor: {0} Length: {1} Read: {2}",
                this.Descriptor, this.Length, this.TotalRead);
        }

        public FtpBlockHeader(byte[] header) {
            this.Descriptor = (FtpBlockDescriptor)header[0];
            this.Length = (int)(header[1] << 8 | header[2]);
        }
    }
}

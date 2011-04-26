using System;
using System.Collections.Generic;
using System.Text;

namespace System.Net.FtpClient {
	public class FtpTransferInfo : EventArgs {
		FtpTransferType _xferType;
		/// <summary>
		/// Indicates if the transfer is an upload or download
		/// </summary>
		public FtpTransferType TransferType {
			get { return _xferType; }
			private set { _xferType = value; }
		}

		string _file = null;
		/// <summary>
		/// The full path to remote file
		/// </summary>
		public string FileName {
			get { return _file; }
			private set { _file = value; }
		}

		long _length = 0;
		/// <summary>
		/// The total number of bytes to be transferred
		/// </summary>
		public long Length {
			get { return _length; }
			private set { _length = value; }
		}

		long _transferred = 0;
		/// <summary>
		/// The number of bytes transferred
		/// </summary>
		public long Transferred {
			get { return _transferred; }
			private set { _transferred = value; }
		}

		/// <summary>
		/// Percentage of the transfer that has been completed
		/// </summary>
		public double Percentage {
			get {
				if (this.Length > 0 && this.Transferred > 0) {
					return Math.Round(((double)this.Transferred / (double)this.Length) * 100, 1);
				}

				return 0;
			}
		}

		DateTime _start = DateTime.MinValue;
		/// <summary>
		/// The start time of the transfer
		/// </summary>
		public DateTime Start {
			get { return _start; }
			private set { _start = value; }
		}

		DateTime _now = DateTime.Now;
		/// <summary>
		/// The current time used for calculating bps
		/// </summary>
		public DateTime Now {
			get { return _now; }
			private set { _now = value; }
		}

		/// <summary>
		/// Transfer average
		/// </summary>
		public long BytesPerSecond {
			get {
				TimeSpan t = this.Now.Subtract(this.Start);

				if (this.Transferred > 0 && t.TotalSeconds > 0) {
					return (long)Math.Round(this.Transferred / t.TotalSeconds, 0);
				}

				return 0;
			}
		}

		/// <summary>
		/// Gets a value indicating if the transfer is complete
		/// </summary>
		public bool Complete {
			get {
				if (this.Transferred == this.Length) {
					return true;
				}

				return false;
			}
		}

		bool _cancel = false;
		/// <summary>
		/// Cancels the transfer
		/// </summary>
		public bool Cancel {
			get { return _cancel; }
			set { _cancel = value; }
		}

		public FtpTransferInfo(FtpTransferType type, string file, long length, long transferred, DateTime start) {
			this.TransferType = type;
			this.FileName = file;
			this.Length = length;
			this.Transferred = transferred;
			this.Start = start;
		}
	}
}

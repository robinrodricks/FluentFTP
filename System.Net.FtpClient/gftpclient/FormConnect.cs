using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.FtpClient;

namespace gftpclient {
	public partial class FormConnect : Form {
		public string Server {
			get { return this.txtServer.Text; }
			set { this.txtServer.Text = value; }
		}

		public int Port {
			get {
				int port;

				if (int.TryParse(this.txtPort.Text, out port)) {
					return port;
				}

				return 21;
			}
			set {
				this.txtPort.Text = value.ToString();
			}
		}

		public string Username {
			get { return this.txtUsername.Text; }
			set { this.txtUsername.Text = value; }
		}

		public string Password {
			get { return this.txtPassword.Text; }
			set { this.txtPassword.Text = value; }
		}

		public FtpSslMode SslMode {
			get {
				if (rdoExplicitEncryption.Checked)
					return FtpSslMode.Explicit;
				else if (rdoImplicitEncryption.Checked)
					return FtpSslMode.Implicit;
				return FtpSslMode.None;
			}
			set {
				switch (value) {
					case FtpSslMode.Explicit:
						rdoExplicitEncryption.Checked = true;
						break;
					case FtpSslMode.Implicit:
						rdoImplicitEncryption.Checked = true;
						break;
					case FtpSslMode.None:
						rdoNoEncryption.Checked = true;
						break;
				}
			}
		}

		public FormConnect() {
			InitializeComponent();
			this.DialogResult = DialogResult.Cancel;
		}

		private void cmdConnect_Click(object sender, EventArgs e) {
			this.DialogResult = DialogResult.OK;
			this.Close();
		}

		private void OnCheckChanged(object sender, EventArgs e) {
			if (sender == rdoExplicitEncryption || sender == rdoNoEncryption) {
				this.Port = 21;
			}
			else if (sender == rdoImplicitEncryption) {
				this.Port = 990;
			}
		}
	}
}

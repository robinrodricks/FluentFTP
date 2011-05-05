using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace gftpclient {
	public partial class FormTransfer : Form {
		public FormTransfer() {
			InitializeComponent();
		}

		public void SetInformation(string text) {
			if (this.InvokeRequired) {
				this.Invoke(new MethodInvoker(delegate() {
					this.SetInformation(text);
				}));
			}
			else {
				lblInfo.Text = text;
				lblInfo.Refresh();
			}
		}

		public void SetProgress(double perc) {
			if (this.InvokeRequired) {
				this.Invoke(new MethodInvoker(delegate() {
					this.SetProgress(perc);
				}));
			}
			else {
				progressBar.Value = (int)Math.Round(perc, 0);
				progressBar.Refresh();
			}
		}

		public void SetProgressText(string text) {
			if (InvokeRequired) {
				this.Invoke(new MethodInvoker(delegate() {
					this.SetProgressText(text);
				}));
			}
			else {
				progressBar.Text = text;
				progressBar.Refresh();
			}
		}

		public new void Close() {
			if (this.InvokeRequired) {
				this.Invoke(new MethodInvoker(delegate() {
					this.Close();
				}));
			}
			else {
				base.Close();
			}
		}
	}
}

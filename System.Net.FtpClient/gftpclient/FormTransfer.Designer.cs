namespace gftpclient {
	partial class FormTransfer {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormTransfer));
			this.lblInfo = new System.Windows.Forms.Label();
			this.progressBar = new CustomControls.ProgressBarEx();
			this.SuspendLayout();
			// 
			// lblInfo
			// 
			this.lblInfo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.lblInfo.AutoEllipsis = true;
			this.lblInfo.Location = new System.Drawing.Point(12, 2);
			this.lblInfo.Name = "lblInfo";
			this.lblInfo.Size = new System.Drawing.Size(323, 37);
			this.lblInfo.TabIndex = 0;
			this.lblInfo.Text = "Starting...";
			this.lblInfo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// progressBar
			// 
			this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.progressBar.BackColor = System.Drawing.Color.White;
			this.progressBar.BarColorLeft = System.Drawing.Color.Silver;
			this.progressBar.BarColorRight = System.Drawing.Color.WhiteSmoke;
			this.progressBar.BarMargin = 2;
			this.progressBar.BorderColor = System.Drawing.Color.Black;
			this.progressBar.BorderWidth = 1;
			this.progressBar.ForeColor = System.Drawing.Color.Black;
			this.progressBar.Location = new System.Drawing.Point(12, 42);
			this.progressBar.Maximum = 100;
			this.progressBar.Minimum = 0;
			this.progressBar.Name = "progressBar";
			this.progressBar.Size = new System.Drawing.Size(323, 29);
			this.progressBar.TabIndex = 1;
			this.progressBar.Value = 0;
			// 
			// FormTransfer
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(347, 88);
			this.Controls.Add(this.progressBar);
			this.Controls.Add(this.lblInfo);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FormTransfer";
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "File Transfer";
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Label lblInfo;
		private CustomControls.ProgressBarEx progressBar;
	}
}
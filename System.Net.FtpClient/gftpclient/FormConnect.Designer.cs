namespace gftpclient {
	partial class FormConnect {
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormConnect));
			this.label1 = new System.Windows.Forms.Label();
			this.txtServer = new System.Windows.Forms.TextBox();
			this.txtPort = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.txtUsername = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.txtPassword = new System.Windows.Forms.TextBox();
			this.label4 = new System.Windows.Forms.Label();
			this.rdoNoEncryption = new System.Windows.Forms.RadioButton();
			this.rdoExplicitEncryption = new System.Windows.Forms.RadioButton();
			this.rdoImplicitEncryption = new System.Windows.Forms.RadioButton();
			this.cmdConnect = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(12, 11);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(60, 20);
			this.label1.TabIndex = 0;
			this.label1.Text = "Server:";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// txtServer
			// 
			this.txtServer.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.txtServer.Location = new System.Drawing.Point(78, 11);
			this.txtServer.Name = "txtServer";
			this.txtServer.Size = new System.Drawing.Size(205, 20);
			this.txtServer.TabIndex = 1;
			// 
			// txtPort
			// 
			this.txtPort.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.txtPort.Location = new System.Drawing.Point(329, 12);
			this.txtPort.Name = "txtPort";
			this.txtPort.Size = new System.Drawing.Size(39, 20);
			this.txtPort.TabIndex = 3;
			// 
			// label2
			// 
			this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.label2.Location = new System.Drawing.Point(287, 12);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(36, 20);
			this.label2.TabIndex = 2;
			this.label2.Text = "Port:";
			this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// txtUsername
			// 
			this.txtUsername.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.txtUsername.Location = new System.Drawing.Point(78, 37);
			this.txtUsername.Name = "txtUsername";
			this.txtUsername.Size = new System.Drawing.Size(205, 20);
			this.txtUsername.TabIndex = 5;
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(12, 37);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(60, 20);
			this.label3.TabIndex = 4;
			this.label3.Text = "Username:";
			this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// txtPassword
			// 
			this.txtPassword.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.txtPassword.Location = new System.Drawing.Point(78, 63);
			this.txtPassword.Name = "txtPassword";
			this.txtPassword.PasswordChar = '*';
			this.txtPassword.Size = new System.Drawing.Size(205, 20);
			this.txtPassword.TabIndex = 7;
			// 
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(12, 63);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(60, 20);
			this.label4.TabIndex = 6;
			this.label4.Text = "Password:";
			this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// rdoNoEncryption
			// 
			this.rdoNoEncryption.AutoSize = true;
			this.rdoNoEncryption.Location = new System.Drawing.Point(259, 100);
			this.rdoNoEncryption.Name = "rdoNoEncryption";
			this.rdoNoEncryption.Size = new System.Drawing.Size(92, 17);
			this.rdoNoEncryption.TabIndex = 8;
			this.rdoNoEncryption.Text = "No Encryption";
			this.rdoNoEncryption.UseVisualStyleBackColor = true;
			this.rdoNoEncryption.CheckedChanged += new System.EventHandler(this.OnCheckChanged);
			// 
			// rdoExplicitEncryption
			// 
			this.rdoExplicitEncryption.AutoSize = true;
			this.rdoExplicitEncryption.Checked = true;
			this.rdoExplicitEncryption.Location = new System.Drawing.Point(26, 100);
			this.rdoExplicitEncryption.Name = "rdoExplicitEncryption";
			this.rdoExplicitEncryption.Size = new System.Drawing.Size(111, 17);
			this.rdoExplicitEncryption.TabIndex = 9;
			this.rdoExplicitEncryption.TabStop = true;
			this.rdoExplicitEncryption.Text = "Explicit Encryption";
			this.rdoExplicitEncryption.UseVisualStyleBackColor = true;
			this.rdoExplicitEncryption.CheckedChanged += new System.EventHandler(this.OnCheckChanged);
			// 
			// rdoImplicitEncryption
			// 
			this.rdoImplicitEncryption.AutoSize = true;
			this.rdoImplicitEncryption.Location = new System.Drawing.Point(143, 100);
			this.rdoImplicitEncryption.Name = "rdoImplicitEncryption";
			this.rdoImplicitEncryption.Size = new System.Drawing.Size(110, 17);
			this.rdoImplicitEncryption.TabIndex = 10;
			this.rdoImplicitEncryption.Text = "Implicit Encryption";
			this.rdoImplicitEncryption.UseVisualStyleBackColor = true;
			this.rdoImplicitEncryption.CheckedChanged += new System.EventHandler(this.OnCheckChanged);
			// 
			// cmdConnect
			// 
			this.cmdConnect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.cmdConnect.Location = new System.Drawing.Point(290, 130);
			this.cmdConnect.Name = "cmdConnect";
			this.cmdConnect.Size = new System.Drawing.Size(89, 26);
			this.cmdConnect.TabIndex = 12;
			this.cmdConnect.Text = "&Connect";
			this.cmdConnect.UseVisualStyleBackColor = true;
			this.cmdConnect.Click += new System.EventHandler(this.cmdConnect_Click);
			// 
			// FormConnect
			// 
			this.AcceptButton = this.cmdConnect;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(391, 168);
			this.Controls.Add(this.cmdConnect);
			this.Controls.Add(this.rdoImplicitEncryption);
			this.Controls.Add(this.rdoExplicitEncryption);
			this.Controls.Add(this.rdoNoEncryption);
			this.Controls.Add(this.txtPassword);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.txtUsername);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.txtPort);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.txtServer);
			this.Controls.Add(this.label1);
			this.DoubleBuffered = true;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FormConnect";
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Connect to Server";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox txtServer;
		private System.Windows.Forms.TextBox txtPort;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox txtUsername;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.TextBox txtPassword;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.RadioButton rdoNoEncryption;
		private System.Windows.Forms.RadioButton rdoExplicitEncryption;
		private System.Windows.Forms.RadioButton rdoImplicitEncryption;
		private System.Windows.Forms.Button cmdConnect;
	}
}
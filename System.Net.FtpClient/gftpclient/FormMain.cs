using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Net.FtpClient;

namespace gftpclient {
	public partial class FormMain : Form {
		FtpClient _client = new FtpClient();
		public FtpClient Client {
			get { return _client; }
			private set { _client = value; }
		}

		FormTransfer FormTransfer = null;

		public FormMain() {
			InitializeComponent();
			this.Shown += new EventHandler(OnFormShown);
			this.lvFiles.MouseDoubleClick += new MouseEventHandler(OnOpenItem);
			this.lvFiles.AfterLabelEdit += new LabelEditEventHandler(OnRenameObject);
			this.cmdDirectoryUp.Click += new EventHandler(OnDirectoryUp);
			this.cmdRefresh.Click += new EventHandler(OnRefreshListing);
			this.Client.InvalidCertificate += new FtpInvalidCertificate(OnInvalidCertificate);
			this.Client.ResponseReceived += new ResponseReceived(OnResponseReceived);
			this.Client.ConnectionReady += new FtpChannelConnected(OnConnected);
			this.Client.ConnectionClosed += new FtpChannelDisconnected(OnDisconnected);
			this.Client.TransferProgress += new FtpTransferProgress(OnTransferProgress);
		}

		void OnTransferProgress(FtpTransferInfo e) {
			if (this.FormTransfer != null) {
				this.FormTransfer.SetProgress(e.Percentage);
				this.FormTransfer.SetProgressText(string.Format("{0}/{1} @ {2}/s {3}%",
					Program.FormatBytes(e.Transferred),
					Program.FormatBytes(e.Length),
					Program.FormatBytes(e.BytesPerSecond),
					e.Percentage));
				this.FormTransfer.SetInformation(string.Format(
					"{0} {1}",
					e.TransferType == FtpTransferType.Download ? "Downloading" : "Uploading",
					System.IO.Path.GetFileName(e.FileName)));

				if (e.Complete)
					this.FormTransfer.Close();
			}
			else {
				e.Cancel = true;
			}
		}

		void OnRenameObject(object sender, LabelEditEventArgs e) {
			string label = e.Label;
			int idx = e.Item;
			object item = lvFiles.Items[idx].Tag;

			if (label != null && label.Length > 0) {
				Thread t = new Thread(new ThreadStart(delegate() {
					string name = "";
					this.SetWaitCursor(true);

					try {
						if (item is FtpDirectory) {
							name = ((FtpDirectory)item).Name;

							if (name.Length > 0) {
								this.Client.Rename(name, label);
								((FtpDirectory)item).Name = label;
							}
						}
						else if (item is FtpFile) {
							name = ((FtpFile)item).Name;
							if (name.Length > 0) {
								this.Client.Rename(name, label);
								((FtpFile)item).Name = label;
							}
						}
					}
					catch (Exception ex) {
						this.ShowError(ex.Message, "Error Renaming Object");

						this.Invoke(new MethodInvoker(delegate() {
							// reset label
							lvFiles.Items[idx].Text = name;
						}));
					}
					finally {
						this.SetWaitCursor(false);
					}
				}));

				t.Start();
			}
		}

		void OnRefreshListing(object sender, EventArgs e) {
			this.LoadListing();
		}

		void OnDirectoryUp(object sender, EventArgs e) {
			this.LoadListing("..");
		}

		void OnOpenItem(object sender, MouseEventArgs e) {
			if (this.lvFiles.SelectedItems.Count > 0) {
				ListViewItem item = this.lvFiles.SelectedItems[0];

				if (item.Tag is FtpDirectory) {
					this.LoadListing(((FtpDirectory)item.Tag).FullName);
				}
				else if (item.Tag is FtpFile) {
					this.DownloadFile(((FtpFile)item.Tag));
				}
			}
		}

		void DownloadFile(FtpFile file) {
			SaveFileDialog sfd = new SaveFileDialog();
			this.FormTransfer = new FormTransfer();

			sfd.Title = "Select the location to save the file to..";
			sfd.FileName = file.Name;

			if (sfd.ShowDialog(this) == DialogResult.OK) {
				Thread t = new Thread(new ThreadStart(delegate() {
					try {
						this.Client.Download(file, sfd.FileName);
					}
					catch (Exception ex) {
						this.ShowError(ex.Message, "Error Downloading File");
					}
					finally {
						this.FormTransfer = null;
					}
				}));

				t.Start();
				this.FormTransfer.ShowDialog(this);
			}
		}

		void OnFormShown(object sender, EventArgs e) {
			// open connect dialog on initial showing
			this.Connect();
		}

		void OnDisconnected(FtpChannel c) {
			if (this.InvokeRequired) {
				this.BeginInvoke(new MethodInvoker(delegate() {
					this.OnDisconnected(c);
				}));
			}
			else {
				// update connection info instatus bar
				lblConnectionInfo.Text = "-";
			}
		}

		void OnConnected(FtpChannel c) {
			if (this.InvokeRequired) {
				this.BeginInvoke(new MethodInvoker(delegate() {
					this.OnConnected(c);
				}));
			}
			else {
				// update connection info instatus bar
				lblConnectionInfo.Text = string.Format("Connected to: {0}:{1}",
					this.Client.Server, this.Client.Port);
			}
		}

		void OnResponseReceived(string status, string response) {
			this.AddLogEntry(status, response);
		}

		void OnInvalidCertificate(FtpChannel c, InvalidCertificateInfo e) {
			e.Ignore = true;
		}

		public void AddLogEntry(string status, string message) {
			if (this.InvokeRequired) {
				this.BeginInvoke(new MethodInvoker(delegate() {
					this.AddLogEntry(status, message);
				}));
			}
			else {
				ListViewItem lv = new ListViewItem();

				lv.SubItems.Add(status);
				lv.SubItems.Add(message);

				switch (status[0]) {
					case '4':
					case '5':
						lv.ForeColor = Color.Red;
						lv.Font = new Font(Font, FontStyle.Bold);
						lv.ImageIndex = 1;
						break;
					default:
						lv.ForeColor = Color.FromKnownColor(KnownColor.ControlText);
						lv.ImageIndex = 0;
						break;
				}

				lvLog.Items.Add(lv);
				lvLog.EnsureVisible(lvLog.Items.Count - 1);
			}
		}

		private void SetWaitCursor(bool on) {
			if (this.InvokeRequired) {
				this.Invoke(new MethodInvoker(delegate() {
					this.SetWaitCursor(on);
				}));
			}
			else {
				if (on) {
					this.Cursor = Cursors.WaitCursor;
				}
				else {
					this.Cursor = Cursors.Default;
				}

				this.Update();
			}
		}

		private void ShowError(string message, string caption) {
			if (this.InvokeRequired) {
				this.Invoke(new MethodInvoker(delegate() {
					this.ShowError(message, caption);
				}));
			}
			else {
				MessageBox.Show(this, message, caption, MessageBoxButtons.OK,
					MessageBoxIcon.Error);
			}
		}

		private void ClearFileList() {
			if (this.InvokeRequired) {
				this.Invoke(new MethodInvoker(delegate() {
					this.ClearFileList();
				}));
			}
			else {
				lvFiles.Items.Clear();
			}
		}

		public void LoadListing() {
			this.LoadListing(this.Client.CurrentDirectory.FullName);
		}

		public void AddFile(FtpFile file) {
			if (this.InvokeRequired) {
				this.BeginInvoke(new MethodInvoker(delegate() {
					this.AddFile(file);
				}));
			}
			else {
				try {
					ListViewItem item = new ListViewItem(file.Name);

					if (file.LastWriteTime != DateTime.MinValue) {
						item.SubItems.Add(file.LastWriteTime.ToString());
					}
					else {
						item.SubItems.Add("");
					}

					item.SubItems.Add(Program.FormatBytes(file.Length));
					item.ImageIndex = 1;
					item.Tag = file;

					lvFiles.Items.Add(item);
				}
				catch (Exception ex) {
					this.ShowError(ex.Message, "Error adding file");
				}
			}
		}

		public void AddDirectory(FtpDirectory dir) {
			this.AddDirectory(dir, false);
		}

		public void AddDirectory(FtpDirectory dir, bool selected) {
			this.AddDirectory(dir, selected, false);
		}

		public void AddDirectory(FtpDirectory dir, bool selected, bool editMode) {
			if (this.InvokeRequired) {
				MethodInvoker mi = new MethodInvoker(delegate() {
					this.AddDirectory(dir, selected, editMode);
				});

				//if (!editMode) {
				this.BeginInvoke(mi);
				//}
				//else {
				//	this.Invoke(mi);
				//}
			}
			else {
				try {
					ListViewItem item = new ListViewItem(dir.Name);

					if (dir.LastWriteTime != DateTime.MinValue) {
						item.SubItems.Add(dir.LastWriteTime.ToString());
					}
					else {
						item.SubItems.Add("");
					}

					item.SubItems.Add("");
					item.ImageIndex = 0;
					item.Tag = dir;

					lvFiles.Items.Add(item);

					if (selected) {
						item.Selected = true;
					}

					if (editMode) {
						item.BeginEdit();
					}
				}
				catch (Exception ex) {
					this.ShowError(ex.Message, "Error adding directory");
				}
			}
		}

		public void LoadListing(string path) {
			Thread t = new Thread(new ThreadStart(delegate() {
				this.SetWaitCursor(true);

				try {
					if (path != this.Client.CurrentDirectory.FullName) {
						this.Client.SetWorkingDirectory(path);
					}

					this.ClearFileList();

					foreach (FtpDirectory d in this.Client.CurrentDirectory.Directories) {
						this.AddDirectory(d);
					}

					foreach (FtpFile f in this.Client.CurrentDirectory.Files) {
						this.AddFile(f);
					}
				}
				catch (Exception ex) {
					this.ShowError(ex.Message, "Error acquiring directory listing");
				}
				finally {
					this.SetWaitCursor(false);
				}
			}));

			t.Start();
		}

		public void Connect() {
			FormConnect frmConn = new FormConnect();

			frmConn.Server = this.Client.Server;
			frmConn.Port = this.Client.Port;
			frmConn.SslMode = this.Client.SslMode;
			frmConn.Username = this.Client.Username;
			frmConn.Password = this.Client.Password;

			if (frmConn.ShowDialog(this) == DialogResult.OK) {
				this.Client.Server = frmConn.Server;
				this.Client.Port = frmConn.Port;
				this.Client.SslMode = frmConn.SslMode;
				this.Client.Username = frmConn.Username;
				this.Client.Password = frmConn.Password;

				Thread t = new Thread(new ThreadStart(delegate() {
					try {
						this.SetWaitCursor(true);

						if (this.Client.Connected) {
							this.Client.Disconnect();
						}

						this.Client.Connect();
					}
					catch (Exception ex) {
						this.ShowError(ex.Message, "Error establishing connection");
						this.Client.Disconnect();
					}
					finally {
						this.SetWaitCursor(false);

						if (this.Client.Connected) {
							this.LoadListing();
						}
					}
				}));

				t.Start();
			}
		}

		private void connectToolStripMenuItem_Click(object sender, EventArgs e) {
			this.Connect();
		}

		private void detailsToolStripMenuItem_Click(object sender, EventArgs e) {
			this.lvFiles.View = View.Details;
		}

		private void largeIconsToolStripMenuItem_Click(object sender, EventArgs e) {
			this.lvFiles.View = View.LargeIcon;
		}

		private void smallIconsToolStripMenuItem_Click(object sender, EventArgs e) {
			this.lvFiles.View = View.SmallIcon;
		}

		private void tilesToolStripMenuItem_Click(object sender, EventArgs e) {
			this.lvFiles.View = View.Tile;
		}

		private void cmdHome_Click(object sender, EventArgs e) {
			this.LoadListing("/");
		}

		private void cmdNewDirectory_Click(object sender, EventArgs e) {
			Thread t = new Thread(new ThreadStart(delegate() {
				this.SetWaitCursor(true);

				try {
					FtpDirectory dir = new FtpDirectory(this.Client,
						string.Format("{0}/New Folder", this.Client.CurrentDirectory.FullName));
					dir.Create();
					this.AddDirectory(dir, true, true);
				}
				catch (Exception ex) {
					this.ShowError(ex.Message, "Error Creating Directory");
				}
				finally {
					this.SetWaitCursor(false);
				}
			}));

			t.Start();
		}

		private void cmdDelete_Click(object sender, EventArgs e) {
			if (MessageBox.Show(this, "Are you sure you want to remove the selected objects?",
				"Confirm Removal", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK) {
				ListViewItem[] items = new ListViewItem[this.lvFiles.SelectedItems.Count];
				Thread t = new Thread(new ThreadStart(delegate() {
					this.SetWaitCursor(true);

					try {
						foreach (ListViewItem lv in items) {
							ListViewItem item = lv;

							try {
								if (item.Tag is FtpFile) {
									((FtpFile)item.Tag).Delete();
								}
								else if (item.Tag is FtpDirectory) {
									((FtpDirectory)item.Tag).Delete(true);
								}

								this.BeginInvoke(new MethodInvoker(delegate() {
									this.lvFiles.Items.Remove(item);
								}));
							}
							catch (Exception ex) {
								this.ShowError(ex.Message, "Error Removing Object");
							}
						}
					}
					finally {
						this.SetWaitCursor(false);
					}
				}));

				this.lvFiles.SelectedItems.CopyTo(items, 0);
				t.Start();
			}
		}

		private void cmdUpload_Click(object sender, EventArgs e) {
			OpenFileDialog ofd = new OpenFileDialog();

			ofd.Title = "Select the file you want to upload";
			ofd.Multiselect = false;

			if (ofd.ShowDialog(this) == DialogResult.OK) {
				string f = ofd.FileName;
				Thread t = new Thread(new ThreadStart(delegate() {
					this.SetWaitCursor(true);

					try {
						this.Client.Upload(f);
						this.AddFile(new FtpFile(this.Client,
							string.Format("{0}/{1}", this.Client.CurrentDirectory.FullName,
							System.IO.Path.GetFileName(f))));
					}
					catch (Exception ex) {
						this.ShowError(ex.Message, "Error Uploading the Selected Files");
					}
					finally {
						this.SetWaitCursor(false);
						this.FormTransfer = null;
					}
				}));

				this.FormTransfer = new gftpclient.FormTransfer();
				t.Start();
				this.FormTransfer.ShowDialog(this);
			}
		}
	}
}

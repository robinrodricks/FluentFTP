namespace gftpclient {
	partial class FormMain {
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
			this.statusStrip1 = new System.Windows.Forms.StatusStrip();
			this.lblConnectionInfo = new System.Windows.Forms.ToolStripStatusLabel();
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.connectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.disconnectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStrip1 = new System.Windows.Forms.ToolStrip();
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.lvFiles = new System.Windows.Forms.ListView();
			this.lvLog = new System.Windows.Forms.ListView();
			this.colIcon = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.colStatus = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.colMessage = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.imgListLog = new System.Windows.Forms.ImageList(this.components);
			this.cmdDirectoryUp = new System.Windows.Forms.ToolStripButton();
			this.cmdRefresh = new System.Windows.Forms.ToolStripButton();
			this.imgListFilesSmall = new System.Windows.Forms.ImageList(this.components);
			this.imgListFilesBig = new System.Windows.Forms.ImageList(this.components);
			this.colFileName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.colFileModDate = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.colFileSize = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.detailsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.largeIconsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.smallIconsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.tilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.cmdHome = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.cmdNewDirectory = new System.Windows.Forms.ToolStripButton();
			this.cmdDelete = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.cmdUpload = new System.Windows.Forms.ToolStripButton();
			this.statusStrip1.SuspendLayout();
			this.menuStrip1.SuspendLayout();
			this.toolStrip1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			this.SuspendLayout();
			// 
			// statusStrip1
			// 
			this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblConnectionInfo});
			this.statusStrip1.Location = new System.Drawing.Point(0, 512);
			this.statusStrip1.Name = "statusStrip1";
			this.statusStrip1.Size = new System.Drawing.Size(707, 22);
			this.statusStrip1.TabIndex = 0;
			this.statusStrip1.Text = "statusStrip1";
			// 
			// lblConnectionInfo
			// 
			this.lblConnectionInfo.Name = "lblConnectionInfo";
			this.lblConnectionInfo.Size = new System.Drawing.Size(11, 17);
			this.lblConnectionInfo.Text = "-";
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.viewToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this.menuStrip1.Size = new System.Drawing.Size(707, 24);
			this.menuStrip1.TabIndex = 1;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.connectToolStripMenuItem,
            this.disconnectToolStripMenuItem,
            this.toolStripMenuItem1,
            this.exitToolStripMenuItem});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(35, 20);
			this.fileToolStripMenuItem.Text = "&File";
			// 
			// connectToolStripMenuItem
			// 
			this.connectToolStripMenuItem.Name = "connectToolStripMenuItem";
			this.connectToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.C)));
			this.connectToolStripMenuItem.Size = new System.Drawing.Size(172, 22);
			this.connectToolStripMenuItem.Text = "&Connect";
			this.connectToolStripMenuItem.Click += new System.EventHandler(this.connectToolStripMenuItem_Click);
			// 
			// disconnectToolStripMenuItem
			// 
			this.disconnectToolStripMenuItem.Name = "disconnectToolStripMenuItem";
			this.disconnectToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.D)));
			this.disconnectToolStripMenuItem.Size = new System.Drawing.Size(172, 22);
			this.disconnectToolStripMenuItem.Text = "&Disconnect";
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new System.Drawing.Size(169, 6);
			// 
			// exitToolStripMenuItem
			// 
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.Size = new System.Drawing.Size(172, 22);
			this.exitToolStripMenuItem.Text = "E&xit";
			// 
			// toolStrip1
			// 
			this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.cmdDirectoryUp,
            this.toolStripSeparator1,
            this.cmdHome,
            this.cmdRefresh,
            this.toolStripSeparator2,
            this.cmdNewDirectory,
            this.cmdDelete,
            this.toolStripSeparator3,
            this.cmdUpload});
			this.toolStrip1.Location = new System.Drawing.Point(0, 24);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this.toolStrip1.Size = new System.Drawing.Size(707, 25);
			this.toolStrip1.TabIndex = 2;
			this.toolStrip1.Text = "toolStrip1";
			// 
			// splitContainer1
			// 
			this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer1.Location = new System.Drawing.Point(0, 49);
			this.splitContainer1.Name = "splitContainer1";
			this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add(this.lvFiles);
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add(this.lvLog);
			this.splitContainer1.Size = new System.Drawing.Size(707, 463);
			this.splitContainer1.SplitterDistance = 402;
			this.splitContainer1.TabIndex = 3;
			// 
			// lvFiles
			// 
			this.lvFiles.AllowDrop = true;
			this.lvFiles.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colFileName,
            this.colFileModDate,
            this.colFileSize});
			this.lvFiles.Dock = System.Windows.Forms.DockStyle.Fill;
			this.lvFiles.FullRowSelect = true;
			this.lvFiles.LabelEdit = true;
			this.lvFiles.LargeImageList = this.imgListFilesBig;
			this.lvFiles.Location = new System.Drawing.Point(0, 0);
			this.lvFiles.Name = "lvFiles";
			this.lvFiles.Size = new System.Drawing.Size(707, 402);
			this.lvFiles.SmallImageList = this.imgListFilesSmall;
			this.lvFiles.TabIndex = 0;
			this.lvFiles.UseCompatibleStateImageBehavior = false;
			this.lvFiles.View = System.Windows.Forms.View.Tile;
			// 
			// lvLog
			// 
			this.lvLog.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colIcon,
            this.colStatus,
            this.colMessage});
			this.lvLog.Dock = System.Windows.Forms.DockStyle.Fill;
			this.lvLog.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lvLog.FullRowSelect = true;
			this.lvLog.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.lvLog.Location = new System.Drawing.Point(0, 0);
			this.lvLog.Name = "lvLog";
			this.lvLog.Size = new System.Drawing.Size(707, 57);
			this.lvLog.SmallImageList = this.imgListLog;
			this.lvLog.TabIndex = 0;
			this.lvLog.UseCompatibleStateImageBehavior = false;
			this.lvLog.View = System.Windows.Forms.View.Details;
			// 
			// colIcon
			// 
			this.colIcon.Text = "";
			this.colIcon.Width = 37;
			// 
			// colStatus
			// 
			this.colStatus.Text = "Status";
			this.colStatus.Width = 64;
			// 
			// colMessage
			// 
			this.colMessage.Text = "Message";
			this.colMessage.Width = 565;
			// 
			// imgListLog
			// 
			this.imgListLog.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imgListLog.ImageStream")));
			this.imgListLog.TransparentColor = System.Drawing.Color.Transparent;
			this.imgListLog.Images.SetKeyName(0, "INFO.ICO");
			this.imgListLog.Images.SetKeyName(1, "error.ico");
			// 
			// cmdDirectoryUp
			// 
			this.cmdDirectoryUp.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.cmdDirectoryUp.Image = ((System.Drawing.Image)(resources.GetObject("cmdDirectoryUp.Image")));
			this.cmdDirectoryUp.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.cmdDirectoryUp.Name = "cmdDirectoryUp";
			this.cmdDirectoryUp.Size = new System.Drawing.Size(23, 22);
			this.cmdDirectoryUp.Text = "Up";
			this.cmdDirectoryUp.ToolTipText = "Go to the parent directory";
			// 
			// cmdRefresh
			// 
			this.cmdRefresh.Image = ((System.Drawing.Image)(resources.GetObject("cmdRefresh.Image")));
			this.cmdRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.cmdRefresh.Name = "cmdRefresh";
			this.cmdRefresh.Size = new System.Drawing.Size(65, 22);
			this.cmdRefresh.Text = "&Refresh";
			this.cmdRefresh.ToolTipText = "Refresh Listing";
			// 
			// imgListFilesSmall
			// 
			this.imgListFilesSmall.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imgListFilesSmall.ImageStream")));
			this.imgListFilesSmall.TransparentColor = System.Drawing.Color.Transparent;
			this.imgListFilesSmall.Images.SetKeyName(0, "folderopen.ico");
			this.imgListFilesSmall.Images.SetKeyName(1, "UtilityText.ico");
			// 
			// imgListFilesBig
			// 
			this.imgListFilesBig.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imgListFilesBig.ImageStream")));
			this.imgListFilesBig.TransparentColor = System.Drawing.Color.Transparent;
			this.imgListFilesBig.Images.SetKeyName(0, "folderopen.ico");
			this.imgListFilesBig.Images.SetKeyName(1, "UtilityText.ico");
			// 
			// colFileName
			// 
			this.colFileName.Text = "Name";
			this.colFileName.Width = 452;
			// 
			// colFileModDate
			// 
			this.colFileModDate.Text = "Last Write Time";
			this.colFileModDate.Width = 172;
			// 
			// colFileSize
			// 
			this.colFileSize.Text = "Size";
			this.colFileSize.Width = 127;
			// 
			// viewToolStripMenuItem
			// 
			this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.detailsToolStripMenuItem,
            this.largeIconsToolStripMenuItem,
            this.smallIconsToolStripMenuItem,
            this.tilesToolStripMenuItem});
			this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
			this.viewToolStripMenuItem.Size = new System.Drawing.Size(41, 20);
			this.viewToolStripMenuItem.Text = "&View";
			// 
			// detailsToolStripMenuItem
			// 
			this.detailsToolStripMenuItem.Name = "detailsToolStripMenuItem";
			this.detailsToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.detailsToolStripMenuItem.Text = "&Details";
			this.detailsToolStripMenuItem.Click += new System.EventHandler(this.detailsToolStripMenuItem_Click);
			// 
			// largeIconsToolStripMenuItem
			// 
			this.largeIconsToolStripMenuItem.Name = "largeIconsToolStripMenuItem";
			this.largeIconsToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.largeIconsToolStripMenuItem.Text = "&Large Icons";
			this.largeIconsToolStripMenuItem.Click += new System.EventHandler(this.largeIconsToolStripMenuItem_Click);
			// 
			// smallIconsToolStripMenuItem
			// 
			this.smallIconsToolStripMenuItem.Name = "smallIconsToolStripMenuItem";
			this.smallIconsToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.smallIconsToolStripMenuItem.Text = "&Small Icons";
			this.smallIconsToolStripMenuItem.Click += new System.EventHandler(this.smallIconsToolStripMenuItem_Click);
			// 
			// tilesToolStripMenuItem
			// 
			this.tilesToolStripMenuItem.Name = "tilesToolStripMenuItem";
			this.tilesToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.tilesToolStripMenuItem.Text = "&Tiles";
			this.tilesToolStripMenuItem.Click += new System.EventHandler(this.tilesToolStripMenuItem_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
			// 
			// cmdHome
			// 
			this.cmdHome.Image = ((System.Drawing.Image)(resources.GetObject("cmdHome.Image")));
			this.cmdHome.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.cmdHome.Name = "cmdHome";
			this.cmdHome.Size = new System.Drawing.Size(54, 22);
			this.cmdHome.Text = "&Home";
			this.cmdHome.ToolTipText = "Go back to the root directory";
			this.cmdHome.Click += new System.EventHandler(this.cmdHome_Click);
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
			// 
			// cmdNewDirectory
			// 
			this.cmdNewDirectory.Image = ((System.Drawing.Image)(resources.GetObject("cmdNewDirectory.Image")));
			this.cmdNewDirectory.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.cmdNewDirectory.Name = "cmdNewDirectory";
			this.cmdNewDirectory.Size = new System.Drawing.Size(95, 22);
			this.cmdNewDirectory.Text = "&New Directory";
			this.cmdNewDirectory.ToolTipText = "Create new directory";
			this.cmdNewDirectory.Click += new System.EventHandler(this.cmdNewDirectory_Click);
			// 
			// cmdDelete
			// 
			this.cmdDelete.Image = ((System.Drawing.Image)(resources.GetObject("cmdDelete.Image")));
			this.cmdDelete.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.cmdDelete.Name = "cmdDelete";
			this.cmdDelete.Size = new System.Drawing.Size(58, 22);
			this.cmdDelete.Text = "Delete";
			this.cmdDelete.Click += new System.EventHandler(this.cmdDelete_Click);
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(6, 25);
			// 
			// cmdUpload
			// 
			this.cmdUpload.Image = ((System.Drawing.Image)(resources.GetObject("cmdUpload.Image")));
			this.cmdUpload.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.cmdUpload.Name = "cmdUpload";
			this.cmdUpload.Size = new System.Drawing.Size(79, 22);
			this.cmdUpload.Text = "&Upload File";
			this.cmdUpload.Click += new System.EventHandler(this.cmdUpload_Click);
			// 
			// FormMain
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(707, 534);
			this.Controls.Add(this.splitContainer1);
			this.Controls.Add(this.toolStrip1);
			this.Controls.Add(this.statusStrip1);
			this.Controls.Add(this.menuStrip1);
			this.DoubleBuffered = true;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "FormMain";
			this.Text = "gFtpClient";
			this.statusStrip1.ResumeLayout(false);
			this.statusStrip1.PerformLayout();
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
			this.splitContainer1.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.StatusStrip statusStrip1;
		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem connectToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem disconnectToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
		private System.Windows.Forms.ToolStrip toolStrip1;
		private System.Windows.Forms.SplitContainer splitContainer1;
		private System.Windows.Forms.ListView lvFiles;
		private System.Windows.Forms.ListView lvLog;
		private System.Windows.Forms.ColumnHeader colIcon;
		private System.Windows.Forms.ColumnHeader colStatus;
		private System.Windows.Forms.ColumnHeader colMessage;
		private System.Windows.Forms.ImageList imgListLog;
		private System.Windows.Forms.ToolStripStatusLabel lblConnectionInfo;
		private System.Windows.Forms.ToolStripButton cmdDirectoryUp;
		private System.Windows.Forms.ToolStripButton cmdRefresh;
		private System.Windows.Forms.ImageList imgListFilesBig;
		private System.Windows.Forms.ImageList imgListFilesSmall;
		private System.Windows.Forms.ColumnHeader colFileName;
		private System.Windows.Forms.ColumnHeader colFileModDate;
		private System.Windows.Forms.ColumnHeader colFileSize;
		private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem detailsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem largeIconsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem smallIconsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem tilesToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripButton cmdHome;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripButton cmdNewDirectory;
		private System.Windows.Forms.ToolStripButton cmdDelete;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.ToolStripButton cmdUpload;
	}
}


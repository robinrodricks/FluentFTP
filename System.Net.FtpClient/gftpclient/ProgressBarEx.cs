using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace CustomControls {
	public delegate void ProgressBarExValueChanged(object sender, EventArgs e);

	public partial class ProgressBarEx : UserControl {
		string _text = null;
		public override string Text {
			get { return _text; }
			set { _text = value; this.Refresh(); }
		}

		event ProgressBarExValueChanged _valueChanged = null;
		public event ProgressBarExValueChanged ValueChanged {
			add { this._valueChanged += value; }
			remove { this._valueChanged -= value; }
		}

		int _minVal = 0;
		public int Minimum {
			get { return _minVal; }
			set { _minVal = value; this.Refresh(); }
		}

		int _maxVal = 100;
		public int Maximum {
			get { return _maxVal; }
			set { _maxVal = value; this.Refresh(); }
		}

		int _val = 0;
		public int Value {
			get { return _val; }
			set {
				_val = value;
				this.Refresh();
				this.OnValueChanged();
			}
		}

		double Percentage {
			get {
				if (this.Value > 0 && (this.Maximum - this.Minimum > 0)) {
					return (double)this.Value / (double)(this.Maximum - this.Minimum);
				}

				return 0;
			}
		}

		Color _borderColor = Color.Black;
		/// <summary>
		/// Gets or sets the border color
		/// </summary>
		public Color BorderColor {
			get { return _borderColor; }
			set { _borderColor = value; this.Refresh(); }
		}

		Color _barColorLeft = Color.FromKnownColor(KnownColor.Highlight);
		/// <summary>
		/// Gets or sets the gradient color on the left side
		/// </summary>
		public Color BarColorLeft {
			get { return _barColorLeft; }
			set { _barColorLeft = value; this.Refresh(); }
		}

		Color _barColorRight = Color.FromKnownColor(KnownColor.Highlight);
		/// <summary>
		/// Gets or sets the gradient color on the right side
		/// </summary>
		public Color BarColorRight {
			get { return _barColorRight; }
			set { _barColorRight = value; this.Refresh(); }
		}

		int _borderWidth = 1;
		/// <summary>
		/// Gets or sets the width of the border. You probably only
		/// want to use this with BorderStyle = None.
		/// </summary>
		public int BorderWidth {
			get { return _borderWidth; }
			set { _borderWidth = value; this.Refresh(); }
		}

		int _borderRadius = 0;
		/// <summary>
		/// Needs work
		/// </summary>
		private int BorderRadius {
			get { return _borderRadius; }
			set { _borderRadius = value; this.Refresh(); }
		}

		int _barMargin = 0;
		/// <summary>
		/// Gets or sets the margin around the progress bar
		/// </summary>
		public int BarMargin {
			get { return _barMargin; }
			set { _barMargin = value; }
		}

		public void Increment(int val) {
			if ((this.Value + val) < this.Maximum) {
				this.Value += val;
			}
		}

		int BarWidth {
			get {
				return this.ClientRectangle.Width - (this.BorderWidth * 2) - (this.BarMargin * 2);
			}
		}

		int BarHeight {
			get {
				return this.ClientRectangle.Height - (this.BorderWidth * 2) - (this.BarMargin * 2);
			}
		}

		int BarX {
			get {
				return this.BorderWidth + this.BarMargin;
			}
		}

		int BarY {
			get {
				return this.BorderWidth + this.BarMargin;
			}
		}

		int BarFillWidth {
			get {
				return (int)Math.Round(this.Percentage * this.BarWidth, 0);
			}
		}

		int BarNoFillWidth {
			get {
				return this.BarWidth - this.BarFillWidth - 1;
			}
		}

		public void OnValueChanged() {
			if (this._valueChanged != null) {
				this._valueChanged(this, new EventArgs());
			}
		}

		GraphicsPath GetRoundedRectangle(int x, int y, int width, int height, int radius) {
			GraphicsPath path = new GraphicsPath();
			Rectangle r = new Rectangle(x, y, width, height);
			int d = radius * 2;

			path.AddLine(r.Left + d, r.Top, r.Right - d, r.Top);
			path.AddArc(Rectangle.FromLTRB(r.Right - d, r.Top, r.Right, r.Top + d), -90, 90);
			path.AddLine(r.Right, r.Top + d, r.Right, r.Bottom - d);
			path.AddArc(Rectangle.FromLTRB(r.Right - d, r.Bottom - d, r.Right, r.Bottom), 0, 90);
			path.AddLine(r.Right - d, r.Bottom, r.Left + d, r.Bottom);
			path.AddArc(Rectangle.FromLTRB(r.Left, r.Bottom - d, r.Left + d, r.Bottom), 90, 90);
			path.AddLine(r.Left, r.Bottom - d, r.Left, r.Top + d);
			path.AddArc(Rectangle.FromLTRB(r.Left, r.Top, r.Left + d, r.Top + d), 180, 90);
			path.CloseFigure();

			return path;
		}

		void DrawRoundedRectangle(Graphics g, Pen p, int x, int y, int width, int height, int radius) {
			using (GraphicsPath path = this.GetRoundedRectangle(x, y, width, height, radius)) {
				SmoothingMode mode = g.SmoothingMode;

				try {
					g.SmoothingMode = SmoothingMode.AntiAlias;
					g.DrawPath(p, path);
				}
				finally {
					g.SmoothingMode = mode;
				}
			}
		}

		void FillRoundedRectangle(Graphics g, Brush b, int x, int y, int width, int height, int radius) {
			using (GraphicsPath path = this.GetRoundedRectangle(x, y, width, height, radius)) {
				SmoothingMode mode = g.SmoothingMode;

				try {
					g.SmoothingMode = SmoothingMode.AntiAlias;
					g.FillPath(b, path);
				}
				finally {
					g.SmoothingMode = mode;
				}
			}
		}

		void DrawBorder(Graphics g) {
			Pen p = new Pen(this.BorderColor);

			for (int width = 0; width < this.BorderWidth; width++) {
				if (this.BorderRadius > 0) {
					this.DrawRoundedRectangle(g, p, width, width, (this.ClientRectangle.Width - 1) - (width * 2),
						(this.ClientRectangle.Height - 1) - (width * 2), this.BorderRadius);
				}
				else {
					g.DrawRectangle(p, width, width, 
						(this.ClientSize.Width - 1) - (width * 2), 
						(this.ClientSize.Height - 1) - (width * 2));
				}
			}
		}

		void DrawProgress(Graphics g) {
			Pen pNoFill = new Pen(this.BackColor);
			LinearGradientBrush lgb = new LinearGradientBrush(new Point(0, 0), 
				new Point(this.Size), this.BarColorLeft, this.BarColorRight);			

			if (this.BorderRadius > 0) {
				this.FillRoundedRectangle(g, lgb, this.BarX, this.BarY, this.BarFillWidth, this.BarHeight, this.BorderRadius);
				this.FillRoundedRectangle(g, pNoFill.Brush, this.BarFillWidth > 0 ? this.BarFillWidth + 1 : this.BarX,
					this.BarY, this.BarNoFillWidth, this.BarHeight, this.BorderRadius);
			}
			else {
				g.FillRectangle(lgb, this.BarX, this.BarY, this.BarFillWidth, this.BarHeight);
				g.FillRectangle(pNoFill.Brush, this.BarFillWidth > 0 ? this.BarFillWidth + 1 : this.BarX,
					this.BarY, this.BarNoFillWidth, this.BarHeight);
			}
		}

		void DrawText(Graphics g) {
			TextRenderer.DrawText(g, this.Text, this.Font, new Rectangle(this.BarX, this.BarY, this.BarWidth, this.BarHeight),
				this.ForeColor, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
		}

		void OnPaint(object sender, PaintEventArgs e) {
			if (this.BorderWidth > 0) {
				this.DrawBorder(e.Graphics);
			}

			this.DrawProgress(e.Graphics);

			if (this.Text != null && this.Text.Length > 0) {
				this.DrawText(e.Graphics);
			}
		}

		public ProgressBarEx() {
			InitializeComponent();
			this.DoubleBuffered = true;
			this.ForeColor = Color.FromKnownColor(KnownColor.HighlightText);
			this.Paint += new PaintEventHandler(OnPaint);
			this.Resize += new EventHandler(ProgressBarEx_Resize);
		}

		void ProgressBarEx_Resize(object sender, EventArgs e) {
			this.Refresh();
		}
	}
}
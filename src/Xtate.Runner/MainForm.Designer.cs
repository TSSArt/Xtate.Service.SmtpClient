using System;
using System.Drawing;
using System.Windows.Forms;

namespace Xtate.Runner
{
	partial class MainForm
	{
		/// <summary>
		///  Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		private readonly StateMachineHost _stateMachineHost;
		private          TabControl       tabControl;


		/// <summary>
		///  Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		///  Required method for Designer support - do not modify
		///  the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.tabControl = new TabControl();
			this.SuspendLayout();
			this.tabControl.Dock = DockStyle.Fill;
			this.tabControl.Location = new Point(0, 0);
			this.tabControl.Multiline = true;
			this.tabControl.Name = "tabControl";
			this.tabControl.SelectedIndex = 0;
			this.tabControl.Size = new Size(1198, 741);
			this.tabControl.TabIndex = 0;
			this.AutoScaleDimensions = new SizeF(7f, 15f);
			this.AutoScaleMode = AutoScaleMode.Font;
			this.ClientSize = new Size(1198, 741);
			this.Controls.Add((Control) this.tabControl);
			this.Name = "Form1";
			this.Text = "Form1";
			this.ResumeLayout(false);
		}

		#endregion
	}
}


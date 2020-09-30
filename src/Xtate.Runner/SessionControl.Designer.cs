using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Xtate.Runner
{
	partial class SessionControl
	{
		/// <summary>
		///     Required designer variable.
		/// </summary>
		private readonly StateMachineHost _host;

		private readonly IContainer     components = null;
		private          string        _sessionId;
		private          TextBox        dataModel;
		private          Label          label1;
		private          Label          label2;
		private          Label          label3;
		private          ListBox        log;
		private          Panel          panel1;
		private          Panel          panel2;
		private          Panel          panel3;
		private          Panel          panel4;
		private          Panel          panel5;
		private          Panel          panel6;
		private          Panel          panel7;
		private          TextBox        scxml;
		private          SplitContainer splitContainer1;
		private          SplitContainer splitContainer2;
		private          Button         startButton;
		private          Button         stopButton;

		/// <summary>
		///     Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && components != null)
			{
				components.Dispose();
			}

			base.Dispose(disposing);
		}

	#region Component Designer generated code

		/// <summary>
		///     Required method for Designer support - do not modify
		///     the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.scxml = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.log = new System.Windows.Forms.ListBox();
			this.label2 = new System.Windows.Forms.Label();
			this.dataModel = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.startButton = new System.Windows.Forms.Button();
			this.stopButton = new System.Windows.Forms.Button();
			this.panel1 = new System.Windows.Forms.Panel();
			this.SaveBtn = new System.Windows.Forms.Button();
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.panel5 = new System.Windows.Forms.Panel();
			this.panel2 = new System.Windows.Forms.Panel();
			this.splitContainer2 = new System.Windows.Forms.SplitContainer();
			this.panel6 = new System.Windows.Forms.Panel();
			this.panel4 = new System.Windows.Forms.Panel();
			this.panel7 = new System.Windows.Forms.Panel();
			this.panel3 = new System.Windows.Forms.Panel();
			this.ClearLogBtn = new System.Windows.Forms.Button();
			this.panel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			this.panel5.SuspendLayout();
			this.panel2.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
			this.splitContainer2.Panel1.SuspendLayout();
			this.splitContainer2.Panel2.SuspendLayout();
			this.splitContainer2.SuspendLayout();
			this.panel6.SuspendLayout();
			this.panel4.SuspendLayout();
			this.panel7.SuspendLayout();
			this.panel3.SuspendLayout();
			this.SuspendLayout();
			// 
			// scxml
			// 
			this.scxml.Dock = System.Windows.Forms.DockStyle.Fill;
			this.scxml.Location = new System.Drawing.Point(0, 0);
			this.scxml.Multiline = true;
			this.scxml.Name = "scxml";
			this.scxml.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.scxml.Size = new System.Drawing.Size(370, 612);
			this.scxml.TabIndex = 0;
			this.scxml.WordWrap = false;
			// 
			// label1
			// 
			this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.label1.Location = new System.Drawing.Point(0, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(370, 27);
			this.label1.TabIndex = 1;
			this.label1.Text = "SCXML";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// log
			// 
			this.log.Dock = System.Windows.Forms.DockStyle.Fill;
			this.log.FormattingEnabled = true;
			this.log.ItemHeight = 15;
			this.log.Location = new System.Drawing.Point(0, 0);
			this.log.Name = "log";
			this.log.Size = new System.Drawing.Size(245, 612);
			this.log.TabIndex = 2;
			this.log.SelectedIndexChanged += new System.EventHandler(this.Log_SelectedIndexChanged);
			// 
			// label2
			// 
			this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.label2.Location = new System.Drawing.Point(0, 0);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(245, 27);
			this.label2.TabIndex = 1;
			this.label2.Text = "Log";
			this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// dataModel
			// 
			this.dataModel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.dataModel.Location = new System.Drawing.Point(0, 0);
			this.dataModel.Multiline = true;
			this.dataModel.Name = "dataModel";
			this.dataModel.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.dataModel.Size = new System.Drawing.Size(487, 612);
			this.dataModel.TabIndex = 0;
			this.dataModel.WordWrap = false;
			// 
			// label3
			// 
			this.label3.Dock = System.Windows.Forms.DockStyle.Fill;
			this.label3.Location = new System.Drawing.Point(0, 0);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(487, 27);
			this.label3.TabIndex = 1;
			this.label3.Text = "Data Model";
			this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// startButton
			// 
			this.startButton.Location = new System.Drawing.Point(10, 9);
			this.startButton.Name = "startButton";
			this.startButton.Size = new System.Drawing.Size(75, 23);
			this.startButton.TabIndex = 3;
			this.startButton.Text = "Start";
			this.startButton.UseVisualStyleBackColor = true;
			this.startButton.Click += new System.EventHandler(this.Start_Click);
			// 
			// stopButton
			// 
			this.stopButton.Location = new System.Drawing.Point(92, 9);
			this.stopButton.Name = "stopButton";
			this.stopButton.Size = new System.Drawing.Size(75, 23);
			this.stopButton.TabIndex = 3;
			this.stopButton.Text = "Stop";
			this.stopButton.UseVisualStyleBackColor = true;
			this.stopButton.Click += new System.EventHandler(this.Stop_Click);
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.ClearLogBtn);
			this.panel1.Controls.Add(this.SaveBtn);
			this.panel1.Controls.Add(this.startButton);
			this.panel1.Controls.Add(this.stopButton);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel1.Location = new System.Drawing.Point(0, 0);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(1110, 44);
			this.panel1.TabIndex = 4;
			// 
			// SaveBtn
			// 
			this.SaveBtn.Location = new System.Drawing.Point(268, 9);
			this.SaveBtn.Name = "SaveBtn";
			this.SaveBtn.Size = new System.Drawing.Size(75, 23);
			this.SaveBtn.TabIndex = 3;
			this.SaveBtn.Text = "Save";
			this.SaveBtn.UseVisualStyleBackColor = true;
			this.SaveBtn.Click += new System.EventHandler(this.Save_Click);
			// 
			// splitContainer1
			// 
			this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer1.Location = new System.Drawing.Point(0, 44);
			this.splitContainer1.Name = "splitContainer1";
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add(this.panel5);
			this.splitContainer1.Panel1.Controls.Add(this.panel2);
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
			this.splitContainer1.Size = new System.Drawing.Size(1110, 639);
			this.splitContainer1.SplitterDistance = 370;
			this.splitContainer1.TabIndex = 5;
			this.splitContainer1.Text = "splitContainer1";
			// 
			// panel5
			// 
			this.panel5.Controls.Add(this.scxml);
			this.panel5.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel5.Location = new System.Drawing.Point(0, 27);
			this.panel5.Name = "panel5";
			this.panel5.Size = new System.Drawing.Size(370, 612);
			this.panel5.TabIndex = 1;
			// 
			// panel2
			// 
			this.panel2.Controls.Add(this.label1);
			this.panel2.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel2.Location = new System.Drawing.Point(0, 0);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(370, 27);
			this.panel2.TabIndex = 0;
			// 
			// splitContainer2
			// 
			this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer2.Location = new System.Drawing.Point(0, 0);
			this.splitContainer2.Name = "splitContainer2";
			// 
			// splitContainer2.Panel1
			// 
			this.splitContainer2.Panel1.Controls.Add(this.panel6);
			this.splitContainer2.Panel1.Controls.Add(this.panel4);
			// 
			// splitContainer2.Panel2
			// 
			this.splitContainer2.Panel2.Controls.Add(this.panel7);
			this.splitContainer2.Panel2.Controls.Add(this.panel3);
			this.splitContainer2.Size = new System.Drawing.Size(736, 639);
			this.splitContainer2.SplitterDistance = 245;
			this.splitContainer2.TabIndex = 0;
			this.splitContainer2.Text = "splitContainer2";
			// 
			// panel6
			// 
			this.panel6.Controls.Add(this.log);
			this.panel6.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel6.Location = new System.Drawing.Point(0, 27);
			this.panel6.Name = "panel6";
			this.panel6.Size = new System.Drawing.Size(245, 612);
			this.panel6.TabIndex = 3;
			// 
			// panel4
			// 
			this.panel4.Controls.Add(this.label2);
			this.panel4.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel4.Location = new System.Drawing.Point(0, 0);
			this.panel4.Name = "panel4";
			this.panel4.Size = new System.Drawing.Size(245, 27);
			this.panel4.TabIndex = 0;
			// 
			// panel7
			// 
			this.panel7.Controls.Add(this.dataModel);
			this.panel7.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel7.Location = new System.Drawing.Point(0, 27);
			this.panel7.Name = "panel7";
			this.panel7.Size = new System.Drawing.Size(487, 612);
			this.panel7.TabIndex = 1;
			// 
			// panel3
			// 
			this.panel3.Controls.Add(this.label3);
			this.panel3.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel3.Location = new System.Drawing.Point(0, 0);
			this.panel3.Name = "panel3";
			this.panel3.Size = new System.Drawing.Size(487, 27);
			this.panel3.TabIndex = 0;
			// 
			// ClearLogBtn
			// 
			this.ClearLogBtn.Location = new System.Drawing.Point(423, 9);
			this.ClearLogBtn.Name = "ClearLogBtn";
			this.ClearLogBtn.Size = new System.Drawing.Size(75, 23);
			this.ClearLogBtn.TabIndex = 3;
			this.ClearLogBtn.Text = "Clear Log";
			this.ClearLogBtn.UseVisualStyleBackColor = true;
			this.ClearLogBtn.Click += new System.EventHandler(this.ClearLog_Click);
			// 
			// SessionControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.splitContainer1);
			this.Controls.Add(this.panel1);
			this.Name = "SessionControl";
			this.Size = new System.Drawing.Size(1110, 683);
			this.panel1.ResumeLayout(false);
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
			this.splitContainer1.ResumeLayout(false);
			this.panel5.ResumeLayout(false);
			this.panel5.PerformLayout();
			this.panel2.ResumeLayout(false);
			this.splitContainer2.Panel1.ResumeLayout(false);
			this.splitContainer2.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
			this.splitContainer2.ResumeLayout(false);
			this.panel6.ResumeLayout(false);
			this.panel4.ResumeLayout(false);
			this.panel7.ResumeLayout(false);
			this.panel7.PerformLayout();
			this.panel3.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private Button SaveBtn;
		private Button ClearLogBtn;
	}
}
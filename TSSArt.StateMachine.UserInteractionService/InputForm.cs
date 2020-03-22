using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace TSSArt.StateMachine.Services
{
	public partial class InputForm : Form
	{
		private int _count;

		public InputForm()
		{
			InitializeComponent();

			var okButton = new Button
						   {
								   Top = 10,
								   Left = 10,
								   Height = 16,
								   Width = 48,
								   Text = @"OK"
						   };

			okButton.Click += (sender, args) => { Close(DialogResult.OK, GetInputResult()); };

			var cancelButton = new Button
							   {
									   Top = 10,
									   Left = 60,
									   Height = 16,
									   Width = 48,
									   Text = @"Cancel"
							   };

			cancelButton.Click += (sender, args) => { Close(DialogResult.Cancel, result: default); };

			Controls.Add(okButton);
			Controls.Add(cancelButton);
		}

		public IDictionary<string, string>? Result { get; private set; }

		private IDictionary<string, string> GetInputResult()
		{
			return Controls.OfType<TextBox>().ToDictionary(tb => (string) tb.Tag, tb => tb.Text);
		}

		public void Close(DialogResult dialogResult, IDictionary<string, string>? result)
		{
			Result = result;
			DialogResult = dialogResult;

			if (InvokeRequired)
			{
				BeginInvoke(new Action(Close));
			}
			else
			{
				Close();
			}
		}

		public void AddInput(string? name, string? location, string? type)
		{
			_count ++;

			var label = new Label
						{
								Top = 40 + _count * 32,
								Left = 10,
								Text = name,
								AutoSize = true
						};

			var input = new TextBox
						{
								Top = 40 + _count * 32,
								Left = 50,
								Width = 200,
								UseSystemPasswordChar = type == "password",
								Tag = location
						};

			Controls.Add(label);
			Controls.Add(input);
		}
	}
}
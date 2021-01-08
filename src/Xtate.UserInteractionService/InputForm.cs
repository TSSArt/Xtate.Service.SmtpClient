#region Copyright © 2019-2021 Sergii Artemenko

// This file is part of the Xtate project. <https://xtate.net/>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Xtate.Service
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

			okButton.Click += (_, _) => { Close(DialogResult.OK, GetInputResult()); };

			var cancelButton = new Button
							   {
									   Top = 10,
									   Left = 60,
									   Height = 16,
									   Width = 48,
									   Text = @"Cancel"
							   };

			cancelButton.Click += (_, _) => { Close(DialogResult.Cancel, result: default); };

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
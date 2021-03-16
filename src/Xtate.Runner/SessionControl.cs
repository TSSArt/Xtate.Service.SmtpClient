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
using System.IO;
using System.Windows.Forms;

namespace Xtate.Runner
{
	public partial class SessionControl : UserControl
	{
		public SessionControl(
#nullable enable
				StateMachineHost host, Uri source, string? sessionId)
		{
			_host = host;
			Source = source;
			Name = nameof(SessionControl);
			InitializeComponent();
			SessionId = sessionId;
		}

		public Uri Source { get; }

		public string? SessionId
		{
			get => _sessionId;
			private set
			{
				_sessionId = value;
				startButton.Enabled = value is null;
				stopButton.Enabled = value is not null;
			}
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			Dock = DockStyle.Fill;
			scxml.Text = File.ReadAllText(Source.IsAbsoluteUri ? Source.LocalPath : Source.OriginalString);
			scxml.Select(start: 0, length: 0);
		}

		public void AddLog(string message,
						   DataModelList? list,
						   string? dataModelAsText,
						   string? dataAsText,
						   Exception? exception)
		{
			log.Items.Add(new LogItem(message, list, dataModelAsText, dataAsText, exception));
		}

		public void AssignToSession(string sessionId)
		{
			SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
			startButton.Enabled = false;
			stopButton.Enabled = true;
		}

		private async void Start_Click(object sender, EventArgs e)
		{
			if (SessionId is not null)
			{
				return;
			}

			SessionId = Xtate.SessionId.New().Value;
			await _host.StartStateMachineAsync(scxml.Text, Source, SessionId);
		}

		private async void Stop_Click(object sender, EventArgs e)
		{
			await _host.DestroyStateMachineAsync(SessionId!);

			Stop();
		}

		public void Stop() => SessionId = default;

		private void Log_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (log.SelectedItem is LogItem selectedItem)
			{
				if (IfErrorPresent(selectedItem, out var error))
				{
					dataModel.Text = selectedItem.DataModelAsText + @"

EXCEPTION:

" + error;
				}
				else if (selectedItem.DataAsText is not null)
				{
					dataModel.Text = selectedItem.DataModelAsText + @"

DATA:

" + selectedItem.DataAsText;
				}
				else
				{
					dataModel.Text = selectedItem.DataModelAsText;
				}
			}
		}

		private static bool IfErrorPresent(LogItem logItem, out string? error)
		{
			if (logItem.Exception is not null)
			{
				error = logItem.Exception.ToString();
				return true;
			}

			error = default;
			if (logItem.DataModel is null)
			{
				return false;
			}

			var dataModelValue = logItem.DataModel["_event"];
			DataModelList list = dataModelValue.AsListOrEmpty();
			dataModelValue = list["name"];
			var str1 = dataModelValue.AsStringOrDefault();
			if (str1 is null || !str1.StartsWith("error."))
			{
				return false;
			}

			dataModelValue = list["data"];
			dataModelValue = dataModelValue.AsListOrEmpty()["text"];
			var str2 = dataModelValue.AsStringOrDefault();
			error = str2;
			return true;
		}

		private void Save_Click(object sender, EventArgs e)
		{
			File.WriteAllText(Source.IsAbsoluteUri ? Source.LocalPath : Source.OriginalString, scxml.Text);
		}

		private void ClearLog_Click(object sender, EventArgs e)
		{
			log.Items.Clear();
		}
	}
}
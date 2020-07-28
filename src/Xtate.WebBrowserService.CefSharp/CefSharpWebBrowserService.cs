#region Copyright © 2019-2020 Sergii Artemenko
// 
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
// 
#endregion

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Xtate.Service
{
	public class CefSharpWebBrowserService : WebBrowserService
	{
		protected override ValueTask<DataModelValue> Execute()
		{
			var task = Task.Factory.StartNew(Show, StopToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);

			return new ValueTask<DataModelValue>(task);
		}

		[SuppressMessage(category: "ReSharper", checkId: "AccessToDisposedClosure", Justification = "Form closed by external event")]
		private DataModelValue Show()
		{
			var url = Source?.ToString();
			var content = RawContent ?? Content.AsStringOrDefault();

			using var form = new BrowserForm(url != null ? new Uri(url) : null, content);

			using var registration = StopToken.Register(() => form.Close(DialogResult.Abort, result: default));

			form.Closed += (sender, args) => Application.ExitThread();

			Application.Run(form);

			var result = new DataModelObject();

			if (form.DialogResult == DialogResult.OK)
			{
				result.Add(key: "status", value: "ok");

				if (form.Result != null)
				{
					var parameters = new DataModelObject();

					foreach (var pair in form.Result)
					{
						parameters.Add(pair.Key, pair.Value);
					}

					result.Add(key: "parameters", parameters);
				}
				else
				{
					result.Add(key: "parameters", DataModelObject.Empty);
				}
			}
			else
			{
				result.Add(key: "status", value: "cancel");
			}

			return result;
		}
	}
}
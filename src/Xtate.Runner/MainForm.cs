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
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Xtate.Core;
using Xtate.CustomAction;

namespace Xtate.Runner
{
	public partial class MainForm : Form, ILoggerOld
	{
		public MainForm()
		{
			var baseUri = new Uri("http://localhost:5000/");
			_stateMachineHost = new StateMachineHostBuilder()
								.SetLogger(this)

								//TODO:
								//.AddResourceLoaderFactory(FileResourceLoaderFactory.Instance)
								//.AddResourceLoaderFactory(ResxResourceLoaderFactory.Instance)
								.AddServices(s => s.AddEcmaScript().AddXPath())
								.AddHttpIoProcessor(baseUri)
								.AddSystemAction()
								.AddSmtpClient()
								.AddHttpClient()
								.AddCustomActionFactory(BasicCustomActionFactory.Instance)
								.AddCustomActionFactory(MimeCustomActionFactory.Instance)
								//.AddCustomActionFactory(MidCustomActionFactory.Instance)
								.AddCefSharpWebBrowser()
								.AddUserInteraction()
								.SetConfigurationValue(key: "uiEndpoint", value: "http://localhost:5000/dialog")
								.SetConfigurationValue(key: "mailEndpoint", value: "http://mid.dev.tssart.com/MailServer/Web2/api/Mail/")
								.SetBaseUri(new Uri(Environment.CurrentDirectory + "\\"))
								.Build(ServiceLocator.Default);

			InitializeComponent();
		}

	#region Interface ILogger

		public ValueTask TraceSendEvent(IOutgoingEvent outgoingEvent)
		{
			DataModelList dataModelObject1 = new();
			var name = (DataModelValue) EventName.ToName(outgoingEvent.NameParts);
			dataModelObject1.Add(key: "Name", name);
			var sendId = (DataModelValue) outgoingEvent.SendId;
			dataModelObject1.Add(key: "SendId", sendId);
			DataModelValue delayMs = outgoingEvent.DelayMs;
			dataModelObject1.Add(key: "DelayMs", delayMs);
			var type = outgoingEvent.Type;
			var dataModelValue1 = (DataModelValue) type?.ToString();
			dataModelObject1.Add(key: "Type", dataModelValue1);
			var target = outgoingEvent.Target;
			var dataModelValue2 = (DataModelValue) target?.ToString();
			dataModelObject1.Add(key: "Target", dataModelValue2);
			var data = outgoingEvent.Data;
			dataModelObject1.Add(key: "Data", data);
			var dataModelObject2 = dataModelObject1;
			return new ValueTask(WriteLog("[SEND] Name: " + EventName.ToName(outgoingEvent.NameParts), data: dataModelObject2));
		}

		public ValueTask TraceCancelEvent(SendId sendId) =>
			new(WriteLog("[CANCEL] SendId: " + sendId.Value + "."));

		public ValueTask TraceStartInvoke(InvokeData invokeData)
		{
			DataModelList dataModelObject1 = new();
			var invokeId = (DataModelValue) invokeData.InvokeId;
			dataModelObject1.Add(key: "InvokeId", invokeId);
			var dataModelValue1 = (DataModelValue) invokeData.Type.ToString();
			dataModelObject1.Add(key: "Type", dataModelValue1);
			var source = invokeData.Source;
			var dataModelValue2 = (DataModelValue) source?.ToString();
			dataModelObject1.Add(key: "Source", dataModelValue2);
			var rawContent = (DataModelValue) invokeData.RawContent;
			dataModelObject1.Add(key: "RawContent", rawContent);
			var content = invokeData.Content;
			dataModelObject1.Add(key: "Content", content);
			var parameters = invokeData.Parameters;
			dataModelObject1.Add(key: "Parameters", parameters);
			var dataModelObject2 = dataModelObject1;
			return new ValueTask(WriteLog(message: "[INVOKE]", data: dataModelObject2));
		}

		public ValueTask TraceCancelInvoke(InvokeId invokeId) =>
			new();

		ValueTask ILoggerOld.ExecuteLogOld(LogLevel logLevel,
									 string? message,
									 DataModelValue data,
									 Exception? exception) =>
			new(WriteLog("[" + logLevel + "] Label: " + message, exception: exception, data: data));

		ValueTask ILoggerOld.LogErrorOld(ErrorType errorType,
								   Exception exception,
								   string? sourceEntityId) =>
			new(WriteLog(string.Format(format: "[ERROR] Type: {0}. Entity: {1}", errorType, sourceEntityId), exception: exception));

		ValueTask ILoggerOld.TraceProcessingEvent(IEvent evt) =>
			new(WriteLog("[EVENT] Name: " + EventName.ToName(evt.NameParts) + "."));

		ValueTask ILoggerOld.TraceEnteringState(IIdentifier stateId) =>
			new(WriteLog("[ENTER] State: " + stateId.Value + "."));

		ValueTask ILoggerOld.TraceEnteredState(IIdentifier stateId) =>
			new();

		ValueTask ILoggerOld.TraceExitingState(IIdentifier stateId) =>
			new();

		ValueTask ILoggerOld.TraceExitedState(IIdentifier stateId) =>
			new(WriteLog("[EXIT] State: " + stateId.Value + "."));

		ValueTask ILoggerOld.TracePerformingTransition(TransitionType type,
													string? eventDescriptor,
													string? target) =>
			new(WriteLog("[TRANSITION] Target: " + target + "."));

		ValueTask ILoggerOld.TracePerformedTransition(TransitionType type,
												   string? eventDescriptor,
												   string? target) =>
			new();

		async ValueTask ILoggerOld.TraceInterpreterState(StateMachineInterpreterState state)
		{
			var stop = state == StateMachineInterpreterState.Exited || state == StateMachineInterpreterState.Halted;
			await WriteLog(string.Format(format: "[INTERPRETER] State: {0}.", state), stop);
		}

		bool ILoggerOld.IsTracingEnabled => true;

	#endregion

		protected override async void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			await _stateMachineHost.StartHostAsync();
			var path = Path.Combine(Environment.CurrentDirectory, path2: "../../../Scxml");
			var scxmlList = Directory.EnumerateFiles(path, searchPattern: "*.scxml");
			foreach (var str in scxmlList)
			{
				AddSessionControlTab(new Uri(str, UriKind.RelativeOrAbsolute), sessionId: null);
			}
		}

		private SessionControl AddSessionControlTab(Uri uri, string? sessionId)
		{
			TabPage tabPage = new(Path.GetFileNameWithoutExtension(uri.IsAbsoluteUri ? uri.LocalPath : uri.OriginalString));
			SessionControl sessionControl = new(_stateMachineHost, uri, sessionId);
			tabPage.Controls.Add(sessionControl);
			tabControl.TabPages.Add(tabPage);
			return sessionControl;
		}

		protected override async void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);
			await _stateMachineHost.StopHostAsync();
		}

		private Task WriteLog(string message,
							  bool stop = false,
							  Exception? exception = default,
							  DataModelValue data = default)
		{
			DataModelValue dataModel = DataModelValue.Null;
			string? dataModelAsText;
			string? dataAsText;
			SessionId? sessionId;

			/*if (loggerContext is IInterpreterLoggerContext interpreterLoggerContext)
			{
				dataModel = interpreterLoggerContext.GetDataModel();
				dataModelAsText = interpreterLoggerContext.ConvertToText(dataModel);
				dataAsText = interpreterLoggerContext.ConvertToText(data);
				sessionId = interpreterLoggerContext.SessionId;
			}
			else*/
			{
				//dataModel = loggerContext?.GetProperties();
				dataModelAsText = dataModel.ToString(CultureInfo.InvariantCulture);
				dataAsText = data.ToString(CultureInfo.InvariantCulture);
				sessionId = default;
			}

			if (InvokeRequired)
			{
				return Task.Factory.FromAsync(BeginInvoke((Action) (
															  () => WriteLogSafe(sessionId, stop, exception, message, dataModel, dataModelAsText, dataAsText))),
											  EndInvoke);
			}

			WriteLogSafe(sessionId, stop, exception, message, dataModel, dataModelAsText, dataAsText);

			return Task.CompletedTask;
		}

		private void WriteLogSafe(SessionId? sessionId,
								  bool stop,
								  Exception? exception,
								  string message,
								  DataModelValue dataModel,
								  string? dataModelAsText,
								  string? dataAsText)
		{
			if (sessionId is null)
			{
				return;
			}

			foreach (var control1 in tabControl.TabPages.OfType<TabPage>())
			{
				if (control1.Controls["sessionControl"] is SessionControl control && control.SessionId == sessionId.Value)
				{
					control.AddLog(message, dataModel, dataModelAsText, dataAsText, exception);
					if (!stop)
					{
						return;
					}

					control.Stop();
					return;
				}
			}

			string? str;
			if (dataModel.IsUndefinedOrNull())
			{
				str = default;
			}
			else
			{
				var value = dataModel.AsList()["_x"];
				value = value.AsList()["host"];
				value = value.AsList()["location"];
				str = value.AsString();
			}

			var uriString = str;
			if (uriString is null)
			{
				return;
			}

			Uri uri = new(uriString, UriKind.RelativeOrAbsolute);
			foreach (var control1 in tabControl.TabPages.OfType<TabPage>())
			{
				if (control1.Controls["sessionControl"] is SessionControl control && control.Source == uri && control.SessionId is null)
				{
					control.AssignToSession(sessionId.Value);
					control.AddLog(message, dataModel, dataModelAsText, dataAsText, exception);
					if (!stop)
					{
						return;
					}

					control.Stop();
					return;
				}
			}

			var sessionControl = AddSessionControlTab(uri, sessionId.Value);
			sessionControl.AddLog(message, dataModel, dataModelAsText, dataAsText, exception);
			if (stop)
			{
				sessionControl.Stop();
			}
		}
	}
}
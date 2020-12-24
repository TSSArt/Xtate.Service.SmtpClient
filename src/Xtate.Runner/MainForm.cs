#region Copyright © 2019-2020 Sergii Artemenko

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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Xtate.CustomAction;

namespace Xtate.Runner
{
	public partial class MainForm : Form, ILogger
	{
		public MainForm()
		{
			var baseUri = new Uri("http://localhost:5000/");
			_stateMachineHost = new StateMachineHostBuilder()
								.SetLogger(this)
								.AddResourceLoader(FileResourceLoader.Instance)
								.AddEcmaScript()
								.AddXPath()
								.AddHttpIoProcessor(baseUri)
								.AddSystemAction()
								.AddSmtpClient()
								.AddHttpClient()
								.AddCustomActionFactory(BasicCustomActionFactory.Instance).AddCustomActionFactory(MimeCustomActionFactory.Instance)
								.AddCustomActionFactory(MidCustomActionFactory.Instance).AddResourceLoader(ResxResourceLoader.Instance)
								.AddCefSharpWebBrowser()
								.AddUserInteraction()
								.SetConfigurationValue(key: "uiEndpoint", value: "http://localhost:5000/dialog")
								.SetConfigurationValue(key: "mailEndpoint", value: "http://mid.dev.tssart.com/MailServer/Web2/api/Mail/")
								.SetBaseUri(new Uri(Environment.CurrentDirectory + "\\"))
								.Build();

			InitializeComponent();
		}

	#region Interface ILogger

		public ValueTask TraceSendEvent(ILoggerContext loggerContext,
										IOutgoingEvent evt,
										CancellationToken token)
		{
			DataModelList dataModelObject1 = new();
			var name = (DataModelValue) EventName.ToName(evt.NameParts);
			dataModelObject1.Add(key: "Name", in name);
			var sendId = (DataModelValue) evt.SendId;
			dataModelObject1.Add(key: "SendId", in sendId);
			DataModelValue delayMs = evt.DelayMs;
			dataModelObject1.Add(key: "DelayMs", in delayMs);
			var type = evt.Type;
			var dataModelValue1 = (DataModelValue) type?.ToString();
			dataModelObject1.Add(key: "Type", in dataModelValue1);
			var target = evt.Target;
			var dataModelValue2 = (DataModelValue) target?.ToString();
			dataModelObject1.Add(key: "Target", in dataModelValue2);
			var data = evt.Data;
			dataModelObject1.Add(key: "Data", in data);
			DataModelList dataModelObject2 = dataModelObject1;
			return new ValueTask(WriteLog(loggerContext, "[SEND] Name: " + EventName.ToName(evt.NameParts), data: dataModelObject2));
		}

		public ValueTask TraceCancelEvent(ILoggerContext loggerContext,
										  SendId sendId,
										  CancellationToken token) =>
				new(WriteLog(loggerContext, "[CANCEL] SendId: " + sendId.Value + "."));

		public ValueTask TraceStartInvoke(ILoggerContext loggerContext,
										  InvokeData invokeData,
										  CancellationToken token)
		{
			DataModelList dataModelObject1 = new();
			var invokeId = (DataModelValue) invokeData.InvokeId;
			dataModelObject1.Add(key: "InvokeId", in invokeId);
			var dataModelValue1 = (DataModelValue) invokeData.Type.ToString();
			dataModelObject1.Add(key: "Type", in dataModelValue1);
			var source = invokeData.Source;
			var dataModelValue2 = (DataModelValue) source?.ToString();
			dataModelObject1.Add(key: "Source", in dataModelValue2);
			var rawContent = (DataModelValue) invokeData.RawContent;
			dataModelObject1.Add(key: "RawContent", in rawContent);
			var content = invokeData.Content;
			dataModelObject1.Add(key: "Content", in content);
			var parameters = invokeData.Parameters;
			dataModelObject1.Add(key: "Parameters", in parameters);
			DataModelList dataModelObject2 = dataModelObject1;
			return new ValueTask(WriteLog(loggerContext, message: "[INVOKE]", data: dataModelObject2));
		}

		public ValueTask TraceCancelInvoke(ILoggerContext loggerContext,
										   InvokeId invokeId,
										   CancellationToken token) =>
				new();

		ValueTask ILogger.ExecuteLog(ILoggerContext loggerContext,
									 LogLevel logLevel,
									 string? message,
									 DataModelValue data,
									 Exception? exception,
									 CancellationToken token) =>
				new(WriteLog(loggerContext, "[" + logLevel + "] Label: " + message, data: data, exception: exception));

		ValueTask ILogger.LogError(ILoggerContext loggerContext,
								   ErrorType errorType,
								   Exception exception,
								   string? sourceEntityId,
								   CancellationToken token) =>
				new(WriteLog(loggerContext, string.Format(format: "[ERROR] Type: {0}. Entity: {1}", errorType, sourceEntityId), exception: exception));

		ValueTask ILogger.TraceProcessingEvent(ILoggerContext loggerContext,
											   IEvent evt,
											   CancellationToken token) =>
				new(WriteLog(loggerContext, "[EVENT] Name: " + EventName.ToName(evt.NameParts) + "."));

		ValueTask ILogger.TraceEnteringState(ILoggerContext loggerContext,
											 IIdentifier stateId,
											 CancellationToken token) =>
				new(WriteLog(loggerContext, "[ENTER] State: " + stateId.Value + "."));

		ValueTask ILogger.TraceEnteredState(ILoggerContext loggerContext,
											IIdentifier stateId,
											CancellationToken token) =>
				new();

		ValueTask ILogger.TraceExitingState(ILoggerContext loggerContext,
											IIdentifier stateId,
											CancellationToken token) =>
				new();

		ValueTask ILogger.TraceExitedState(ILoggerContext loggerContext,
										   IIdentifier stateId,
										   CancellationToken token) =>
				new(WriteLog(loggerContext, "[EXIT] State: " + stateId.Value + "."));

		ValueTask ILogger.TracePerformingTransition(ILoggerContext loggerContext,
													TransitionType type,
													string? eventDescriptor,
													string? target,
													CancellationToken token) =>
				new(WriteLog(loggerContext, "[TRANSITION] Target: " + target + "."));

		ValueTask ILogger.TracePerformedTransition(ILoggerContext loggerContext,
												   TransitionType type,
												   string? eventDescriptor,
												   string? target,
												   CancellationToken token) =>
				new();

		async ValueTask ILogger.TraceInterpreterState(ILoggerContext loggerContext,
													  StateMachineInterpreterState state,
													  CancellationToken token)
		{
			var stop = state == StateMachineInterpreterState.Exited || state == StateMachineInterpreterState.Halted;
			await WriteLog(loggerContext, string.Format(format: "[INTERPRETER] State: {0}.", state), stop);
		}

		bool ILogger.IsTracingEnabled => true;

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

		private Task WriteLog(ILoggerContext loggerContext,
							  string message,
							  bool stop = false,
							  Exception? exception = null,
							  DataModelValue data = default)
		{
			var dataModel = loggerContext.GetDataModel();
			var dataModelAsText = loggerContext.GetDataModelAsText();
			var dataAsText = !data.IsUndefined() ? loggerContext.ConvertToText(in data) : null;
			if (InvokeRequired)
			{
				return Task.Factory.FromAsync(BeginInvoke((Action) (
																  () => WriteLogSafe(loggerContext.SessionId, stop, exception, message, dataModel, dataModelAsText, dataAsText))),
											  EndInvoke);
			}

			WriteLogSafe(loggerContext.SessionId, stop, exception, message, dataModel, dataModelAsText, dataAsText);

			return Task.CompletedTask;
		}

		private void WriteLogSafe(SessionId? sessionId,
								  bool stop,
								  Exception? exception,
								  string message,
								  DataModelList? dataModel,
								  string? dataModelAsText,
								  string? dataAsText)
		{
			if (sessionId is null)
			{
				return;
			}

			foreach (var control1 in tabControl.TabPages.OfType<TabPage>())
			{
				if (control1?.Controls["sessionControl"] is SessionControl control && control.SessionId == sessionId.Value)
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
			if (dataModel is null)
			{
				str = null;
			}
			else
			{
				var dataModelValue = dataModel["_x"];
				dataModelValue = dataModelValue.AsList()["host"];
				dataModelValue = dataModelValue.AsList()["location"];
				str = dataModelValue.AsString();
			}

			var uriString = str;
			if (uriString is null)
			{
				return;
			}

			Uri uri = new(uriString, UriKind.RelativeOrAbsolute);
			foreach (var control1 in tabControl.TabPages.OfType<TabPage>())
			{
				if (control1?.Controls["sessionControl"] is SessionControl control && control.Source == uri && control.SessionId is null)
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

			SessionControl sessionControl = AddSessionControlTab(uri, sessionId.Value);
			sessionControl.AddLog(message, dataModel, dataModelAsText, dataAsText, exception);
			if (stop)
			{
				sessionControl.Stop();
			}
		}
	}
}
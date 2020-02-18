using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Xml;

namespace TSSArt.StateMachine
{
	public class ScxmlDirector : XmlDirector<ScxmlDirector>
	{
		private const string ScxmlNs       = "http://www.w3.org/2005/07/scxml";
		private const string TSSArtScxmlNs = "http://tssart.com/scxml";
		private const char   Space         = ' ';

		private static readonly char[]  SpaceSplitter = { Space };
		private static readonly Options TSSArtSpace   = new Options { Namespace = TSSArtScxmlNs };

		private readonly IBuilderFactory _factory;

		public ScxmlDirector(XmlReader xmlReader, IBuilderFactory factory) : base(xmlReader, new GlobalOptions { ElementDefaultNamespace = ScxmlNs }) => _factory = factory;

		private IIdentifier AsIdentifier()
		{
			try
			{
				return (Identifier) Current;
			}
			catch (ArgumentException ex)
			{
				throw GetXmlException(ex.Message);
			}
		}

		private IOutgoingEvent AsEvent()
		{
			try
			{
				return new Event(Current) { Target = Event.InternalTarget };
			}
			catch (ArgumentException ex)
			{
				throw GetXmlException(ex.Message);
			}
		}

		private ImmutableArray<IIdentifier> AsIdentifierList()
		{
			var val = Current;

			if (string.IsNullOrWhiteSpace(val))
			{
				throw GetXmlException(message: "List of identifiers cannot be empty");
			}

			try
			{
				if (val.IndexOf(Space) < 0)
				{
					return ImmutableArray.Create<IIdentifier>((Identifier) val);
				}

				var identifiers = val.Split(SpaceSplitter, StringSplitOptions.RemoveEmptyEntries);

				var builder = ImmutableArray.CreateBuilder<IIdentifier>(identifiers.Length);

				foreach (var identifier in identifiers)
				{
					builder.Add((Identifier) identifier);
				}

				return builder.MoveToImmutable();
			}
			catch (ArgumentException ex)
			{
				throw GetXmlException(ex.Message);
			}
		}

		private ImmutableArray<IEventDescriptor> AsEventDescriptorList()
		{
			var val = Current;

			if (string.IsNullOrEmpty(val))
			{
				throw GetXmlException(message: "List of events cannot be empty");
			}

			try
			{
				if (val.IndexOf(Space) < 0)
				{
					return ImmutableArray.Create<IEventDescriptor>((EventDescriptor) val);
				}

				var eventDescriptors = val.Split(SpaceSplitter, StringSplitOptions.RemoveEmptyEntries);

				var builder = ImmutableArray.CreateBuilder<IEventDescriptor>(eventDescriptors.Length);

				foreach (var identifier in eventDescriptors)
				{
					builder.Add((EventDescriptor) identifier);
				}

				return builder.MoveToImmutable();
			}
			catch (ArgumentException ex)
			{
				throw GetXmlException(ex.Message);
			}
		}

		private IConditionExpression AsConditionalExpression() => new ConditionExpression { Expression = Current };

		private ILocationExpression AsLocationExpression() => new LocationExpression { Expression = Current };

		private ImmutableArray<ILocationExpression> AsLocationExpressionList()
		{
			var expression = Current;

			if (string.IsNullOrEmpty(expression))
			{
				throw GetXmlException(message: "List of locations cannot be empty");
			}

			if (expression.IndexOf(Space) < 0)
			{
				return ImmutableArray.Create<ILocationExpression>(new LocationExpression { Expression = expression });
			}

			var locationExpressions = expression.Split(SpaceSplitter, StringSplitOptions.RemoveEmptyEntries);

			var builder = ImmutableArray.CreateBuilder<ILocationExpression>(locationExpressions.Length);

			foreach (var locationExpression in locationExpressions)
			{
				builder.Add(new LocationExpression { Expression = locationExpression });
			}

			return builder.MoveToImmutable();
		}

		private IValueExpression AsValueExpression() => new ValueExpression { Expression = Current };

		private IScriptExpression AsScriptExpression() => new ScriptExpression { Expression = Current };

		private IExternalScriptExpression AsExternalScriptExpression() => new ExternalScriptExpression { Uri = new Uri(Current, UriKind.RelativeOrAbsolute) };

		private IExternalDataExpression AsExternalDataExpression() => new ExternalDataExpression { Uri = new Uri(Current, UriKind.RelativeOrAbsolute) };

		private Uri AsUri() => new Uri(Current, UriKind.RelativeOrAbsolute);

		private T AsEnum<T>() where T : struct
		{
			var val = Current;

			if (!Enum.TryParse(val, ignoreCase: true, out T result) || val.ToLowerInvariant() != val)
			{
				throw GetXmlException($"Value cannot be parsed for type {typeof(T).Name}");
			}

			return result;
		}

		private int AsMilliseconds()
		{
			var val = Current;

			if (val == null || val == "0")
			{
				return 0;
			}

			if (val.EndsWith(value: "ms", StringComparison.Ordinal))
			{
				return int.Parse(val.Substring(startIndex: 0, val.Length - 2), NumberFormatInfo.InvariantInfo);
			}

			if (val.EndsWith(value: "s", StringComparison.Ordinal))
			{
				return int.Parse(val.Substring(startIndex: 0, val.Length - 1), NumberFormatInfo.InvariantInfo) * 1000;
			}

			throw GetXmlException(message: "Delay parsing error. Format is ###0(ms|s).");
		}

		public IStateMachine ConstructStateMachine() => ReadStateMachine();

		private void CheckScxmlVersion(string version)
		{
			if (version == "1.0")
			{
				return;
			}

			throw GetXmlException(message: "Unsupported SCXML version.");
		}

		private IStateMachine ReadStateMachine() =>
				Populate(_factory.CreateStateMachineBuilder(),
						 pb => pb
							   .ValidateElementName(name: "scxml")
							   .RequiredAttribute(name: "version", (dr, b) => dr.CheckScxmlVersion(dr.Current))
							   .OptionalAttribute(name: "initial", (dr, b) => b.SetInitial(dr.AsIdentifierList()))
							   .OptionalAttribute(name: "datamodel", (dr, b) => b.SetDataModelType(dr.Current))
							   .OptionalAttribute(name: "binding", (dr, b) => b.SetBindingType(dr.AsEnum<BindingType>()))
							   .OptionalAttribute(name: "name", (dr, b) => b.SetName(dr.Current))
							   .MultipleElements(name: "state", (dr, b) => b.AddState(dr.ReadState()))
							   .MultipleElements(name: "parallel", (dr, b) => b.AddParallel(dr.ReadParallel()))
							   .MultipleElements(name: "final", (dr, b) => b.AddFinal(dr.ReadFinal()))
							   .OptionalElement(name: "datamodel", (dr, b) => b.SetDataModel(dr.ReadDataModel()))
							   .OptionalElement(name: "script", (dr, b) => b.SetScript(dr.ReadScript()))
							   .OptionalAttribute(TSSArtSpace, name: "synchronous", (dr, b) => (b as IStateMachineOptionsBuilder)?.SetSynchronousEventProcessing(XmlConvert.ToBoolean(dr.Current)))
							   .OptionalAttribute(TSSArtSpace, name: "queueSize", (dr, b) => (b as IStateMachineOptionsBuilder)?.SetExternalQueueSize(XmlConvert.ToInt32(dr.Current)))
							   .OptionalAttribute(TSSArtSpace, name: "persistence",
												  (dr, b) => (b as IStateMachineOptionsBuilder)?.SetPersistenceLevel((PersistenceLevel) Enum.Parse(typeof(PersistenceLevel), dr.Current)))).Build();

		private IState ReadState() =>
				Populate(_factory.CreateStateBuilder(),
						 pb => pb
							   .OptionalAttribute(name: "id", (dr, b) => b.SetId(dr.AsIdentifier()))
							   .OptionalAttribute(name: "initial", (dr, b) => b.SetInitial(dr.AsIdentifierList()))
							   .MultipleElements(name: "state", (dr, b) => b.AddState(dr.ReadState()))
							   .MultipleElements(name: "parallel", (dr, b) => b.AddParallel(dr.ReadParallel()))
							   .MultipleElements(name: "final", (dr, b) => b.AddFinal(dr.ReadFinal()))
							   .MultipleElements(name: "history", (dr, b) => b.AddHistory(dr.ReadHistory()))
							   .MultipleElements(name: "invoke", (dr, b) => b.AddInvoke(dr.ReadInvoke()))
							   .MultipleElements(name: "transition", (dr, b) => b.AddTransition(dr.ReadTransition()))
							   .MultipleElements(name: "onentry", (dr, b) => b.AddOnEntry(dr.ReadOnEntry()))
							   .MultipleElements(name: "onexit", (dr, b) => b.AddOnExit(dr.ReadOnExit()))
							   .OptionalElement(name: "initial", (dr, b) => b.SetInitial(dr.ReadInitial()))
							   .OptionalElement(name: "datamodel", (dr, b) => b.SetDataModel(dr.ReadDataModel()))).Build();

		private IParallel ReadParallel() =>
				Populate(_factory.CreateParallelBuilder(),
						 pb => pb
							   .OptionalAttribute(name: "id", (dr, b) => b.SetId(dr.AsIdentifier()))
							   .MultipleElements(name: "state", (dr, b) => b.AddState(dr.ReadState()))
							   .MultipleElements(name: "parallel", (dr, b) => b.AddParallel(dr.ReadParallel()))
							   .MultipleElements(name: "history", (dr, b) => b.AddHistory(dr.ReadHistory()))
							   .MultipleElements(name: "invoke", (dr, b) => b.AddInvoke(dr.ReadInvoke()))
							   .MultipleElements(name: "transition", (dr, b) => b.AddTransition(dr.ReadTransition()))
							   .MultipleElements(name: "onentry", (dr, b) => b.AddOnEntry(dr.ReadOnEntry()))
							   .MultipleElements(name: "onexit", (dr, b) => b.AddOnExit(dr.ReadOnExit()))
							   .OptionalElement(name: "datamodel", (dr, b) => b.SetDataModel(dr.ReadDataModel()))).Build();

		private IFinal ReadFinal() =>
				Populate(_factory.CreateFinalBuilder(),
						 pb => pb
							   .OptionalAttribute(name: "id", (dr, b) => b.SetId(dr.AsIdentifier()))
							   .MultipleElements(name: "onentry", (dr, b) => b.AddOnEntry(dr.ReadOnEntry()))
							   .MultipleElements(name: "onexit", (dr, b) => b.AddOnExit(dr.ReadOnExit()))
							   .OptionalElement(name: "donedata", (dr, b) => b.SetDoneData(dr.ReadDoneData()))).Build();

		private IInitial ReadInitial() =>
				Populate(_factory.CreateInitialBuilder(),
						 pb => pb
								 .SingleElement(name: "transition", (dr, b) => b.SetTransition(dr.ReadTransition()))).Build();

		private IHistory ReadHistory() =>
				Populate(_factory.CreateHistoryBuilder(),
						 pb => pb
							   .OptionalAttribute(name: "id", (dr, b) => b.SetId(dr.AsIdentifier()))
							   .OptionalAttribute(name: "type", (dr, b) => b.SetType(dr.AsEnum<HistoryType>()))
							   .SingleElement(name: "transition", (dr, b) => b.SetTransition(dr.ReadTransition()))).Build();

		private ITransition ReadTransition() =>
				Populate(_factory.CreateTransitionBuilder(),
						 pb => pb
							   .OptionalAttribute(name: "event", (dr, b) => b.SetEvent(dr.AsEventDescriptorList()))
							   .OptionalAttribute(name: "cond", (dr, b) => b.SetCondition(dr.AsConditionalExpression()))
							   .OptionalAttribute(name: "target", (dr, b) => b.SetTarget(dr.AsIdentifierList()))
							   .OptionalAttribute(name: "type", (dr, b) => b.SetType(dr.AsEnum<TransitionType>()))
							   .MultipleElements(name: "assign", (dr, b) => b.AddAction(dr.ReadAssign()))
							   .MultipleElements(name: "foreach", (dr, b) => b.AddAction(dr.ReadForeach()))
							   .MultipleElements(name: "if", (dr, b) => b.AddAction(dr.ReadIf()))
							   .MultipleElements(name: "log", (dr, b) => b.AddAction(dr.ReadLog()))
							   .MultipleElements(name: "raise", (dr, b) => b.AddAction(dr.ReadRaise()))
							   .MultipleElements(name: "send", (dr, b) => b.AddAction(dr.ReadSend()))
							   .MultipleElements(name: "cancel", (dr, b) => b.AddAction(dr.ReadCancel()))
							   .MultipleElements(name: "script", (dr, b) => b.AddAction(dr.ReadScript()))
							   .UnknownElement((dr, b) => b.AddAction(dr.ReadCustomAction()))).Build();

		private ILog ReadLog() =>
				Populate(_factory.CreateLogBuilder(),
						 pb => pb
							   .OptionalAttribute(name: "label", (dr, b) => b.SetLabel(dr.Current))
							   .OptionalAttribute(name: "expr", (dr, b) => b.SetExpression(dr.AsValueExpression()))).Build();

		private ISend ReadSend() =>
				Populate(_factory.CreateSendBuilder(),
						 pb => pb
							   .OptionalAttribute(name: "event", (dr, b) => b.SetEvent(dr.Current))
							   .OptionalAttribute(name: "eventexpr", (dr, b) => b.SetEventExpression(dr.AsValueExpression()))
							   .OptionalAttribute(name: "target", (dr, b) => b.SetTarget(dr.AsUri()))
							   .OptionalAttribute(name: "targetexpr", (dr, b) => b.SetTargetExpression(dr.AsValueExpression()))
							   .OptionalAttribute(name: "type", (dr, b) => b.SetType(dr.AsUri()))
							   .OptionalAttribute(name: "typeexpr", (dr, b) => b.SetTypeExpression(dr.AsValueExpression()))
							   .OptionalAttribute(name: "id", (dr, b) => b.SetId(dr.Current))
							   .OptionalAttribute(name: "idlocation", (dr, b) => b.SetIdLocation(dr.AsLocationExpression()))
							   .OptionalAttribute(name: "delay", (dr, b) => b.SetDelay(dr.AsMilliseconds()))
							   .OptionalAttribute(name: "delayexpr", (dr, b) => b.SetDelayExpression(dr.AsValueExpression()))
							   .OptionalAttribute(name: "namelist", (dr, b) => b.SetNameList(dr.AsLocationExpressionList()))
							   .MultipleElements(name: "param", (dr, b) => b.AddParameter(dr.ReadParam()))
							   .OptionalElement(name: "content", (dr, b) => b.SetContent(dr.ReadContent()))).Build();

		private IParam ReadParam() =>
				Populate(_factory.CreateParamBuilder(),
						 pb => pb
							   .RequiredAttribute(name: "name", (dr, b) => b.SetName(dr.Current))
							   .OptionalAttribute(name: "expr", (dr, b) => b.SetExpression(dr.AsValueExpression()))
							   .OptionalAttribute(name: "location", (dr, b) => b.SetLocation(dr.AsLocationExpression()))).Build();

		private IContent ReadContent() =>
				Populate(_factory.CreateContentBuilder(),
						 pb => pb
							   .OptionalAttribute(name: "expr", (dr, b) => b.SetExpression(dr.AsValueExpression()))
							   .RawContent((dr, b) => b.SetBody(dr.Current))).Build();

		private IOnEntry ReadOnEntry() =>
				Populate(_factory.CreateOnEntryBuilder(),
						 pb => pb
							   .MultipleElements(name: "assign", (dr, b) => b.AddAction(dr.ReadAssign()))
							   .MultipleElements(name: "foreach", (dr, b) => b.AddAction(dr.ReadForeach()))
							   .MultipleElements(name: "if", (dr, b) => b.AddAction(dr.ReadIf()))
							   .MultipleElements(name: "log", (dr, b) => b.AddAction(dr.ReadLog()))
							   .MultipleElements(name: "raise", (dr, b) => b.AddAction(dr.ReadRaise()))
							   .MultipleElements(name: "send", (dr, b) => b.AddAction(dr.ReadSend()))
							   .MultipleElements(name: "cancel", (dr, b) => b.AddAction(dr.ReadCancel()))
							   .MultipleElements(name: "script", (dr, b) => b.AddAction(dr.ReadScript()))
							   .UnknownElement((dr, b) => b.AddAction(dr.ReadCustomAction()))).Build();

		private IOnExit ReadOnExit() =>
				Populate(_factory.CreateOnExitBuilder(),
						 pb => pb
							   .MultipleElements(name: "assign", (dr, b) => b.AddAction(dr.ReadAssign()))
							   .MultipleElements(name: "foreach", (dr, b) => b.AddAction(dr.ReadForeach()))
							   .MultipleElements(name: "if", (dr, b) => b.AddAction(dr.ReadIf()))
							   .MultipleElements(name: "log", (dr, b) => b.AddAction(dr.ReadLog()))
							   .MultipleElements(name: "raise", (dr, b) => b.AddAction(dr.ReadRaise()))
							   .MultipleElements(name: "send", (dr, b) => b.AddAction(dr.ReadSend()))
							   .MultipleElements(name: "cancel", (dr, b) => b.AddAction(dr.ReadCancel()))
							   .MultipleElements(name: "script", (dr, b) => b.AddAction(dr.ReadScript()))
							   .UnknownElement((dr, b) => b.AddAction(dr.ReadCustomAction()))).Build();

		private IInvoke ReadInvoke() =>
				Populate(_factory.CreateInvokeBuilder(),
						 pb => pb
							   .OptionalAttribute(name: "type", (dr, b) => b.SetType(dr.AsUri()))
							   .OptionalAttribute(name: "typeexpr", (dr, b) => b.SetTypeExpression(dr.AsValueExpression()))
							   .OptionalAttribute(name: "src", (dr, b) => b.SetSource(dr.AsUri()))
							   .OptionalAttribute(name: "srcexpr", (dr, b) => b.SetSourceExpression(dr.AsValueExpression()))
							   .OptionalAttribute(name: "id", (dr, b) => b.SetId(dr.Current))
							   .OptionalAttribute(name: "idlocation", (dr, b) => b.SetIdLocation(dr.AsLocationExpression()))
							   .OptionalAttribute(name: "namelist", (dr, b) => b.SetNameList(dr.AsLocationExpressionList()))
							   .OptionalAttribute(name: "autoforward", (dr, b) => b.SetAutoForward(XmlConvert.ToBoolean(dr.Current)))
							   .MultipleElements(name: "param", (dr, b) => b.AddParam(dr.ReadParam()))
							   .OptionalElement(name: "finalize", (dr, b) => b.SetFinalize(dr.ReadFinalize()))
							   .OptionalElement(name: "content", (dr, b) => b.SetContent(dr.ReadContent()))).Build();

		private IFinalize ReadFinalize() =>
				Populate(_factory.CreateFinalizeBuilder(),
						 pb => pb
							   .MultipleElements(name: "assign", (dr, b) => b.AddAction(dr.ReadAssign()))
							   .MultipleElements(name: "foreach", (dr, b) => b.AddAction(dr.ReadForeach()))
							   .MultipleElements(name: "if", (dr, b) => b.AddAction(dr.ReadIf()))
							   .MultipleElements(name: "log", (dr, b) => b.AddAction(dr.ReadLog()))
							   .MultipleElements(name: "cancel", (dr, b) => b.AddAction(dr.ReadCancel()))
							   .MultipleElements(name: "script", (dr, b) => b.AddAction(dr.ReadScript()))
							   .UnknownElement((dr, b) => b.AddAction(dr.ReadCustomAction()))).Build();

		private ICustomAction ReadCustomAction()
		{
			var builder = _factory.CreateCustomActionBuilder();
			builder.SetXml(ReadOuterXml());
			return builder.Build();
		}

		private IScript ReadScript() =>
				Populate(_factory.CreateScriptBuilder(),
						 pb => pb
							   .OptionalAttribute(name: "src", (dr, b) => b.SetSource(dr.AsExternalScriptExpression()))
							   .RawContent((dr, b) => b.SetBody(dr.AsScriptExpression()))).Build();

		private IDataModel ReadDataModel() =>
				Populate(_factory.CreateDataModelBuilder(),
						 pb => pb.MultipleElements(name: "data", (dr, b) => b.AddData(dr.ReadData()))).Build();

		private IData ReadData() =>
				Populate(_factory.CreateDataBuilder(),
						 pb => pb
							   .RequiredAttribute(name: "id", (dr, b) => b.SetId(dr.Current))
							   .OptionalAttribute(name: "src", (dr, b) => b.SetSource(dr.AsExternalDataExpression()))
							   .OptionalAttribute(name: "expr", (dr, b) => b.SetExpression(dr.AsValueExpression()))
							   .RawContent((dr, b) => b.SetInlineContent(dr.Current))).Build();

		private IDoneData ReadDoneData() =>
				Populate(_factory.CreateDoneDataBuilder(),
						 pb => pb
							   .OptionalElement(name: "content", (dr, b) => b.SetContent(dr.ReadContent()))
							   .MultipleElements(name: "param", (dr, b) => b.AddParameter(dr.ReadParam()))).Build();

		private IForEach ReadForeach() =>
				Populate(_factory.CreateForeachBuilder(),
						 pb => pb
							   .RequiredAttribute(name: "array", (dr, b) => b.SetArray(dr.AsValueExpression()))
							   .RequiredAttribute(name: "item", (dr, b) => b.SetItem(dr.AsLocationExpression()))
							   .OptionalAttribute(name: "index", (dr, b) => b.SetIndex(dr.AsLocationExpression()))
							   .MultipleElements(name: "assign", (dr, b) => b.AddAction(dr.ReadAssign()))
							   .MultipleElements(name: "foreach", (dr, b) => b.AddAction(dr.ReadForeach()))
							   .MultipleElements(name: "if", (dr, b) => b.AddAction(dr.ReadIf()))
							   .MultipleElements(name: "log", (dr, b) => b.AddAction(dr.ReadLog()))
							   .MultipleElements(name: "raise", (dr, b) => b.AddAction(dr.ReadRaise()))
							   .MultipleElements(name: "send", (dr, b) => b.AddAction(dr.ReadSend()))
							   .MultipleElements(name: "cancel", (dr, b) => b.AddAction(dr.ReadCancel()))
							   .MultipleElements(name: "script", (dr, b) => b.AddAction(dr.ReadScript()))
							   .UnknownElement((dr, b) => b.AddAction(dr.ReadCustomAction()))).Build();

		private IIf ReadIf() =>
				Populate(_factory.CreateIfBuilder(),
						 pb => pb
							   .RequiredAttribute(name: "cond", (dr, b) => b.SetCondition(dr.AsConditionalExpression()))
							   .MultipleElements(name: "elseif", (dr, b) => b.AddAction(dr.ReadElseIf()))
							   .MultipleElements(name: "else", (dr, b) => b.AddAction(dr.ReadElse()))
							   .MultipleElements(name: "assign", (dr, b) => b.AddAction(dr.ReadAssign()))
							   .MultipleElements(name: "foreach", (dr, b) => b.AddAction(dr.ReadForeach()))
							   .MultipleElements(name: "if", (dr, b) => b.AddAction(dr.ReadIf()))
							   .MultipleElements(name: "log", (dr, b) => b.AddAction(dr.ReadLog()))
							   .MultipleElements(name: "raise", (dr, b) => b.AddAction(dr.ReadRaise()))
							   .MultipleElements(name: "send", (dr, b) => b.AddAction(dr.ReadSend()))
							   .MultipleElements(name: "cancel", (dr, b) => b.AddAction(dr.ReadCancel()))
							   .MultipleElements(name: "script", (dr, b) => b.AddAction(dr.ReadScript()))
							   .UnknownElement((dr, b) => b.AddAction(dr.ReadCustomAction()))).Build();

		private IElse ReadElse() => Populate(_factory.CreateElseBuilder(), pb => { }).Build();

		private IElseIf ReadElseIf() =>
				Populate(_factory.CreateElseIfBuilder(),
						 pb => pb.RequiredAttribute(name: "cond", (dr, b) => b.SetCondition(dr.AsConditionalExpression()))).Build();

		private IRaise ReadRaise() =>
				Populate(_factory.CreateRaiseBuilder(),
						 pb => pb.RequiredAttribute(name: "event", (dr, b) => b.SetEvent(dr.AsEvent()))).Build();

		private IAssign ReadAssign() =>
				Populate(_factory.CreateAssignBuilder(),
						 pb => pb
							   .RequiredAttribute(name: "location", (dr, b) => b.SetLocation(dr.AsLocationExpression()))
							   .OptionalAttribute(name: "expr", (dr, b) => b.SetExpression(dr.AsValueExpression()))
							   .RawContent((dr, b) => b.SetInlineContent(dr.Current))).Build();

		private ICancel ReadCancel() =>
				Populate(_factory.CreateCancelBuilder(),
						 pb => pb
							   .OptionalAttribute(name: "sendid", (dr, b) => b.SetSendId(dr.Current))
							   .OptionalAttribute(name: "sendidexpr", (dr, b) => b.SetSendIdExpression(dr.AsValueExpression()))).Build();
	}
}
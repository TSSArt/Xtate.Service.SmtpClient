using System;
using System.Collections./**/Immutable;
using System.Globalization;
using System.Xml;

namespace TSSArt.StateMachine
{
	public class ScxmlDirector : XmlDirector<ScxmlDirector>
	{
		private const string ScxmlNs = "http://www.w3.org/2005/07/scxml";
		private const char   Space   = ' ';

		private static readonly char[] SpaceSplitter = { Space };

		private readonly IBuilderFactory _factory;

		public ScxmlDirector(XmlReader xmlReader, IBuilderFactory factory) : base(xmlReader, new GlobalOptions { ElementDefaultNamespace = ScxmlNs }) => _factory = factory;

		private Identifier ToIdentifier(string val)
		{
			try
			{
				return (Identifier) val;
			}
			catch (ArgumentException ex)
			{
				throw GetXmlException(ex.Message);
			}
		}

		private Event ToEvent(string val)
		{
			try
			{
				return new Event(val) { Target = Event.InternalTarget };
			}
			catch (ArgumentException ex)
			{
				throw GetXmlException(ex.Message);
			}
		}

		private IdentifierList ToIdentifierList(string val)
		{
			if (string.IsNullOrEmpty(val))
			{
				throw GetXmlException(message: "List of identifiers cannot be empty");
			}

			if (val.IndexOf(Space) < 0)
			{
				return IdentifierList.Create(new[] { ToIdentifier(val) });
			}

			var list = new List<IIdentifier>();

			foreach (var idRef in val.Split(SpaceSplitter, StringSplitOptions.RemoveEmptyEntries))
			{
				list.Add(ToIdentifier(idRef));
			}

			return IdentifierList.Create(list);
		}

		private EventDescriptor ToEventDescriptor(string val)
		{
			if (string.IsNullOrEmpty(val))
			{
				throw GetXmlException(message: "Event cannot be empty");
			}

			for (var i = 0; i < val.Length; i ++)
			{
				if (char.IsWhiteSpace(val, i))
				{
					throw GetXmlException(message: "Event cannot contains whitespace");
				}
			}

			return val;
		}

		private EventDescriptorList ToEventDescriptorList(string val)
		{
			if (string.IsNullOrEmpty(val))
			{
				throw GetXmlException(message: "List of events cannot be empty");
			}

			if (val.IndexOf(Space) < 0)
			{
				return EventDescriptorList.Create(new[] { ToEventDescriptor(val) });
			}

			var list = new List<IEventDescriptor>();

			foreach (var idRef in val.Split(SpaceSplitter, StringSplitOptions.RemoveEmptyEntries))
			{
				list.Add(ToEventDescriptor(idRef));
			}

			return EventDescriptorList.Create(list);
		}

		private ConditionExpression ToConditionalExpression(string expression) => new ConditionExpression { Expression = expression };

		private LocationExpression ToLocationExpression(string expression) => new LocationExpression { Expression = expression };

		private LocationExpressionList ToLocationExpressionList(string expression)
		{
			if (string.IsNullOrEmpty(expression))
			{
				throw GetXmlException(message: "List of locations cannot be empty");
			}

			if (expression.IndexOf(Space) < 0)
			{
				return LocationExpressionList.Create(new[] { (ILocationExpression) ToLocationExpression(expression) });
			}

			var list = new List<ILocationExpression>();

			foreach (var locationExpression in expression.Split(SpaceSplitter, StringSplitOptions.RemoveEmptyEntries))
			{
				list.Add(ToLocationExpression(locationExpression));
			}

			return LocationExpressionList.Create(list);
		}

		private ValueExpression ToValueExpression(string expression) => new ValueExpression { Expression = expression };

		private ScriptExpression ToScriptExpression(string expression) => new ScriptExpression { Expression = expression };

		private ExternalScriptExpression ToExternalScriptExpression(string uri) => new ExternalScriptExpression { Uri = ToUri(uri) };

		private ExternalDataExpression ToExternalDataExpression(string uri) => new ExternalDataExpression { Uri = ToUri(uri) };

		private Uri ToUri(string val) => new Uri(val, UriKind.RelativeOrAbsolute);

		private T ToEnum<T>(string val) where T : struct
		{
			if (!Enum.TryParse(val, ignoreCase: true, out T result) || val.ToLowerInvariant() != val)
			{
				throw GetXmlException($"Value cannot be parsed for type {typeof(T).Name}");
			}

			return result;
		}

		private int ToMilliseconds(string val)
		{
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
							   .OptionalAttribute(name: "initial", (dr, b) => b.SetInitial(dr.ToIdentifierList(dr.Current)))
							   .OptionalAttribute(name: "datamodel", (dr, b) => b.SetDataModelType(dr.Current))
							   .OptionalAttribute(name: "binding", (dr, b) => b.SetBindingType(ToEnum<BindingType>(dr.Current)))
							   .OptionalAttribute(name: "name", (dr, b) => b.SetName(dr.Current))
							   .MultipleElements(name: "state", (dr, b) => b.AddState(dr.ReadState()))
							   .MultipleElements(name: "parallel", (dr, b) => b.AddParallel(dr.ReadParallel()))
							   .MultipleElements(name: "final", (dr, b) => b.AddFinal(dr.ReadFinal()))
							   .OptionalElement(name: "datamodel", (dr, b) => b.SetDataModel(dr.ReadDataModel()))
							   .OptionalElement(name: "script", (dr, b) => b.SetScript(dr.ReadScript()))).Build();

		private IState ReadState() =>
				Populate(_factory.CreateStateBuilder(),
						 pb => pb
							   .OptionalAttribute(name: "id", (dr, b) => b.SetId(dr.ToIdentifier(dr.Current)))
							   .OptionalAttribute(name: "initial", (dr, b) => b.SetInitial(dr.ToIdentifierList(dr.Current)))
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
							   .OptionalAttribute(name: "id", (dr, b) => b.SetId(dr.ToIdentifier(dr.Current)))
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
							   .OptionalAttribute(name: "id", (dr, b) => b.SetId(dr.ToIdentifier(dr.Current)))
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
							   .OptionalAttribute(name: "id", (dr, b) => b.SetId(dr.ToIdentifier(dr.Current)))
							   .OptionalAttribute(name: "type", (dr, b) => b.SetType(dr.ToEnum<HistoryType>(dr.Current)))
							   .SingleElement(name: "transition", (dr, b) => b.SetTransition(dr.ReadTransition()))).Build();

		private ITransition ReadTransition() =>
				Populate(_factory.CreateTransitionBuilder(),
						 pb => pb
							   .OptionalAttribute(name: "event", (dr, b) => b.SetEvent(dr.ToEventDescriptorList(dr.Current)))
							   .OptionalAttribute(name: "cond", (dr, b) => b.SetCondition(ToConditionalExpression(dr.Current)))
							   .OptionalAttribute(name: "target", (dr, b) => b.SetTarget(dr.ToIdentifierList(dr.Current)))
							   .OptionalAttribute(name: "type", (dr, b) => b.SetType(dr.ToEnum<TransitionType>(dr.Current)))
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
							   .OptionalAttribute(name: "expr", (dr, b) => b.SetExpression(ToValueExpression(dr.Current)))).Build();

		private ISend ReadSend() =>
				Populate(_factory.CreateSendBuilder(),
						 pb => pb
							   .OptionalAttribute(name: "event", (dr, b) => b.SetEvent(dr.Current))
							   .OptionalAttribute(name: "eventexpr", (dr, b) => b.SetEventExpression(ToValueExpression(dr.Current)))
							   .OptionalAttribute(name: "target", (dr, b) => b.SetTarget(ToUri(dr.Current)))
							   .OptionalAttribute(name: "targetexpr", (dr, b) => b.SetTargetExpression(ToValueExpression(dr.Current)))
							   .OptionalAttribute(name: "type", (dr, b) => b.SetType(ToUri(dr.Current)))
							   .OptionalAttribute(name: "typeexpr", (dr, b) => b.SetTypeExpression(ToValueExpression(dr.Current)))
							   .OptionalAttribute(name: "id", (dr, b) => b.SetId(dr.Current))
							   .OptionalAttribute(name: "idlocation", (dr, b) => b.SetIdLocation(ToLocationExpression(dr.Current)))
							   .OptionalAttribute(name: "delay", (dr, b) => b.SetDelay(ToMilliseconds(dr.Current)))
							   .OptionalAttribute(name: "delayexpr", (dr, b) => b.SetDelayExpression(ToValueExpression(dr.Current)))
							   .OptionalAttribute(name: "namelist", (dr, b) => b.SetNameList(ToLocationExpressionList(dr.Current)))
							   .MultipleElements(name: "param", (dr, b) => b.AddParameter(dr.ReadParam()))
							   .OptionalElement(name: "content", (dr, b) => b.SetContent(dr.ReadContent()))).Build();

		private IParam ReadParam() =>
				Populate(_factory.CreateParamBuilder(),
						 pb => pb
							   .RequiredAttribute(name: "name", (dr, b) => b.SetName(dr.Current))
							   .OptionalAttribute(name: "expr", (dr, b) => b.SetExpression(ToValueExpression(dr.Current)))
							   .OptionalAttribute(name: "location", (dr, b) => b.SetLocation(ToLocationExpression(dr.Current)))).Build();

		private IContent ReadContent() =>
				Populate(_factory.CreateContentBuilder(),
						 pb => pb
							   .OptionalAttribute(name: "expr", (dr, b) => b.SetExpression(ToValueExpression(dr.Current)))
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
							   .OptionalAttribute(name: "type", (dr, b) => b.SetType(ToUri(dr.Current)))
							   .OptionalAttribute(name: "typeexpr", (dr, b) => b.SetTypeExpression(ToValueExpression(dr.Current)))
							   .OptionalAttribute(name: "src", (dr, b) => b.SetSource(ToUri(dr.Current)))
							   .OptionalAttribute(name: "srcexpr", (dr, b) => b.SetSourceExpression(ToValueExpression(dr.Current)))
							   .OptionalAttribute(name: "id", (dr, b) => b.SetId(dr.Current))
							   .OptionalAttribute(name: "idlocation", (dr, b) => b.SetIdLocation(ToLocationExpression(dr.Current)))
							   .OptionalAttribute(name: "namelist", (dr, b) => b.SetNameList(ToLocationExpressionList(dr.Current)))
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
							   .OptionalAttribute(name: "src", (dr, b) => b.SetSource(ToExternalScriptExpression(dr.Current)))
							   .RawContent((dr, b) => b.SetBody(ToScriptExpression(dr.Current)))).Build();

		private IDataModel ReadDataModel() =>
				Populate(_factory.CreateDataModelBuilder(),
						 pb => pb.MultipleElements(name: "data", (dr, b) => b.AddData(dr.ReadData()))).Build();

		private IData ReadData() =>
				Populate(_factory.CreateDataBuilder(),
						 pb => pb
							   .RequiredAttribute(name: "id", (dr, b) => b.SetId(dr.Current))
							   .OptionalAttribute(name: "src", (dr, b) => b.SetSource(ToExternalDataExpression(dr.Current)))
							   .OptionalAttribute(name: "expr", (dr, b) => b.SetExpression(ToValueExpression(dr.Current)))
							   .RawContent((dr, b) => b.SetInlineContent(dr.Current))).Build();

		private IDoneData ReadDoneData() =>
				Populate(_factory.CreateDoneDataBuilder(),
						 pb => pb
							   .OptionalElement(name: "content", (dr, b) => b.SetContent(dr.ReadContent()))
							   .MultipleElements(name: "param", (dr, b) => b.AddParameter(dr.ReadParam()))).Build();

		private IForEach ReadForeach() =>
				Populate(_factory.CreateForeachBuilder(),
						 pb => pb
							   .RequiredAttribute(name: "array", (dr, b) => b.SetArray(ToValueExpression(dr.Current)))
							   .RequiredAttribute(name: "item", (dr, b) => b.SetItem(ToLocationExpression(dr.Current)))
							   .OptionalAttribute(name: "index", (dr, b) => b.SetIndex(ToLocationExpression(dr.Current)))
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
							   .RequiredAttribute(name: "cond", (dr, b) => b.SetCondition(ToConditionalExpression(dr.Current)))
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
						 pb => pb.RequiredAttribute(name: "cond", (dr, b) => b.SetCondition(ToConditionalExpression(dr.Current)))).Build();

		private IRaise ReadRaise() =>
				Populate(_factory.CreateRaiseBuilder(),
						 pb => pb.RequiredAttribute(name: "event", (dr, b) => b.SetEvent(ToEvent(dr.Current)))).Build();

		private IAssign ReadAssign() =>
				Populate(_factory.CreateAssignBuilder(),
						 pb => pb
							   .RequiredAttribute(name: "location", (dr, b) => b.SetLocation(ToLocationExpression(dr.Current)))
							   .OptionalAttribute(name: "expr", (dr, b) => b.SetExpression(ToValueExpression(dr.Current)))
							   .RawContent((dr, b) => b.SetInlineContent(dr.Current))).Build();

		private ICancel ReadCancel() =>
				Populate(_factory.CreateCancelBuilder(),
						 pb => pb
							   .OptionalAttribute(name: "sendid", (dr, b) => b.SetSendId(dr.Current))
							   .OptionalAttribute(name: "sendidexpr", (dr, b) => b.SetSendIdExpression(ToValueExpression(dr.Current)))).Build();
	}
}
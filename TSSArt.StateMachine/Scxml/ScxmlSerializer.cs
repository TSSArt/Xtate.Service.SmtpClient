using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace TSSArt.StateMachine
{
	public class ScxmlSerializer : StateMachineVisitor
	{
		private XmlWriter _writer;

		public void Serialize(IStateMachine stateMachine, XmlWriter writer)
		{
			_writer = writer;

			Visit(ref stateMachine);
		}

		private string ToString(IOutgoingEvent @event) => EventName.ToName(@event.NameParts);

		private string ToString(IEnumerable<IIdentifier> list)
		{
			return string.Join(separator: " ", list.Select(id => id.ToString()));
		}

		private string ToString(IEnumerable<IEventDescriptor> list)
		{
			return string.Join(separator: " ", list.Select(id => id.ToString()));
		}

		private string ToString(IEnumerable<ILocationExpression> list)
		{
			return string.Join(separator: " ", list.Select(id => id.Expression));
		}

		protected override void Build(ref IStateMachine entity, ref StateMachine properties)
		{
			_writer.WriteStartElement(prefix: "", localName: "scxml", ns: "http://www.w3.org/2005/07/scxml");
			_writer.WriteAttributeString(localName: "version", value: "1.0");

			if (properties.DataModelType != null)
			{
				_writer.WriteAttributeString(localName: "datamodel", properties.DataModelType);
			}

			var target = properties.Initial?.Transition?.Target;
			if (target != null)
			{
				_writer.WriteAttributeString(localName: "initial", ToString(target));
			}

			properties.Initial = null;
			base.Build(ref entity, ref properties);

			_writer.WriteEndElement();
		}

		protected override void Visit(ref IInitial entity)
		{
			_writer.WriteStartElement("initial");

			base.Visit(ref entity);

			_writer.WriteEndElement();
		}

		protected override void Visit(ref IState entity)
		{
			if (entity == null) throw new ArgumentNullException(nameof(entity));

			_writer.WriteStartElement("state");

			if (entity.Id != null)
			{
				_writer.WriteAttributeString(localName: "id", entity.Id.ToString());
			}

			base.Visit(ref entity);

			_writer.WriteEndElement();
		}

		protected override void Visit(ref IParallel entity)
		{
			if (entity == null) throw new ArgumentNullException(nameof(entity));

			_writer.WriteStartElement("parallel");

			if (entity.Id != null)
			{
				_writer.WriteAttributeString(localName: "id", entity.Id.ToString());
			}

			base.Visit(ref entity);

			_writer.WriteEndElement();
		}

		protected override void Visit(ref IHistory entity)
		{
			if (entity == null) throw new ArgumentNullException(nameof(entity));

			_writer.WriteStartElement("history");

			if (entity.Id != null)
			{
				_writer.WriteAttributeString(localName: "id", entity.Id.ToString());
			}

			if (entity.Type != HistoryType.Shallow)
			{
				_writer.WriteAttributeString(localName: "type", value: "deep");
			}

			base.Visit(ref entity);

			_writer.WriteEndElement();
		}

		protected override void Visit(ref IFinal entity)
		{
			if (entity == null) throw new ArgumentNullException(nameof(entity));

			_writer.WriteStartElement("final");

			if (entity.Id != null)
			{
				_writer.WriteAttributeString(localName: "id", entity.Id.ToString());
			}

			base.Visit(ref entity);

			_writer.WriteEndElement();
		}

		protected override void Visit(ref ITransition entity)
		{
			if (entity == null) throw new ArgumentNullException(nameof(entity));

			_writer.WriteStartElement("transition");

			if (entity.Type != TransitionType.External)
			{
				_writer.WriteAttributeString(localName: "type", value: "internal");
			}

			if (entity.Event != null)
			{
				_writer.WriteAttributeString(localName: "event", ToString(entity.Event));
			}

			if (entity.Condition != null)
			{
				_writer.WriteAttributeString(localName: "cond", ((IConditionExpression) entity.Condition).Expression);
			}

			if (entity.Target != null)
			{
				_writer.WriteAttributeString(localName: "target", ToString(entity.Target));
			}

			base.Visit(ref entity);

			_writer.WriteEndElement();
		}

		protected override void Visit(ref ISend entity)
		{
			if (entity == null) throw new ArgumentNullException(nameof(entity));

			_writer.WriteStartElement("send");

			if (entity.Event != null)
			{
				_writer.WriteAttributeString(localName: "event", entity.Event);
			}

			if (entity.EventExpression != null)
			{
				_writer.WriteAttributeString(localName: "eventexpr", entity.EventExpression.Expression);
			}

			if (entity.Target != null)
			{
				_writer.WriteAttributeString(localName: "target", entity.Target.ToString());
			}

			if (entity.TargetExpression != null)
			{
				_writer.WriteAttributeString(localName: "targetexpr", entity.TargetExpression.Expression);
			}

			if (entity.Type != null)
			{
				_writer.WriteAttributeString(localName: "type", entity.Type.ToString());
			}

			if (entity.TypeExpression != null)
			{
				_writer.WriteAttributeString(localName: "typeexpr", entity.TypeExpression.Expression);
			}

			if (entity.Id != null)
			{
				_writer.WriteAttributeString(localName: "id", entity.Id);
			}

			if (entity.IdLocation != null)
			{
				_writer.WriteAttributeString(localName: "idlocation", entity.IdLocation.Expression);
			}

			if (entity.DelayMs != null)
			{
				_writer.WriteAttributeString(localName: "delay", entity.DelayMs.Value + "ms");
			}

			if (entity.DelayExpression != null)
			{
				_writer.WriteAttributeString(localName: "delayexpr", entity.DelayExpression.Expression);
			}

			if (entity.NameList != null)
			{
				_writer.WriteAttributeString(localName: "namelist", ToString(entity.NameList));
			}

			base.Visit(ref entity);

			_writer.WriteEndElement();
		}

		protected override void Visit(ref IScript entity)
		{
			if (entity == null) throw new ArgumentNullException(nameof(entity));

			_writer.WriteStartElement("script");

			if (entity.Source != null)
			{
				_writer.WriteAttributeString(localName: "src", entity.Source.Uri.ToString());
			}

			if (entity.Content != null)
			{
				_writer.WriteRaw(entity.Content.Expression);
			}

			_writer.WriteEndElement();
		}

		protected override void Visit(ref IRaise entity)
		{
			if (entity == null) throw new ArgumentNullException(nameof(entity));

			_writer.WriteStartElement("raise");

			_writer.WriteAttributeString(localName: "event", ToString(entity.Event));

			_writer.WriteEndElement();
		}

		protected override void Visit(ref ILog entity)
		{
			if (entity == null) throw new ArgumentNullException(nameof(entity));

			_writer.WriteStartElement("log");

			if (entity.Label != null)
			{
				_writer.WriteAttributeString(localName: "label", entity.Label);
			}

			if (entity.Expression != null)
			{
				_writer.WriteAttributeString(localName: "expr", entity.Expression.Expression);
			}

			_writer.WriteEndElement();
		}

		protected override void Visit(ref IIf entity)
		{
			if (entity == null) throw new ArgumentNullException(nameof(entity));

			_writer.WriteStartElement("if");

			_writer.WriteAttributeString(localName: "cond", entity.Condition.Expression);

			base.Visit(ref entity);

			_writer.WriteEndElement();
		}

		protected override void Visit(ref IForEach entity)
		{
			if (entity == null) throw new ArgumentNullException(nameof(entity));

			_writer.WriteStartElement("foreach");

			_writer.WriteAttributeString(localName: "array", entity.Array.Expression);

			_writer.WriteAttributeString(localName: "item", entity.Item.Expression);

			if (entity.Index != null)
			{
				_writer.WriteAttributeString(localName: "index", entity.Index.Expression);
			}

			base.Visit(ref entity);

			_writer.WriteEndElement();
		}

		protected override void Visit(ref IElseIf entity)
		{
			if (entity == null) throw new ArgumentNullException(nameof(entity));

			_writer.WriteStartElement("elseif");

			_writer.WriteAttributeString(localName: "cond", entity.Condition.Expression);

			_writer.WriteEndElement();
		}

		protected override void Visit(ref IElse entity)
		{
			_writer.WriteStartElement("else");
			_writer.WriteEndElement();
		}

		protected override void Visit(ref ICancel entity)
		{
			if (entity == null) throw new ArgumentNullException(nameof(entity));

			_writer.WriteStartElement("cancel");

			if (entity.SendId != null)
			{
				_writer.WriteAttributeString(localName: "index", entity.SendId);
			}

			if (entity.SendIdExpression != null)
			{
				_writer.WriteAttributeString(localName: "index", entity.SendIdExpression.Expression);
			}

			_writer.WriteEndElement();
		}

		protected override void Visit(ref IAssign entity)
		{
			if (entity == null) throw new ArgumentNullException(nameof(entity));

			_writer.WriteStartElement("assign");

			_writer.WriteAttributeString(localName: "location", entity.Location.Expression);

			if (entity.Expression != null)
			{
				_writer.WriteAttributeString(localName: "expr", entity.Expression.Expression);
			}

			if (entity.InlineContent != null)
			{
				_writer.WriteRaw(entity.InlineContent);
			}

			_writer.WriteEndElement();
		}

		protected override void Visit(ref IDataModel entity)
		{
			_writer.WriteStartElement("datamodel");

			base.Visit(ref entity);

			_writer.WriteEndElement();
		}

		protected override void Visit(ref IOnExit entity)
		{
			_writer.WriteStartElement("onexit");

			base.Visit(ref entity);

			_writer.WriteEndElement();
		}

		protected override void Visit(ref IOnEntry entity)
		{
			_writer.WriteStartElement("onentry");

			base.Visit(ref entity);

			_writer.WriteEndElement();
		}

		protected override void Visit(ref IData entity)
		{
			if (entity == null) throw new ArgumentNullException(nameof(entity));

			_writer.WriteStartElement("data");

			_writer.WriteAttributeString(localName: "id", entity.Id);

			if (entity.Source != null)
			{
				_writer.WriteAttributeString(localName: "src", entity.Source.Uri.ToString());
			}

			if (entity.Expression != null)
			{
				_writer.WriteAttributeString(localName: "expr", entity.Expression.Expression);
			}

			if (entity.InlineContent != null)
			{
				_writer.WriteRaw(entity.InlineContent);
			}

			_writer.WriteEndElement();
		}

		protected override void Visit(ref IInvoke entity)
		{
			if (entity == null) throw new ArgumentNullException(nameof(entity));

			_writer.WriteStartElement("invoke");

			if (entity.Type != null)
			{
				_writer.WriteAttributeString(localName: "type", entity.Type.ToString());
			}

			if (entity.TypeExpression != null)
			{
				_writer.WriteAttributeString(localName: "typeexpr", entity.TypeExpression.Expression);
			}

			if (entity.Source != null)
			{
				_writer.WriteAttributeString(localName: "src", entity.Source.ToString());
			}

			if (entity.SourceExpression != null)
			{
				_writer.WriteAttributeString(localName: "eventexpr", entity.SourceExpression.Expression);
			}

			if (entity.Id != null)
			{
				_writer.WriteAttributeString(localName: "id", entity.Id);
			}

			if (entity.IdLocation != null)
			{
				_writer.WriteAttributeString(localName: "idlocation", entity.IdLocation.Expression);
			}

			if (entity.NameList != null)
			{
				_writer.WriteAttributeString(localName: "namelist", ToString(entity.NameList));
			}

			if (entity.AutoForward)
			{
				_writer.WriteAttributeString(localName: "autoforward", value: "true");
			}

			base.Visit(ref entity);

			_writer.WriteEndElement();
		}

		protected override void Visit(ref IFinalize entity)
		{
			_writer.WriteStartElement("finalize");

			base.Visit(ref entity);

			_writer.WriteEndElement();
		}

		protected override void Visit(ref IParam entity)
		{
			if (entity == null) throw new ArgumentNullException(nameof(entity));

			_writer.WriteStartElement("param");

			_writer.WriteAttributeString(localName: "name", entity.Name);

			if (entity.Expression != null)
			{
				_writer.WriteAttributeString(localName: "expr", entity.Expression.Expression);
			}

			if (entity.Location != null)
			{
				_writer.WriteAttributeString(localName: "location", entity.Location.Expression);
			}

			_writer.WriteEndElement();
		}

		protected override void Visit(ref IContent entity)
		{
			if (entity == null) throw new ArgumentNullException(nameof(entity));

			_writer.WriteStartElement("content");

			if (entity.Expression != null)
			{
				_writer.WriteAttributeString(localName: "expr", entity.Expression.Expression);
			}

			if (entity.Value != null)
			{
				_writer.WriteRaw(entity.Value);
			}

			_writer.WriteEndElement();
		}

		protected override void Visit(ref IDoneData entity)
		{
			_writer.WriteStartElement("donedata");

			base.Visit(ref entity);

			_writer.WriteEndElement();
		}
	}
}
using System;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public class SendBuilder : ISendBuilder
	{
		private readonly List<IParam>                       _parameters = new List<IParam>();
		private          IContent                           _content;
		private          IValueExpression                   _delayExpression;
		private          int?                               _delayMs;
		private          string                             _event;
		private          IValueExpression                   _eventExpression;
		private          string                             _id;
		private          ILocationExpression                _idLocation;
		private          ImmutableArray<ILocationExpression> _nameList;
		private          Uri                                _target;
		private          IValueExpression                   _targetExpression;
		private          Uri                                _type;
		private          IValueExpression                   _typeExpression;

		public ISend Build()
		{
			if (_event != null && _eventExpression != null || _event != null && _content != null || _eventExpression != null && _content != null)
			{
				throw new InvalidOperationException(message: "Event, EventExpression and Content can't be used at the same time in Send element");
			}

			if (_target != null && _targetExpression != null)
			{
				throw new InvalidOperationException(message: "Target and TargetExpression can't be used at the same time in Send element");
			}

			if (_type != null && _typeExpression != null)
			{
				throw new InvalidOperationException(message: "Type and TypeExpression can't be used at the same time in Send element");
			}

			if (_id != null && _idLocation != null)
			{
				throw new InvalidOperationException(message: "Id and IdLocation can't be used at the same time in Send element");
			}

			if (_delayMs != null && _delayExpression != null)
			{
				throw new InvalidOperationException(message: "Event and EventExpression can't be used at the same time in Send element");
			}

			if (_nameList != null && _content != null)
			{
				throw new InvalidOperationException(message: "NameList and Content can't be used at the same time in Send element");
			}

			if (_parameters.Count > 0 && _content != null)
			{
				throw new InvalidOperationException(message: "Parameters and Content can't be used at the same time in Send element");
			}

			if (_event == null && _eventExpression == null && _content == null)
			{
				throw new InvalidOperationException(message: "Must be present Event or EventExpression or Content in Send element");
			}

			return new Send
				   {
						   Event = _event, EventExpression = _eventExpression, Target = _target, TargetExpression = _targetExpression,
						   Type = _type, TypeExpression = _typeExpression, Id = _id, IdLocation = _idLocation, DelayMs = _delayMs,
						   DelayExpression = _delayExpression, NameList = _nameList, Parameters = ParamList.Create(_parameters), Content = _content
				   };
		}

		public void SetEvent(string @event)
		{
			if (string.IsNullOrEmpty(@event)) throw new ArgumentException(message: "Value cannot be null or empty.", nameof(@event));

			_event = @event;
		}

		public void SetEventExpression(IValueExpression eventExpression) => _eventExpression = eventExpression ?? throw new ArgumentNullException(nameof(eventExpression));

		public void SetTarget(Uri target) => _target = target ?? throw new ArgumentNullException(nameof(target));

		public void SetTargetExpression(IValueExpression targetExpression) => _targetExpression = targetExpression ?? throw new ArgumentNullException(nameof(targetExpression));

		public void SetType(Uri type) => _type = type ?? throw new ArgumentNullException(nameof(type));

		public void SetTypeExpression(IValueExpression typeExpression) => _typeExpression = typeExpression ?? throw new ArgumentNullException(nameof(typeExpression));

		public void SetId(string id)
		{
			if (string.IsNullOrEmpty(id)) throw new ArgumentException(message: "Value cannot be null or empty.", nameof(id));

			_id = id;
		}

		public void SetIdLocation(ILocationExpression idLocation) => _idLocation = idLocation ?? throw new ArgumentNullException(nameof(idLocation));

		public void SetDelay(int delay)
		{
			if (delay < 0) throw new ArgumentOutOfRangeException(nameof(delay));

			_delayMs = delay;
		}

		public void SetDelayExpression(IValueExpression delayExpression) => _delayExpression = delayExpression ?? throw new ArgumentNullException(nameof(delayExpression));

		public void SetNameList(ImmutableArray<ILocationExpression> nameList) => _nameList = LocationExpressionList.Create(nameList ?? throw new ArgumentNullException(nameof(nameList)));

		public void AddParameter(IParam param)
		{
			if (param == null) throw new ArgumentNullException(nameof(param));

			_parameters.Add(param);
		}

		public void SetContent(IContent content) => _content = content ?? throw new ArgumentNullException(nameof(content));
	}
}
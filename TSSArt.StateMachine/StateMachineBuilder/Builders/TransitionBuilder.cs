using System;
using System.Collections./**/Immutable;

namespace TSSArt.StateMachine
{
	public class TransitionBuilder : ITransitionBuilder
	{
		private readonly List<IExecutableEntity>         _actions = new List<IExecutableEntity>();
		private          IExecutableEntity               _condition;
		private          /**/ImmutableArray<IEventDescriptor> _eventDescriptors;
		private          /**/ImmutableArray<IIdentifier>      _target;
		private          TransitionType                  _type;

		public ITransition Build()
		{
			if ((_eventDescriptors?.Count ?? 0) == 0 && _condition == null && (_target?.Count ?? 0) == 0)
			{
				throw new InvalidOperationException(message: "Must be present at least Event or Condition or Target in Transition element");
			}

			return new Transition
				   {
						   Event = _eventDescriptors,
						   Condition = _condition,
						   Target = _target,
						   Type = _type,
						   Action = ExecutableEntityList.Create(_actions)
				   };
		}

		public void SetCondition(IExecutableEntity condition) => _condition = condition;

		public void SetTarget(/**/ImmutableArray<IIdentifier> target) => _target = target;

		public void SetType(TransitionType type) => _type = type;

		public void SetEvent(/**/ImmutableArray<IEventDescriptor> eventDescriptors) => _eventDescriptors = eventDescriptors;

		public void AddAction(IExecutableEntity action) => _actions.Add(action);
	}
}
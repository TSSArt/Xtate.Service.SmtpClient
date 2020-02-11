using System;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public class TransitionBuilder : ITransitionBuilder
	{
		private ImmutableArray<IExecutableEntity>.Builder _actions;
		private IExecutableEntity                         _condition;
		private ImmutableArray<IEventDescriptor>          _eventDescriptors;
		private ImmutableArray<IIdentifier>               _target;
		private TransitionType                            _type;

		public ITransition Build()
		{
			if (_eventDescriptors.IsDefaultOrEmpty && _condition == null && _target.IsDefaultOrEmpty)
			{
				throw new InvalidOperationException(message: "Must be present at least Event or Condition or Target in Transition element");
			}

			return new Transition
				   {
						   Event = _eventDescriptors, Condition = _condition, Target = _target,
						   Type = _type, Action = _actions?.ToImmutable() ?? default
				   };
		}

		public void SetCondition(IExecutableEntity condition) => _condition = condition;

		public void SetTarget(ImmutableArray<IIdentifier> target) => _target = target;

		public void SetType(TransitionType type) => _type = type;

		public void SetEvent(ImmutableArray<IEventDescriptor> eventDescriptors) => _eventDescriptors = eventDescriptors;

		public void AddAction(IExecutableEntity action) => (_actions ??= ImmutableArray.CreateBuilder<IExecutableEntity>()).Add(action);
	}
}
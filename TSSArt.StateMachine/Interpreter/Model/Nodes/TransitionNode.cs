using System;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public class TransitionNode : ITransition, IStoreSupport, IAncestorProvider, IDocumentId, IDebugEntityId
	{
		private readonly LinkedListNode<int> _documentIdNode;
		private readonly Transition          _transition;

		public TransitionNode(LinkedListNode<int> documentIdNode, in Transition transition, ImmutableArray<StateEntityNode> target = null)
		{
			_transition = transition;
			_documentIdNode = documentIdNode;
			TargetState = target;
			ActionEvaluators = transition.Action.AsListOf<IExecEvaluator>() ?? Array.Empty<IExecEvaluator>();
			ConditionEvaluator = transition.Condition.As<IBooleanEvaluator>();
		}

		public ImmutableArray<StateEntityNode> TargetState        { get; private set; }
		public StateEntityNode                Source             { get; private set; }
		public ImmutableArray<IExecEvaluator>  ActionEvaluators   { get; }
		public IBooleanEvaluator              ConditionEvaluator { get; }

		object IAncestorProvider.Ancestor => _transition.Ancestor;

		public FormattableString EntityId => $"(#{DocumentId})";

		public int DocumentId => _documentIdNode.Value;

		void IStoreSupport.Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.TransitionNode);
			bucket.Add(Key.DocumentId, DocumentId);
			bucket.AddEntityList(Key.Event, Event);
			bucket.AddEntity(Key.Condition, Condition);
			bucket.AddEntityList(Key.Target, Target);
			bucket.Add(Key.TransitionType, Type);
			bucket.AddEntityList(Key.Action, Action);
		}

		public ImmutableArray<IEventDescriptor> Event => _transition.Event;

		public IExecutableEntity Condition => _transition.Condition;

		public ImmutableArray<IIdentifier> Target => _transition.Target;

		public TransitionType Type => _transition.Type;

		public ImmutableArray<IExecutableEntity> Action => _transition.Action;

		public void MapTarget(Dictionary<IIdentifier, StateEntityNode> idMap)
		{
			TargetState = StateEntityNodeList.Create(Target, id => idMap[id]);
		}

		public void SetSource(StateEntityNode source) => Source = source;

		private class StateEntityNodeList : ValidatedArrayBuilder<>
		{
			protected override Options GetOptions() => Options.NullIfEmpty;
		}
	}
}
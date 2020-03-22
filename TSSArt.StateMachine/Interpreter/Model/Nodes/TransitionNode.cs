using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	internal sealed class TransitionNode : ITransition, IStoreSupport, IAncestorProvider, IDocumentId, IDebugEntityId
	{
		private readonly LinkedListNode<int> _documentIdNode;
		private readonly TransitionEntity    _transition;

		public TransitionNode(LinkedListNode<int> documentIdNode, in TransitionEntity transition, ImmutableArray<StateEntityNode> target = default)
		{
			_transition = transition;
			_documentIdNode = documentIdNode;
			TargetState = target;
			ActionEvaluators = transition.Action.AsArrayOf<IExecutableEntity, IExecEvaluator>(true);
			ConditionEvaluator = transition.Condition?.As<IBooleanEvaluator>();
			Source = null!;
		}

		public ImmutableArray<StateEntityNode> TargetState        { get; private set; }
		public StateEntityNode                 Source             { get; private set; }
		public ImmutableArray<IExecEvaluator>  ActionEvaluators   { get; }
		public IBooleanEvaluator?              ConditionEvaluator { get; }

		object? IAncestorProvider.Ancestor => _transition.Ancestor;

		public FormattableString EntityId => @$"(#{DocumentId})";

		public int DocumentId => _documentIdNode.Value;

		void IStoreSupport.Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.TransitionNode);
			bucket.Add(Key.DocumentId, DocumentId);
			bucket.AddEntityList(Key.Event, EventDescriptors);
			bucket.AddEntity(Key.Condition, Condition);
			bucket.AddEntityList(Key.Target, Target);
			bucket.Add(Key.TransitionType, Type);
			bucket.AddEntityList(Key.Action, Action);
		}

		public ImmutableArray<IEventDescriptor> EventDescriptors => _transition.EventDescriptors;

		public IExecutableEntity? Condition => _transition.Condition;

		public ImmutableArray<IIdentifier> Target => _transition.Target;

		public TransitionType Type => _transition.Type;

		public ImmutableArray<IExecutableEntity> Action => _transition.Action;

		public void MapTarget(Dictionary<IIdentifier, StateEntityNode> idMap) => TargetState = ImmutableArray.CreateRange(Target, id => idMap[id]);

		public void SetSource(StateEntityNode source) => Source = source;
	}
}
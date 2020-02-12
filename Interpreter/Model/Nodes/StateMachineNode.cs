using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	internal sealed class StateMachineNode : StateEntityNode, IStateMachine, IAncestorProvider, IDebugEntityId
	{
		private readonly StateMachine _stateMachine;

		public StateMachineNode(LinkedListNode<int> documentIdNode, in StateMachine stateMachine) : base(documentIdNode, GetChildNodes(stateMachine.Initial, stateMachine.States))
		{
			_stateMachine = stateMachine;
			Initial = stateMachine.Initial.As<InitialNode>();
			ScriptEvaluator = stateMachine.Script.As<ScriptNode>();
			DataModel = stateMachine.DataModel.As<DataModelNode>();
		}

		public override DataModelNode DataModel { get; }

		public InitialNode    Initial         { get; }
		public IExecEvaluator ScriptEvaluator { get; }

		object IAncestorProvider.Ancestor => _stateMachine.Ancestor;

		FormattableString IDebugEntityId.EntityId => $"{Name}(#{DocumentId})";

		public BindingType                         Binding       => _stateMachine.Binding;
		public string                              Name          => _stateMachine.Name;
		public string                              DataModelType => _stateMachine.DataModelType;
		public IExecutableEntity                   Script        => _stateMachine.Script;
		public ImmutableDictionary<string, string> Options       => _stateMachine.Options;

		IDataModel IStateMachine.                  DataModel => _stateMachine.DataModel;
		IInitial IStateMachine.                    Initial   => _stateMachine.Initial;
		ImmutableArray<IStateEntity> IStateMachine.States    => _stateMachine.States;

		protected override void Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.StateMachineNode);
			bucket.Add(Key.DocumentId, DocumentId);
			bucket.Add(Key.Name, Name);
			bucket.Add(Key.DataModelType, DataModelType);
			bucket.Add(Key.Binding, Binding);
			bucket.AddEntity(Key.Script, Script);
			bucket.AddEntity(Key.DataModel, DataModel);
			bucket.AddEntity(Key.Initial, Initial);
			bucket.AddEntityList(Key.States, _stateMachine.States);
			bucket.AddDictionary(Key.Options, _stateMachine.Options);
		}
	}
}
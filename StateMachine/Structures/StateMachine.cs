using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public struct StateMachine : IStateMachine, IEntity<StateMachine, IStateMachine>, IAncestorProvider, IDebugEntityId
	{
		public string                      DataModelType;
		public IInitial                    Initial;
		public string                      Name;
		public BindingType                 Binding;
		public IReadOnlyList<IStateEntity> States;
		public IDataModel                  DataModel;
		public IExecutableEntity           Script;

		string IStateMachine.Name => Name;

		string IStateMachine.DataModelType => DataModelType;

		BindingType IStateMachine.Binding => Binding;

		IInitial IStateMachine.Initial => Initial;

		IReadOnlyList<IStateEntity> IStateMachine.States => States;

		IDataModel IStateMachine.DataModel => DataModel;

		IExecutableEntity IStateMachine.Script => Script;

		void IEntity<StateMachine, IStateMachine>.Init(IStateMachine source)
		{
			Ancestor = source;
			Name = source.Name;
			Initial = source.Initial;
			DataModelType = source.DataModelType;
			Binding = source.Binding;
			States = source.States;
			DataModel = source.DataModel;
			Script = source.Script;
		}

		bool IEntity<StateMachine, IStateMachine>.RefEquals(in StateMachine other) =>
				Binding == other.Binding &&
				ReferenceEquals(Name, other.Name) &&
				ReferenceEquals(DataModel, other.DataModel) &&
				ReferenceEquals(DataModelType, other.DataModelType) &&
				ReferenceEquals(Initial, other.Initial) &&
				ReferenceEquals(States, other.States) &&
				ReferenceEquals(Script, other.Script);

		internal object Ancestor;

		object IAncestorProvider.Ancestor => Ancestor;

		FormattableString IDebugEntityId.EntityId => $"{Name}";
	}
}
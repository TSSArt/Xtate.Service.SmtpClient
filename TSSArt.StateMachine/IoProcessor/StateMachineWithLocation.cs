using System;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	internal class StateMachineWithLocation : IStateMachine, IAncestorProvider, ILocation
	{
		private readonly IStateMachine _stateMachine;

		public StateMachineWithLocation(IStateMachine stateMachine, Uri location)
		{
			_stateMachine = stateMachine;
			Uri = location;
		}

	#region Interface IAncestorProvider

		public object? Ancestor => _stateMachine;

	#endregion

	#region Interface ILocation

		public Uri Uri { get; }

	#endregion

	#region Interface IStateMachine

		public string?                      Name          => _stateMachine.Name;
		public string?                      DataModelType => _stateMachine.DataModelType;
		public BindingType                  Binding       => _stateMachine.Binding;
		public IInitial?                    Initial       => _stateMachine.Initial;
		public ImmutableArray<IStateEntity> States        => _stateMachine.States;
		public IDataModel?                  DataModel     => _stateMachine.DataModel;
		public IExecutableEntity?           Script        => _stateMachine.Script;

	#endregion
	}
}
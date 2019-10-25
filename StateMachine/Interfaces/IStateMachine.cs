using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public interface IStateMachine : IEntity
	{
		string                      Name          { get; }
		string                      DataModelType { get; }
		BindingType                 Binding       { get; }
		IInitial                    Initial       { get; }
		IReadOnlyList<IStateEntity> States        { get; }
		IDataModel                  DataModel     { get; }
		IExecutableEntity           Script        { get; }
	}
}
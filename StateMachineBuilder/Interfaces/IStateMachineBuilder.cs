using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public interface IStateMachineBuilder
	{
		IStateMachine Build();

		void SetInitial(IReadOnlyList<IIdentifier> initial);
		void AddState(IState state);
		void AddParallel(IParallel parallel);
		void AddFinal(IFinal final);
		void SetDataModel(IDataModel dataModel);
		void SetScript(IScript script);
		void SetName(string name);
		void SetDataModelType(string dataModelType);
		void SetBindingType(BindingType bindingType);
	}
}
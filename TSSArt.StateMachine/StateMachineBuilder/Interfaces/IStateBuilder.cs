using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public interface IStateBuilder
	{
		IState Build();

		void SetId(IIdentifier id);
		void SetInitial(IReadOnlyList<IIdentifier> initial);
		void AddState(IState state);
		void AddParallel(IParallel parallel);
		void AddFinal(IFinal final);
		void SetInitial(IInitial initial);
		void AddHistory(IHistory history);
		void AddTransition(ITransition transition);
		void AddInvoke(IInvoke action);
		void AddOnEntry(IOnEntry action);
		void AddOnExit(IOnExit action);
		void SetDataModel(IDataModel dataModel);
	}
}
using System.Collections.Immutable;

namespace Xtate
{
	public interface IDataModelHandler
	{
		void Process(ref IExecutableEntity executableEntity);

		void Process(ref IDataModel dataModel);

		void Process(ref IDoneData doneData);

		void Process(ref IInvoke invoke);

		void ExecutionContextCreated(IExecutionContext executionContext, out ImmutableDictionary<string, string> dataModelVars);
	}
}
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public class InterpreterModel
	{
		public InterpreterModel(StateMachineNode root, int maxConfigurationLength, Dictionary<int, IEntity> entityMap, List<DataModelNode> dataModelList)
		{
			Root = root;
			MaxConfigurationLength = maxConfigurationLength;
			EntityMap = entityMap;
			DataModelList = dataModelList;
		}

		public StateMachineNode Root { get; }

		public int MaxConfigurationLength { get; }

		public Dictionary<int, IEntity> EntityMap { get; }

		public List<DataModelNode> DataModelList { get; }
	}
}
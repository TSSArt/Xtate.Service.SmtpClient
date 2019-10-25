using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public class FinalizeNode : IFinalize, IStoreSupport, IAncestorProvider
	{
		private readonly Finalize _finalize;

		public FinalizeNode(in Finalize finalize)
		{
			_finalize = finalize;
			ActionEvaluators = finalize.Action.AsListOf<IExecEvaluator>();
		}

		public IReadOnlyList<IExecEvaluator> ActionEvaluators { get; }

		object IAncestorProvider.Ancestor => _finalize.Ancestor;

		public IReadOnlyList<IExecutableEntity> Action => _finalize.Action;

		void IStoreSupport.Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.FinalizeNode);
			bucket.AddEntityList(Key.Action, Action);
		}
	}
}
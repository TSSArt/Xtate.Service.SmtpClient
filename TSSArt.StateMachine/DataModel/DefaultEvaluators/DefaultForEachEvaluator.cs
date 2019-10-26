using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public class DefaultForEachEvaluator : IForEach, IExecEvaluator, IAncestorProvider
	{
		private readonly ForEach _forEach;

		public DefaultForEachEvaluator(in ForEach forEach)
		{
			_forEach = forEach;

			ArrayEvaluator = forEach.Array.As<IArrayEvaluator>();
			ItemEvaluator = forEach.Item.As<ILocationEvaluator>();
			IndexEvaluator = forEach.Index.As<ILocationEvaluator>();
			ActionEvaluatorList = forEach.Action.AsListOf<IExecEvaluator>();
		}

		public IArrayEvaluator               ArrayEvaluator      { get; }
		public ILocationEvaluator            ItemEvaluator       { get; }
		public ILocationEvaluator            IndexEvaluator      { get; }
		public IReadOnlyList<IExecEvaluator> ActionEvaluatorList { get; }

		object IAncestorProvider.Ancestor => _forEach.Ancestor;

		public virtual async ValueTask Execute(IExecutionContext executionContext, CancellationToken token)
		{
			var array = await ArrayEvaluator.EvaluateArray(executionContext, token).ConfigureAwait(false);

			for (var i = 0; i < array.Length; i ++)
			{
				var instance = array[i];

				ItemEvaluator.SetValue(instance, executionContext);

				IndexEvaluator?.SetValue(new DefaultObject(i), executionContext);

				foreach (var execEvaluator in ActionEvaluatorList)
				{
					await execEvaluator.Execute(executionContext, token).ConfigureAwait(false);
				}
			}
		}

		public IValueExpression                 Array  => _forEach.Array;
		public ILocationExpression              Item   => _forEach.Item;
		public ILocationExpression              Index  => _forEach.Index;
		public IReadOnlyList<IExecutableEntity> Action => _forEach.Action;
	}
}
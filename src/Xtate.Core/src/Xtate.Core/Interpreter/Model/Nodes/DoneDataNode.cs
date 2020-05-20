using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Xtate
{
	internal sealed class DoneDataNode : IDoneData, IStoreSupport, IAncestorProvider
	{
		private readonly IValueEvaluator?             _contentBodyEvaluator;
		private readonly IObjectEvaluator?            _contentExpressionEvaluator;
		private readonly DoneDataEntity               _doneData;
		private readonly ImmutableArray<DefaultParam> _parameterList;

		public DoneDataNode(in DoneDataEntity doneData)
		{
			_doneData = doneData;
			_contentExpressionEvaluator = doneData.Content?.Expression?.As<IObjectEvaluator>();
			_contentBodyEvaluator = doneData.Content?.Body?.As<IValueEvaluator>();
			_parameterList = doneData.Parameters.AsArrayOf<IParam, DefaultParam>();
		}

	#region Interface IAncestorProvider

		object? IAncestorProvider.Ancestor => _doneData.Ancestor;

	#endregion

	#region Interface IDoneData

		public IContent? Content => _doneData.Content;

		public ImmutableArray<IParam> Parameters => _doneData.Parameters;

	#endregion

	#region Interface IStoreSupport

		void IStoreSupport.Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.DoneDataNode);
			bucket.AddEntity(Key.Content, Content);
			bucket.AddEntityList(Key.Parameters, Parameters);
		}

	#endregion

		public ValueTask<DataModelValue> Evaluate(IExecutionContext executionContext, CancellationToken token) =>
				DataConverter.GetData(_contentBodyEvaluator, _contentExpressionEvaluator, nameEvaluatorList: default, _parameterList, executionContext, token);
	}
}
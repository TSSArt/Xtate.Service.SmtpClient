using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public class DoneDataNode : IDoneData, IStoreSupport, IAncestorProvider
	{
		private readonly DoneData                    _doneData;
		private readonly IObjectEvaluator            _contentExpressionEvaluator;
		private readonly IReadOnlyList<DefaultParam> _parameterList;

		public DoneDataNode(in DoneData doneData)
		{
			_doneData = doneData;
			_contentExpressionEvaluator = doneData.Content?.Expression.As<IObjectEvaluator>();
			_parameterList = doneData.Parameters.AsListOf<DefaultParam>();
		}

		object IAncestorProvider.Ancestor => _doneData.Ancestor;

		public IContent Content => _doneData.Content;

		public IReadOnlyList<IParam> Parameters => _doneData.Parameters;

		void IStoreSupport.Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.DoneDataNode);
			bucket.AddEntity(Key.Content, Content);
			bucket.AddEntityList(Key.Parameters, Parameters);
		}

		public Task<DataModelValue> Evaluate(IExecutionContext executionContext, CancellationToken token)
		{
			return Converter.GetData(_doneData.Content?.Value, _contentExpressionEvaluator, nameEvaluatorList: null, _parameterList, executionContext, token);
		}
	}
}
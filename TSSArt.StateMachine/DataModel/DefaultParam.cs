using System;

namespace TSSArt.StateMachine
{
	public class DefaultParam : IParam, IAncestorProvider, IDebugEntityId
	{
		private readonly Param _param;

		public DefaultParam(in Param param)
		{
			_param = param;
			ExpressionEvaluator = param.Expression.As<IObjectEvaluator>();
			LocationEvaluator = param.Location.As<ILocationEvaluator>();
		}

		public IObjectEvaluator   ExpressionEvaluator { get; }
		public ILocationEvaluator LocationEvaluator   { get; }

		object IAncestorProvider.Ancestor => _param.Ancestor;

		FormattableString IDebugEntityId.EntityId => $"{_param.Name}";

		public string              Name       => _param.Name;
		IValueExpression IParam.   Expression => _param.Expression;
		ILocationExpression IParam.Location   => _param.Location;
	}
}
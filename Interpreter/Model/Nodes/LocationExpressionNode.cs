namespace TSSArt.StateMachine
{
	internal sealed class LocationExpressionNode : ILocationExpression, IStoreSupport, IAncestorProvider
	{
		private readonly LocationExpression _locationExpression;

		public LocationExpressionNode(in LocationExpression locationExpression)
		{
			Infrastructure.Assert(locationExpression.Expression != null);

			_locationExpression = locationExpression;
		}

		object? IAncestorProvider.Ancestor => _locationExpression.Ancestor;

		public string Expression => _locationExpression.Expression!;

		void IStoreSupport.Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.LocationExpressionNode);
			bucket.Add(Key.Expression, Expression);
		}
	}
}
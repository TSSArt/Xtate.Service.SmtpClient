using System;

namespace TSSArt.StateMachine
{
	internal sealed class IdentifierNode : IIdentifier, IStoreSupport, IAncestorProvider, IDebugEntityId
	{
		private readonly IIdentifier _identifier;

		public IdentifierNode(IIdentifier id) => _identifier = id ?? throw new ArgumentNullException(nameof(id));

	#region Interface IAncestorProvider

		object? IAncestorProvider.Ancestor => _identifier;

	#endregion

	#region Interface IDebugEntityId

		FormattableString IDebugEntityId.EntityId => @$"{_identifier}";

	#endregion

	#region Interface IStoreSupport

		void IStoreSupport.Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.IdentifierNode);
			bucket.Add(Key.Id, _identifier.As<string>());
		}

	#endregion
	}
}
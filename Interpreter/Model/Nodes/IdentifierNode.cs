using System;

namespace TSSArt.StateMachine
{
	public class IdentifierNode : IIdentifier, IStoreSupport, IAncestorProvider, IDebugEntityId
	{
		public IdentifierNode(IIdentifier id) => Identifier = id ?? throw new ArgumentNullException(nameof(id));
		public IIdentifier Identifier { get; }

		object IAncestorProvider.Ancestor => Identifier;

		FormattableString IDebugEntityId.EntityId => $"{Identifier}";

		void IStoreSupport.Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.IdentifierNode);
			bucket.Add(Key.Id, Identifier?.ToString());
		}
	}
}
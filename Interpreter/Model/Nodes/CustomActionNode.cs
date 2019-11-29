using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public class CustomActionNode : ExecutableEntityNode, ICustomAction, IAncestorProvider
	{
		private readonly CustomAction _entity;

		public CustomActionNode(LinkedListNode<int> documentIdNode, in CustomAction entity) : base(documentIdNode, (ICustomAction) entity.Ancestor) => _entity = entity;

		object IAncestorProvider.Ancestor => _entity.Ancestor;

		public string Xml => _entity.Xml;

		protected override void Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.CustomActionNode);
			bucket.Add(Key.DocumentId, DocumentId);
			bucket.Add(Key.Content, Xml);
		}
	}
}
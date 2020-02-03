using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public class ScriptNode : ExecutableEntityNode, IScript, IAncestorProvider
	{
		private readonly Script _entity;

		public ScriptNode(LinkedListNode<int> documentIdNode, in Script entity) : base(documentIdNode, (IScript) entity.Ancestor) => _entity = entity;

		object IAncestorProvider.Ancestor => _entity.Ancestor;

		public IScriptExpression Content => _entity.Content;

		public IExternalScriptExpression Source => _entity.Source;

		protected override void Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.ScriptNode);
			bucket.Add(Key.DocumentId, DocumentId);
			bucket.AddEntity(Key.Content, Content);
			bucket.AddEntity(Key.Source, Source);
		}
	}
}
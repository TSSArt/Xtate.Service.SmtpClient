using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	internal sealed class SendNode : ExecutableEntityNode, ISend, IAncestorProvider, IDebugEntityId
	{
		private readonly Send _entity;

		public SendNode(LinkedListNode<int> documentIdNode, in Send entity) : base(documentIdNode, (ISend) entity.Ancestor) => _entity = entity;

		object IAncestorProvider.Ancestor => _entity.Ancestor;

		FormattableString IDebugEntityId.EntityId => $"{Id}(#{DocumentId})";

		public string                              Event            => _entity.Event;
		public IValueExpression                    EventExpression  => _entity.EventExpression;
		public Uri                                 Target           => _entity.Target;
		public IValueExpression                    TargetExpression => _entity.TargetExpression;
		public Uri                                 Type             => _entity.Type;
		public IValueExpression                    TypeExpression   => _entity.TypeExpression;
		public string                              Id               => _entity.Id;
		public ILocationExpression                 IdLocation       => _entity.IdLocation;
		public int?                                DelayMs          => _entity.DelayMs;
		public IValueExpression                    DelayExpression  => _entity.DelayExpression;
		public ImmutableArray<ILocationExpression> NameList         => _entity.NameList;
		public ImmutableArray<IParam>              Parameters       => _entity.Parameters;
		public IContent                            Content          => _entity.Content;

		protected override void Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.SendNode);
			bucket.Add(Key.DocumentId, DocumentId);
			bucket.Add(Key.Id, Id);
			bucket.Add(Key.Type, Type);
			bucket.Add(Key.Event, Event);
			bucket.Add(Key.Target, Target);
			bucket.Add(Key.DelayMs, DelayMs ?? 0);
			bucket.AddEntity(Key.TypeExpression, TypeExpression);
			bucket.AddEntity(Key.EventExpression, EventExpression);
			bucket.AddEntity(Key.TargetExpression, TargetExpression);
			bucket.AddEntity(Key.DelayExpression, DelayExpression);
			bucket.AddEntity(Key.IdLocation, IdLocation);
			bucket.AddEntityList(Key.NameList, NameList);
			bucket.AddEntityList(Key.Parameters, Parameters);
			bucket.AddEntity(Key.Content, Content);
		}
	}
}
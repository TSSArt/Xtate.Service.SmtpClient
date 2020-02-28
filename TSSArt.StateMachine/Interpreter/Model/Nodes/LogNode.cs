﻿using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	internal sealed class LogNode : ExecutableEntityNode, ILog, IAncestorProvider, IDebugEntityId
	{
		private readonly Log _entity;

		public LogNode(LinkedListNode<int> documentIdNode, in Log entity) : base(documentIdNode, (ILog) entity.Ancestor) => _entity = entity;

		object IAncestorProvider.Ancestor => _entity.Ancestor;

		FormattableString IDebugEntityId.EntityId => $"(#{DocumentId})";

		public string Label => _entity.Label;

		public IValueExpression Expression => _entity.Expression;

		protected override void Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.LogNode);
			bucket.Add(Key.DocumentId, DocumentId);
			bucket.Add(Key.Label, Label);
			bucket.AddEntity(Key.Expression, Expression);
		}
	}
}
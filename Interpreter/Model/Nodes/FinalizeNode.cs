﻿using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	internal sealed class FinalizeNode : IFinalize, IStoreSupport, IAncestorProvider
	{
		private readonly FinalizeEntity _finalize;

		public FinalizeNode(in FinalizeEntity finalize)
		{
			_finalize = finalize;
			ActionEvaluators = finalize.Action.AsArrayOf<IExecutableEntity, IExecEvaluator>();
		}

		public ImmutableArray<IExecEvaluator> ActionEvaluators { get; }

		object? IAncestorProvider.Ancestor => _finalize.Ancestor;

		public ImmutableArray<IExecutableEntity> Action => _finalize.Action;

		void IStoreSupport.Store(Bucket bucket)
		{
			bucket.Add(Key.TypeInfo, TypeInfo.FinalizeNode);
			bucket.AddEntityList(Key.Action, Action);
		}
	}
}
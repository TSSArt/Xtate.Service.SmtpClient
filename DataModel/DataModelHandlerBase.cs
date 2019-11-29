using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public abstract class DataModelHandlerBase : StateMachineVisitor, IDataModelHandler
	{
		protected DataModelHandlerBase() : base(trackPath: true) => ValidationOnly = true;

		protected DataModelHandlerBase(StateMachineVisitor masterVisitor) : base(masterVisitor) { }

		protected bool ValidationOnly { get; }

		public virtual void ExecutionContextCreated(IExecutionContext executionContext, IDictionary<string, string> dataModelVars) { }

		void IDataModelHandler.Process(ref IExecutableEntity executableEntity)
		{
			Visit(ref executableEntity);
		}

		void IDataModelHandler.Process(ref IDataModel dataModel)
		{
			Visit(ref dataModel);
		}

		void IDataModelHandler.Process(ref IDoneData doneData)
		{
			Visit(ref doneData);
		}

		void IDataModelHandler.Process(ref IInvoke invoke)
		{
			Visit(ref invoke);
		}

		protected override void Visit(ref IValueExpression expression)
		{
			base.Visit(ref expression);

			if (!(expression is IValueEvaluator))
			{
				AddErrorMessage(message: "'Value expression' does not implement IValueEvaluator.");
			}
		}

		protected override void Visit(ref ILocationExpression expression)
		{
			base.Visit(ref expression);

			if (!(expression is ILocationEvaluator))
			{
				AddErrorMessage(message: "'Location expression' does not implement ILocationEvaluator.");
			}
		}

		protected override void Build(ref ILog log, ref Log logProperties)
		{
			base.Build(ref log, ref logProperties);

			if (ValidationOnly)
			{
				return;
			}

			log = new DefaultLogEvaluator(logProperties);
		}

		protected override void Build(ref ISend send, ref Send sendProperties)
		{
			base.Build(ref send, ref sendProperties);

			if (ValidationOnly)
			{
				return;
			}

			send = new DefaultSendEvaluator(sendProperties);
		}

		protected override void Build(ref IParam param, ref Param paramProperties)
		{
			base.Build(ref param, ref paramProperties);

			if (ValidationOnly)
			{
				return;
			}

			param = new DefaultParam(paramProperties);
		}

		protected override void Build(ref ICancel cancel, ref Cancel cancelProperties)
		{
			base.Build(ref cancel, ref cancelProperties);

			if (ValidationOnly)
			{
				return;
			}

			cancel = new DefaultCancelEvaluator(cancelProperties);
		}

		protected override void Build(ref IIf @if, ref If ifProperties)
		{
			base.Build(ref @if, ref ifProperties);

			var condition = true;

			foreach (var op in @if.Action)
			{
				switch (op)
				{
					case IElseIf _:
						if (!condition)
						{
							AddErrorMessage(message: "<elseif> can not follow <else>.");
						}

						break;

					case IElse _:
						if (!condition)
						{
							AddErrorMessage(message: "<else> can be used only once.");
						}

						condition = false;
						break;
				}
			}

			if (ValidationOnly)
			{
				return;
			}

			@if = new DefaultIfEvaluator(ifProperties);
		}

		protected override void Build(ref IRaise raise, ref Raise raiseProperties)
		{
			base.Build(ref raise, ref raiseProperties);

			if (ValidationOnly)
			{
				return;
			}

			raise = new DefaultRaiseEvaluator(raiseProperties);
		}

		protected override void Build(ref IForEach forEach, ref ForEach forEachProperties)
		{
			base.Build(ref forEach, ref forEachProperties);

			if (ValidationOnly)
			{
				return;
			}

			forEach = new DefaultForEachEvaluator(forEachProperties);
		}

		protected override void Build(ref IAssign assign, ref Assign assignProperties)
		{
			base.Build(ref assign, ref assignProperties);

			if (ValidationOnly)
			{
				return;
			}

			assign = new DefaultAssignEvaluator(assignProperties);
		}

		protected override void Build(ref IScript script, ref Script scriptProperties)
		{
			base.Build(ref script, ref scriptProperties);

			if (ValidationOnly)
			{
				return;
			}

			script = new DefaultScriptEvaluator(scriptProperties);
		}

		protected override void Build(ref ICustomAction customAction, ref CustomAction customActionProperties)
		{
			base.Build(ref customAction, ref customActionProperties);

			if (ValidationOnly)
			{
				return;
			}

			customAction = new DefaultCustomActionEvaluator(customActionProperties);
		}

		protected override void Build(ref IInvoke invoke, ref Invoke invokeProperties)
		{
			base.Build(ref invoke, ref invokeProperties);

			if (ValidationOnly)
			{
				return;
			}

			invoke = new DefaultInvokeEvaluator(invokeProperties);
		}
	}
}
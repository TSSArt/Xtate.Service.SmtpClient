﻿using System;
using System.Collections.Generic;
using System.Globalization;
using Jint;
using Jint.Native;
using Jint.Parser.Ast;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Environments;
using Jint.Runtime.Interop;
using Xtate.Annotations;

namespace Xtate.DataModel.EcmaScript
{
	[PublicAPI]
	internal class EcmaScriptEngine
	{
		public static readonly object Key = new object();

		private readonly Engine          _jintEngine;
		private readonly HashSet<string> _variableSet = new HashSet<string>();

		public EcmaScriptEngine(IExecutionContext executionContext)
		{
			_jintEngine = new Engine(options => options.Culture(CultureInfo.InvariantCulture).LimitRecursion(1024).Strict());

			var global = _jintEngine.Global;
			var inFunction = new DelegateWrapper(_jintEngine, new Func<string, bool>(state => executionContext.InState((Identifier) state)));
			global.FastAddProperty(EcmaScriptHelper.InFunctionName, inFunction, writable: false, enumerable: false, configurable: false);
		}

		public static EcmaScriptEngine GetEngine(IExecutionContext executionContext)
		{
			var engine = (EcmaScriptEngine?) executionContext.RuntimeItems[Key];

			Infrastructure.Assert(engine != null);

			engine.SyncRootVariables(executionContext.DataModel);

			return engine;
		}

		private void SyncRootVariables(DataModelObject dataModel)
		{
			var global = _jintEngine.Global;
			List<string>? toRemove = null;
			foreach (var name in _variableSet)
			{
				if (!dataModel.TryGet(name, caseInsensitive: false, out _))
				{
					toRemove ??= new List<string>();
					toRemove.Add(name);
				}
			}

			if (toRemove != null)
			{
				foreach (var property in toRemove)
				{
					_variableSet.Remove(property);
					global.RemoveOwnProperty(property);
				}
			}

			foreach (var keyValue in dataModel.KeyValues)
			{
				if (!string.IsNullOrEmpty(keyValue.Key) && global.GetOwnProperty(keyValue.Key) == PropertyDescriptor.Undefined)
				{
					var descriptor = EcmaScriptHelper.CreatePropertyAccessor(_jintEngine, dataModel, keyValue.Key);
					global.FastSetProperty(keyValue.Key, descriptor);
					_variableSet.Add(keyValue.Key);
				}
			}
		}

		public void EnterExecutionContext()
		{
			var lexicalEnvironment = LexicalEnvironment.NewDeclarativeEnvironment(_jintEngine, _jintEngine.ExecutionContext.LexicalEnvironment);
			_jintEngine.EnterExecutionContext(lexicalEnvironment, lexicalEnvironment, _jintEngine.ExecutionContext.ThisBinding);
		}

		public void LeaveExecutionContext()
		{
			_jintEngine.LeaveExecutionContext();
		}

		public JsValue Eval(Program program, bool startNewScope)
		{
			if (!startNewScope)
			{
				return _jintEngine.Execute(program).GetCompletionValue();
			}

			EnterExecutionContext();
			try
			{
				return _jintEngine.Execute(program).GetCompletionValue();
			}
			finally
			{
				LeaveExecutionContext();
			}
		}

		public JsValue Eval(Expression expression, bool startNewScope)
		{
			if (!startNewScope)
			{
				return JsValue.FromObject(_jintEngine, _jintEngine.EvaluateExpression(expression));
			}

			EnterExecutionContext();
			try
			{
				return JsValue.FromObject(_jintEngine, _jintEngine.EvaluateExpression(expression));
			}
			finally
			{
				LeaveExecutionContext();
			}
		}

		public void Exec(Program program, bool startNewScope)
		{
			if (!startNewScope)
			{
				_jintEngine.Execute(program);

				return;
			}

			EnterExecutionContext();
			try
			{
				_jintEngine.Execute(program);
			}
			finally
			{
				LeaveExecutionContext();
			}
		}

		public void Exec(Expression expression, bool startNewScope)
		{
			if (!startNewScope)
			{
				_jintEngine.EvaluateExpression(expression);

				return;
			}

			EnterExecutionContext();
			try
			{
				_jintEngine.EvaluateExpression(expression);
			}
			finally
			{
				LeaveExecutionContext();
			}
		}
	}
}
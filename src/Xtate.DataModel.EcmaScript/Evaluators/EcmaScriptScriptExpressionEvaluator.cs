﻿#region Copyright © 2019-2020 Sergii Artemenko

// This file is part of the Xtate project. <https://xtate.net/>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

using System.Threading;
using System.Threading.Tasks;
using Jint.Parser.Ast;

namespace Xtate.DataModel.EcmaScript
{
	internal class EcmaScriptScriptExpressionEvaluator : IScriptExpression, IExecEvaluator, IAncestorProvider
	{
		private readonly Program           _program;
		private readonly IScriptExpression _scriptExpression;

		public EcmaScriptScriptExpressionEvaluator(IScriptExpression scriptExpression, Program program)
		{
			_scriptExpression = scriptExpression;
			_program = program;
		}

	#region Interface IAncestorProvider

		object? IAncestorProvider.Ancestor => _scriptExpression;

	#endregion

	#region Interface IExecEvaluator

		public ValueTask Execute(IExecutionContext executionContext, CancellationToken token)
		{
			executionContext.Engine().Exec(_program, startNewScope: true);

			return default;
		}

	#endregion

	#region Interface IScriptExpression

		public string? Expression => _scriptExpression.Expression;

	#endregion
	}
}
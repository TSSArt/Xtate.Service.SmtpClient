using System;
using System.Linq;
using Jint.Parser.Ast;
using JintIdentifier = Jint.Parser.Ast.Identifier;

namespace TSSArt.StateMachine.EcmaScript
{
	internal class EcmaScriptLocationExpressionEvaluator : ILocationEvaluator, ILocationExpression, IAncestorProvider
	{
		private readonly Program?           _declare;
		private readonly Expression         _leftExpression;
		private readonly LocationExpression _locationExpression;
		private readonly string?            _name;
		private readonly Program            _program;

		public EcmaScriptLocationExpressionEvaluator(in LocationExpression locationExpression, Program program, Expression leftExpression)
		{
			_locationExpression = locationExpression;
			_program = program;
			_leftExpression = leftExpression;

			switch (leftExpression)
			{
				case null:
					break;

				case JintIdentifier identifier:
					_name = identifier.Name;
					_declare = CreateDeclareStatement(identifier);
					break;

				case MemberExpression memberExpression:
					_name = ((JintIdentifier) memberExpression.Property).Name;
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(leftExpression));
			}
		}

		object? IAncestorProvider.Ancestor => EcmaScriptHelper.GetAncestor(_locationExpression);

		public IObject GetValue(IExecutionContext executionContext) => new EcmaScriptObject(executionContext.Engine().Eval(_program, startNewScope: true));

		public string GetName(IExecutionContext executionContext) => _name ?? throw new StateMachineExecutionException(Resources.Exception_Name_of_Location_Expression_can_t_be_evaluated);

		public void SetValue(IObject value, IExecutionContext executionContext)
		{
			var rightValue = value is EcmaScriptObject ecmaScriptObject ? ecmaScriptObject.JsValue : value.ToObject();
			var assignmentExpression = new AssignmentExpression
									   {
											   Type = SyntaxNodes.AssignmentExpression,
											   Left = _leftExpression,
											   Operator = AssignmentOperator.Assign,
											   Right = new Literal { Type = SyntaxNodes.Literal, Value = rightValue }
									   };

			executionContext.Engine().Exec(assignmentExpression, startNewScope: false);
		}

		public void DeclareLocalVariable(IExecutionContext executionContext)
		{
			if (_declare == null)
			{
				throw new StateMachineExecutionException(Resources.Exception_InvalidLocalVariableName);
			}

			executionContext.Engine().Exec(_declare, startNewScope: false);
		}

		public string? Expression => _locationExpression.Expression;

		private static Program CreateDeclareStatement(JintIdentifier identifier)
		{
			var declarators = new[] { new VariableDeclarator { Id = identifier, Type = SyntaxNodes.VariableDeclarator } };
			var declarations = new[] { new VariableDeclaration { Declarations = declarators, Type = SyntaxNodes.VariableDeclaration } };

			return new Program { VariableDeclarations = declarations, Body = Array.Empty<Statement>(), FunctionDeclarations = Array.Empty<FunctionDeclaration>(), Type = SyntaxNodes.Program };
		}

		public static Expression? GetLeftExpression(Program program)
		{
			if (program.Body.Count != 1)
			{
				return null;
			}

			if (!(program.Body.First() is ExpressionStatement expressionStatement))
			{
				return null;
			}

			return expressionStatement.Expression switch
			{
					JintIdentifier identifier => (Expression) identifier,
					MemberExpression memberExpression => memberExpression,
					_ => null
			};
		}
	}
}
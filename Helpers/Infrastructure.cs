using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public static class Infrastructure
	{
		[AssertionMethod]
		public static void Assert([AssertionCondition(AssertionConditionType.IS_TRUE)] [DoesNotReturnIf(false)]
								  bool condition)
		{
			if (condition)
			{
				return;
			}

			throw new StateMachineInfrastructureException(Resources.Exception_AssertionFailed);
		}

		[AssertionMethod]
		public static void Assert([AssertionCondition(AssertionConditionType.IS_TRUE)] [DoesNotReturnIf(false)]
								  bool condition, string message)
		{
			if (condition)
			{
				return;
			}

			throw new StateMachineInfrastructureException(message);
		}

		[AssertionMethod]
		[DoesNotReturn]
		public static void Fail()
		{
			Assert(true);

			throw new NotSupportedException();
		}

		[AssertionMethod]
		[DoesNotReturn]
		public static void Fail(string message)
		{
			Assert(condition: true, message);

			throw new NotSupportedException();
		}

		[AssertionMethod]
		[DoesNotReturn]
		public static T Fail<T>()
		{
			Assert(condition: true);

			throw new NotSupportedException();
		}

		[AssertionMethod]
		[DoesNotReturn]
		public static T Fail<T>(string message)
		{
			Assert(condition: true, message);

			throw new NotSupportedException();
		}

		[AssertionMethod]
		[DoesNotReturn]
		public static void UnexpectedValue()
		{
			Assert(condition: true, Resources.Exception_UnexpectedValue);

			throw new NotSupportedException();
		}

		[AssertionMethod]
		[DoesNotReturn]
		public static T UnexpectedValue<T>()
		{
			Assert(condition: true, Resources.Exception_UnexpectedValue);

			throw new NotSupportedException();
		}
	}
}
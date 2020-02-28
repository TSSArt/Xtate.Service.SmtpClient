using System.Threading;

namespace TSSArt.StateMachine
{
	internal static class CancellationTokenHelper
	{
		public static CancellationToken Any(CancellationToken token1, CancellationToken token2, CancellationToken token3)
		{
			if (!token1.CanBeCanceled)
			{
				return token2.CanBeCanceled ? token3.CanBeCanceled ? CancellationTokenSource.CreateLinkedTokenSource(token2, token3).Token : token2 : token3;
			}

			if (!token2.CanBeCanceled)
			{
				return token3.CanBeCanceled ? CancellationTokenSource.CreateLinkedTokenSource(token1, token3).Token : token1;
			}

			return token3.CanBeCanceled
					? CancellationTokenSource.CreateLinkedTokenSource(token1, token2, token3).Token
					: CancellationTokenSource.CreateLinkedTokenSource(token1, token2).Token;
		}
	}
}

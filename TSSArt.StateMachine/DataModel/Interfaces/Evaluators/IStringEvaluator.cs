using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public interface IStringEvaluator : IValueEvaluator
	{
		ValueTask<string> EvaluateString(IExecutionContext executionContext, CancellationToken token);
	}
}
using System;

namespace TSSArt.StateMachine
{
	public interface IExternalScriptExpression : IExecutableEntity
	{
		Uri? Uri { get; }
	}
}
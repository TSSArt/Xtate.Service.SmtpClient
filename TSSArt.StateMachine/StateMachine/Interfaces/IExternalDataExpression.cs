using System;

namespace TSSArt.StateMachine
{
	public interface IExternalDataExpression : IEntity
	{
		Uri? Uri { get; }
	}
}
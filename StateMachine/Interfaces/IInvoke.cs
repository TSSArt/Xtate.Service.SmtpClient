using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public interface IInvoke : IEntity
	{
		Uri                                Type             { get; }
		IValueExpression                   TypeExpression   { get; }
		Uri                                Source           { get; }
		IValueExpression                   SourceExpression { get; }
		string                             Id               { get; }
		ILocationExpression                IdLocation       { get; }
		IReadOnlyList<ILocationExpression> NameList         { get; }
		bool                               AutoForward      { get; }
		IReadOnlyList<IParam>              Parameters       { get; }
		IFinalize                          Finalize         { get; }
		IContent                           Content          { get; }
	}
}
using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public interface ISend : IExecutableEntity
	{
		string                             Event            { get; }
		IValueExpression                   EventExpression  { get; }
		Uri                                Target           { get; }
		IValueExpression                   TargetExpression { get; }
		Uri                                Type             { get; }
		IValueExpression                   TypeExpression   { get; }
		string                             Id               { get; }
		ILocationExpression                IdLocation       { get; }
		int?                               DelayMs          { get; }
		IValueExpression                   DelayExpression  { get; }
		IReadOnlyList<ILocationExpression> NameList         { get; }
		IReadOnlyList<IParam>              Parameters       { get; }
		IContent                           Content          { get; }
	}
}
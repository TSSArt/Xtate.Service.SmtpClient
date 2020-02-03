using System;
using System.Collections.Immutable;

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
		ImmutableArray<ILocationExpression> NameList         { get; }
		ImmutableArray<IParam>              Parameters       { get; }
		IContent                           Content          { get; }
	}
}
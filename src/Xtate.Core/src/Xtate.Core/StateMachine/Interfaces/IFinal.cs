using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public interface IFinal : IStateEntity
	{
		IIdentifier?             Id       { get; }
		ImmutableArray<IOnEntry> OnEntry  { get; }
		ImmutableArray<IOnExit>  OnExit   { get; }
		IDoneData?               DoneData { get; }
	}
}
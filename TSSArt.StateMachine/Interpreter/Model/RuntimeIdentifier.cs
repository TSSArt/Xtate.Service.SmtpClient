namespace TSSArt.StateMachine
{
	internal sealed class RuntimeIdentifier : IIdentifier
	{
		private string _val;

		public override string ToString() => _val ??= IdGenerator.NewUniqueStateId();
	}
}
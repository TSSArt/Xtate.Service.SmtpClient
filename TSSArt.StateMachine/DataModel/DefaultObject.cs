namespace TSSArt.StateMachine
{
	internal sealed class DefaultObject : IObject
	{
		private readonly object _value;

		public DefaultObject(object value) => _value = value;

	#region Interface IObject

		public object ToObject() => _value;

	#endregion
	}
}
namespace TSSArt.StateMachine
{
	public class DefaultObject : IObject
	{
		private readonly object _value;

		public DefaultObject(object value) => _value = value;

		public object ToObject() => _value;
	}
}
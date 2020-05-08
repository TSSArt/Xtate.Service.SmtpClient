namespace TSSArt.StateMachine
{
	internal sealed class DefaultObject : IObject
	{
		private readonly object? _value;

		public DefaultObject(object? value) => _value = value;

	#region Interface IObject

		public object? ToObject()
		{
			var val = _value;
			while (val is ILazyValue lazyValue)
			{
				val = lazyValue.Value.ToObject();
			}

			return val;
		}

	#endregion
	}
}
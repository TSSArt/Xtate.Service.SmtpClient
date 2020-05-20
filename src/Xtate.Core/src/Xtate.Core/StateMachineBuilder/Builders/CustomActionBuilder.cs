using System;

namespace TSSArt.StateMachine
{
	public class CustomActionBuilder : BuilderBase, ICustomActionBuilder
	{
		private string? _xml;

		public CustomActionBuilder(IErrorProcessor errorProcessor, object? ancestor) : base(errorProcessor, ancestor) { }

	#region Interface ICustomActionBuilder

		public ICustomAction Build() => new CustomAction { Ancestor = Ancestor, Xml = _xml };

		public void SetXml(string xml) => _xml = xml ?? throw new ArgumentNullException(nameof(xml));

	#endregion
	}
}
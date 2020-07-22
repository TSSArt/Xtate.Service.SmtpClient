using System.Xml.XPath;

namespace Xtate.DataModel.XPath.Functions
{
	internal sealed class In : XPathFunctionDescriptorBase
	{
		public In() : base(string.Empty, name: @"In", new[] { XPathResultType.String }, XPathResultType.Boolean) { }

		protected override object Invoke(XPathResolver resolver, object[] args) => resolver.ExecutionContext?.InState((Identifier) XPathObject.ToString(args[0])) ?? false;
	}
}
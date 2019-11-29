using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace TSSArt.StateMachine
{
	public class Base64DecodeAction : CustomActionBase
	{
		private readonly string _destination;
		private readonly string _source;

		public Base64DecodeAction(XmlReader xmlReader)
		{
			if (xmlReader == null) throw new ArgumentNullException(nameof(xmlReader));

			_source = xmlReader.GetAttribute("source");
			_destination = xmlReader.GetAttribute("destination");
		}

		public override ValueTask Action(IExecutionContext context, CancellationToken token)
		{
			if (context == null) throw new ArgumentNullException(nameof(context));

			var source = context.DataModel[_source].AsString();

			var result = Encoding.UTF8.GetString(Convert.FromBase64String(source));

			context.DataModel[_destination] = new DataModelValue(result);

			return default;
		}
	}
}
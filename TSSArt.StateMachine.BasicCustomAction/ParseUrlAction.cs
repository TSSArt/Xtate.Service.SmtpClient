using System;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.WebUtilities;

namespace TSSArt.StateMachine
{
	public class ParseUrlAction : CustomActionBase
	{
		private readonly string _destination;
		private readonly string _parameter;
		private readonly string _source;

		public ParseUrlAction(XmlReader xmlReader)
		{
			if (xmlReader == null) throw new ArgumentNullException(nameof(xmlReader));

			_source = xmlReader.GetAttribute("source");
			_destination = xmlReader.GetAttribute("destination");
			_parameter = xmlReader.GetAttribute("parameter");
		}

		public override ValueTask Action(IExecutionContext context, CancellationToken token)
		{
			if (context == null) throw new ArgumentNullException(nameof(context));

			var source = context.DataModel[_source].AsString();

			if (!Uri.TryCreate(source, UriKind.RelativeOrAbsolute, out var uri))
			{
				throw new InvalidOperationException("Can't parse url");
			}

			if (!string.IsNullOrEmpty(_parameter))
			{
				var parameters = QueryHelpers.ParseNullableQuery(uri.OriginalString);
				var values = parameters[_parameter];
				if (values.Count > 0)
				{
					context.DataModel[_destination] = new DataModelValue(values[0]);
				}
			}

			return default;
		}
	}
}
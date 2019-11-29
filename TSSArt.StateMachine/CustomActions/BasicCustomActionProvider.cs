using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace TSSArt.StateMachine
{
	public class BasicCustomActionProvider : ICustomActionProvider
	{
		private const string Namespace = "http://tssart.com/scxml/customaction/basic";

		public static readonly ICustomActionProvider Instance = new BasicCustomActionProvider();

		private readonly Dictionary<string, Func<XmlReader, CustomActionBase>> _actions = new Dictionary<string, Func<XmlReader, CustomActionBase>>();

		private BasicCustomActionProvider()
		{
			_actions.Add(key: "base64decode", xmlReader => new Base64DecodeAction(xmlReader));
		}

		public bool CanHandle(string ns, string name) => ns == Namespace && _actions.ContainsKey(name);

		public Func<IExecutionContext, CancellationToken, ValueTask> GetAction(string xml)
		{
			using var stringReader = new StringReader(xml);
			using var xmlReader = XmlReader.Create(stringReader);

			xmlReader.MoveToContent();

			return _actions[xmlReader.Name](xmlReader).Action;
		}
	}
}
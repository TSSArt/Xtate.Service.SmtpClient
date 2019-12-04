using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace TSSArt.StateMachine
{
	public abstract class CustomActionProviderBase : ICustomActionProvider
	{
		private readonly Dictionary<string, Func<XmlReader, CustomActionBase>> _actions = new Dictionary<string, Func<XmlReader, CustomActionBase>>();

		private readonly string _namespace;

		protected CustomActionProviderBase()
		{
			var customActionProviderAttribute = GetType().GetCustomAttribute<CustomActionProviderAttribute>();

			if (customActionProviderAttribute == null)
			{
				throw new InvalidOperationException("CustomActionProviderAttribute did not provided for type " + GetType());
			}

			_namespace = customActionProviderAttribute.Namespace;
		}

		public bool CanHandle(string ns, string name) => ns == _namespace && _actions.ContainsKey(name);

		public Func<IExecutionContext, CancellationToken, ValueTask> GetAction(string xml)
		{
			using var stringReader = new StringReader(xml);
			using var xmlReader = XmlReader.Create(stringReader);

			xmlReader.MoveToContent();

			return _actions[xmlReader.Name](xmlReader).Action;
		}

		protected void Register(string name, Func<XmlReader, CustomActionBase> actionFactory)
		{
			if (name == null) throw new ArgumentNullException(nameof(name));
			if (actionFactory == null) throw new ArgumentNullException(nameof(actionFactory));

			_actions.Add(name, actionFactory);
		}
	}
}
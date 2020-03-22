using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;

namespace TSSArt.StateMachine
{
	public abstract class CustomActionFactoryBase : ICustomActionFactory
	{
		private readonly Dictionary<string, Func<XmlReader, CustomActionBase>> _actions = new Dictionary<string, Func<XmlReader, CustomActionBase>>();

		private readonly string _namespace;

		protected CustomActionFactoryBase()
		{
			var customActionProviderAttribute = GetType().GetCustomAttribute<CustomActionProviderAttribute>();

			if (customActionProviderAttribute == null)
			{
				throw new StateMachineInfrastructureException(Res.Format(Resources.Exception_CustomActionProviderAttributeWasNotProvided, GetType()));
			}

			_namespace = customActionProviderAttribute.Namespace;
		}

		public bool CanHandle(string ns, string name) => ns == _namespace && _actions.ContainsKey(name);

		public ICustomActionExecutor CreateExecutor(string xml)
		{
			using var stringReader = new StringReader(xml);
			using var xmlReader = XmlReader.Create(stringReader);

			xmlReader.MoveToContent();

			return _actions[xmlReader.LocalName](xmlReader);
		}

		protected void Register(string name, Func<XmlReader, CustomActionBase> executorFactory)
		{
			if (name == null) throw new ArgumentNullException(nameof(name));
			if (executorFactory == null) throw new ArgumentNullException(nameof(executorFactory));

			_actions.Add(name, executorFactory);
		}
	}
}
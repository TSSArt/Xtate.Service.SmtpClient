using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;

namespace TSSArt.StateMachine
{
	public abstract class CustomActionFactoryBase : ICustomActionFactory
	{
		private readonly Dictionary<string, Func<XmlReader, ICustomActionContext, ICustomActionExecutor>> _actions =
				new Dictionary<string, Func<XmlReader, ICustomActionContext, ICustomActionExecutor>>();

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

	#region Interface ICustomActionFactory

		public bool CanHandle(string ns, string name) => ns == _namespace && _actions.ContainsKey(name);

		public ICustomActionExecutor CreateExecutor(ICustomActionContext customActionContext)
		{
			if (customActionContext == null) throw new ArgumentNullException(nameof(customActionContext));

			using var stringReader = new StringReader(customActionContext.Xml);

			var nameTable = new NameTable();
			FillXmlNameTable(nameTable);
			var nsManager = new XmlNamespaceManager(nameTable);
			var context = new XmlParserContext(nameTable, nsManager, xmlLang: null, xmlSpace: default);

			using var xmlReader = XmlReader.Create(stringReader, settings: null, context);

			xmlReader.MoveToContent();

			return _actions[xmlReader.LocalName](xmlReader, customActionContext);
		}

		void ICustomActionFactory.FillXmlNameTable(XmlNameTable xmlNameTable) => FillXmlNameTable(xmlNameTable);

	#endregion

		protected virtual void FillXmlNameTable(XmlNameTable xmlNameTable)
		{
			if (xmlNameTable == null) throw new ArgumentNullException(nameof(xmlNameTable));

			xmlNameTable.Add(_namespace);

			foreach (var name in _actions.Keys)
			{
				xmlNameTable.Add(name);
			}
		}

		protected void Register(string name, Func<XmlReader, ICustomActionContext, ICustomActionExecutor> executorFactory)
		{
			if (name == null) throw new ArgumentNullException(nameof(name));
			if (executorFactory == null) throw new ArgumentNullException(nameof(executorFactory));

			_actions.Add(name, executorFactory);
		}
	}
}
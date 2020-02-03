using System;
using System.Collections./**/Immutable;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace TSSArt.StateMachine
{
	public abstract class XmlDirector<TDirector> where TDirector : XmlDirector<TDirector>
	{
		private readonly GlobalOptions _globalOptions;
		private readonly XmlReader     _xmlReader;
		private          string        _current;
		private          object        _exceptionTag;

		protected XmlDirector(XmlReader xmlReader, GlobalOptions globalOptions)
		{
			_xmlReader = xmlReader ?? throw new ArgumentNullException(nameof(xmlReader));
			_globalOptions = globalOptions ?? throw new ArgumentNullException(nameof(globalOptions));
		}

		protected string Current => _current ?? _xmlReader.Value;

		protected string ReadOuterXml() => _xmlReader.ReadOuterXml();

		protected void Skip() => _xmlReader.Skip();

		protected Exception GetXmlException(string message)
		{
			var exception = _xmlReader is IXmlLineInfo xmlLineInfo
					? new XmlException(message, innerException: null, xmlLineInfo.LineNumber, xmlLineInfo.LinePosition)
					: new XmlException(message);

			if (_exceptionTag == null)
			{
				_exceptionTag = new object();
			}

			exception.Data[_exceptionTag] = _exceptionTag;

			return exception;
		}

		private bool IsTopLevelExceptionsInternal(Exception exception)
		{
			if (_exceptionTag == null)
			{
				return false;
			}

			if (exception is AggregateException aggregateException)
			{
				return aggregateException.InnerExceptions.All(ex => ex.Data.Contains(_exceptionTag));
			}

			return exception.Data.Contains(_exceptionTag);
		}

		protected Exception GetXmlException(string message, Exception innerException)
		{
			if (innerException == null) throw new ArgumentNullException(nameof(innerException));

			if (IsTopLevelExceptionsInternal(innerException))
			{
				return innerException;
			}

			var exception = _xmlReader is IXmlLineInfo xmlLineInfo
					? new XmlException(message, innerException, xmlLineInfo.LineNumber, xmlLineInfo.LinePosition)
					: new XmlException(message, innerException);

			if (_exceptionTag == null)
			{
				_exceptionTag = new object();
			}

			exception.Data[_exceptionTag] = _exceptionTag;

			return exception;
		}

		protected TEntity Populate<TEntity>(TEntity entity, Action<IPolicyBuilder<TEntity>> buildPolicy)
		{
			if (buildPolicy == null) throw new ArgumentNullException(nameof(buildPolicy));

			var policy = GetPolicy(buildPolicy);

			var validationContext = policy.CreateValidationContext(_xmlReader);

			try
			{
				PopulateAttributes(entity, policy, validationContext);

				if (_xmlReader.IsEmptyElement)
				{
					_xmlReader.ReadStartElement();

					return entity;
				}

				var policyRawContentAction = policy.RawContentAction;
				if (policyRawContentAction != null)
				{
					_current = _xmlReader.ReadInnerXml();

					try
					{
						policyRawContentAction((TDirector) this, entity);
					}
					catch (Exception ex)
					{
						validationContext.AddException(GetXmlException(message: "Failure on raw content processing", ex));
					}

					_current = null;

					return entity;
				}

				PopulateElements(entity, policy, validationContext);

				return entity;
			}
			finally
			{
				validationContext.ThrowIfErrors();
			}
		}

		private Policy<TEntity> GetPolicy<TEntity>(Action<PolicyBuilder<TEntity>> buildPolicy)
		{
			var policy = new Policy<TEntity>();
			buildPolicy(new PolicyBuilder<TEntity>(policy, _globalOptions));
			return policy;
		}

		private void PopulateAttributes<TEntity>(TEntity entity, Policy<TEntity> policy, Policy<TEntity>.ValidationContext validationContext)
		{
			for (var exists = _xmlReader.MoveToFirstAttribute(); exists; exists = _xmlReader.MoveToNextAttribute())
			{
				var ns = _xmlReader.NamespaceURI;
				var name = _xmlReader.LocalName;
				validationContext.ValidateAttribute(ns, name);
				var located = policy.AttributeLocated(ns, name);
				if (located != null)
				{
					try
					{
						located((TDirector) this, entity);
					}
					catch (Exception ex)
					{
						validationContext.AddException(GetXmlException(message: "Failure on attribute processing", ex));
					}
				}
			}

			_xmlReader.MoveToElement();

			validationContext.ProcessAttributesCompleted();
		}

		private void PopulateElements<TEntity>(TEntity entity, Policy<TEntity> policy, Policy<TEntity>.ValidationContext validationContext)
		{
			_xmlReader.ReadStartElement();

			while (_xmlReader.IsStartElement())
			{
				var ns = _xmlReader.NamespaceURI;
				var name = _xmlReader.LocalName;
				validationContext.ValidateElement(ns, name);
				var located = policy.ElementLocated(ns, name);
				if (located != null)
				{
					try
					{
						located((TDirector) this, entity);
					}
					catch (Exception ex)
					{
						validationContext.AddException(GetXmlException(message: "Failure on element processing", ex));
					}
				}
				else
				{
					_xmlReader.Skip();
				}
			}

			validationContext.ProcessElementsCompleted();

			_xmlReader.ReadEndElement();
		}

		protected interface IPolicyBuilder<out TEntity>
		{
			IPolicyBuilder<TEntity> ValidateElementName(string name);
			IPolicyBuilder<TEntity> RequiredAttribute(string name, Action<TDirector, TEntity> located);
			IPolicyBuilder<TEntity> OptionalAttribute(string name, Action<TDirector, TEntity> located);
			IPolicyBuilder<TEntity> OptionalElement(string name, Action<TDirector, TEntity> located);
			IPolicyBuilder<TEntity> SingleElement(string name, Action<TDirector, TEntity> located);
			IPolicyBuilder<TEntity> MultipleElements(string name, Action<TDirector, TEntity> located);
			IPolicyBuilder<TEntity> ValidateElementName(Options options, string name);
			IPolicyBuilder<TEntity> RequiredAttribute(Options options, string name, Action<TDirector, TEntity> located);
			IPolicyBuilder<TEntity> OptionalAttribute(Options options, string name, Action<TDirector, TEntity> located);
			IPolicyBuilder<TEntity> OptionalElement(Options options, string name, Action<TDirector, TEntity> located);
			IPolicyBuilder<TEntity> SingleElement(Options options, string name, Action<TDirector, TEntity> located);
			IPolicyBuilder<TEntity> MultipleElements(Options options, string name, Action<TDirector, TEntity> located);
			IPolicyBuilder<TEntity> IgnoreUnknownElements();
			IPolicyBuilder<TEntity> DenyUnknownElements();
			IPolicyBuilder<TEntity> RawContent(Action<TDirector, TEntity> action);
			IPolicyBuilder<TEntity> UnknownElement(Action<TDirector, TEntity> located);
		}

		private enum AttributeType
		{
			Optional         = 0,
			Required         = 1,
			SysIncrement     = 2,
			SysOptionalFound = 2,
			SysRequiredFound = 3
		}

		// ReSharper disable UnusedMember.Local
		private enum ElementType
		{
			ZeroToMany         = 0,
			ZeroToOne          = 1,
			One                = 2,
			OneToMany          = 3,
			SysIncrement       = 4,
			SysZeroToManyFound = 4,
			SysZeroToOneFound  = 5,
			SysOneFound        = 6,
			SysOneToManyFound  = 7
		}

		// ReSharper restore UnusedMember.Local

		private class Policy<TEntity>
		{
			private readonly Dictionary<(string ns, string name), (Action<TDirector, TEntity> located, AttributeType type)> _attributes =
					new Dictionary<(string ns, string name), (Action<TDirector, TEntity> located, AttributeType type)>();

			private readonly Dictionary<(string ns, string name), (Action<TDirector, TEntity> located, ElementType type)> _elements =
					new Dictionary<(string ns, string name), (Action<TDirector, TEntity> located, ElementType type)>();

			public Action<TDirector, TEntity> RawContentAction { get; set; }

			public Action<TDirector, TEntity> UnknownElementAction { get; set; }

			public bool   IgnoreUnknownElements { get; set; }
			public string ElementNamespace      { get; set; }
			public string ElementName           { get; set; }

			public void AddAttribute(string ns, string name, Action<TDirector, TEntity> located, AttributeType type)
			{
				_attributes.Add((ns, name), (located, type));
			}

			public void AddElement(string ns, string name, Action<TDirector, TEntity> located, ElementType type)
			{
				_elements.Add((ns, name), (located, type));
			}

			public Action<TDirector, TEntity> AttributeLocated(string ns, string name) => _attributes.TryGetValue((ns, name), out var val) ? val.located : null;

			public Action<TDirector, TEntity> ElementLocated(string ns, string name) => _elements.TryGetValue((ns, name), out var val) ? val.located : UnknownElementAction;

			public ValidationContext CreateValidationContext(XmlReader xmlReader) => new ValidationContext(this, xmlReader);

			public class ValidationContext
			{
				private readonly Dictionary<(string ns, string name), AttributeType> _attributes;
				private readonly Dictionary<(string ns, string name), ElementType>   _elements;
				private readonly bool                                                _ignoreUnknownElements;
				private readonly XmlReader                                           _xmlReader;
				private          List<Exception>                                     _exceptions;

				public ValidationContext(Policy<TEntity> policy, XmlReader xmlReader)
				{
					_xmlReader = xmlReader;

					if (policy._attributes.Count > 0)
					{
						_attributes = new Dictionary<(string ns, string name), AttributeType>(policy._attributes.Count);
						foreach (var pair in policy._attributes)
						{
							_attributes.Add(pair.Key, pair.Value.type);
						}
					}

					if (policy._elements.Count > 0)
					{
						_elements = new Dictionary<(string ns, string name), ElementType>(policy._elements.Count);
						foreach (var pair in policy._elements)
						{
							_elements.Add(pair.Key, pair.Value.type);
						}
					}

					_ignoreUnknownElements = policy.IgnoreUnknownElements || policy.UnknownElementAction != null;

					if (policy.ElementName != null)
					{
						if (!xmlReader.IsStartElement(policy.ElementName, policy.ElementNamespace))
						{
							AddException(GetXmlException(format: "Expected element '{0}' was not found.", policy.ElementNamespace, policy.ElementName));
						}
					}
				}

				public void ValidateAttribute(string ns, string name)
				{
					if (_attributes != null && _attributes.TryGetValue((ns, name), out var type))
					{
						if (type == AttributeType.SysOptionalFound || type == AttributeType.SysRequiredFound)
						{
							AddException(GetXmlException(format: "Found duplicate attribute '{0}'.", ns, name));
						}

						_attributes[(ns, name)] = type + (int) AttributeType.SysIncrement;
					}
				}

				public void ProcessAttributesCompleted()
				{
					if (_attributes != null && _attributes.Any(p => p.Value == AttributeType.Required))
					{
						var query = _attributes.Where(p => p.Value == AttributeType.Required).Select(p => p.Key);
						AddException(GetXmlException(format: "Missed required attributes '{0}'", delimiter: "', '", query));
					}
				}

				public void ValidateElement(string ns, string name)
				{
					if (_elements != null && _elements.TryGetValue((ns, name), out var type))
					{
						if (type == ElementType.SysOneFound || type == ElementType.SysZeroToOneFound)
						{
							AddException(GetXmlException(format: "Only one <{0}> element allowed.", ns, name));
						}

						_elements[(ns, name)] = type + (int) ElementType.SysIncrement;
					}
					else if (!_ignoreUnknownElements)
					{
						AddException(GetXmlException(format: "Detected unknown element <{0}>.", ns, name));
					}
				}

				public void ProcessElementsCompleted()
				{
					if (_elements != null && _elements.Any(p => p.Value == ElementType.One || p.Value == ElementType.OneToMany))
					{
						var query = _elements.Where(p => p.Value == ElementType.One || p.Value == ElementType.OneToMany).Select(p => p.Key);
						AddException(GetXmlException(format: "Missed required elements <{0}>", delimiter: ">, <", query));
					}
				}

				private XmlException GetXmlException(string format, string ns, string name) => GetXmlException(format, string.Empty, new[] { (ns, name) });

				private XmlException GetXmlException(string format, string delimiter, IEnumerable<(string ns, string name)> names)
				{
					var query = names.Select(n => string.IsNullOrEmpty(n.ns) ? n.name : n.ns + ":" + n.name);
					var message = string.Format(CultureInfo.InvariantCulture, format, string.Join(delimiter, query));

					if (_xmlReader is IXmlLineInfo xmlLineInfo)
					{
						return new XmlException(message, innerException: null, xmlLineInfo.LineNumber, xmlLineInfo.LinePosition);
					}

					return new XmlException(message);
				}

				public void AddException(Exception ex)
				{
					if (_exceptions == null)
					{
						_exceptions = new List<Exception>();
					}

					_exceptions.Add(ex);

					if (_exceptions.Count >= 20)
					{
						throw new AggregateException(_exceptions);
					}
				}

				public void ThrowIfErrors()
				{
					if (_exceptions != null)
					{
						if (_exceptions.Count == 1)
						{
							throw _exceptions[0];
						}

						throw new AggregateException(_exceptions);
					}
				}
			}
		}

		private class PolicyBuilder<TEntity> : IPolicyBuilder<TEntity>
		{
			private readonly GlobalOptions   _globalOptions;
			private readonly Policy<TEntity> _policy;
			private          bool?           _rawContent;

			public PolicyBuilder(Policy<TEntity> policy, GlobalOptions globalOptions)
			{
				_policy = policy;
				_globalOptions = globalOptions;
				_policy.IgnoreUnknownElements = globalOptions.IgnoreUnknownElements;
			}

			public IPolicyBuilder<TEntity> ValidateElementName(string name) => ValidateElementName(options: null, name);

			public IPolicyBuilder<TEntity> RequiredAttribute(string name, Action<TDirector, TEntity> located) => RequiredAttribute(options: null, name, located);

			public IPolicyBuilder<TEntity> OptionalAttribute(string name, Action<TDirector, TEntity> located) => OptionalAttribute(options: null, name, located);

			public IPolicyBuilder<TEntity> OptionalElement(string name, Action<TDirector, TEntity> located) => OptionalElement(options: null, name, located);

			public IPolicyBuilder<TEntity> SingleElement(string name, Action<TDirector, TEntity> located) => SingleElement(options: null, name, located);

			public IPolicyBuilder<TEntity> MultipleElements(string name, Action<TDirector, TEntity> located) => MultipleElements(options: null, name, located);

			public IPolicyBuilder<TEntity> ValidateElementName(Options options, string name)
			{
				if (string.IsNullOrEmpty(name)) throw new ArgumentException(message: "Value cannot be null or empty.", nameof(name));

				_policy.ElementNamespace = options?.Namespace ?? _globalOptions.ElementDefaultNamespace ?? string.Empty;
				_policy.ElementName = name;

				return this;
			}

			public IPolicyBuilder<TEntity> RequiredAttribute(Options options, string name, Action<TDirector, TEntity> located)
			{
				if (located == null) throw new ArgumentNullException(nameof(located));
				if (string.IsNullOrEmpty(name)) throw new ArgumentException(message: "Value cannot be null or empty.", nameof(name));

				_policy.AddAttribute(options?.Namespace ?? _globalOptions.AttributeDefaultNamespace ?? string.Empty, name, located, AttributeType.Required);

				return this;
			}

			public IPolicyBuilder<TEntity> OptionalAttribute(Options options, string name, Action<TDirector, TEntity> located)
			{
				if (located == null) throw new ArgumentNullException(nameof(located));
				if (string.IsNullOrEmpty(name)) throw new ArgumentException(message: "Value cannot be null or empty.", nameof(name));

				_policy.AddAttribute(options?.Namespace ?? _globalOptions.AttributeDefaultNamespace ?? string.Empty, name, located, AttributeType.Optional);

				return this;
			}

			public IPolicyBuilder<TEntity> OptionalElement(Options options, string name, Action<TDirector, TEntity> located)
			{
				if (located == null) throw new ArgumentNullException(nameof(located));
				if (string.IsNullOrEmpty(name)) throw new ArgumentException(message: "Value cannot be null or empty.", nameof(name));

				_policy.AddElement(options?.Namespace ?? _globalOptions.ElementDefaultNamespace ?? string.Empty, name, located, ElementType.ZeroToOne);

				return this;
			}

			public IPolicyBuilder<TEntity> SingleElement(Options options, string name, Action<TDirector, TEntity> located)
			{
				if (located == null) throw new ArgumentNullException(nameof(located));
				if (string.IsNullOrEmpty(name)) throw new ArgumentException(message: "Value cannot be null or empty.", nameof(name));

				UseRawContent(val: false);
				_policy.AddElement(options?.Namespace ?? _globalOptions.ElementDefaultNamespace ?? string.Empty, name, located, ElementType.One);

				return this;
			}

			public IPolicyBuilder<TEntity> MultipleElements(Options options, string name, Action<TDirector, TEntity> located)
			{
				if (located == null) throw new ArgumentNullException(nameof(located));
				if (string.IsNullOrEmpty(name)) throw new ArgumentException(message: "Value cannot be null or empty.", nameof(name));

				UseRawContent(val: false);
				_policy.AddElement(options?.Namespace ?? _globalOptions.ElementDefaultNamespace ?? string.Empty, name, located, ElementType.ZeroToMany);

				return this;
			}

			public IPolicyBuilder<TEntity> UnknownElement(Action<TDirector, TEntity> located)
			{
				_policy.UnknownElementAction = located ?? throw new ArgumentNullException(nameof(located));

				return this;
			}

			public IPolicyBuilder<TEntity> IgnoreUnknownElements()
			{
				_policy.IgnoreUnknownElements = true;

				return this;
			}

			public IPolicyBuilder<TEntity> DenyUnknownElements()
			{
				_policy.IgnoreUnknownElements = false;

				return this;
			}

			public IPolicyBuilder<TEntity> RawContent(Action<TDirector, TEntity> action)
			{
				UseRawContent(val: true);
				_policy.RawContentAction = action;

				return this;
			}

			private void UseRawContent(bool val)
			{
				if (_rawContent == val)
				{
					if (val)
					{
						throw new InvalidOperationException(message: "Can not register raw content more than one time");
					}

					return;
				}

				if (_rawContent == null)
				{
					_rawContent = val;

					return;
				}

				if (val)
				{
					throw new InvalidOperationException(message: "Can not read raw content due to registered elements");
				}

				throw new InvalidOperationException(message: "Can not register component due to registered raw content");
			}
		}
	}
}
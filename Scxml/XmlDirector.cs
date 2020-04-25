﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Xml;
using TSSArt.StateMachine.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public abstract class XmlDirector<TDirector> : IXmlLineInfo where TDirector : XmlDirector<TDirector>
	{
		private readonly IErrorProcessor _errorProcessor;
		private readonly IXmlLineInfo?   _xmlLineInfo;
		private readonly XmlReader       _xmlReader;
		private          string?         _current;

		protected XmlDirector(XmlReader xmlReader, IErrorProcessor errorProcessor)
		{
			_xmlReader = xmlReader ?? throw new ArgumentNullException(nameof(xmlReader));
			_errorProcessor = errorProcessor ?? throw new ArgumentNullException(nameof(errorProcessor));

			_xmlLineInfo = _xmlReader as IXmlLineInfo;
		}

		protected string Current => _current ?? _xmlReader.Value;

	#region Interface IXmlLineInfo

		public bool HasLineInfo() => _xmlLineInfo?.HasLineInfo() ?? false;

		public int LineNumber => _xmlLineInfo?.LineNumber ?? 0;

		public int LinePosition => _xmlLineInfo?.LinePosition ?? 0;

	#endregion

		protected string ReadOuterXml() => _xmlReader.ReadOuterXml();

		protected void Skip() => _xmlReader.Skip();

		protected TEntity Populate<TEntity>(TEntity entity, Policy<TEntity> policy)
		{
			if (policy == null) throw new ArgumentNullException(nameof(policy));

			var validationContext = policy.CreateValidationContext(_xmlReader, _errorProcessor);

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
					AddError(Resources.ErrorMessage_Failure_on_raw_content_processing, ex);
				}

				_current = null;

				return entity;
			}

			PopulateElements(entity, policy, validationContext);

			return entity;
		}

		protected static Policy<TEntity> BuildPolicy<TEntity>(Action<IPolicyBuilder<TEntity>> buildPolicy)
		{
			if (buildPolicy == null) throw new ArgumentNullException(nameof(buildPolicy));

			var policy = new Policy<TEntity>();
			buildPolicy(new PolicyBuilder<TEntity>(policy));
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
						AddError(Resources.ErrorMessage_Failure_on_attribute_processing, ex);
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
						AddError(Resources.ErrorMessage_Failure_on_element_processing, ex);
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

		private void AddError(string message, Exception exception) => _errorProcessor.AddError<XmlDirector<TDirector>>(_xmlReader, message, exception);

		[PublicAPI]
		protected interface IPolicyBuilder<out TEntity>
		{
			IPolicyBuilder<TEntity> IgnoreUnknownElements(bool value);
			IPolicyBuilder<TEntity> ValidateElementName([Localizable(false)] string name);
			IPolicyBuilder<TEntity> RequiredAttribute([Localizable(false)] string name, Action<TDirector, TEntity> located);
			IPolicyBuilder<TEntity> OptionalAttribute([Localizable(false)] string name, Action<TDirector, TEntity> located);
			IPolicyBuilder<TEntity> OptionalElement([Localizable(false)] string name, Action<TDirector, TEntity> located);
			IPolicyBuilder<TEntity> SingleElement([Localizable(false)] string name, Action<TDirector, TEntity> located);
			IPolicyBuilder<TEntity> MultipleElements([Localizable(false)] string name, Action<TDirector, TEntity> located);
			IPolicyBuilder<TEntity> ValidateElementName([Localizable(false)] string ns, [Localizable(false)] string name);
			IPolicyBuilder<TEntity> RequiredAttribute([Localizable(false)] string ns, [Localizable(false)] string name, Action<TDirector, TEntity> located);
			IPolicyBuilder<TEntity> OptionalAttribute([Localizable(false)] string ns, [Localizable(false)] string name, Action<TDirector, TEntity> located);
			IPolicyBuilder<TEntity> OptionalElement([Localizable(false)] string ns, [Localizable(false)] string name, Action<TDirector, TEntity> located);
			IPolicyBuilder<TEntity> SingleElement([Localizable(false)] string ns, [Localizable(false)] string name, Action<TDirector, TEntity> located);
			IPolicyBuilder<TEntity> MultipleElements([Localizable(false)] string ns, [Localizable(false)] string name, Action<TDirector, TEntity> located);
			IPolicyBuilder<TEntity> IgnoreUnknownElements();
			IPolicyBuilder<TEntity> DenyUnknownElements();
			IPolicyBuilder<TEntity> RawContent(Action<TDirector, TEntity> action);
			IPolicyBuilder<TEntity> UnknownElement(Action<TDirector, TEntity> located);
		}

		[PublicAPI]
		protected enum AttributeType
		{
			Optional         = 0,
			Required         = 1,
			SysIncrement     = 2,
			SysOptionalFound = 2,
			SysRequiredFound = 3
		}

		[PublicAPI]
		protected enum ElementType
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

		protected class Policy<TEntity>
		{
			private readonly Dictionary<(string ns, string name), (Action<TDirector, TEntity> located, AttributeType type)> _attributes =
					new Dictionary<(string ns, string name), (Action<TDirector, TEntity> located, AttributeType type)>();

			private readonly Dictionary<(string ns, string name), (Action<TDirector, TEntity> located, ElementType type)> _elements =
					new Dictionary<(string ns, string name), (Action<TDirector, TEntity> located, ElementType type)>();

			public Action<TDirector, TEntity>? RawContentAction { get; set; }

			public Action<TDirector, TEntity>? UnknownElementAction { get; set; }

			public bool    IgnoreUnknownElements { get; set; }
			public string? ElementNamespace      { get; set; }
			public string? ElementName           { get; set; }

			public void AddAttribute(string ns, string name, Action<TDirector, TEntity> located, AttributeType type)
			{
				_attributes.Add((ns, name), (located, type));
			}

			public void AddElement(string ns, string name, Action<TDirector, TEntity> located, ElementType type)
			{
				_elements.Add((ns, name), (located, type));
			}

			public Action<TDirector, TEntity>? AttributeLocated(string ns, string name) => _attributes.TryGetValue((ns, name), out var val) ? val.located : null;

			public Action<TDirector, TEntity>? ElementLocated(string ns, string name) => _elements.TryGetValue((ns, name), out var val) ? val.located : UnknownElementAction;

			public ValidationContext CreateValidationContext(XmlReader xmlReader, IErrorProcessor errorProcessor) => new ValidationContext(this, xmlReader, errorProcessor);

			public class ValidationContext
			{
				private readonly Dictionary<(string ns, string name), AttributeType>? _attributes;
				private readonly Dictionary<(string ns, string name), ElementType>?   _elements;
				private readonly IErrorProcessor                                      _errorProcessor;
				private readonly bool                                                 _ignoreUnknownElements;
				private readonly XmlReader                                            _xmlReader;

				public ValidationContext(Policy<TEntity> policy, XmlReader xmlReader, IErrorProcessor errorProcessor)
				{
					if (policy == null) throw new ArgumentNullException(nameof(policy));
					_xmlReader = xmlReader ?? throw new ArgumentNullException(nameof(xmlReader));
					_errorProcessor = errorProcessor;

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
							AddError(CreateMessage(Resources.ErrorMessage_Expected_element_was_not_found, policy.ElementNamespace!, policy.ElementName));
						}
					}
				}

				public void ValidateAttribute(string ns, string name)
				{
					if (_attributes != null && _attributes.TryGetValue((ns, name), out var type))
					{
						if (type == AttributeType.SysOptionalFound || type == AttributeType.SysRequiredFound)
						{
							AddError(CreateMessage(Resources.ErrorMessage_Found_duplicate_attribute, ns, name));
						}

						_attributes[(ns, name)] = type + (int) AttributeType.SysIncrement;
					}
				}

				public void ProcessAttributesCompleted()
				{
					if (_attributes != null && _attributes.Any(p => p.Value == AttributeType.Required))
					{
						var query = _attributes.Where(p => p.Value == AttributeType.Required).Select(p => p.Key);
						AddError(CreateMessage(Resources.ErrorMessage_Missed_required_attributes, delimiter: @"', '", query));
					}
				}

				public void ValidateElement(string ns, string name)
				{
					if (_elements != null && _elements.TryGetValue((ns, name), out var type))
					{
						if (type == ElementType.SysOneFound || type == ElementType.SysZeroToOneFound)
						{
							AddError(CreateMessage(Resources.ErrorMessage_Only_one_element_allowed, ns, name));
						}

						_elements[(ns, name)] = type + (int) ElementType.SysIncrement;
					}
					else if (!_ignoreUnknownElements)
					{
						AddError(CreateMessage(Resources.ErrorMessage_Detected_unknown_element, ns, name));
					}
				}

				public void ProcessElementsCompleted()
				{
					if (_elements != null && _elements.Any(p => p.Value == ElementType.One || p.Value == ElementType.OneToMany))
					{
						var query = _elements.Where(p => p.Value == ElementType.One || p.Value == ElementType.OneToMany).Select(p => p.Key);
						AddError(CreateMessage(Resources.ErrorMessage_Missed_required_elements, delimiter: @">, <", query));
					}
				}

				private static string CreateMessage(string format, string ns, string name) => string.Format(CultureInfo.InvariantCulture, format, string.IsNullOrEmpty(ns) ? name : ns + @":" + name);

				private static string CreateMessage(string format, string delimiter, IEnumerable<(string ns, string name)> names)
				{
					var query = names.Select(n => string.IsNullOrEmpty(n.ns) ? n.name : n.ns + @":" + n.name);
					return string.Format(CultureInfo.InvariantCulture, format, string.Join(delimiter, query));
				}

				private void AddError(string message) => _errorProcessor.AddError<XmlDirector<TDirector>>(_xmlReader, message);
			}
		}

		private class PolicyBuilder<TEntity> : IPolicyBuilder<TEntity>
		{
			private readonly Policy<TEntity> _policy;
			private          bool?           _rawContent;

			public PolicyBuilder(Policy<TEntity> policy) => _policy = policy;

		#region Interface XmlDirector<TDirector>.IPolicyBuilder<TEntity>

			public IPolicyBuilder<TEntity> IgnoreUnknownElements(bool value)
			{
				_policy.IgnoreUnknownElements = value;

				return this;
			}

			public IPolicyBuilder<TEntity> ValidateElementName(string name) => ValidateElementName(string.Empty, name);

			public IPolicyBuilder<TEntity> RequiredAttribute(string name, Action<TDirector, TEntity> located) => RequiredAttribute(string.Empty, name, located);

			public IPolicyBuilder<TEntity> OptionalAttribute(string name, Action<TDirector, TEntity> located) => OptionalAttribute(string.Empty, name, located);

			public IPolicyBuilder<TEntity> OptionalElement(string name, Action<TDirector, TEntity> located) => OptionalElement(string.Empty, name, located);

			public IPolicyBuilder<TEntity> SingleElement(string name, Action<TDirector, TEntity> located) => SingleElement(string.Empty, name, located);

			public IPolicyBuilder<TEntity> MultipleElements(string name, Action<TDirector, TEntity> located) => MultipleElements(string.Empty, name, located);

			public IPolicyBuilder<TEntity> ValidateElementName(string ns, string name)
			{
				if (string.IsNullOrEmpty(name)) throw new ArgumentException(Resources.Exception_ValueCannotBeNullOrEmpty, nameof(name));

				_policy.ElementNamespace = ns ?? throw new ArgumentNullException(nameof(ns));
				_policy.ElementName = name;

				return this;
			}

			public IPolicyBuilder<TEntity> RequiredAttribute(string ns, string name, Action<TDirector, TEntity> located)
			{
				if (ns == null) throw new ArgumentNullException(nameof(ns));
				if (located == null) throw new ArgumentNullException(nameof(located));
				if (string.IsNullOrEmpty(name)) throw new ArgumentException(Resources.Exception_ValueCannotBeNullOrEmpty, nameof(name));

				_policy.AddAttribute(ns, name, located, AttributeType.Required);

				return this;
			}

			public IPolicyBuilder<TEntity> OptionalAttribute(string ns, string name, Action<TDirector, TEntity> located)
			{
				if (ns == null) throw new ArgumentNullException(nameof(ns));
				if (located == null) throw new ArgumentNullException(nameof(located));
				if (string.IsNullOrEmpty(name)) throw new ArgumentException(Resources.Exception_ValueCannotBeNullOrEmpty, nameof(name));

				_policy.AddAttribute(ns, name, located, AttributeType.Optional);

				return this;
			}

			public IPolicyBuilder<TEntity> OptionalElement(string ns, string name, Action<TDirector, TEntity> located)
			{
				if (ns == null) throw new ArgumentNullException(nameof(ns));
				if (located == null) throw new ArgumentNullException(nameof(located));
				if (string.IsNullOrEmpty(name)) throw new ArgumentException(Resources.Exception_ValueCannotBeNullOrEmpty, nameof(name));

				_policy.AddElement(ns, name, located, ElementType.ZeroToOne);

				return this;
			}

			public IPolicyBuilder<TEntity> SingleElement(string ns, string name, Action<TDirector, TEntity> located)
			{
				if (ns == null) throw new ArgumentNullException(nameof(ns));
				if (located == null) throw new ArgumentNullException(nameof(located));
				if (string.IsNullOrEmpty(name)) throw new ArgumentException(Resources.Exception_ValueCannotBeNullOrEmpty, nameof(name));

				UseRawContent(val: false);
				_policy.AddElement(ns, name, located, ElementType.One);

				return this;
			}

			public IPolicyBuilder<TEntity> MultipleElements(string ns, string name, Action<TDirector, TEntity> located)
			{
				if (ns == null) throw new ArgumentNullException(nameof(ns));
				if (located == null) throw new ArgumentNullException(nameof(located));
				if (string.IsNullOrEmpty(name)) throw new ArgumentException(Resources.Exception_ValueCannotBeNullOrEmpty, nameof(name));

				UseRawContent(val: false);
				_policy.AddElement(ns, name, located, ElementType.ZeroToMany);

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

		#endregion

			private void UseRawContent(bool val)
			{
				if (_rawContent == val)
				{
					if (val)
					{
						throw new ArgumentException(Resources.Exception_Can_not_register_raw_content_more_than_one_time);
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
					throw new ArgumentException(Resources.Exception_Can_not_read_raw_content_due_to_registered_elements);
				}

				throw new ArgumentException(Resources.Exception_Can_not_register_component_due_to_registered_raw_content);
			}
		}
	}
}
using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public class InvokeBuilder : IInvokeBuilder
	{
		private readonly List<IParam>                       _parameters = new List<IParam>();
		private          bool                               _autoForward;
		private          IContent                           _content;
		private          IFinalize                          _finalize;
		private          string                             _id;
		private          ILocationExpression                _idLocation;
		private          IReadOnlyList<ILocationExpression> _nameList;
		private          Uri                                _source;
		private          IValueExpression                   _sourceExpression;
		private          Uri                                _type;
		private          IValueExpression                   _typeExpression;

		public IInvoke Build()
		{
			if (_type != null && _typeExpression != null)
			{
				throw new InvalidOperationException(message: "Type and TypeExpression can't be used at the same time in Invoke element");
			}

			if (_id != null && _idLocation != null)
			{
				throw new InvalidOperationException(message: "Id and IdLocation can't be used at the same time in Invoke element");
			}

			if (_source != null && _sourceExpression != null)
			{
				throw new InvalidOperationException(message: "Source and SourceExpression can't be used at the same time in Invoke element");
			}

			if (_nameList != null && _parameters.Count > 0)
			{
				throw new InvalidOperationException(message: "NameList and Parameters can't be used at the same time in Invoke element");
			}

			return new Invoke
				   {
						   Type = _type, TypeExpression = _typeExpression, Source = _source, SourceExpression = _sourceExpression, Id = _id, IdLocation = _idLocation,
						   NameList = _nameList, AutoForward = _autoForward, Parameters = ParamList.Create(_parameters), Finalize = _finalize, Content = _content
				   };
		}

		public void SetType(Uri type) => _type = type ?? throw new ArgumentNullException(nameof(type));

		public void SetTypeExpression(IValueExpression typeExpression) => _typeExpression = typeExpression ?? throw new ArgumentNullException(nameof(typeExpression));

		public void SetSource(Uri source) => _source = source ?? throw new ArgumentNullException(nameof(source));

		public void SetSourceExpression(IValueExpression sourceExpression) => _sourceExpression = sourceExpression ?? throw new ArgumentNullException(nameof(sourceExpression));

		public void SetId(string id)
		{
			if (string.IsNullOrEmpty(id)) throw new ArgumentException(message: "Value cannot be null or empty.", nameof(id));

			_id = id;
		}

		public void SetIdLocation(ILocationExpression idLocation) => _idLocation = idLocation ?? throw new ArgumentNullException(nameof(idLocation));

		public void SetNameList(IReadOnlyList<ILocationExpression> nameList) => _nameList = LocationExpressionList.Create(nameList ?? throw new ArgumentNullException(nameof(nameList)));

		public void SetAutoForward(bool autoForward) => _autoForward = autoForward;

		public void AddParam(IParam param)
		{
			if (param == null) throw new ArgumentNullException(nameof(param));

			_parameters.Add(param);
		}

		public void SetFinalize(IFinalize finalize) => _finalize = finalize ?? throw new ArgumentNullException(nameof(finalize));

		public void SetContent(IContent content) => _content = content ?? throw new ArgumentNullException(nameof(content));
	}
}
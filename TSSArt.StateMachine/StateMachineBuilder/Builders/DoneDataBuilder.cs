using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public class DoneDataBuilder : IDoneDataBuilder
	{
		private readonly List<IParam> _parameters = new List<IParam>();
		private          IContent     _content;

		public IDoneData Build() => new DoneData { Content = _content, Parameters = ParamList.Create(_parameters) };

		public void SetContent(IContent content) => _content = content ?? throw new ArgumentNullException(nameof(content));

		public void AddParameter(IParam parameter)
		{
			if (parameter == null) throw new ArgumentNullException(nameof(parameter));

			_parameters.Add(parameter);
		}
	}
}
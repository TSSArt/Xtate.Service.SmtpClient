using System;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public class DoneDataBuilder : IDoneDataBuilder
	{
		private IContent                       _content;
		private ImmutableArray<IParam>.Builder _parameters;

		public IDoneData Build() => new DoneData { Content = _content, Parameters = _parameters?.ToImmutable() ?? default };

		public void SetContent(IContent content) => _content = content ?? throw new ArgumentNullException(nameof(content));

		public void AddParameter(IParam parameter)
		{
			if (parameter == null) throw new ArgumentNullException(nameof(parameter));

			(_parameters ??= ImmutableArray.CreateBuilder<IParam>()).Add(parameter);
		}
	}
}
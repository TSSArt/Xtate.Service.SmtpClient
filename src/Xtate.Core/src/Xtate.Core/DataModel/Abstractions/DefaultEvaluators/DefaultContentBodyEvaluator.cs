﻿#region Copyright © 2019-2020 Sergii Artemenko

// This file is part of the Xtate project. <https://xtate.net/>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

using System;
using System.Threading;
using System.Threading.Tasks;
using Xtate.Annotations;

namespace Xtate.DataModel
{
	[PublicAPI]
	public class DefaultContentBodyEvaluator : IContentBody, IObjectEvaluator, IStringEvaluator, IAncestorProvider
	{
		private readonly ContentBody    _contentBody;
		private          DataModelValue _parsedValue;
		private          Exception?     _parsingException;

		public DefaultContentBodyEvaluator(in ContentBody contentBody)
		{
			Infrastructure.NotNull(contentBody.Value);

			_contentBody = contentBody;
		}

	#region Interface IAncestorProvider

		object? IAncestorProvider.Ancestor => _contentBody.Ancestor;

	#endregion

	#region Interface IContentBody

		public string Value => _contentBody.Value!;

	#endregion

	#region Interface IObjectEvaluator

		public virtual ValueTask<IObject> EvaluateObject(IExecutionContext executionContext, CancellationToken token)
		{
			if (_parsingException is null && _parsedValue.IsUndefined())
			{
				_parsedValue = ParseToDataModel(ref _parsingException);
				_parsedValue.MakeDeepConstant();
			}

			if (_parsingException is not null)
			{
				Infrastructure.IgnoredException(_parsingException);
			}

			return new ValueTask<IObject>(_parsedValue.CloneAsWritable());
		}

	#endregion

	#region Interface IStringEvaluator

		public virtual ValueTask<string> EvaluateString(IExecutionContext executionContext, CancellationToken token) => new(Value);

	#endregion

		protected virtual DataModelValue ParseToDataModel(ref Exception? parseException) => Value;
	}
}
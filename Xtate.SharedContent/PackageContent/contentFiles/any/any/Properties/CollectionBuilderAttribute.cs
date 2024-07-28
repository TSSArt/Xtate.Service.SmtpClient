// Copyright © 2019-2024 Sergii Artemenko
// 
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

// ReSharper disable All
// ReSharper disable InvalidXmlDocComment

#if !NET8_OR_GREATER
namespace System.Runtime.CompilerServices
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, Inherited = false)]
	internal sealed class CollectionBuilderAttribute : Attribute
	{
		/// <summary>Initialize the attribute to refer to the <paramref name="methodName"/> method on the <paramref name="builderType"/> type.</summary>
		/// <param name="builderType">The type of the builder to use to construct the collection.</param>
		/// <param name="methodName">The name of the method on the builder to use to construct the collection.</param>
		/// <remarks>
		/// <paramref name="methodName"/> must refer to a static method that accepts a single parameter of
		/// type <see cref="ReadOnlySpan{T}"/> and returns an instance of the collection being built containing
		/// a copy of the data from that span.  In future releases of .NET, additional patterns may be supported.
		/// </remarks>
		public CollectionBuilderAttribute(Type builderType, string methodName)
		{
			BuilderType = builderType;
			MethodName = methodName;
		}

		/// <summary>Gets the type of the builder to use to construct the collection.</summary>
		public Type BuilderType { get; }

		/// <summary>Gets the name of the method on the builder to use to construct the collection.</summary>
		/// <remarks>This should match the metadata name of the target method. For example, this might be ".ctor" if targeting the type's constructor.</remarks>
		public string MethodName { get; }
	}
}
#endif
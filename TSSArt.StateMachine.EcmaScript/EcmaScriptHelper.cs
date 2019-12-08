using System;
using System.Reflection;
using Jint;
using Jint.Native;
using Jint.Native.Array;
using Jint.Native.Object;
using Jint.Runtime;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;

namespace TSSArt.StateMachine.EcmaScript
{
	internal static class EcmaScriptHelper
	{
		public const string JintVersionPropertyName = "JintVersion";
		public const string InFunctionName          = "In";

		public static readonly  string             JintVersionValue                    = typeof(Engine).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
		private static readonly PropertyDescriptor ReadonlyUndefinedPropertyDescriptor = new PropertyDescriptor(JsValue.Undefined, writable: false, enumerable: false, configurable: false);

		public static PropertyDescriptor CreatePropertyAccessor(Engine engine, DataModelObject obj, string property)
		{
			if (obj.IsReadOnly && !obj.Contains(property))
			{
				return ReadonlyUndefinedPropertyDescriptor;
			}

			var jsGet = new DelegateWrapper(engine, new Func<JsValue>(Getter));
			var jsSet = new DelegateWrapper(engine, new Action<JsValue>(Setter));

			return new PropertyDescriptor(jsGet, jsSet, enumerable: true, configurable: false);

			JsValue Getter() => ConvertToJsValue(engine, obj[property]);

			void Setter(JsValue value) => obj[property] = ConvertFromJsValue(value);
		}

		public static PropertyDescriptor CreateArrayIndexAccessor(Engine engine, DataModelArray array, int index)
		{
			if (array.IsReadOnly && index >= array.Length)
			{
				return ReadonlyUndefinedPropertyDescriptor;
			}

			var jsGet = new DelegateWrapper(engine, new Func<JsValue>(Getter));
			var jsSet = new DelegateWrapper(engine, new Action<JsValue>(Setter));

			return new PropertyDescriptor(jsGet, jsSet, enumerable: true, configurable: false);

			JsValue Getter() => ConvertToJsValue(engine, array[index]);

			void Setter(JsValue value) => array[index] = ConvertFromJsValue(value);
		}

		public static JsValue ConvertToJsValue(Engine engine, DataModelValue value)
		{
			return value.Type switch
			{
					DataModelValueType.Null => JsValue.Null,
					DataModelValueType.Undefined => JsValue.Undefined,
					DataModelValueType.Boolean => new JsValue(value.AsBoolean()),
					DataModelValueType.String => new JsValue(value.AsString()),
					DataModelValueType.Number => new JsValue(value.AsNumber()),
					DataModelValueType.Object => new JsValue(new DataModelObjectWrapper(engine, value.AsObject())),
					DataModelValueType.Array => new JsValue(new DataModelArrayWrapper(engine, value.AsArray())),
					_ => throw new ArgumentOutOfRangeException(nameof(value), value.Type, Resources.Exception_UnsupportedValueType)
			};
		}

		public static DataModelValue ConvertFromJsValue(JsValue value)
		{
			return value.Type switch
			{
					Types.Null => DataModelValue.Null,
					Types.Undefined => DataModelValue.Undefined,
					Types.Boolean => new DataModelValue(value.AsBoolean()),
					Types.String => new DataModelValue(value.AsString()),
					Types.Number => new DataModelValue(value.AsNumber()),
					Types.Object => CreateDataModelValue(value.AsObject()),
					_ => throw new ArgumentOutOfRangeException(nameof(value), value.Type, Resources.Exception_UnsupportedValueType)
			};
		}

		private static DataModelValue CreateDataModelValue(ObjectInstance objectInstance)
		{
			switch (objectInstance)
			{
				case ArrayInstance array:
					var dataModelArray = new DataModelArray((int) array.GetLength());

					foreach (var pair in array.GetOwnProperties())
					{
						if (ArrayInstance.IsArrayIndex(pair.Key, out var index))
						{
							dataModelArray[(int) index] = ConvertFromJsValue(array.Get(pair.Key));
						}
					}

					return new DataModelValue(dataModelArray);

				default:
					var dataModelObject = new DataModelObject();

					foreach (var pair in objectInstance.GetOwnProperties())
					{
						dataModelObject[pair.Key] = ConvertFromJsValue(objectInstance.Get(pair.Key));
					}

					return new DataModelValue(dataModelObject);
			}
		}

		public static object GetAncestor<T>(in T ancestorProvider) where T : IAncestorProvider => ancestorProvider.Ancestor;
	}
}
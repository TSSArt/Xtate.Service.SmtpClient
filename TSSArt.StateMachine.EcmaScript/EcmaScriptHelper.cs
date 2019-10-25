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
			switch (value.Type)
			{
				case DataModelValueType.Null: return JsValue.Null;
				case DataModelValueType.Undefined: return JsValue.Undefined;
				case DataModelValueType.String: return new JsValue(value.AsString());
				case DataModelValueType.Number: return new JsValue(value.AsNumber());
				case DataModelValueType.Object: return new JsValue(new DataModelObjectWrapper(engine, value.AsObject()));
				case DataModelValueType.Array: return new JsValue(new DataModelArrayWrapper(engine, value.AsArray()));
				default: throw new ArgumentOutOfRangeException();
			}
		}

		public static DataModelValue ConvertFromJsValue(JsValue value)
		{
			switch (value.Type)
			{
				case Types.Null: return new DataModelValue((string) null);
				case Types.Undefined: return DataModelValue.Undefined();
				case Types.String: return new DataModelValue(value.AsString());
				case Types.Number: return new DataModelValue(value.AsNumber());
				case Types.Object: return CreateDataModelValue(value.AsObject());
				default: throw new ArgumentOutOfRangeException();
			}
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
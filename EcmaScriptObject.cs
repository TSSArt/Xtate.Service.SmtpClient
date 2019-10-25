using Jint.Native;

namespace TSSArt.StateMachine.EcmaScript
{
	internal class EcmaScriptObject : IObject
	{
		public EcmaScriptObject(JsValue jsValue) => JsValue = jsValue;

		public JsValue JsValue { get; }

		public object ToObject() => JsValue.ToObject();
	}
}
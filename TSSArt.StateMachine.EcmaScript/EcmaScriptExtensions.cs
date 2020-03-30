using System;

namespace TSSArt.StateMachine.EcmaScript
{
	public static class EcmaScriptExtensions
	{
		public static IoProcessorOptionsBuilder AddEcmaScript(this IoProcessorOptionsBuilder builder)
		{
			if (builder == null) throw new ArgumentNullException(nameof(builder));

			builder.AddDataModelHandlerFactory(EcmaScriptDataModelHandler.Factory);

			return builder;
		}
	}
}
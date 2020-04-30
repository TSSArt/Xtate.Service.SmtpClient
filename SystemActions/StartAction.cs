using System;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace TSSArt.StateMachine
{
	public class StartAction : ICustomActionExecutor
	{
		private const string Source     = "src";
		private const string SourceExpr = "srcexpr";
		private const string IdLocation = "idlocation";

		private readonly IExpressionEvaluator? _sourceExpression;
		private readonly Uri?                  _source;
		private readonly ILocationAssigner?    _idLocation;

		public StartAction(XmlReader xmlReader, ICustomActionContext access)
		{
			if (xmlReader == null) throw new ArgumentNullException(nameof(xmlReader));
			if (access == null) throw new ArgumentNullException(nameof(access));

			var source = xmlReader.GetAttribute(Source);
			var sourceExpression = xmlReader.GetAttribute(SourceExpr);
			var idLocation = xmlReader.GetAttribute(IdLocation);

			if (source == null && sourceExpression == null)
			{
				access.AddValidationError<StartAction>(Resources.ErrorMessage_At_least_one_source_must_be_specified);
			}

			if (source != null && sourceExpression != null)
			{
				access.AddValidationError<StartAction>(Resources.ErrorMessage_src_and_srcexpr_attributes_should_not_be_assigned_in_Start_element);
			}

			if (source != null && !Uri.TryCreate(source, UriKind.RelativeOrAbsolute, out _source))
			{
				access.AddValidationError<StartAction>(Resources.ErrorMessage_source__has_invalid_URI_format);
			}

			if (sourceExpression != null)
			{
				_sourceExpression = access.RegisterValueExpression(sourceExpression);
			}

			if (idLocation != null)
			{
				_idLocation = access.RegisterLocationExpression(idLocation);
			}
		}

		internal static void FillXmlNameTable(XmlNameTable xmlNameTable)
		{
			xmlNameTable.Add(Source);
			xmlNameTable.Add(SourceExpr);
			xmlNameTable.Add(IdLocation);
		}

		public async ValueTask Execute(IExecutionContext executionContext, CancellationToken token)
		{
			if (executionContext == null) throw new ArgumentNullException(nameof(executionContext));

			var host = GetHost(executionContext);
			var baseUri = GetBaseUri(executionContext);
			var source = await GetSource(executionContext, token).ConfigureAwait(false);

			if (source == null)
			{
				throw new StateMachineProcessorException(Resources.StartAction_Execute_Source_not_specified);
			}

			var sessionId = await host.StartStateMachine(new StateMachineOrigin(source, baseUri), DataModelValue.Undefined, token).ConfigureAwait(false);

			if (_idLocation != null)
			{
				await _idLocation.Assign(executionContext, sessionId, token).ConfigureAwait(false);
			}
		}

		private static Uri? GetBaseUri(IExecutionContext executionContext)
		{
			var val = executionContext.DataModel["_x"].AsObjectOrEmpty()["host"].AsObjectOrEmpty()["location"].AsStringOrDefault();

			return val != null ? new Uri(val) : null;
		}

		private static IHost GetHost(IExecutionContext executionContext)
		{
			if (executionContext.RuntimeItems[typeof(IHost)] is IHost host)
			{
				return host;
			}

			throw new StateMachineProcessorException(Resources.Exception_Can_t_get_access_to_IHost_interface);
		}

		private async ValueTask<Uri?> GetSource(IExecutionContext executionContext, CancellationToken token)
		{
			if (_source != null)
			{
				return _source;
			}

			if (_sourceExpression != null)
			{
				var val = await _sourceExpression.Evaluate(executionContext, token).ConfigureAwait(false);

				return new Uri(val.AsString(), UriKind.RelativeOrAbsolute);
			}

			return null;
		}
	}
}
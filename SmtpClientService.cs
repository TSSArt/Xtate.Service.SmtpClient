using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace TSSArt.StateMachine.Services
{
	[SimpleService("http://tssart.com/scxml/service/#SMTPClient", Alias = "smtp")]
	public class SmtpClientService : SimpleServiceBase
	{
		public static readonly IServiceFactory Factory = SimpleServiceFactory<SmtpClientService>.Instance;

		private static string GetString(DataModelValue val, string key, string defaultValue = null)
		{
			var dataModelValue = val.AsObject()[key];

			return dataModelValue.Type != DataModelValueType.Undefined ? dataModelValue.AsString() : defaultValue;
		}

		private static int GetInt32(DataModelValue val, string key, int defaultValue = 0)
		{
			var dataModelValue = val.AsObject()[key];

			return dataModelValue.Type != DataModelValueType.Undefined ? (int) dataModelValue.AsNumber() : defaultValue;
		}

		protected override async ValueTask<DataModelValue> Execute()
		{
			var host = GetString(Parameters, key: "server");
			var port = GetInt32(Parameters, key: "port", defaultValue: 25);
			using var client = new SmtpClient(host, port);

			var fromEmail = GetString(Parameters, key: "from");
			var fromName = GetString(Parameters, key: "fromName");
			var from = fromName != null ? new MailAddress(fromEmail, fromName, Encoding.UTF8) : new MailAddress(fromEmail);

			var toEmail = GetString(Parameters, key: "to");
			var toName = GetString(Parameters, key: "toName");
			var to = toName != null ? new MailAddress(toEmail, toName, Encoding.UTF8) : new MailAddress(toEmail);

			var textBody = GetString(Parameters, key: "body");
			var htmlBody = GetString(Parameters, key: "htmlBody");

			using var message = new MailMessage(from, to)
								{
										Body = htmlBody ?? textBody,
										IsBodyHtml = htmlBody != null,
										BodyEncoding = Encoding.UTF8,
										Subject = GetString(Parameters, key: "subject"),
										SubjectEncoding = Encoding.UTF8
								};

			await client.SendMailAsync(message).ConfigureAwait(false);

			return new DataModelValue("OK");
		}
	}
}
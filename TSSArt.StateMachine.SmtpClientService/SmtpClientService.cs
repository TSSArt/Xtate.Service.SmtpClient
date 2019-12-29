using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace TSSArt.StateMachine.Services
{
	[SimpleService("http://tssart.com/scxml/service/#SMTPClient", Alias = "smtp")]
	public class SmtpClientService : SimpleServiceBase
	{
		public static readonly IServiceFactory Factory = SimpleServiceFactory<SmtpClientService>.Instance;

		protected override async ValueTask<DataModelValue> Execute()
		{
            var parameters = Parameters.AsObjectOrEmpty();
            var host = parameters["server"].AsString();
			var port = (int?)parameters["port"].AsNumberOrDefault() ?? 25;
			using var client = new SmtpClient(host, port);

            var fromEmail = parameters["from"].AsStringOrDefault(); 
			var fromName = parameters["fromName"].AsStringOrDefault();
			var from = fromName != null ? new MailAddress(fromEmail, fromName, Encoding.UTF8) : new MailAddress(fromEmail);

			var toEmail = parameters["to"].AsStringOrDefault();
			var toName = parameters["toName"].AsStringOrDefault();
			var to = toName != null ? new MailAddress(toEmail, toName, Encoding.UTF8) : new MailAddress(toEmail);

			var textBody = parameters["body"].AsStringOrDefault();
			var htmlBody = parameters["htmlBody"].AsStringOrDefault();

			using var message = new MailMessage(from, to)
								{
										Body = htmlBody ?? textBody,
										IsBodyHtml = htmlBody != null,
										BodyEncoding = Encoding.UTF8,
										Subject = parameters["subject"].AsStringOrDefault(),
										SubjectEncoding = Encoding.UTF8
								};

			await client.SendMailAsync(message).ConfigureAwait(false);

			return default;
		}
	}
}
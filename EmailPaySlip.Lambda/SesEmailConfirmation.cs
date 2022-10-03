using Amazon.SimpleEmailV2;
using MimeKit;

namespace EmailPaySlip.Lambda;

public class SesEmailConfirmation : IEmailConfirmation
{
	public async Task SendConfirmationEmail(string messageId, string subject, DateTime payPeriod,
		string baseHourly,
		string annualLeave)
	{
		var msg = new MimeMessage
		{
			From =
			{
				new MailboxAddress(Environment.GetEnvironmentVariable("FromName"),
					Environment.GetEnvironmentVariable("FromAddress"))
			},
			Date = DateTime.UtcNow,
			Subject = subject,
			To =
			{
				new MailboxAddress(Environment.GetEnvironmentVariable("ToName"),
					Environment.GetEnvironmentVariable("ToAddress"))
			},
			References = {messageId},
			Body = new TextPart
			{
				Text = $"PayPeriod: {payPeriod:dd/MM/yyyy}\nBaseHourly: {baseHourly}\nAnnualLeave: {annualLeave}"
			}
		};
		var msgStream = new MemoryStream();
		await msg.WriteToAsync(msgStream);
		msgStream.Seek(0, SeekOrigin.Begin);
		var ses = new AmazonSimpleEmailServiceV2Client();
		await ses.SendEmailAsync(new()
		{
			FromEmailAddress = Environment.GetEnvironmentVariable("FromAddress"),
			Destination = new()
			{
				ToAddresses = new() {Environment.GetEnvironmentVariable("ToAddress")}
			},
			Content = new()
			{
				Raw = new()
				{
					Data = msgStream
				}
			}
		});
	}
}
using MimeKit;

namespace EmailPaySlip.Lambda;

public interface IEmailConfirmation
{
	Task SendConfirmationEmail(string messageId, string subject, DateTime payPeriod,
		string baseHourly,
		string annualLeave);
}
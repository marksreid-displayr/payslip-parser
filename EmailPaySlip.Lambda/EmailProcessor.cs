using Amazon.Lambda.S3Events;
using Microsoft.Extensions.Options;

namespace EmailPaySlip.Lambda;

public class EmailProcessor : IEmailProcessor
{
	private IEmailExtractor EmailExtractor { get; }
	private ISavePayslip SavePayslip { get; }
	private IEmailConfirmation EmailConfirmation { get; }
	private ISavePDF SavePDF { get; }
	private ICompleteEmailProcessing CompleteEmailProcessing { get; }
	private EmailProcessorOptions EmailProcessingOptions { get; }

	public EmailProcessor(IEmailExtractor emailExtractor, ISavePayslip savePayslip, IEmailConfirmation emailConfirmation, ISavePDF savePDF, ICompleteEmailProcessing completeEmailProcessing, IOptions<EmailProcessorOptions> emailProcesingOptions)
	{
		EmailExtractor = emailExtractor;
		SavePayslip = savePayslip;
		EmailConfirmation = emailConfirmation;
		SavePDF = savePDF;
		CompleteEmailProcessing = completeEmailProcessing;
		EmailProcessingOptions = emailProcesingOptions.Value;
	}

	public async Task ProcessPayslip(S3Event s3Event)
	{
		var payslip = await EmailExtractor.ExtractPayslipFromEmail(s3Event);
		await SavePayslip.SavePayslipRecord(payslip.PayPeriodStart, payslip.PayPeriodEnd, payslip.BaseHourly!, payslip.AnnualLeave!, payslip.Company!);
		await EmailConfirmation.SendConfirmationEmail(payslip.MessageId!, payslip.Subject!, payslip.PayPeriodStart, payslip.BaseHourly!, payslip.AnnualLeave!);
		await SavePDF.Save(payslip.PDFData!, EmailProcessingOptions.Company!, payslip.PayPeriodStart);
		await CompleteEmailProcessing.Process(s3Event);
	}
}
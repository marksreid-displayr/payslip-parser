using Amazon.Lambda.S3Events;

namespace EmailPaySlip.Lambda;

public interface IEmailExtractor
{
	Task<PaySlip> ExtractPayslipFromEmail(S3Event s3Event);
}
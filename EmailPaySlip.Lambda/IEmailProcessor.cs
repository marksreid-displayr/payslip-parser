using Amazon.Lambda.S3Events;

namespace EmailPaySlip.Lambda;

public interface IEmailProcessor
{
	Task ProcessPayslip(S3Event s3Event);
}
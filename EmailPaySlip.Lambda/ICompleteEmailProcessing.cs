using Amazon.Lambda.S3Events;

namespace EmailPaySlip.Lambda;

public interface ICompleteEmailProcessing
{
	Task Process(S3Event s3event);
}
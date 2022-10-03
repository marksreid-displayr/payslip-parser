using Amazon.Lambda.S3Events;
using Amazon.S3;

namespace EmailPaySlip.Lambda;

public class CompleteEmailProcessing : ICompleteEmailProcessing
{
	private AmazonS3Client S3Client { get; }

	public CompleteEmailProcessing(AmazonS3Client s3Client)
	{
		S3Client = s3Client;
	}

	public async Task Process(S3Event s3event)
	{
		var src = s3event.Records.FirstOrDefault(r => r.EventName == EventType.ObjectCreatedPut)!;
		await S3Client.DeleteObjectAsync(new() 
		{
			BucketName = src.S3.Bucket.Name,
			Key = src.S3.Object.Key
		});
	}
}
using Amazon.S3;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EmailPaySlip.Lambda;

public class SavePDF : ISavePDF
{
	private AmazonS3Client S3Client { get; }
	public ILogger<SavePDF> Logger { get; }
	private string? Bucket { get; }
	private string? Key { get; }

	public SavePDF(AmazonS3Client s3Client, IOptions<SavePdfOptions> options, ILogger<SavePDF> logger)
	{
		S3Client = s3Client;
		Logger = logger;
		Bucket = options.Value.Bucket;
		Key = options.Value.Key;
	}

	public async Task Save(byte[] pdfData, string company, DateTime payPeriodStart)
	{
		await using var ms = new MemoryStream(pdfData);
		var key = Key ?? "";
		if (key.Length > 0)
		{
			key += "/";
		}
		
		var pdfKey = key + company + "/" + payPeriodStart.ToString("yyyyMMdd") + ".pdf";
		Logger.LogDebug("Bucket {Bucket} Key {Key}", Bucket, pdfKey);

		await S3Client.PutObjectAsync(new()
		{
			ContentType = "application/pdf",
			BucketName = Bucket,
			Key = pdfKey,
			InputStream = ms,
		});
	}
}
using Amazon.S3;
using Microsoft.Extensions.Options;

namespace EmailPaySlip.Lambda;

public class SavePDF : ISavePDF
{
	private AmazonS3Client S3Client { get; }
	private string? Bucket { get; }
	private string? Key { get; }

	public SavePDF(AmazonS3Client s3Client, IOptions<SavePdfOptions> options)
	{
		S3Client = s3Client;
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
		
		await S3Client.PutObjectAsync(new()
		{
			ContentType = "application/pdf",
			BucketName = Bucket,
			Key = key + company + "/" + payPeriodStart.ToString("yyyyMMdd") + ".pdf",
			InputStream = ms,
		});
	}
}
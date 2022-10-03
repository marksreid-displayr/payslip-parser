using System.Globalization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.Textract;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace EmailPaySlip.Lambda;

public class TourismHoldingsEmailExtractor : IEmailExtractor
{
	private ILogger<TourismHoldingsEmailExtractor> Logger { get; }
	private AmazonS3Client S3Client { get; }
	private AmazonTextractClient TextractClient { get; }

	public TourismHoldingsEmailExtractor(ILogger<TourismHoldingsEmailExtractor> logger, AmazonS3Client s3Client,
		AmazonTextractClient textractClient)
	{
		Logger = logger;
		S3Client = s3Client;
		TextractClient = textractClient;
	}

	public async Task<PaySlip> ExtractPayslipFromEmail(S3Event s3Event)
	{
		Logger.LogInformation("Received event {@Event}", s3Event);
		foreach (var r in s3Event.Records)
		{
			if (r.EventName != EventType.ObjectCreatedPut) continue;
			var obj = await S3Client.GetObjectAsync(new()
			{
				BucketName = r.S3.Bucket.Name,
				Key = r.S3.Object.Key
			});
			var mimeMsg = await MimeMessage.LoadAsync(obj.ResponseStream);
			foreach (var a in mimeMsg.Attachments)
			{
				await using var ms = new MemoryStream();

				switch (a)
				{
					case MessagePart part:
					{
						var filename = part.ContentDisposition?.FileName;
						if (Path.GetExtension(filename) != ".pdf") continue;
						await part.Message.WriteToAsync(ms);
						break;
					}
					case MimePart part:
					{
						var filename = part.FileName;
						if (Path.GetExtension(filename) != ".pdf") continue;
						await part.Content.DecodeToAsync(ms);
						break;
					}
				}

				ms.Seek(0, SeekOrigin.Begin);
				var textResults = await TextractClient.DetectDocumentTextAsync(new()
				{
					Document = new()
					{
						Bytes = ms
					}
				});

				var l = textResults.Blocks.ToList();
				Logger.LogInformation("Results {@Results}", textResults);
				var payPeriodIndex = l.FindIndex(_ => _.Text == "Pay Period From:");
				if (payPeriodIndex == -1)
				{
					payPeriodIndex = l.FindIndex(_ => _.Text == "From:");
				}

				var payPeriod = DateTime.ParseExact(l[payPeriodIndex + 1].Text,
					new[] {"d/M/yyyy"}, CultureInfo.InvariantCulture);
				var baseHourlyIndex = l.FindIndex(_ => _.Text == "Base Hourly");
				var baseHourly = baseHourlyIndex == -1 ? "0" : l[baseHourlyIndex + 1].Text;
				if (baseHourly?.FirstOrDefault() == '$')
				{
					baseHourly = "0";
				}

				var annualLeaveIndex = l.FindIndex(_ => _.Text == "Annual Leave Pay");
				var annualLeave = annualLeaveIndex == -1 ? "0" : l[annualLeaveIndex + 1].Text;
				if (annualLeave?.FirstOrDefault() == '$')
				{
					annualLeave = "0";
				}

				Logger.LogInformation("{PayPeriod} {BaseHourly} {AnnualLeave}", payPeriod, baseHourly, annualLeave);
				return new()
				{
					Subject = mimeMsg.Subject,
					MessageId = mimeMsg.MessageId,
					AnnualLeave = annualLeave!,
					BaseHourly = baseHourly!,
					PayPeriodStart = payPeriod,
					Company = "Tourism Holdings"
				};
			}
		}

		throw new("Could not extract payslip details");
	}
}
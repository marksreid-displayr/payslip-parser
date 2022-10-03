using System.Globalization;
using System.Text.RegularExpressions;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.Textract;
using Microsoft.Extensions.Logging;
using MimeKit;
using EventType = Amazon.S3.EventType;

namespace EmailPaySlip.Lambda;

public class DisplayrEmailExtractor : IEmailExtractor
{
	private ILogger<DisplayrEmailExtractor> Logger { get; }
	private AmazonS3Client S3Client { get; }
	private AmazonTextractClient TextractClient { get; }

	public DisplayrEmailExtractor(ILogger<DisplayrEmailExtractor> logger, AmazonS3Client s3Client,
		AmazonTextractClient textractClient)
	{
		Logger = logger;
		S3Client = s3Client;
		TextractClient = textractClient;
	}


	public async Task<PaySlip> ExtractPayslipFromEmail(S3Event s3Event)
	{
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

				var payPeriodText = l.Find(_ => _.Text?.StartsWith("Pay Period: ") ?? false);
				var m = Regex.Match(payPeriodText!.Text,
					@"^Pay Period: (?<start>\d{2}\/\d{2}\/\d{4}) - (?<end>\d{2}\/\d{2}\/\d{4})");

				if (!m.Success) continue;
				var payPeriod = DateTime.ParseExact(m.Groups["start"].Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
				var payPeriodEnd =
					DateTime.ParseExact(m.Groups["end"].Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
				var baseHourly = "0";
				for (var i = 0; i < l.Count; ++i)
				{
					if (l[i].Text != "Ordinary" || l[i + 1].Text != "Hours") continue;
					baseHourly = l[i + 2].Text;
					break;
				}

				var annualLeave = "0";
				for (var i = 0; i < l.Count; ++i)
				{
					if (l[i].Text != "Annual" || l[i + 1].Text != "Leave" || l[i + 2].Text == "in") continue;
					annualLeave = l[i + 2].Text;
					break;
				}

				Logger.LogInformation("{PayPeriod} {BaseHourly} {AnnualLeave} ", payPeriod, baseHourly, annualLeave);
				return new()
				{
					Subject = mimeMsg.Subject,
					MessageId = mimeMsg.MessageId,
					AnnualLeave = annualLeave,
					BaseHourly = baseHourly,
					PayPeriodStart = payPeriod,
					PayPeriodEnd = payPeriodEnd,
					Company = "Displayr"
				};
				//await SavePayslip.SavePayslipRecord(payPeriod, payPeriodEnd, baseHourly, annualLeave);
				//await EmailConfirmation.SendConfirmationEmail(mimeMsg.MessageId, mimeMsg.Subject, payPeriod, baseHourly, annualLeave);
			}
		}

		throw new("Could not extract payslip details");
	}
}
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.S3Events;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.S3;
using Amazon.Textract;
using EmailPaySlip.Lambda;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration()
	.WriteTo.Console(new CompactJsonFormatter())
	.CreateBootstrapLogger();

var host = Host.CreateDefaultBuilder()
	.ConfigureServices(_ =>
	{
		_.AddSingleton<AmazonTextractClient>();
		_.AddSingleton<AmazonS3Client>();
		_.AddSingleton<ISavePayslip, SavePayslipDynamoDb>();
		_.AddSingleton<IEmailConfirmation, SesEmailConfirmation>();
		_.Configure<SavePdfOptions>(options =>
		{
			options.Bucket = Environment.GetEnvironmentVariable("SavePDFBucket");
			options.Key = Environment.GetEnvironmentVariable("SavePDFKey");
		});
		var company = Environment.GetEnvironmentVariable("Company");
		_.Configure<EmailProcessorOptions>(options =>
		{
			options.Company = company!;
		});
		_.AddSingleton<ISavePDF, SavePDF>();
		_.AddSingleton<ICompleteEmailProcessing, CompleteEmailProcessing>();
		_.AddSingleton<IEmailProcessor, EmailProcessor>();
		if (company == "Displayr")
		{
			_.AddSingleton<IEmailExtractor,DisplayrEmailExtractor>();
		}
		else
		{
			_.AddSingleton<IEmailExtractor,TourismHoldingsEmailExtractor>();
		}
	})
	.UseSerilog((c, l) =>
	{
		l.WriteTo.Console(new CompactJsonFormatter())
			.ReadFrom.Configuration(c.Configuration);
	}).Build();

// The function handler that will be called for each Lambda event
// ReSharper disable once ConvertToLocalFunction
var handler = async (S3Event s3Event, ILambdaContext context) =>
{
	var emailProcessor = host.Services.GetService<IEmailProcessor>();
	await emailProcessor!.ProcessPayslip(s3Event);
};

// Build the Lambda runtime client passing in the handler to call for each
// event and the JSON serializer to use for translating Lambda JSON documents
// to .NET types.
await LambdaBootstrapBuilder.Create(handler, new DefaultLambdaJsonSerializer())
		.Build()
		.RunAsync();
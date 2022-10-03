using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amazon.CDK;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Lambda.EventSources;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.S3.Notifications;
using Amazon.CDK.AWS.SES;
using Amazon.CDK.AWS.SES.Actions;
using Constructs;

// ReSharper disable VirtualMemberCallInConstructor

namespace EmailPaySlipCdk
{
	public class EmailPaySlipCdkStack : Stack
	{
		internal EmailPaySlipCdkStack(Construct scope, string id, IStackProps props, Config config) : base(scope, id,
			props)
		{
			var s3 = new Bucket(this, "payslips-bucket", new BucketProps
			{
				BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
				Encryption = BucketEncryption.S3_MANAGED,
				RemovalPolicy = RemovalPolicy.DESTROY
			});

			s3.AddToResourcePolicy(new PolicyStatement(new PolicyStatementProps
			{
				Principals = new IPrincipal[] {new ServicePrincipal("ses.amazonaws.com")},
				Conditions = new Dictionary<string, object>
				{
					{
						"StringEquals", new Dictionary<string, object>
						{
							{"AWS:SourceAccount", Account}
						}
					}
				},
				Actions = new[] {"s3:PutObject"},
				Effect = Effect.ALLOW,
				Resources = new[] {s3.BucketArn + "/*"}
			}));

			var table = new Table(this, "payslip-dynamo", new TableProps
			{
				PartitionKey = new Attribute
				{
					Name = "Date",
					Type = AttributeType.NUMBER
				},
				SortKey = new Attribute
				{
					Name = "Company",
					Type = AttributeType.STRING
				}
			});

			var policy = new ManagedPolicy(this, "PayslipLambdaPolicy", new ManagedPolicyProps
			{
				Statements = new[]
				{
					new PolicyStatement(new PolicyStatementProps
					{
						Actions = new[] {"s3:GetObject", "s3:PutObject"},
						Effect = Effect.ALLOW,
						Resources = new[] {s3.BucketArn + "/*"}
					}),
					new PolicyStatement(new PolicyStatementProps
					{
						Actions = new[] {"textract:DetectDocumentText"},
						Effect = Effect.ALLOW,
						Resources = new[] {"*"}
					}),
					new PolicyStatement(new PolicyStatementProps
					{
						Actions = new[] {"dynamodb:PutItem"},
						Effect = Effect.ALLOW,
						Resources = new[] {table.TableArn}
					}),
					new PolicyStatement(new PolicyStatementProps
					{
						Effect = Effect.ALLOW,
						Actions = new[] {"ses:SendEmail", "ses:SendRawEmail"},
						Resources = config.EmailIdentities
							.Select(_ => _.Replace("{Region}", Region))
							.Select(_ => _.Replace("{Account}", Account))
							.ToArray()
					})
				}
			});

			var lambdaRole = new Role(this, "LambdaRole", new RoleProps
			{
				AssumedBy = new ServicePrincipal("lambda.amazonaws.com"),
				ManagedPolicies = new[]
				{
					policy,
					ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSLambdaBasicExecutionRole")
				}
			});

			var function = new Function(this, "EmailFunction", new FunctionProps
			{
				Code = Code.FromAsset(Path.Join("..", "EmailPaySlip.Lambda", "bin", "release", "net6.0")),
				Runtime = Runtime.DOTNET_6,
				Handler = "EmailPaySlip.Lambda",
				Timeout = Duration.Minutes(1),
				Role = lambdaRole
			});

			s3.AddEventNotification(EventType.OBJECT_CREATED, new LambdaDestination(function), new NotificationKeyFilter
			{
				Prefix = "TourismHoldings/Incoming/"
			});

			var displayrFunction = new Function(this, "DisplayrEmailFunction", new FunctionProps
			{
				Code = Code.FromAsset(Path.Join("..", "EmailPaySlip.Lambda", "bin", "Release", "net6.0")),
				Runtime = Runtime.DOTNET_6,
				Handler = "EmailPaySlip.Lambda",
				Timeout = Duration.Minutes(1),
				Environment = new Dictionary<string, string>
				{
					{"Company", "Displayr"},
					{"TableName", table.TableName},
					{"FromName", config.FromAddress.Name},
					{"FromAddress", config.FromAddress.Address},
					{"ToName", config.ToAddress.Name},
					{"ToAddress", config.ToAddress.Address},
					{"SavePDFBucket", string.IsNullOrWhiteSpace(config.SavePDF.Bucket) ? s3.BucketName : config.SavePDF.Bucket},
					{"SavePDFKey", config.SavePDF.Key}
				},
				Role = lambdaRole
			});

			s3.AddEventNotification(EventType.OBJECT_CREATED, new LambdaDestination(displayrFunction),
				new NotificationKeyFilter
				{
					Prefix = "Displayr/Incoming/"
				});

			var ses = ReceiptRuleSet.FromReceiptRuleSetName(this, "default-rule-set", "default-rule-set");
			ses.AddRule("payslips", new ReceiptRuleOptions
			{
				ReceiptRuleName = "DisplayrPaySlips",
				Enabled = true,
				ScanEnabled = true,
				Recipients = new[] {"displayrpayslips@payslips.marksreid.com"},
				TlsPolicy = TlsPolicy.REQUIRE,
				Actions = new IReceiptRuleAction[]
				{
					new S3(new S3Props
					{
						Bucket = s3,
						ObjectKeyPrefix = "Displayr/Incoming/"
					})
				}
			});
		}
	}
}
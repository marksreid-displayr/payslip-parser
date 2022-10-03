using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace EmailPaySlip.Lambda;

public class SavePayslipDynamoDb : ISavePayslip
{
	public async Task SavePayslipRecord(DateTime payPeriod, DateTime payPeriodEnd, string baseHourly,
		string annualLeave, string company)
	{
		var dynamodbClient = new AmazonDynamoDBClient();
		await dynamodbClient.PutItemAsync(new()
		{
			TableName = Environment
				.GetEnvironmentVariable("TableName"), // "EmailPaySlipCdkStack-payslipdynamo580D16DA-BDIYEBGOZKUI",
			Item = new()
			{
				{
					"Date", new()
					{
						N = int.Parse(payPeriod.ToString("yyyyMMdd")).ToString()
					}
				},
				{
					"EndDate", new()
					{
						N = int.Parse(payPeriodEnd.ToString("yyyyMMdd")).ToString()
					}
				},
				{
					"Company", new()
					{
						S = company
					}
				},
				{
					"BaseHourly", new()
					{
						N = baseHourly
					}
				},
				{
					"AnnualLeave", new AttributeValue
					{
						N = annualLeave
					}
				}
			}
		});
	}	
}
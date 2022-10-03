namespace EmailPaySlip.Lambda;

public interface ISavePayslip
{
	Task SavePayslipRecord(DateTime payPeriod, DateTime payPeriodEnd, string baseHourly,
		string annualLeave, string company);
}
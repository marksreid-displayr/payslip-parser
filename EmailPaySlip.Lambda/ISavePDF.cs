namespace EmailPaySlip.Lambda;

public interface ISavePDF
{
	Task Save(byte[] pdfData, string company, DateTime payPeriodStart);
}
namespace EmailPaySlip.Lambda;

public record PaySlip
{
	public DateTime PayPeriodStart { get; init; }
	public DateTime PayPeriodEnd { get; init; }
	public string? AnnualLeave { get; init; }
	public string? BaseHourly { get; init; }
	public byte[]? PDFData { get; init; }
	public string? MessageId { get; init; }
	public string? Subject { get; init; }
	public string? Company { get; init; }
}
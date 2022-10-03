namespace EmailPaySlip.Lambda;

public class SavePdfOptions
{
	public string? Bucket { get; set; }
	public string? Key { get; set; }
}
namespace ISO11820.Models;

public class ProductMaster
{
    public string ProductId { get; set; } = string.Empty;
    public string TestId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Specification { get; set; } = string.Empty;
    public double Height { get; set; }
    public double Diameter { get; set; }
}

namespace Software.Api.Vendors;

public class VendorEntity
{
    public Guid Id { get; set; }
    public required string Name { get; init; }
    public required string Url { get; init; }
    public required VendorPointOfContact PointOfContact { get; init; }
    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}
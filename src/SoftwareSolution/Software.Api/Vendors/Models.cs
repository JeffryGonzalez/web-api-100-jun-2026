using System.ComponentModel.DataAnnotations;

namespace Software.Api.Vendors;

public record VendorDetailsModel
{
    public Guid Id { get; set; }
    public required string Name { get; init; }
    public required string Url { get; init; }
    public required VendorPointOfContact PointOfContact { get; init; }
}

public record VendorPointOfContact
{
    public required string Name { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init;  } = string.Empty;
}

public record VendorCreateModel
{
    [MinLength(5), MaxLength(100)]
    public required string Name { get; init;  }
    [Url]
    public required string Url { get; init; }
    public required VendorPointOfContact PointOfContact { get; init; } 
}

public record VendorSummaryModel
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Url { get; init; }
}

public record VendorSummary
{
    public required IReadOnlyList<VendorSummaryModel> Vendors { get; init; }
    public required int Page  { get; init; }
    public required int PageSize { get; init; } 
}
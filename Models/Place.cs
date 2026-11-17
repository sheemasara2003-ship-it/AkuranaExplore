using System.ComponentModel.DataAnnotations;

namespace AkuranaExplore.Models;

public class Place
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public int CategoryId { get; set; }

    public Category? Category { get; set; }

    [Required, StringLength(240)]
    public string ShortDescription { get; set; } = string.Empty;

    [Required, StringLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Address { get; set; } = string.Empty;

    [Required, StringLength(1000)]
    public string TravelTips { get; set; } = string.Empty;

    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }

    [Range(0, 25, ErrorMessage = "Distance must be within the 25 km study area around Akurana.")]
    [Display(Name = "Distance from Akurana (km)")]
    public double DistanceFromAkuranaKm { get; set; }

    [Required, StringLength(120)]
    public string OpeningHours { get; set; } = "Open daily";

    [Range(0, 100000)]
    [Display(Name = "Entry fee (LKR)")]
    public decimal EntryFeeLkr { get; set; }

    [Range(15, 1440)]
    [Display(Name = "Suggested visit duration (minutes)")]
    public int SuggestedDurationMinutes { get; set; } = 60;

    [Range(0, 5)]
    public double Rating { get; set; } = 4.5;

    public bool IsFeatured { get; set; }

    [StringLength(200)]
    [Display(Name = "Image path")]
    public string? ImageUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string EntryFeeLabel => EntryFeeLkr <= 0 ? "Free" : $"LKR {EntryFeeLkr:N0}";

    public string DurationLabel => SuggestedDurationMinutes < 60
        ? $"{SuggestedDurationMinutes} min"
        : SuggestedDurationMinutes % 60 == 0
            ? $"{SuggestedDurationMinutes / 60} hr"
            : $"{SuggestedDurationMinutes / 60} hr {SuggestedDurationMinutes % 60} min";

    public string DistanceLabel => $"{DistanceFromAkuranaKm:0.#} km from Akurana";

    public string ImagePath => string.IsNullOrWhiteSpace(ImageUrl)
        ? (Category?.ImageUrl ?? "/images/nature.jpg")
        : ImageUrl.StartsWith("/") || ImageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? ImageUrl
            : "/" + ImageUrl;

    public string MapUrl => $"https://www.openstreetmap.org/?mlat={Latitude.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture)}&mlon={Longitude.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture)}#map=15/{Latitude.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture)}/{Longitude.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture)}";

    public string DirectionsUrl => $"https://www.google.com/maps/dir/?api=1&destination={Latitude.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture)},{Longitude.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture)}";

    /// <summary>Great-circle distance in kilometres between two coordinates.</summary>
    public static double DistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371.0;
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2))
                * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return earthRadiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    /// <summary>Rough driving time assuming an average of 25 km/h on local roads.</summary>
    public static int DrivingMinutes(double distanceKm)
    {
        if (distanceKm <= 0) return 0;
        return Math.Max(3, (int)Math.Round(distanceKm / 25.0 * 60));
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}

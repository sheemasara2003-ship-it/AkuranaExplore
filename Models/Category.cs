using System.ComponentModel.DataAnnotations;

namespace AkuranaExplore.Models;

public class Category
{
    public int Id { get; set; }

    [Required, StringLength(60)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(60)]
    public string Slug { get; set; } = string.Empty;

    [Required, StringLength(12)]
    public string Icon { get; set; } = "📍";

    [StringLength(200)]
    public string Blurb { get; set; } = string.Empty;

    [StringLength(200)]
    public string ImageUrl { get; set; } = "/images/nature.jpg";

    public int SortOrder { get; set; }

    public ICollection<Place> Places { get; set; } = new List<Place>();
}

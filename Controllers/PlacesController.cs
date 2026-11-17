using AkuranaExplore.Data;
using AkuranaExplore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AkuranaExplore.Controllers;

public class PlacesController(ApplicationDbContext db) : Controller
{
    private readonly ApplicationDbContext _db = db;

    public async Task<IActionResult> Index(string? q, string? category, string? sort)
    {
        var query = _db.Places
            .Include(p => p.Category)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(p =>
                p.Name.Contains(term) ||
                p.ShortDescription.Contains(term) ||
                p.Description.Contains(term) ||
                p.Address.Contains(term) ||
                (p.Category != null && p.Category.Name.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(p => p.Category != null && p.Category.Slug == category);
        }

        query = sort switch
        {
            "name" => query.OrderBy(p => p.Name),
            "distance" => query.OrderBy(p => p.DistanceFromAkuranaKm).ThenBy(p => p.Name),
            "duration" => query.OrderBy(p => p.SuggestedDurationMinutes).ThenBy(p => p.Name),
            _ => query.OrderByDescending(p => p.IsFeatured).ThenByDescending(p => p.Rating).ThenBy(p => p.Name)
        };

        var places = await query.ToListAsync();

        var categories = await _db.Categories
            .AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

        var categoryCounts = await _db.Places
            .AsNoTracking()
            .GroupBy(p => p.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count);

        var model = new PlacesIndexViewModel
        {
            Places = places,
            Categories = categories,
            CategoryCounts = categoryCounts,
            Query = q?.Trim() ?? string.Empty,
            CategorySlug = category ?? string.Empty,
            Sort = string.IsNullOrWhiteSpace(sort) ? "recommended" : sort,
            TotalPlaces = await _db.Places.CountAsync(),
            PlanIds = PlanSession.Get(HttpContext.Session).ToHashSet()
        };

        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var place = await _db.Places
            .Include(p => p.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (place is null)
        {
            return NotFound();
        }

        var others = await _db.Places
            .Include(p => p.Category)
            .AsNoTracking()
            .Where(p => p.Id != id)
            .ToListAsync();

        var nearby = others
            .Select(p => new NearbyPlace(p, Place.DistanceKm(place.Latitude, place.Longitude, p.Latitude, p.Longitude)))
            .OrderBy(x => x.DistanceKm)
            .Take(3)
            .ToList();

        var model = new PlaceDetailsViewModel
        {
            Place = place,
            Nearby = nearby,
            IsInPlan = PlanSession.Get(HttpContext.Session).Contains(place.Id),
            PlanCount = PlanSession.Get(HttpContext.Session).Count
        };

        return View(model);
    }
}

public class PlacesIndexViewModel
{
    public IReadOnlyList<Place> Places { get; set; } = [];
    public IReadOnlyList<Category> Categories { get; set; } = [];
    public Dictionary<int, int> CategoryCounts { get; set; } = [];
    public string Query { get; set; } = string.Empty;
    public string CategorySlug { get; set; } = string.Empty;
    public string Sort { get; set; } = "recommended";
    public int TotalPlaces { get; set; }
    public HashSet<int> PlanIds { get; set; } = [];
}

public class PlaceDetailsViewModel
{
    public Place Place { get; set; } = new();
    public IReadOnlyList<NearbyPlace> Nearby { get; set; } = [];
    public bool IsInPlan { get; set; }
    public int PlanCount { get; set; }
}

public record NearbyPlace(Place Place, double DistanceKm)
{
    public string DistanceLabel => $"{DistanceKm:0.#} km away";
}

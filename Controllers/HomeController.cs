using System.Diagnostics;
using AkuranaExplore.Data;
using AkuranaExplore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AkuranaExplore.Controllers;

public class HomeController(ApplicationDbContext db) : Controller
{
    private readonly ApplicationDbContext _db = db;

    public async Task<IActionResult> Index()
    {
        var places = await _db.Places
            .Include(p => p.Category)
            .AsNoTracking()
            .ToListAsync();

        var categories = await _db.Categories
            .Include(c => c.Places)
            .AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

        var featured = places
            .Where(p => p.IsFeatured)
            .OrderByDescending(p => p.Rating)
            .ThenBy(p => p.Name)
            .Take(6)
            .ToList();

        if (featured.Count < 3)
        {
            featured = places.OrderByDescending(p => p.Rating).Take(6).ToList();
        }

        var model = new HomeViewModel
        {
            Featured = featured,
            Categories = categories,
            PlaceCount = places.Count,
            CategoryCount = categories.Count,
            AverageRating = places.Count > 0 ? Math.Round(places.Average(p => p.Rating), 1) : 0,
            PlanCount = PlanSession.Get(HttpContext.Session).Count
        };

        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult StatusCodePage(int id)
    {
        Response.StatusCode = id;

        if (id == StatusCodes.Status404NotFound)
        {
            return View("NotFound");
        }

        return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

public class HomeViewModel
{
    public IReadOnlyList<Place> Featured { get; set; } = [];
    public IReadOnlyList<Category> Categories { get; set; } = [];
    public int PlaceCount { get; set; }
    public int CategoryCount { get; set; }
    public double AverageRating { get; set; }
    public int PlanCount { get; set; }
}

public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}

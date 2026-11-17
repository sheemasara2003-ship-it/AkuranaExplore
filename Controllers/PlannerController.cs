using System.Text.Json;
using AkuranaExplore.Data;
using AkuranaExplore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AkuranaExplore.Controllers;

public class PlannerController(ApplicationDbContext db) : Controller
{
    private const double AkuranaLatitude = 7.2797;
    private const double AkuranaLongitude = 80.6280;

    private readonly ApplicationDbContext _db = db;

    public async Task<IActionResult> Index()
    {
        var ids = PlanSession.Get(HttpContext.Session);
        var model = new PlannerViewModel();

        if (ids.Count > 0)
        {
            var places = await _db.Places
                .Include(p => p.Category)
                .AsNoTracking()
                .Where(p => ids.Contains(p.Id))
                .ToListAsync();

            var ordered = ids
                .Select(id => places.FirstOrDefault(p => p.Id == id))
                .Where(p => p is not null)
                .Cast<Place>()
                .ToList();

            if (ordered.Count != ids.Count)
            {
                PlanSession.Save(HttpContext.Session, ordered.Select(p => p.Id));
            }

            var cursor = new TimeSpan(8, 30, 0);
            Place? previous = null;
            var stops = new List<PlanStop>();
            var totalDistance = 0.0;
            var totalTravel = 0;

            for (var i = 0; i < ordered.Count; i++)
            {
                var place = ordered[i];
                var legDistance = previous is null
                    ? place.DistanceFromAkuranaKm
                    : Place.DistanceKm(previous.Latitude, previous.Longitude, place.Latitude, place.Longitude);
                var travelMinutes = Place.DrivingMinutes(legDistance);

                cursor += TimeSpan.FromMinutes(travelMinutes);
                var arrival = cursor;
                cursor += TimeSpan.FromMinutes(place.SuggestedDurationMinutes);

                stops.Add(new PlanStop
                {
                    Order = i + 1,
                    Place = place,
                    DistanceFromPreviousKm = legDistance,
                    TravelMinutesFromPrevious = travelMinutes,
                    ArrivalLabel = FormatTime(arrival),
                    DepartureLabel = FormatTime(cursor)
                });

                totalDistance += legDistance;
                totalTravel += travelMinutes;
                previous = place;
            }

            model.Stops = stops;
            model.TotalDistanceKm = totalDistance;
            model.TravelMinutes = totalTravel;
            model.VisitMinutes = ordered.Sum(p => p.SuggestedDurationMinutes);
            model.FinishLabel = FormatTime(cursor);
            model.IsLongDay = cursor > new TimeSpan(19, 0, 0);
        }

        model.Suggestions = await _db.Places
            .Include(p => p.Category)
            .AsNoTracking()
            .Where(p => !ids.Contains(p.Id))
            .OrderByDescending(p => p.Rating)
            .ThenBy(p => p.DistanceFromAkuranaKm)
            .Take(6)
            .ToListAsync();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int id, string? returnUrl)
    {
        if (!await _db.Places.AnyAsync(p => p.Id == id))
        {
            TempData["Error"] = "That place could not be found. It may have been removed.";
            return RedirectSafe(returnUrl);
        }

        var ids = PlanSession.Get(HttpContext.Session);
        if (ids.Contains(id))
        {
            TempData["Info"] = "That place is already in your day plan.";
        }
        else
        {
            ids.Add(id);
            PlanSession.Save(HttpContext.Session, ids);
            TempData["Success"] = "Added to your day plan.";
        }

        return RedirectSafe(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Remove(int id, string? returnUrl)
    {
        var ids = PlanSession.Get(HttpContext.Session);
        if (ids.Remove(id))
        {
            PlanSession.Save(HttpContext.Session, ids);
            TempData["Success"] = "Stop removed from your plan.";
        }

        return RedirectSafe(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Move(int id, int direction)
    {
        var ids = PlanSession.Get(HttpContext.Session);
        var index = ids.IndexOf(id);
        var target = index + Math.Sign(direction);

        if (index >= 0 && target >= 0 && target < ids.Count)
        {
            (ids[index], ids[target]) = (ids[target], ids[index]);
            PlanSession.Save(HttpContext.Session, ids);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Clear()
    {
        PlanSession.Clear(HttpContext.Session);
        TempData["Success"] = "Your day plan has been cleared.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Optimize()
    {
        var ids = PlanSession.Get(HttpContext.Session);
        if (ids.Count < 2)
        {
            TempData["Info"] = "Add at least two places before optimizing the route.";
            return RedirectToAction(nameof(Index));
        }

        var places = await _db.Places
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .ToListAsync();

        if (places.Count < 2)
        {
            return RedirectToAction(nameof(Index));
        }

        var remaining = new List<Place>(places);
        var route = new List<Place>();

        var current = remaining.OrderBy(p => p.DistanceFromAkuranaKm).First();
        route.Add(current);
        remaining.Remove(current);

        while (remaining.Count > 0)
        {
            var next = remaining
                .OrderBy(p => Place.DistanceKm(current.Latitude, current.Longitude, p.Latitude, p.Longitude))
                .First();
            route.Add(next);
            remaining.Remove(next);
            current = next;
        }

        PlanSession.Save(HttpContext.Session, route.Select(p => p.Id));
        TempData["Success"] = "Stops reordered to keep travel distances short. You can still move them manually.";
        return RedirectToAction(nameof(Index));
    }

    private IActionResult RedirectSafe(string? returnUrl)
    {
        return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl!) : RedirectToAction(nameof(Index));
    }

    private static string FormatTime(TimeSpan time)
    {
        return DateTime.Today.Add(time).ToString("h:mm tt");
    }
}

public class PlannerViewModel
{
    public IReadOnlyList<PlanStop> Stops { get; set; } = [];
    public IReadOnlyList<Place> Suggestions { get; set; } = [];
    public double TotalDistanceKm { get; set; }
    public int TravelMinutes { get; set; }
    public int VisitMinutes { get; set; }
    public string FinishLabel { get; set; } = string.Empty;
    public bool IsLongDay { get; set; }

    public int TotalMinutes => TravelMinutes + VisitMinutes;
    public string TotalDistanceLabel => $"{TotalDistanceKm:0.#} km";
    public string TotalTimeLabel
    {
        get
        {
            var hours = TotalMinutes / 60;
            var minutes = TotalMinutes % 60;
            return hours > 0 ? $"{hours} hr {minutes} min" : $"{minutes} min";
        }
    }
}

public class PlanStop
{
    public int Order { get; set; }
    public Place Place { get; set; } = new();
    public double DistanceFromPreviousKm { get; set; }
    public int TravelMinutesFromPrevious { get; set; }
    public string ArrivalLabel { get; set; } = string.Empty;
    public string DepartureLabel { get; set; } = string.Empty;
}

/// <summary>Stores the visitor's selected place IDs in the session (no login required).</summary>
public static class PlanSession
{
    public const string Key = "Planner.PlaceIds";

    public static List<int> Get(ISession session)
    {
        var raw = session.GetString(Key);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<int>>(raw) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static void Save(ISession session, IEnumerable<int> ids)
    {
        var clean = ids.Distinct().ToList();
        session.SetString(Key, JsonSerializer.Serialize(clean));
    }

    public static void Clear(ISession session) => session.Remove(Key);
}

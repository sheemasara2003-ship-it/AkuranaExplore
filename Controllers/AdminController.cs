using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.RegularExpressions;
using AkuranaExplore.Data;
using AkuranaExplore.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AkuranaExplore.Controllers;

[Authorize]
[AutoValidateAntiforgeryToken]
public class AdminController(ApplicationDbContext db) : Controller
{
    private readonly ApplicationDbContext _db = db;

    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(nameof(Dashboard));
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(nameof(Dashboard));
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var username = model.Username.Trim();
        var user = await _db.AdminUsers.FirstOrDefaultAsync(u => u.Username == username);

        if (user is null || !PasswordHasher.Verify(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new("DisplayName", user.DisplayName)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var properties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            properties);

        TempData["Success"] = $"Welcome back, {user.DisplayName}.";

        return Url.IsLocalUrl(model.ReturnUrl)
            ? Redirect(model.ReturnUrl!)
            : RedirectToAction(nameof(Dashboard));
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["Success"] = "You have been signed out.";
        return RedirectToAction("Index", "Home");
    }

    public async Task<IActionResult> Dashboard(int? editId)
    {
        return View(await BuildDashboardAsync(editId));
    }

    [HttpPost]
    public async Task<IActionResult> SavePlace(PlaceForm form)
    {
        form.Name = form.Name?.Trim() ?? string.Empty;
        form.Address = form.Address?.Trim() ?? string.Empty;
        form.ImageUrl = string.IsNullOrWhiteSpace(form.ImageUrl) ? null : form.ImageUrl.Trim();

        if (form.ImageUrl is { Length: > 0 }
            && !form.ImageUrl.StartsWith('/')
            && !form.ImageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            form.ImageUrl = "/" + form.ImageUrl;
        }

        if (!await _db.Categories.AnyAsync(c => c.Id == form.CategoryId))
        {
            ModelState.AddModelError(nameof(form.CategoryId), "Choose a valid category.");
        }

        if (await _db.Places.AnyAsync(p => p.Name == form.Name && p.Id != form.Id))
        {
            ModelState.AddModelError(nameof(form.Name), "A place with this name already exists.");
        }

        if (!ModelState.IsValid)
        {
            return View("Dashboard", await BuildDashboardAsync(form.Id > 0 ? form.Id : null, form));
        }

        Place place;
        if (form.Id > 0)
        {
            var existing = await _db.Places.FirstOrDefaultAsync(p => p.Id == form.Id);
            if (existing is null)
            {
                return NotFound();
            }

            place = existing;
        }
        else
        {
            place = new Place();
            _db.Places.Add(place);
        }

        form.ApplyTo(place);
        await _db.SaveChangesAsync();

        TempData["Success"] = form.Id > 0
            ? $"\"{place.Name}\" was updated."
            : $"\"{place.Name}\" was added to the tourism database.";

        return RedirectToAction(nameof(Dashboard));
    }

    [HttpPost]
    public async Task<IActionResult> DeletePlace(int id)
    {
        var place = await _db.Places.FirstOrDefaultAsync(p => p.Id == id);
        if (place is null)
        {
            TempData["Error"] = "That place no longer exists.";
            return RedirectToAction(nameof(Dashboard));
        }

        _db.Places.Remove(place);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"\"{place.Name}\" was deleted.";
        return RedirectToAction(nameof(Dashboard));
    }

    [HttpPost]
    public async Task<IActionResult> SaveCategory(CategoryForm form)
    {
        form.Name = form.Name?.Trim() ?? string.Empty;
        form.Icon = string.IsNullOrWhiteSpace(form.Icon) ? "📍" : form.Icon.Trim();
        form.ImageUrl = string.IsNullOrWhiteSpace(form.ImageUrl) ? "/images/nature.jpg" : form.ImageUrl.Trim();
        form.Blurb = form.Blurb?.Trim() ?? string.Empty;

        var slug = string.IsNullOrWhiteSpace(form.Slug) ? Slugify(form.Name) : Slugify(form.Slug);

        if (string.IsNullOrWhiteSpace(form.Name))
        {
            TempData["Error"] = "A category name is required.";
            return RedirectToAction(nameof(Dashboard));
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            TempData["Error"] = "The category name must contain letters or numbers.";
            return RedirectToAction(nameof(Dashboard));
        }

        if (await _db.Categories.AnyAsync(c => (c.Slug == slug || c.Name == form.Name) && c.Id != form.Id))
        {
            TempData["Error"] = "A category with that name or URL slug already exists.";
            return RedirectToAction(nameof(Dashboard));
        }

        Category category;
        if (form.Id > 0)
        {
            var existing = await _db.Categories.FirstOrDefaultAsync(c => c.Id == form.Id);
            if (existing is null)
            {
                TempData["Error"] = "That category no longer exists.";
                return RedirectToAction(nameof(Dashboard));
            }

            category = existing;
        }
        else
        {
            category = new Category();
            _db.Categories.Add(category);
        }

        category.Name = form.Name;
        category.Slug = slug;
        category.Icon = form.Icon;
        category.Blurb = form.Blurb;
        category.ImageUrl = form.ImageUrl;
        category.SortOrder = form.SortOrder <= 0 ? 99 : form.SortOrder;

        await _db.SaveChangesAsync();

        TempData["Success"] = form.Id > 0
            ? $"Category \"{category.Name}\" was updated."
            : $"Category \"{category.Name}\" was created.";

        return RedirectToAction(nameof(Dashboard));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _db.Categories
            .Include(c => c.Places)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category is null)
        {
            TempData["Error"] = "That category no longer exists.";
        }
        else if (category.Places.Count > 0)
        {
            TempData["Error"] = $"\"{category.Name}\" still contains {category.Places.Count} place(s). Move them to another category first.";
        }
        else
        {
            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Category \"{category.Name}\" was deleted.";
        }

        return RedirectToAction(nameof(Dashboard));
    }

    private async Task<AdminDashboardViewModel> BuildDashboardAsync(int? editId = null, PlaceForm? form = null)
    {
        var places = await _db.Places
            .Include(p => p.Category)
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync();

        var categories = await _db.Categories
            .Include(c => c.Places)
            .AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();

        var editPlace = form;
        if (editPlace is null && editId is > 0)
        {
            var selected = places.FirstOrDefault(p => p.Id == editId);
            if (selected is not null)
            {
                editPlace = PlaceForm.FromPlace(selected);
            }
        }

        editPlace ??= new PlaceForm();

        var username = User.Identity?.Name ?? string.Empty;
        var admin = await _db.AdminUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Username == username);

        return new AdminDashboardViewModel
        {
            Places = places,
            Categories = categories,
            EditPlace = editPlace,
            AdminName = admin?.DisplayName ?? User.FindFirstValue("DisplayName") ?? "Administrator",
            AdminLastLogin = admin?.LastLoginAt,
            Stats = new AdminStats(
                places.Count,
                categories.Count,
                places.Count(p => p.IsFeatured),
                places.Count > 0 ? Math.Round(places.Average(p => p.Rating), 1) : 0,
                await _db.AdminUsers.CountAsync())
        };
    }

    private static string Slugify(string value)
    {
        var slug = value.Trim().ToLowerInvariant();
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"[^a-z0-9-]", string.Empty);
        slug = Regex.Replace(slug, @"-{2,}", "-").Trim('-');
        return slug;
    }
}

public class AdminDashboardViewModel
{
    public IReadOnlyList<Place> Places { get; set; } = [];
    public IReadOnlyList<Category> Categories { get; set; } = [];
    public PlaceForm EditPlace { get; set; } = new();
    public AdminStats Stats { get; set; } = new(0, 0, 0, 0, 0);
    public string AdminName { get; set; } = "Administrator";
    public DateTime? AdminLastLogin { get; set; }

    public bool IsEditing => EditPlace.Id > 0;
}

public record AdminStats(int PlaceCount, int CategoryCount, int FeaturedCount, double AverageRating, int AdminCount);

public class PlaceForm
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Enter the place name.")]
    [StringLength(120)]
    [Display(Name = "Place name")]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Choose a category.")]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Enter a short description.")]
    [StringLength(240)]
    [Display(Name = "Short description")]
    public string ShortDescription { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter the full description.")]
    [StringLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter travel tips for visitors.")]
    [StringLength(1000)]
    [Display(Name = "Travel tips")]
    public string TravelTips { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter the location or address.")]
    [StringLength(200)]
    public string Address { get; set; } = string.Empty;

    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    public double Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
    public double Longitude { get; set; }

    [Range(0, 25, ErrorMessage = "Distance must be within the 25 km study area around Akurana.")]
    [Display(Name = "Distance from Akurana (km)")]
    public double DistanceFromAkuranaKm { get; set; }

    [Required(ErrorMessage = "Enter the opening hours.")]
    [StringLength(120)]
    [Display(Name = "Opening hours")]
    public string OpeningHours { get; set; } = "Open daily";

    [Range(0, 100000, ErrorMessage = "Entry fee must be zero or a positive amount.")]
    [Display(Name = "Entry fee (LKR, 0 = free)")]
    public decimal EntryFeeLkr { get; set; }

    [Range(15, 1440, ErrorMessage = "Suggested duration must be between 15 and 1440 minutes.")]
    [Display(Name = "Suggested visit duration (minutes)")]
    public int SuggestedDurationMinutes { get; set; } = 60;

    [Range(0, 5, ErrorMessage = "Rating must be between 0 and 5.")]
    public double Rating { get; set; } = 4.5;

    [Display(Name = "Feature on the home page")]
    public bool IsFeatured { get; set; }

    [StringLength(200)]
    [Display(Name = "Image path (optional)")]
    public string? ImageUrl { get; set; }

    public static PlaceForm FromPlace(Place place)
    {
        return new PlaceForm
        {
            Id = place.Id,
            Name = place.Name,
            CategoryId = place.CategoryId,
            ShortDescription = place.ShortDescription,
            Description = place.Description,
            TravelTips = place.TravelTips,
            Address = place.Address,
            Latitude = place.Latitude,
            Longitude = place.Longitude,
            DistanceFromAkuranaKm = place.DistanceFromAkuranaKm,
            OpeningHours = place.OpeningHours,
            EntryFeeLkr = place.EntryFeeLkr,
            SuggestedDurationMinutes = place.SuggestedDurationMinutes,
            Rating = place.Rating,
            IsFeatured = place.IsFeatured,
            ImageUrl = place.ImageUrl
        };
    }

    public void ApplyTo(Place place)
    {
        place.Name = Name.Trim();
        place.CategoryId = CategoryId;
        place.ShortDescription = ShortDescription.Trim();
        place.Description = Description.Trim();
        place.TravelTips = TravelTips.Trim();
        place.Address = Address.Trim();
        place.Latitude = Latitude;
        place.Longitude = Longitude;
        place.DistanceFromAkuranaKm = DistanceFromAkuranaKm;
        place.OpeningHours = OpeningHours.Trim();
        place.EntryFeeLkr = EntryFeeLkr;
        place.SuggestedDurationMinutes = SuggestedDurationMinutes;
        place.Rating = Rating;
        place.IsFeatured = IsFeatured;
        place.ImageUrl = ImageUrl;
    }
}

public class CategoryForm
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Enter a category name.")]
    [StringLength(60)]
    public string Name { get; set; } = string.Empty;

    [StringLength(60)]
    [Display(Name = "URL slug")]
    public string? Slug { get; set; }

    [StringLength(12)]
    [Display(Name = "Icon")]
    public string Icon { get; set; } = "📍";

    [StringLength(200)]
    [Display(Name = "Short blurb")]
    public string? Blurb { get; set; }

    [StringLength(200)]
    [Display(Name = "Image path")]
    public string? ImageUrl { get; set; } = "/images/nature.jpg";

    [Display(Name = "Sort order")]
    public int SortOrder { get; set; }
}

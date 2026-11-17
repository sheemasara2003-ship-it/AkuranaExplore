using AkuranaExplore.Models;

namespace AkuranaExplore.Data;

/// <summary>
/// Creates the initial categories, the 10+ places required by the SRS and a single
/// administrator account. The seeder is idempotent, so restarting the app is safe.
/// </summary>
public static class DbSeeder
{
    public const string DefaultAdminUsername = "admin";
    public const string DefaultAdminPassword = "Akurana@2026";

    public static void Seed(ApplicationDbContext db)
    {
        if (!db.Categories.Any())
        {
            db.Categories.AddRange(
                new Category { Name = "Sightseeing", Slug = "sightseeing", Icon = "🌄", SortOrder = 1, ImageUrl = "/images/sightseeing.jpeg", Blurb = "Viewpoints, lakeside walks and easy stops with a view." },
                new Category { Name = "Historical", Slug = "historical", Icon = "🏛️", SortOrder = 2, ImageUrl = "/images/historical.jpg", Blurb = "Cave temples, monuments and places that hold the region's story." },
                new Category { Name = "Cultural", Slug = "cultural", Icon = "🎭", SortOrder = 3, ImageUrl = "/images/cultural.jpg", Blurb = "Museums, craft traditions and living heritage of the hill country." },
                new Category { Name = "Religious", Slug = "religious", Icon = "🛕", SortOrder = 4, ImageUrl = "/images/religioun.jpg", Blurb = "Temples and sacred sites welcoming respectful visitors." },
                new Category { Name = "Nature", Slug = "nature", Icon = "🌿", SortOrder = 5, ImageUrl = "/images/nature.jpg", Blurb = "Gardens, forest reserves and reservoirs around Akurana." },
                new Category { Name = "Restaurants", Slug = "restaurants", Icon = "🍛", SortOrder = 6, ImageUrl = "/images/restaurants.jpg", Blurb = "Local rice and curry, short eats and a proper cup of Ceylon tea." }
            );
            db.SaveChanges();
        }
        else
        {
            // Update existing categories to use new images if needed
            var cats = db.Categories.ToList();
            foreach (var c in cats)
            {
                if (c.Slug == "sightseeing") c.ImageUrl = "/images/sightseeing.jpeg";
                else if (c.Slug == "historical") c.ImageUrl = "/images/historical.jpg";
                else if (c.Slug == "cultural") c.ImageUrl = "/images/cultural.jpg";
                else if (c.Slug == "religious") c.ImageUrl = "/images/religioun.jpg";
                else if (c.Slug == "nature") c.ImageUrl = "/images/nature.jpg";
                else if (c.Slug == "restaurants") c.ImageUrl = "/images/restaurants.jpg";
            }
            db.SaveChanges();
        }

        if (!db.Places.Any())
        {
            var categories = db.Categories.ToDictionary(c => c.Slug);
            db.Places.AddRange(BuildPlaces(categories));
            db.SaveChanges();
        }
        else
        {
            // Assign specific images to places where available
            var places = db.Places.ToList();
            foreach (var p in places)
            {
                var name = p.Name.ToLowerInvariant();
                if (name.IndexOf("kandy lake", StringComparison.OrdinalIgnoreCase) >= 0) p.ImageUrl = "/images/kandy-lake.jpg";
                else if (name.IndexOf("sri dalada", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("tooth", StringComparison.OrdinalIgnoreCase) >= 0) p.ImageUrl = "/images/sri-dalada-maligawa.jpg";
                else if (name.IndexOf("botanic", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("peradeniya", StringComparison.OrdinalIgnoreCase) >= 0) p.ImageUrl = "/images/royal-botanic-gardens-peradeniya.webp";
                else if (name.IndexOf("udawattakele", StringComparison.OrdinalIgnoreCase) >= 0) p.ImageUrl = "/images/udawattakele-forest-reserve.jpeg";
                else if (name.IndexOf("nelligala", StringComparison.OrdinalIgnoreCase) >= 0) p.ImageUrl = "/images/nelligala-international-buddhist-centre.jpeg";
                else if (name.IndexOf("tea museum", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("ceylon tea", StringComparison.OrdinalIgnoreCase) >= 0) p.ImageUrl = "/images/ceylon-tea-museum.jpeg";
                else if (name.IndexOf("bahirawakanda", StringComparison.OrdinalIgnoreCase) >= 0) p.ImageUrl = "/images/bahirawakanda-temple.jpeg";
                else if (name.IndexOf("view point", StringComparison.OrdinalIgnoreCase) >= 0) p.ImageUrl = "/images/kandy-view-point.jpeg";
                else if (name.IndexOf("polgolla", StringComparison.OrdinalIgnoreCase) >= 0) p.ImageUrl = "/images/polgolla-dam.jpeg";
                else if (name.IndexOf("aluviharaya", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("aluvihare", StringComparison.OrdinalIgnoreCase) >= 0) p.ImageUrl = "/images/aluvihare-rock-cave-temple.jpg";
                else if (name.IndexOf("kandy muslim hotel", StringComparison.OrdinalIgnoreCase) >= 0) p.ImageUrl = "/images/kandy-muslim-hotel.jpeg";
                else if (name.IndexOf("akurana junction", StringComparison.OrdinalIgnoreCase) >= 0) p.ImageUrl = "/images/akurana-junction-food-court.jpeg";
                else if (name.IndexOf("national museum", StringComparison.OrdinalIgnoreCase) >= 0) p.ImageUrl = "/images/kandy-national-museum.jpeg";
            }
            db.SaveChanges();
        }

        if (!db.AdminUsers.Any())
        {
            db.AdminUsers.Add(new AdminUser
            {
                Username = DefaultAdminUsername,
                DisplayName = "Site Administrator",
                PasswordHash = PasswordHasher.Hash(DefaultAdminPassword)
            });
            db.SaveChanges();
        }
    }

    private static IEnumerable<Place> BuildPlaces(IReadOnlyDictionary<string, Category> categories)
    {
        return new List<Place>
        {
            new()
            {
                Name = "Kandy Lake",
                CategoryId = categories["sightseeing"].Id,
                ShortDescription = "A calm lakeside walk in the heart of Kandy, ringed by hills and colonial-era paths.",
                Description = "Kandy Lake — locally known as Kiri Muhuda, the 'sea of milk' — was created in 1807 beside the Temple of the Tooth. The paved loop is about 3 km and takes roughly 45 minutes at an easy pace. Early mornings bring mist over the water, joggers and birds; evenings are busier with families and street food. The southern end connects directly to the Sri Dalada Maligawa precinct, making it an easy first stop on a day visit.",
                Address = "Kandy Lake, Kandy 20000, Sri Lanka",
                TravelTips = "Walk the loop early in the morning for cooler weather and fewer crowds. Wear respectful clothing near the temple end of the lake. Do not swim — the lake is sacred and not a bathing spot. Combine the walk with the Temple of the Tooth and Kandy View Point.",
                Latitude = 7.2906,
                Longitude = 80.6410,
                DistanceFromAkuranaKm = 9,
                OpeningHours = "Lake walk 6:00 AM – 7:00 PM daily",
                EntryFeeLkr = 0,
                SuggestedDurationMinutes = 60,
                Rating = 4.7,
                IsFeatured = true
            },
            new()
            {
                Name = "Sri Dalada Maligawa",
                CategoryId = categories["religious"].Id,
                ShortDescription = "The Temple of the Sacred Tooth Relic — Kandy's most important religious and cultural site.",
                Description = "Sri Dalada Maligawa houses the sacred tooth relic of the Buddha and is the spiritual heart of Kandy's UNESCO-listed sacred city. Visitors can explore the ornate inner shrine, the moat-side walkway and the museum displays that trace the relic's history. Three daily pooja ceremonies fill the temple with drumming and the scent of jasmine, and the evening pooja is especially atmospheric.",
                Address = "Temple of the Sacred Tooth Relic, Kandy 20000, Sri Lanka",
                TravelTips = "Cover shoulders and knees, and remove shoes and hats before entering the inner shrine. Photography inside the shrine is restricted. Arrive 20–30 minutes before a pooja (6:30 AM, 10:30 AM or 6:30 PM) for the full experience. Keep valuables secure in the crowded courtyard.",
                Latitude = 7.2937,
                Longitude = 80.6411,
                DistanceFromAkuranaKm = 9,
                OpeningHours = "5:30 AM – 8:00 PM daily; pooja at 6:30 AM, 10:30 AM and 6:30 PM",
                EntryFeeLkr = 2000,
                SuggestedDurationMinutes = 90,
                Rating = 4.9,
                IsFeatured = true
            },
            new()
            {
                Name = "Royal Botanic Gardens, Peradeniya",
                CategoryId = categories["nature"].Id,
                ShortDescription = "Hundreds of acres of palms, orchids and giant fig trees beside the Mahaweli River.",
                Description = "The Royal Botanic Gardens at Peradeniya cover more than 60 hectares and hold one of Asia's finest collections of tropical plants. Highlights include the palm avenue, the orchid house, the giant Java fig tree and the riverside walk. The gardens are a comfortable half-day stop, with plenty of shade and open lawns for a picnic.",
                Address = "Royal Botanic Gardens, Peradeniya 20400, Sri Lanka",
                TravelTips = "Allow at least two hours; three hours if you enjoy gardens. Bring water, sun protection and insect repellent. The orchid house and palm avenue are the standout stops. Weekday mornings are quietest.",
                Latitude = 7.2711,
                Longitude = 80.5963,
                DistanceFromAkuranaKm = 11,
                OpeningHours = "7:30 AM – 5:00 PM daily (last entry 4:30 PM)",
                EntryFeeLkr = 3000,
                SuggestedDurationMinutes = 120,
                Rating = 4.8,
                IsFeatured = true
            },
            new()
            {
                Name = "Udawattakele Forest Reserve",
                CategoryId = categories["nature"].Id,
                ShortDescription = "A quiet rainforest reserve above Kandy, popular for birdwatching and shaded walks.",
                Description = "Udawattakele is a protected forest reserve on the ridge behind Kandy's sacred city. Its trails pass through dense canopy, old reservoirs and viewpoints, and the reserve is a haven for birdlife, monkeys and lizards. The walking is gentle but humid, and the higher paths reward visitors with glimpses over the lake and town.",
                Address = "Udawattakele Forest Reserve, Kandy 20000, Sri Lanka",
                TravelTips = "Go early and consider a local guide for the trail network. Wear closed shoes with good grip and carry water. Leeches are possible after rain, so long socks help. This is a forest reserve — take all litter out with you.",
                Latitude = 7.2995,
                Longitude = 80.6390,
                DistanceFromAkuranaKm = 8,
                OpeningHours = "6:00 AM – 6:00 PM daily",
                EntryFeeLkr = 200,
                SuggestedDurationMinutes = 120,
                Rating = 4.4,
                IsFeatured = false
            },
            new()
            {
                Name = "Nelligala International Buddhist Centre",
                CategoryId = categories["religious"].Id,
                ShortDescription = "A hilltop Buddhist centre with sweeping views over the hills and countryside around Kandy.",
                Description = "Nelligala is a modern, well-kept Buddhist centre set on a ridge with panoramic views of the surrounding hills. The white stupa and surrounding structures are peaceful, and the open grounds make it a serene stop away from the busier parts of Kandy. It's particularly striking around sunrise or late afternoon.",
                Address = "Nelligala, Muruthagahamula, Sri Lanka",
                TravelTips = "Dress modestly and be respectful of worshippers. The approach road is winding but manageable by car or tuk-tuk. Combine with nearby stops if you have a half-day in the area. Early visits are cooler and less hazy.",
                Latitude = 7.2085,
                Longitude = 80.5568,
                DistanceFromAkuranaKm = 10,
                OpeningHours = "6:00 AM – 6:00 PM daily",
                EntryFeeLkr = 0,
                SuggestedDurationMinutes = 60,
                Rating = 4.7,
                IsFeatured = true
            },
            new()
            {
                Name = "Ceylon Tea Museum",
                CategoryId = categories["cultural"].Id,
                ShortDescription = "Housed in a 1920s tea factory, this museum traces the story of Ceylon tea from leaf to cup.",
                Description = "The Ceylon Tea Museum occupies a renovated Hantana tea factory and offers a compact, informative look at Sri Lanka's most famous export. Exhibits cover processing machinery, colonial history and tea grades, with a tasting room on the top floor offering great views over Kandy.",
                Address = "Ceylon Tea Museum, Hantana Road, Kandy 20000, Sri Lanka",
                TravelTips = "Don't miss the tea tasting at the end. The museum is compact, so 45–90 minutes is plenty. Combine with a visit to Hantana for a cool change of air. Tuk-tuks are easy to reach from central Kandy.",
                Latitude = 7.2686,
                Longitude = 80.6337,
                DistanceFromAkuranaKm = 11,
                OpeningHours = "8:30 AM – 5:00 PM daily (closed on Poya days)",
                EntryFeeLkr = 800,
                SuggestedDurationMinutes = 60,
                Rating = 4.5,
                IsFeatured = true
            },
            new()
            {
                Name = "Bahirawakanda Temple",
                CategoryId = categories["religious"].Id,
                ShortDescription = "A hilltop temple with a towering seated Buddha overlooking Kandy Lake and the town below.",
                Description = "The Bahirawakanda Vihara is known for its large white Buddha statue visible from much of Kandy. The temple grounds are peaceful, and the viewpoint offers one of the classic panoramas over the city, lake and surrounding hills — especially at sunset.",
                Address = "Bahirawakanda Vihara, Bahirawakanda, Kandy, Sri Lanka",
                TravelTips = "Climb or take a tuk-tuk to the top. Dress modestly. Sunset is popular, so expect a few other visitors at that time. The view is the main draw, and the grounds are small.",
                Latitude = 7.2952,
                Longitude = 80.6370,
                DistanceFromAkuranaKm = 9,
                OpeningHours = "8:00 AM – 6:00 PM daily",
                EntryFeeLkr = 0,
                SuggestedDurationMinutes = 45,
                Rating = 4.6,
                IsFeatured = false
            },
            new()
            {
                Name = "Kandy View Point",
                CategoryId = categories["sightseeing"].Id,
                ShortDescription = "A roadside lookout above the lake with a classic view of Kandy's hills and rooftops.",
                Description = "The Kandy View Point sits along Rajapihilla Mawatha and gives a wide view over Kandy Lake, the Temple of the Tooth precinct and the surrounding ridge. It's a quick, free stop and works especially well as a warm-up or cool-down on a walking day.",
                Address = "Rajapihilla Mawatha, Kandy 20000, Sri Lanka",
                TravelTips = "Go in the late afternoon for softer light. It's a quick stop — 10–15 minutes is often enough. Watch for traffic if pulling in by tuk-tuk.",
                Latitude = 7.2930,
                Longitude = 80.6385,
                DistanceFromAkuranaKm = 9,
                OpeningHours = "Open 24 hours",
                EntryFeeLkr = 0,
                SuggestedDurationMinutes = 15,
                Rating = 4.3,
                IsFeatured = false
            },
            new()
            {
                Name = "Polgolla Dam",
                CategoryId = categories["nature"].Id,
                ShortDescription = "A quiet reservoir and dam on the Mahaweli River, ideal for a short break away from the crowds.",
                Description = "Polgolla Dam spans the Mahaweli River and creates a broad, tranquil stretch of water just outside central Kandy. It's a low-key stop for a short walk, photos or a break between busier attractions. On breezy days it's noticeably cooler than town.",
                Address = "Polgolla, Kandy District 20810, Sri Lanka",
                TravelTips = "A 15–30 minute stop is usually plenty. Be cautious near wet edges. Tuk-tuks can reach it easily from Akurana or Kandy. Combine with other nearby stops.",
                Latitude = 7.3200,
                Longitude = 80.6460,
                DistanceFromAkuranaKm = 6,
                OpeningHours = "Open daily",
                EntryFeeLkr = 0,
                SuggestedDurationMinutes = 30,
                Rating = 4.2,
                IsFeatured = false
            },
            new()
            {
                Name = "Aluviharaya Rock Cave Temple",
                CategoryId = categories["historical"].Id,
                ShortDescription = "A historic cave temple complex where the Pali Canon is said to have been first committed to writing.",
                Description = "Aluviharaya is famed for its rock caves and its role in Buddhist history — tradition holds that the Tripitaka was transcribed here in the 1st century BCE. The painted caves, reclining Buddha and peaceful setting make it a rewarding stop, especially for those interested in Sri Lanka's religious history.",
                Address = "Aluvihare Temple, Aluvihare, Matale, Sri Lanka",
                TravelTips = "Dress modestly and remove shoes where requested. Climb the steps to the caves — wear shoes with grip. Allow 45–75 minutes. It's a little further out, so best as a standalone stop or on a route toward Matale.",
                Latitude = 7.2965,
                Longitude = 80.6167,
                DistanceFromAkuranaKm = 14,
                OpeningHours = "7:00 AM – 6:00 PM daily",
                EntryFeeLkr = 500,
                SuggestedDurationMinutes = 60,
                Rating = 4.4,
                IsFeatured = true
            },
            new()
            {
                Name = "Kandy Muslim Hotel",
                CategoryId = categories["restaurants"].Id,
                ShortDescription = "A busy, long-running Kandy favourite for biriyani, short eats and strong Ceylon tea.",
                Description = "Kandy Muslim Hotel is a local institution known for its generous portions, quick service and consistently good biriyani. It's a practical lunch stop near the town edge and a good place to try Sri Lankan Muslim cuisine. Expect a busy room at peak times.",
                Address = "Kandy Muslim Hotel, Kotugodella Veediya, Kandy, Sri Lanka",
                TravelTips = "Go for the chicken or mutton biriyani. Peak lunch (12:00–1:00 PM) can be busy — arrive a little early or be patient. Vegetarian options are available. Cash is widely used, though some branches accept cards.",
                Latitude = 7.2920,
                Longitude = 80.6355,
                DistanceFromAkuranaKm = 9,
                OpeningHours = "7:00 AM – 10:00 PM daily",
                EntryFeeLkr = 0,
                SuggestedDurationMinutes = 45,
                Rating = 4.5,
                IsFeatured = false
            },
            new()
            {
                Name = "Akurana Junction Food Court",
                CategoryId = categories["restaurants"].Id,
                ShortDescription = "Right in Akurana, this food court offers quick, tasty short eats and rice and curry at local prices.",
                Description = "Akurana Junction Food Court is a convenient, down-to-earth stop for a bite before or after exploring. You'll find roti, samosas, kottu, rice and curry and sweet treats, along with strong tea. It's ideal for a no-fuss meal close to home base.",
                Address = "Akurana Junction, Akurana 20850, Sri Lanka",
                TravelTips = "Try the hot short eats fresh off the griddle. Prices are local — bring small change. It's busiest around breakfast and evening rush hour. Great for a quick, authentic meal.",
                Latitude = 7.3690,
                Longitude = 80.6170,
                DistanceFromAkuranaKm = 0.5,
                OpeningHours = "6:00 AM – 9:00 PM daily",
                EntryFeeLkr = 0,
                SuggestedDurationMinutes = 45,
                Rating = 4.1,
                IsFeatured = false
            },
            new()
            {
                Name = "Kandy National Museum",
                CategoryId = categories["cultural"].Id,
                ShortDescription = "Housed in a former palace, with collections spanning Kandyan art, regalia, weapons and everyday life.",
                Description = "The Kandy National Museum sits next to the Temple of the Tooth complex in what was once the residence of Kandyan queens. Its galleries showcase traditional costumes, jewellery, woodcarvings, weaponry and royal regalia, offering context for the history and culture of the Kandyan Kingdom.",
                Address = "Kandy National Museum, Kandy 20000, Sri Lanka",
                TravelTips = "Combine with a visit to the Temple of the Tooth — they're adjacent. The museum is not huge, so 45–60 minutes is enough. Photography rules may vary by gallery.",
                Latitude = 7.2939,
                Longitude = 80.6420,
                DistanceFromAkuranaKm = 9,
                OpeningHours = "8:30 AM – 5:00 PM daily (closed on Poya days)",
                EntryFeeLkr = 500,
                SuggestedDurationMinutes = 60,
                Rating = 4.2,
                IsFeatured = false
            }
        };
    }
}

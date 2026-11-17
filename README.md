# AkuranaExplore

A local tourism guide and one-day visit planner for Akurana and the hills around Kandy, Sri Lanka. The app was built as an ITE2953 student project from the supplied proposal and SRS, and covers the required place guide, session-based day planner, and authenticated admin area.

The site is intentionally booking-free: there are no payments, reservations, or visitor accounts. Distances, travel times, opening hours and fees are planning estimates, not live data.

---

## 1. Quick start

### Prerequisites

- Windows with the [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQL Server LocalDB (installed with Visual Studio, or the SQL Server Express LocalDB installer)
- A modern browser

### Run it

```powershell
cd AkuranaExplore
dotnet restore
dotnet run --launch-profile http
```

Open <http://localhost:5080>.

The app creates the `AkuranaExplore` database automatically on first launch using `Database.EnsureCreated()`, then seeds demo data. No migration command or SQL script is needed for a fresh install.

### Administrator sign-in

| Username | Password |
| --- | --- |
| `admin` | `Akurana@2026` |

The credentials are deliberately kept out of the public UI. They live only in this README and in `Data/DbSeeder.cs`. **Change the password before any real deployment** (see [Security notes](#9-security-notes)).

---

## 2. Technology

| Layer | Choice |
| --- | --- |
| Framework | ASP.NET Core MVC, `net9.0` |
| Database | SQL Server / SQL Server LocalDB |
| ORM | EF Core SQL Server 9.0.0 |
| Auth | Cookie authentication, PBKDF2-SHA256 password hashes |
| Planner state | ASP.NET Core session (no visitor accounts) |
| Front end | Razor views, hand-written CSS design system, small vanilla JS file |
| Imagery | Local SVG artwork in `wwwroot/images` (works offline) |

---

## 3. Features and SRS coverage

### Public guide

- **13 seeded places** within roughly 25 km of Akurana, exceeding the 10-place minimum.
- **Six categories** — Sightseeing, Historical, Cultural, Religious, Nature, Restaurants — with icons, blurbs, sort order, and category artwork.
- **Search** across name, short description, full description, address and category name.
- **Category filters**, including live counts per category, and sorting by recommended, name, distance, or visit length.
- **Place details** include the full description, category, distance from Akurana, opening hours, entry fee, suggested visit duration, address, travel tips, map links (OpenStreetMap and Google Maps), a rating, and three nearby places.
- Friendly empty states when filters/search return no results.
- Friendly **404 page** for unknown routes and place IDs.
- Responsive layout for desktop, tablet and mobile, with a collapsible navigation menu.

### Day planner

- Add stops from a place page or from the planner's suggestion list.
- **Duplicate prevention** — the same place cannot appear twice.
- Remove individual stops or clear the whole plan.
- Move stops up/down to reorder them.
- **Shorten the route** uses a nearest-neighbour optimisation starting from the place closest to Akurana.
- Timeline starts at **8:30 AM** from Akurana, and shows arrival/departure, leg distance, estimated travel time and visit duration.
- Summary shows stop count, total distance, total visit + travel time, and estimated finish time.
- Warns when the plan runs past 7:00 PM.
- **Copy plan** and **Print** actions, with a print-specific layout.
- Empty-plan state explains how to begin.

### Admin area

- Cookie-protected `/Admin` area; anonymous visits redirect to `/Admin/Login` with a return URL.
- Sign in, sign out, dashboard statistics, and last sign-in time.
- **Place CRUD** with server-side validation, duplicate-name protection, category validation, and delete confirmation.
- Editable descriptions, categories, opening hours, location, travel tips, fees, duration, distance, rating, featured status and image path/URL.
- **Category CRUD**; categories that still contain places cannot be deleted.
- All admin POST actions use anti-forgery tokens.
- Passwords are stored as PBKDF2-SHA256 hashes with per-user salts; plaintext passwords are never stored or rendered.

### Explicit non-goals

- No booking, ticketing, or payment flow.
- No visitor registration or login. Plans belong to the browser session.
- No live opening-hours, pricing, or traffic feed.

---

## 4. Project structure

```text
AkuranaExplore/
├─ Controllers/
│  ├─ HomeController.cs          # Home, error and status-code pages
│  ├─ PlacesController.cs        # List/search/filter and details
│  ├─ PlannerController.cs       # Session plan, timeline, optimise
│  └─ AdminController.cs         # Login/logout, dashboard, place/category CRUD
├─ Data/
│  ├─ ApplicationDbContext.cs    # DbSets, relationships, indexes
│  ├─ DbSeeder.cs                # 13 places, 6 categories, admin account
│  └─ PlanSession.cs             # Session plan helpers
├─ Models/
│  ├─ Place.cs                   # Place fields, labels, distance helpers
│  ├─ Category.cs
│  └─ AdminUser.cs               # Admin account + PasswordHasher
├─ Views/
│  ├─ Home/                      # Home, NotFound
│  ├─ Places/                    # Index, Details
│  ├─ Planner/                   # Index
│  ├─ Admin/                     # Login, Dashboard, _PlaceForm
│  └─ Shared/                    # Layout, Error, _PlaceCard
├─ wwwroot/
│  ├─ css/site.css               # Design system, responsive + print styles
│  ├─ js/site.js                 # Nav, filters, confirmations, copy/print
│  └─ images/                    # Local SVG artwork
├─ Program.cs                    # DI, auth, session, database, middleware
├─ appsettings.json              # LocalDB connection string
└─ AkuranaExplore.csproj
```

### Seeded places

Kandy Lake · Sri Dalada Maligawa · Royal Botanic Gardens, Peradeniya · Udawattakele Forest Reserve · Nelligala International Buddhist Centre · Ceylon Tea Museum · Bahirawakanda Temple · Kandy View Point · Polgolla Dam · Aluviharaya Rock Cave Temple · Kandy Muslim Hotel · Akurana Junction Food Court · Kandy National Museum.

Seed data is realistic demo content, not a live directory. Confirm hours and fees with the site before travelling.

---

## 5. Database and configuration

`appsettings.json` contains the default connection string:

```json
"Server=(localdb)\\MSSQLLocalDB;Database=AkuranaExplore;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

To use a full SQL Server instance, replace it with your own server/database and credentials. The app reads `ConnectionStrings:DefaultConnection`; if it is missing, it falls back to the LocalDB string above.

### Schema changes and resetting the database

The project intentionally uses `EnsureCreated()` rather than EF migrations. That makes a fresh demo run simple, but **`EnsureCreated` will not update an existing schema after model changes**.

To reset:

1. Stop the app.
2. Drop the database using Visual Studio's SQL Server Object Explorer, SSMS, or `sqlcmd`:

   ```powershell
   sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "DROP DATABASE IF EXISTS AkuranaExplore;"
   ```

3. Run `dotnet run --launch-profile http` again. The database is recreated and reseeded automatically.

Deleting the database removes admin-created places and categories as well as the seeded demo content.

---

## 6. Planner assumptions

- Day starts at **8:30 AM** from Akurana (7.2797, 80.6280).
- Travel time is estimated at **25 km/h** to reflect hill-country roads and stops.
- The first leg uses the stored `DistanceFromAkuranaKm`; later legs use straight-line (Haversine) distance between coordinates. These are approximations for planning, not routing instructions.
- Total distance is one-way along the plan; it does not include the return trip to Akurana.
- Plans are stored in the browser session for up to **2 hours** of inactivity. Closing the browser or clearing cookies loses the plan.
- Reordering is manual (up/down). "Shorten the route" reorders automatically but can still be adjusted afterwards.

---

## 7. Validation and error handling

- Server-side validation covers required fields, length limits, numeric ranges, category existence, and duplicate place names.
- Client-side hints mirror the server rules; server validation remains authoritative.
- Unknown place IDs and routes return a branded 404 page.
- In production, unhandled exceptions route to the shared error page and HTTPS redirection/HSTS are enabled.
- Anti-forgery tokens protect admin and planner state-changing requests.
- Flash messages confirm add/remove/update/delete actions; inline alerts explain blocked operations such as deleting a non-empty category.

---

## 8. Accessibility and responsiveness

- Semantic landmarks (`header`, `nav`, `main`, `footer`), a skip link, labelled form fields, and visible focus styles.
- Status messages use `aria-live`; icon-only buttons have accessible names.
- Images use local assets with alt text where they convey meaning, and empty alt text where they are decorative.
- Responsive breakpoints cover wide desktop, tablet and phone layouts; the main navigation collapses on small screens.
- Print styles hide navigation and interactive controls so a plan prints cleanly.
- Motion respects `prefers-reduced-motion` where reveal animations are used.

---

## 9. Security notes

- Admin passwords are hashed with **PBKDF2-SHA256, 100,000 iterations**, a 128-bit random salt, and a 256-bit derived key. Verification uses a fixed-time comparison.
- The seeded credentials are for local demonstration only. For any real deployment:
  - replace the seeded password (delete the admin row and seed a new hash, or add a controlled password-change flow),
  - use HTTPS and keep the production HTTPS redirection enabled,
  - store the real connection string in user secrets, environment variables, or another secret store rather than source control,
  - consider account lockout/rate limiting for the login endpoint.
- Admin cookies are `HttpOnly`, use `SameSite=Lax` by default, and expire after 8 hours with sliding expiration.
- Public pages never display the admin username, password, or password hash.

---

## 10. Manual test checklist

1. Home page shows 13 places, 6 categories, and featured cards.
2. Search for `biriyani` returns Kandy Muslim Hotel; a nonsense search shows the empty state.
3. Filter by Religious and sort by distance; counts and ordering update.
4. Open a place, add it to the plan, then try adding it again — a duplicate message appears.
5. In the planner, move stops up/down, shorten the route, remove a stop, then clear the plan and confirm the empty state.
6. Visit `/Admin/Dashboard` while signed out — you are redirected to login.
7. Sign in as `admin` / `Akurana@2026`; the dashboard shows live counts.
8. Submit the place form empty — server validation messages appear. Try a duplicate name — it is rejected.
9. Create a place, edit it, confirm the public details update, then delete it after the confirmation prompt.
10. Add an empty category, rename it, delete it. Try deleting Religious — it is blocked while places remain.
11. Sign out; `/Admin/Dashboard` redirects to login again.
12. Visit `/Places/Details/9999` and an unknown URL — both show the branded 404 page.
13. Shrink the browser to phone width and open the navigation menu; check the planner, place cards and admin table remain usable.
14. Print a plan and confirm navigation/interactive controls are hidden.

---

## 11. Known limitations

- Demo data is seeded, static, and not externally verified.
- One administrator account; no role management or self-service password reset.
- Images are stored as local paths or URLs — there is no upload pipeline.
- No EF migrations; dropping and recreating the database is required after schema changes.
- Planner state is session-only and does not sync between devices.
- Travel calculations are estimates, not turn-by-turn routing.
- No booking, payments, or visitor accounts by design.

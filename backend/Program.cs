using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Authentication.Cookies;
using TicketSystem.Api.ViewModels;

var builder = WebApplication.CreateBuilder(args);
var workspaceRoot = Directory.GetParent(builder.Environment.ContentRootPath)?.FullName ?? builder.Environment.ContentRootPath;
var appRoot = Directory.Exists(Path.Combine(workspaceRoot, "ticket-system"))
    ? Path.Combine(workspaceRoot, "ticket-system")
    : workspaceRoot;

builder.Services.AddSingleton(new TicketRepository(appRoot));
builder.Services.AddControllersWithViews();
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/account/login";
        options.LogoutPath = "/account/logout";
        options.AccessDeniedPath = "/account/login";
        options.Cookie.Name = "TicketSystem.Auth";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
builder.Services.AddAuthorization();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

var app = builder.Build();
var staticRoot = new PhysicalFileProvider(appRoot);

app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = staticRoot });
app.UseStaticFiles(new StaticFileOptions { FileProvider = staticRoot });
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapGet("/api/health", () => Results.Ok(new { ok = true, service = "TicketSystem.Api" }));
app.MapPost("/api/system/reset", (HttpContext http, TicketRepository repo) =>
{
    if (!http.User.IsInRole("admin"))
    {
        return Results.Forbid();
    }

    repo.Reset();
    return Results.Ok(new { ok = true });
});

app.MapPost("/api/auth/login", async (HttpContext http, AuthLoginRequest request, TicketRepository repo) =>
{
    if (!repo.Authenticate(request.Email, request.Password, out var user))
    {
        return Results.Unauthorized();
    }

    await http.SignInUserAsync(user!);
    return Results.Ok(UserDto.From(user!));
});

app.MapPost("/api/auth/logout", async (HttpContext http) =>
{
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Ok(new { ok = true });
});

app.MapGet("/api/auth/me", (HttpContext http, TicketRepository repo) =>
{
    var currentUser = http.GetCurrentUser(repo);
    return currentUser is null ? Results.Unauthorized() : Results.Ok(UserDto.From(currentUser));
});

app.MapGet("/api/users", (HttpContext http, TicketRepository repo) =>
{
    if (http.User.Identity?.IsAuthenticated != true)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(repo.GetUsers().Select(UserDto.From));
});
app.MapGet("/api/users/{id:int}", (HttpContext http, int id, TicketRepository repo) =>
{
    if (http.User.Identity?.IsAuthenticated != true)
    {
        return Results.Unauthorized();
    }

    return repo.TryGetUser(id, out var user) ? Results.Ok(UserDto.From(user!)) : Results.NotFound();
});
app.MapGet("/api/users/by-email", (string email, TicketRepository repo) =>
    repo.TryGetUserByEmail(email, out var user) ? Results.Ok(UserDto.From(user!)) : Results.NotFound());
app.MapGet("/api/users/by-username", (string username, TicketRepository repo) =>
    repo.TryGetUserByUsername(username, out var user) ? Results.Ok(UserDto.From(user!)) : Results.NotFound());
app.MapPost("/api/users", (HttpContext http, UserCreateRequest request, TicketRepository repo) =>
{
    try
    {
        var isAuthenticatedAdmin = http.User.IsInRole("admin");
        var effectiveRole = isAuthenticatedAdmin && !string.IsNullOrWhiteSpace(request.Role)
            ? request.Role
            : "user";
        var user = repo.CreateUser(request with { Role = effectiveRole });
        return Results.Created($"/api/users/{user.Id}", UserDto.From(user));
    }
    catch (InvalidOperationException ex)
    {
        return ex.Message.Contains("zaten", StringComparison.OrdinalIgnoreCase)
            ? Results.Conflict(new { message = ex.Message })
            : Results.BadRequest(new { message = ex.Message });
    }
});
app.MapPut("/api/users/{id:int}", (HttpContext http, int id, JsonElement payload, TicketRepository repo) =>
{
    if (!http.User.IsInRole("admin"))
    {
        return Results.Forbid();
    }

    try
    {
        return repo.UpdateUser(id, payload, out var user) ? Results.Ok(UserDto.From(user!)) : Results.NotFound();
    }
    catch (InvalidOperationException ex)
    {
        return ex.Message.Contains("zaten", StringComparison.OrdinalIgnoreCase)
            ? Results.Conflict(new { message = ex.Message })
            : Results.BadRequest(new { message = ex.Message });
    }
});
app.MapDelete("/api/users/{id:int}", (HttpContext http, int id, TicketRepository repo) =>
{
    if (!http.User.IsInRole("admin"))
    {
        return Results.Forbid();
    }

    return repo.DeleteUser(id) ? Results.NoContent() : Results.NotFound();
});

app.MapGet("/api/tickets", (HttpContext http, string? status, string? priority, string? category, int? userId, int? assignedTo, string? search, TicketRepository repo) =>
{
    if (http.User.Identity?.IsAuthenticated != true)
    {
        return Results.Unauthorized();
    }

    var currentUser = http.GetCurrentUser(repo);
    if (currentUser is not null && currentUser.Role == "user")
    {
        userId = currentUser.Id;
        assignedTo = null;
    }

    return Results.Ok(repo.FilterTickets(status, priority, category, userId, assignedTo, search));
});
app.MapGet("/api/tickets/{id:int}", (HttpContext http, int id, TicketRepository repo) =>
{
    if (http.User.Identity?.IsAuthenticated != true)
    {
        return Results.Unauthorized();
    }

    var currentUser = http.GetCurrentUser(repo);
    if (currentUser is null)
    {
        return Results.Unauthorized();
    }

    return repo.TryGetTicket(id, out var ticket) && http.CanViewTicket(ticket!, currentUser)
        ? Results.Ok(ticket)
        : Results.NotFound();
});
app.MapPost("/api/tickets", (HttpContext http, TicketCreateRequest request, TicketRepository repo) =>
{
    try
    {
        var currentUser = http.GetCurrentUser(repo);
        if (currentUser is null)
        {
            return Results.Unauthorized();
        }

        var ticket = repo.CreateTicket(request with { UserId = currentUser.Id });
        return Results.Created($"/api/tickets/{ticket.Id}", ticket);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});
app.MapPut("/api/tickets/{id:int}", (HttpContext http, int id, JsonElement payload, TicketRepository repo) =>
{
    if (!http.User.IsInRole("admin") && !http.User.IsInRole("support"))
    {
        return Results.Forbid();
    }

    return repo.UpdateTicket(id, payload, out var ticket) ? Results.Ok(ticket) : Results.NotFound();
});
app.MapDelete("/api/tickets/{id:int}", (HttpContext http, int id, TicketRepository repo) =>
{
    if (!http.User.IsInRole("admin"))
    {
        return Results.Forbid();
    }

    return repo.DeleteTicket(id) ? Results.NoContent() : Results.NotFound();
});
app.MapPost("/api/tickets/{id:int}/comments", (HttpContext http, int id, AddCommentRequest request, TicketRepository repo) =>
{
    try
    {
        var currentUser = http.GetCurrentUser(repo);
        if (currentUser is null)
        {
            return Results.Unauthorized();
        }

        if (!http.CanCommentOnTicket(id, currentUser, repo))
        {
            return Results.Forbid();
        }

        return repo.AddComment(id, currentUser.Id, request.Text, out var comment)
            ? Results.Created($"/api/tickets/{id}/comments/{comment!.Id}", comment)
            : Results.NotFound();
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});
app.MapGet("/api/stats", (HttpContext http, TicketRepository repo) =>
{
    if (http.User.Identity?.IsAuthenticated != true)
    {
        return Results.Unauthorized();
    }

    var currentUser = http.GetCurrentUser(repo);
    if (currentUser is null)
    {
        return Results.Unauthorized();
    }

    return currentUser.Role == "user"
        ? Results.Ok(repo.GetStatsForUser(currentUser.Id))
        : Results.Ok(repo.GetStats());
});

app.MapGet("/api/categories", (HttpContext http, TicketRepository repo) =>
{
    if (http.User.Identity?.IsAuthenticated != true)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(repo.GetCategories());
});
app.MapPost("/api/categories", (HttpContext http, CategoryCreateRequest request, TicketRepository repo) =>
{
    if (!http.User.IsInRole("admin") && !http.User.IsInRole("support"))
    {
        return Results.Forbid();
    }

    repo.AddCategory(request.Name);
    return Results.Ok(new { ok = true });
});

app.Run();

public static class HttpContextAuthExtensions
{
    public static async Task SignInUserAsync(this HttpContext http, User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.GivenName, user.FullName),
            new(ClaimTypes.Role, user.Role)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var properties = new AuthenticationProperties
        {
            IsPersistent = true,
            AllowRefresh = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        };

        await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);
    }

    public static User? GetCurrentUser(this HttpContext http, TicketRepository repo)
    {
        if (http.User.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var idValue = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idValue, out var userId))
        {
            return null;
        }

        return repo.TryGetUser(userId, out var user) ? user : null;
    }

    public static bool CanViewTicket(this HttpContext http, Ticket ticket, User currentUser)
    {
        return currentUser.Role is "admin" or "support" || ticket.UserId == currentUser.Id;
    }

    public static bool CanCommentOnTicket(this HttpContext http, int ticketId, User currentUser, TicketRepository repo)
    {
        if (currentUser.Role is "admin" or "support")
        {
            return true;
        }

        return repo.TryGetTicket(ticketId, out var ticket) && ticket is not null && ticket.UserId == currentUser.Id;
    }
}

public sealed class TicketRepository
{
    private readonly object _gate = new();
    private readonly string _seedPath;
    private readonly string _runtimePath;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private TicketDatabase _db;

    public TicketRepository(string repoRoot)
    {
        var dataDir = Path.Combine(repoRoot, "data");
        Directory.CreateDirectory(dataDir);
        _seedPath = Path.Combine(dataDir, "db.json");
        _runtimePath = Path.Combine(dataDir, "db.runtime.json");

        if (!File.Exists(_runtimePath) && File.Exists(_seedPath))
        {
            File.Copy(_seedPath, _runtimePath, overwrite: true);
        }

        _db = Load();
        Save();
    }

    public void Reset()
    {
        lock (_gate)
        {
            _db = LoadSeed();
            Save();
        }
    }

    public IReadOnlyList<User> GetUsers()
    {
        lock (_gate)
        {
            return _db.Users.OrderBy(u => u.Id).ToList();
        }
    }

    public bool TryGetUser(int id, out User? user)
    {
        lock (_gate)
        {
            user = _db.Users.FirstOrDefault(u => u.Id == id);
            return user is not null;
        }
    }

    public bool TryGetUserByEmail(string email, out User? user)
    {
        lock (_gate)
        {
            user = _db.Users.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
            return user is not null;
        }
    }

    public bool TryGetUserByUsername(string username, out User? user)
    {
        lock (_gate)
        {
            user = _db.Users.FirstOrDefault(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
            return user is not null;
        }
    }

    public User CreateUser(UserCreateRequest request)
    {
        lock (_gate)
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.FullName))
            {
                throw new InvalidOperationException("Kullanici bilgileri eksik.");
            }

            EnsureUserUnique(request.Email, request.Username);
            var user = new User
            {
                Id = _db.NextUserId++,
                Username = request.Username.Trim(),
                Email = request.Email.Trim(),
                Password = PasswordHasher.Hash(request.Password),
                FullName = request.FullName.Trim(),
                Role = string.IsNullOrWhiteSpace(request.Role) ? "user" : request.Role.Trim(),
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _db.Users.Add(user);
            Save();
            return user;
        }
    }

    public bool UpdateUser(int id, JsonElement payload, out User? user)
    {
        lock (_gate)
        {
            user = _db.Users.FirstOrDefault(u => u.Id == id);
            if (user is null)
            {
                return false;
            }

            if (TryGetString(payload, "username", out var username) && !string.IsNullOrWhiteSpace(username))
            {
                EnsureUsernameUnique(username, id);
                user.Username = username.Trim();
            }

            if (TryGetString(payload, "email", out var email) && !string.IsNullOrWhiteSpace(email))
            {
                EnsureEmailUnique(email, id);
                user.Email = email.Trim();
            }

            if (TryGetString(payload, "password", out var password) && !string.IsNullOrWhiteSpace(password))
            {
                user.Password = PasswordHasher.Hash(password);
            }

            if (TryGetString(payload, "fullName", out var fullName) && !string.IsNullOrWhiteSpace(fullName))
            {
                user.FullName = fullName.Trim();
            }

            if (TryGetString(payload, "role", out var role) && !string.IsNullOrWhiteSpace(role))
            {
                user.Role = role.Trim();
            }

            if (TryGetBool(payload, "isActive", out var isActive))
            {
                user.IsActive = isActive;
            }

            Save();
            return true;
        }
    }

    public bool DeleteUser(int id)
    {
        lock (_gate)
        {
            var user = _db.Users.FirstOrDefault(u => u.Id == id);
            if (user is null)
            {
                return false;
            }

            _db.Users.Remove(user);
            Save();
            return true;
        }
    }

    public bool Authenticate(string email, string password, out User? user)
    {
        lock (_gate)
        {
            user = _db.Users.FirstOrDefault(u =>
                string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase) &&
                PasswordHasher.Verify(password, u.Password) &&
                u.IsActive);
            return user is not null;
        }
    }

    public IReadOnlyList<Ticket> FilterTickets(string? status, string? priority, string? category, int? userId, int? assignedTo, string? search)
    {
        lock (_gate)
        {
            IEnumerable<Ticket> tickets = _db.Tickets;

            if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
            {
                tickets = tickets.Where(t => string.Equals(t.Status, status, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(priority) && !string.Equals(priority, "all", StringComparison.OrdinalIgnoreCase))
            {
                tickets = tickets.Where(t => string.Equals(t.Priority, priority, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(category) && !string.Equals(category, "all", StringComparison.OrdinalIgnoreCase))
            {
                tickets = tickets.Where(t => string.Equals(t.Category, category, StringComparison.OrdinalIgnoreCase));
            }

            if (userId.HasValue)
            {
                tickets = tickets.Where(t => t.UserId == userId.Value);
            }

            if (assignedTo.HasValue)
            {
                tickets = tickets.Where(t => t.AssignedTo == assignedTo.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var query = search.Trim();
                tickets = tickets.Where(t =>
                    Contains(t.Title, query) ||
                    Contains(t.TicketNo, query) ||
                    Contains(t.Description, query));
            }

            return tickets
                .OrderByDescending(t => t.CreatedAt)
                .ToList();
        }
    }

    public bool TryGetTicket(int id, out Ticket? ticket)
    {
        lock (_gate)
        {
            ticket = _db.Tickets.FirstOrDefault(t => t.Id == id);
            return ticket is not null;
        }
    }

    public Ticket CreateTicket(TicketCreateRequest request)
    {
        lock (_gate)
        {
            if (string.IsNullOrWhiteSpace(request.Title) ||
                string.IsNullOrWhiteSpace(request.Description) ||
                string.IsNullOrWhiteSpace(request.Category))
            {
                throw new InvalidOperationException("Talep bilgileri eksik.");
            }

            var now = DateTime.UtcNow;
            var ticket = new Ticket
            {
                Id = _db.NextTicketId,
                TicketNo = $"TKT-{_db.NextTicketId:0000}",
                Title = request.Title.Trim(),
                Description = request.Description.Trim(),
                Category = request.Category.Trim(),
                Priority = string.IsNullOrWhiteSpace(request.Priority) ? "medium" : request.Priority.Trim(),
                Status = "open",
                UserId = request.UserId,
                AssignedTo = null,
                CreatedAt = now,
                UpdatedAt = now,
                Comments = new List<Comment>()
            };

            _db.NextTicketId++;
            _db.Tickets.Add(ticket);
            Save();
            return ticket;
        }
    }

    public bool UpdateTicket(int id, JsonElement payload, out Ticket? ticket)
    {
        lock (_gate)
        {
            ticket = _db.Tickets.FirstOrDefault(t => t.Id == id);
            if (ticket is null)
            {
                return false;
            }

            if (TryGetString(payload, "title", out var title) && !string.IsNullOrWhiteSpace(title))
            {
                ticket.Title = title.Trim();
            }

            if (TryGetString(payload, "description", out var description) && !string.IsNullOrWhiteSpace(description))
            {
                ticket.Description = description.Trim();
            }

            if (TryGetString(payload, "category", out var category) && !string.IsNullOrWhiteSpace(category))
            {
                ticket.Category = category.Trim();
            }

            if (TryGetString(payload, "priority", out var priority) && !string.IsNullOrWhiteSpace(priority))
            {
                ticket.Priority = priority.Trim();
            }

            if (TryGetString(payload, "status", out var status) && !string.IsNullOrWhiteSpace(status))
            {
                ticket.Status = status.Trim();
            }

            if (TryGetInt(payload, "userId", out var userId))
            {
                ticket.UserId = userId;
            }

            if (TryGetNullableInt(payload, "assignedTo", out var assignedTo))
            {
                ticket.AssignedTo = assignedTo;
            }

            ticket.UpdatedAt = DateTime.UtcNow;
            Save();
            return true;
        }
    }

    public bool DeleteTicket(int id)
    {
        lock (_gate)
        {
            var ticket = _db.Tickets.FirstOrDefault(t => t.Id == id);
            if (ticket is null)
            {
                return false;
            }

            _db.Tickets.Remove(ticket);
            Save();
            return true;
        }
    }

    public bool AddComment(int ticketId, int userId, string text, out Comment? comment)
    {
        lock (_gate)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException("Yorum metni bos olamaz.");
            }

            var ticket = _db.Tickets.FirstOrDefault(t => t.Id == ticketId);
            if (ticket is null)
            {
                comment = null;
                return false;
            }

            comment = new Comment
            {
                Id = _db.NextCommentId++,
                UserId = userId,
                Text = text.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            ticket.Comments.Add(comment);
            ticket.UpdatedAt = DateTime.UtcNow;
            Save();
            return true;
        }
    }

    public Stats GetStats()
    {
        lock (_gate)
        {
            return new Stats
            {
                Total = _db.Tickets.Count,
                Open = _db.Tickets.Count(t => t.Status == "open"),
                InProgress = _db.Tickets.Count(t => t.Status == "in-progress"),
                Resolved = _db.Tickets.Count(t => t.Status == "resolved"),
                Closed = _db.Tickets.Count(t => t.Status == "closed"),
                Critical = _db.Tickets.Count(t => t.Priority == "critical"),
                Unassigned = _db.Tickets.Count(t => t.Status == "open" && t.AssignedTo is null)
            };
        }
    }

    public Stats GetStatsForUser(int userId)
    {
        lock (_gate)
        {
            var tickets = _db.Tickets.Where(t => t.UserId == userId).ToList();
            return new Stats
            {
                Total = tickets.Count,
                Open = tickets.Count(t => t.Status == "open"),
                InProgress = tickets.Count(t => t.Status == "in-progress"),
                Resolved = tickets.Count(t => t.Status == "resolved"),
                Closed = tickets.Count(t => t.Status == "closed"),
                Critical = tickets.Count(t => t.Priority == "critical"),
                Unassigned = tickets.Count(t => t.Status == "open" && t.AssignedTo is null)
            };
        }
    }

    public IReadOnlyList<string> GetCategories()
    {
        lock (_gate)
        {
            return _db.Categories.ToList();
        }
    }

    public void AddCategory(string name)
    {
        lock (_gate)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            var trimmed = name.Trim();
            if (_db.Categories.All(c => !string.Equals(c, trimmed, StringComparison.OrdinalIgnoreCase)))
            {
                _db.Categories.Add(trimmed);
                Save();
            }
        }
    }

    private TicketDatabase Load()
    {
        var path = File.Exists(_runtimePath) ? _runtimePath : _seedPath;
        var json = File.ReadAllText(path, Encoding.UTF8);
        return NormalizePasswords(JsonSerializer.Deserialize<TicketDatabase>(json, _jsonOptions) ?? new TicketDatabase());
    }

    private TicketDatabase LoadSeed()
    {
        if (!File.Exists(_seedPath))
        {
            return new TicketDatabase();
        }

        var json = File.ReadAllText(_seedPath, Encoding.UTF8);
        return NormalizePasswords(JsonSerializer.Deserialize<TicketDatabase>(json, _jsonOptions) ?? new TicketDatabase());
    }

    private static TicketDatabase NormalizePasswords(TicketDatabase db)
    {
        foreach (var user in db.Users)
        {
            if (!PasswordHasher.IsHashed(user.Password))
            {
                user.Password = PasswordHasher.Hash(user.Password);
            }
        }

        return db;
    }

    private void Save()
    {
        var json = JsonSerializer.Serialize(_db, _jsonOptions);
        File.WriteAllText(_runtimePath, json, Encoding.UTF8);
    }

    private void EnsureUserUnique(string email, string username)
    {
        EnsureEmailUnique(email);
        EnsureUsernameUnique(username);
    }

    private void EnsureEmailUnique(string email, int? ignoreUserId = null)
    {
        if (_db.Users.Any(u =>
                string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase) &&
                (!ignoreUserId.HasValue || u.Id != ignoreUserId.Value)))
        {
            throw new InvalidOperationException("Bu e-posta adresi zaten kullaniliyor.");
        }
    }

    private void EnsureUsernameUnique(string username, int? ignoreUserId = null)
    {
        if (_db.Users.Any(u =>
                string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase) &&
                (!ignoreUserId.HasValue || u.Id != ignoreUserId.Value)))
        {
            throw new InvalidOperationException("Bu kullanici adi zaten alinmis.");
        }
    }

    private static bool Contains(string? source, string query)
        => !string.IsNullOrWhiteSpace(source) &&
           source.Contains(query, StringComparison.OrdinalIgnoreCase);

    private static bool TryGetString(JsonElement payload, string name, out string? value)
    {
        value = null;
        if (!payload.TryGetProperty(name, out var element))
        {
            return false;
        }

        value = element.ValueKind == JsonValueKind.Null ? null : element.GetString();
        return true;
    }

    private static bool TryGetBool(JsonElement payload, string name, out bool value)
    {
        value = default;
        if (!payload.TryGetProperty(name, out var element) || element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return false;
        }

        value = element.GetBoolean();
        return true;
    }

    private static bool TryGetInt(JsonElement payload, string name, out int value)
    {
        value = default;
        if (!payload.TryGetProperty(name, out var element) || element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return false;
        }

        value = element.GetInt32();
        return true;
    }

    private static bool TryGetNullableInt(JsonElement payload, string name, out int? value)
    {
        value = null;
        if (!payload.TryGetProperty(name, out var element))
        {
            return false;
        }

        if (element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            value = null;
            return true;
        }

        value = element.GetInt32();
        return true;
    }
}

public sealed class TicketDatabase
{
    public List<User> Users { get; set; } = new();
    public List<Ticket> Tickets { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public int NextUserId { get; set; }
    public int NextTicketId { get; set; }
    public int NextCommentId { get; set; }
}

public sealed class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "user";
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}

public sealed class Ticket
{
    public int Id { get; set; }
    public string TicketNo { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Priority { get; set; } = "medium";
    public string Status { get; set; } = "open";
    public int UserId { get; set; }
    public int? AssignedTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<Comment> Comments { get; set; } = new();
}

public sealed class Comment
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public sealed class Stats
{
    public int Total { get; set; }
    public int Open { get; set; }
    public int InProgress { get; set; }
    public int Resolved { get; set; }
    public int Closed { get; set; }
    public int Critical { get; set; }
    public int Unassigned { get; set; }
}

public sealed record AuthLoginRequest(string Email, string Password);
public sealed record UserCreateRequest(string Username, string Email, string Password, string FullName, string? Role);
public sealed record TicketCreateRequest(string Title, string Description, string Category, string? Priority, int UserId);
public sealed record AddCommentRequest(int UserId, string Text);
public sealed record CategoryCreateRequest(string Name);
public sealed record UserDto(int Id, string Username, string Email, string FullName, string Role, DateTime CreatedAt, bool IsActive)
{
    public static UserDto From(User user) =>
        new(user.Id, user.Username, user.Email, user.FullName, user.Role, user.CreatedAt, user.IsActive);
}

public static class PasswordHasher
{
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int KeySize = 32;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        return $"PBKDF2${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string stored)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(stored))
        {
            return false;
        }

        if (!IsHashed(stored))
        {
            return password == stored;
        }

        var parts = stored.Split('$', 4);
        if (parts.Length != 4 || !int.TryParse(parts[1], out var iterations))
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[2]);
        var expected = Convert.FromBase64String(parts[3]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    public static bool IsHashed(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.StartsWith("PBKDF2$", StringComparison.Ordinal);
}

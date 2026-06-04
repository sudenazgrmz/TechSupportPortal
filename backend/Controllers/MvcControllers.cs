using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Api.ViewModels;

namespace TicketSystem.Api.Controllers;

public sealed class HomeController : Controller
{
    public IActionResult Index() => View();
}

public sealed class AccountController : Controller
{
    private readonly TicketRepository _repo;

    public AccountController(TicketRepository repo)
    {
        _repo = repo;
    }

    [HttpGet("/account/login")]
    [AllowAnonymous]
    public IActionResult Login() => View(new LoginViewModel());

    [HttpPost("/account/login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!_repo.Authenticate(model.Email, model.Password, out var user) || user is null)
        {
            ModelState.AddModelError(string.Empty, "E-posta veya şifre hatalı.");
            return View(model);
        }

        await HttpContext.SignInUserAsync(user);
        return Redirect(user.Role == "user" ? "/user-dashboard.html" : "/admin-dashboard.html");
    }

    [HttpGet("/account/register")]
    [AllowAnonymous]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost("/account/register")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!string.Equals(model.Password, model.ConfirmPassword, StringComparison.Ordinal))
        {
            ModelState.AddModelError(nameof(model.ConfirmPassword), "Şifreler eşleşmiyor.");
            return View(model);
        }

        try
        {
            var user = _repo.CreateUser(new UserCreateRequest(model.Username, model.Email, model.Password, model.FullName, "user"));
            await HttpContext.SignInUserAsync(user);
            return Redirect("/user-dashboard.html");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost("/account/logout")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/index.html");
    }
}

public sealed class DashboardController : Controller
{
    private readonly TicketRepository _repo;

    public DashboardController(TicketRepository repo)
    {
        _repo = repo;
    }

    [HttpGet("/dashboard")]
    [Authorize]
    public IActionResult Index()
    {
        var currentUser = HttpContext.GetCurrentUser(_repo);
        if (currentUser is null)
        {
            return Challenge();
        }

        var users = _repo.GetUsers();
        var userMap = users.ToUserDictionary();
        var viewModel = new DashboardViewModel
        {
            CurrentUser = currentUser.ToRow(),
            Stats = currentUser.Role == "user"
                ? MapStats(_repo.GetStatsForUser(currentUser.Id))
                : MapStats(_repo.GetStats()),
            Tickets = currentUser.Role == "user"
                ? _repo.FilterTickets(null, null, null, currentUser.Id, null, null).Select(t => t.ToListItem(userMap)).ToList()
                : _repo.FilterTickets(null, null, null, null, null, null).Select(t => t.ToListItem(userMap)).ToList(),
            Users = currentUser.Role == "user"
                ? Array.Empty<UserRowViewModel>()
                : users.Select(u => u.ToRow()).ToList()
        };

        return View(viewModel);
    }

    private static StatsViewModel MapStats(Stats stats) =>
        new()
        {
            Total = stats.Total,
            Open = stats.Open,
            InProgress = stats.InProgress,
            Resolved = stats.Resolved,
            Closed = stats.Closed,
            Critical = stats.Critical,
            Unassigned = stats.Unassigned
        };
}

public sealed class TicketsController : Controller
{
    private readonly TicketRepository _repo;

    public TicketsController(TicketRepository repo)
    {
        _repo = repo;
    }

    [HttpGet("/tickets/{id:int}")]
    [Authorize]
    public IActionResult Details(int id)
    {
        var currentUser = HttpContext.GetCurrentUser(_repo);
        if (currentUser is null)
        {
            return Challenge();
        }

        if (!_repo.TryGetTicket(id, out var ticket) || ticket is null)
        {
            return NotFound();
        }

        if (currentUser.Role == "user" && ticket.UserId != currentUser.Id)
        {
            return Forbid();
        }

        var users = _repo.GetUsers();
        var userMap = users.ToUserDictionary();
        var model = new TicketDetailsViewModel
        {
            Ticket = ticket.ToListItem(userMap),
            Comments = ticket.Comments.Select(c => c.ToRow(userMap)).ToList(),
            Users = users.ToUserOptions(ticket.AssignedTo),
            CanEdit = currentUser.Role is "admin" or "support",
            CanAssign = currentUser.Role is "admin" or "support"
        };

        return View(model);
    }
}

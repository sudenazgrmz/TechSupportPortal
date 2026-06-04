using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TicketSystem.Api.ViewModels;

public enum TicketStatus
{
    [Display(Name = "Açık")]
    Open,
    [Display(Name = "İşlemde")]
    InProgress,
    [Display(Name = "Çözüldü")]
    Resolved,
    [Display(Name = "Kapalı")]
    Closed
}

public enum TicketPriority
{
    [Display(Name = "Düşük")]
    Low,
    [Display(Name = "Orta")]
    Medium,
    [Display(Name = "Yüksek")]
    High,
    [Display(Name = "Kritik")]
    Critical
}

public enum UserRole
{
    [Display(Name = "Yönetici")]
    Admin,
    [Display(Name = "Destek Ekibi")]
    Support,
    [Display(Name = "Kullanıcı")]
    User
}

public static class MvcEnumExtensions
{
    public static string ToStoreValue(this TicketStatus status) => status switch
    {
        TicketStatus.Open => "open",
        TicketStatus.InProgress => "in-progress",
        TicketStatus.Resolved => "resolved",
        TicketStatus.Closed => "closed",
        _ => "open"
    };

    public static string ToStoreValue(this TicketPriority priority) => priority switch
    {
        TicketPriority.Low => "low",
        TicketPriority.Medium => "medium",
        TicketPriority.High => "high",
        TicketPriority.Critical => "critical",
        _ => "medium"
    };

    public static string ToStoreValue(this UserRole role) => role switch
    {
        UserRole.Admin => "admin",
        UserRole.Support => "support",
        UserRole.User => "user",
        _ => "user"
    };

    public static TicketStatus ParseStatus(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "open" => TicketStatus.Open,
        "in-progress" => TicketStatus.InProgress,
        "resolved" => TicketStatus.Resolved,
        "closed" => TicketStatus.Closed,
        _ => TicketStatus.Open
    };

    public static TicketPriority ParsePriority(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "low" => TicketPriority.Low,
        "medium" => TicketPriority.Medium,
        "high" => TicketPriority.High,
        "critical" => TicketPriority.Critical,
        _ => TicketPriority.Medium
    };

    public static UserRole ParseRole(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "admin" => UserRole.Admin,
        "support" => UserRole.Support,
        _ => UserRole.User
    };
}

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur.")]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Beni hatırla")]
    public bool RememberMe { get; set; }
}

public sealed class RegisterViewModel
{
    [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
    [StringLength(40, MinimumLength = 3, ErrorMessage = "Kullanıcı adı 3-40 karakter olmalıdır.")]
    [Display(Name = "Kullanıcı Adı")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Ad soyad 3-100 karakter olmalıdır.")]
    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Şifre en az 8 karakter olmalıdır.")]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre tekrarı zorunludur.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Şifreler eşleşmiyor.")]
    [Display(Name = "Şifre Tekrar")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class UserEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
    [StringLength(40, MinimumLength = 3, ErrorMessage = "Kullanıcı adı 3-40 karakter olmalıdır.")]
    [Display(Name = "Kullanıcı Adı")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Ad soyad 3-100 karakter olmalıdır.")]
    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Rol")]
    public UserRole Role { get; set; } = UserRole.User;

    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Şifre en az 8 karakter olmalıdır.")]
    [Display(Name = "Yeni Şifre")]
    public string? Password { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;
}

public sealed class TicketFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Başlık zorunludur.")]
    [StringLength(120, MinimumLength = 3, ErrorMessage = "Başlık 3-120 karakter olmalıdır.")]
    [Display(Name = "Başlık")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Açıklama zorunludur.")]
    [StringLength(4000, MinimumLength = 10, ErrorMessage = "Açıklama en az 10 karakter olmalıdır.")]
    [Display(Name = "Açıklama")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kategori zorunludur.")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "Kategori 2-80 karakter olmalıdır.")]
    [Display(Name = "Kategori")]
    public string Category { get; set; } = string.Empty;

    [Display(Name = "Öncelik")]
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;

    [Display(Name = "Durum")]
    public TicketStatus Status { get; set; } = TicketStatus.Open;

    [Display(Name = "Talep Sahibi")]
    public int UserId { get; set; }

    [Display(Name = "Atanan Kişi")]
    public int? AssignedTo { get; set; }

    public IReadOnlyList<SelectListItem> Categories { get; set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> Users { get; set; } = Array.Empty<SelectListItem>();
}

public sealed class TicketCommentViewModel
{
    [Required(ErrorMessage = "Yorum boş olamaz.")]
    [StringLength(2000, MinimumLength = 2, ErrorMessage = "Yorum 2-2000 karakter olmalıdır.")]
    [Display(Name = "Yorum")]
    public string Text { get; set; } = string.Empty;
}

public sealed class TicketAssignViewModel
{
    [Display(Name = "Atanan Kişi")]
    public int? AssignedTo { get; set; }
}

public sealed class UserRowViewModel
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class UserOptionViewModel
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
}

public sealed class TicketListItemViewModel
{
    public int Id { get; set; }
    public string TicketNo { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public TicketPriority Priority { get; set; }
    public TicketStatus Status { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int? AssignedTo { get; set; }
    public string? AssignedToName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int CommentCount { get; set; }
}

public sealed class TicketCommentRowViewModel
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public sealed class TicketDetailsViewModel
{
    public TicketListItemViewModel Ticket { get; set; } = new();
    public IReadOnlyList<TicketCommentRowViewModel> Comments { get; set; } = Array.Empty<TicketCommentRowViewModel>();
    public TicketCommentViewModel Comment { get; set; } = new();
    public TicketAssignViewModel Assign { get; set; } = new();
    public TicketFormViewModel Edit { get; set; } = new();
    public IReadOnlyList<SelectListItem> Users { get; set; } = Array.Empty<SelectListItem>();
    public bool CanEdit { get; set; }
    public bool CanAssign { get; set; }
}

public sealed class DashboardViewModel
{
    public UserRowViewModel CurrentUser { get; set; } = new();
    public StatsViewModel Stats { get; set; } = new();
    public IReadOnlyList<TicketListItemViewModel> Tickets { get; set; } = Array.Empty<TicketListItemViewModel>();
    public IReadOnlyList<UserRowViewModel> Users { get; set; } = Array.Empty<UserRowViewModel>();
}

public sealed class StatsViewModel
{
    public int Total { get; set; }
    public int Open { get; set; }
    public int InProgress { get; set; }
    public int Resolved { get; set; }
    public int Closed { get; set; }
    public int Critical { get; set; }
    public int Unassigned { get; set; }
}

public static class MvcProjection
{
    public static UserRowViewModel ToRow(this global::User user) =>
        new()
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            Role = MvcEnumExtensions.ParseRole(user.Role),
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };

    public static TicketListItemViewModel ToListItem(this global::Ticket ticket, IReadOnlyDictionary<int, global::User> users)
    {
        users.TryGetValue(ticket.UserId, out var owner);
        users.TryGetValue(ticket.AssignedTo ?? -1, out var assigned);

        return new TicketListItemViewModel
        {
            Id = ticket.Id,
            TicketNo = ticket.TicketNo,
            Title = ticket.Title,
            Description = ticket.Description,
            Category = ticket.Category,
            Priority = MvcEnumExtensions.ParsePriority(ticket.Priority),
            Status = MvcEnumExtensions.ParseStatus(ticket.Status),
            UserId = ticket.UserId,
            UserName = owner?.FullName ?? owner?.Username ?? $"Kullanıcı #{ticket.UserId}",
            AssignedTo = ticket.AssignedTo,
            AssignedToName = assigned?.FullName ?? assigned?.Username,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt,
            CommentCount = ticket.Comments.Count
        };
    }

    public static TicketCommentRowViewModel ToRow(this global::Comment comment, IReadOnlyDictionary<int, global::User> users) =>
        new()
        {
            Id = comment.Id,
            UserId = comment.UserId,
            AuthorName = users.TryGetValue(comment.UserId, out var user)
                ? user.FullName
                : $"Kullanıcı #{comment.UserId}",
            Text = comment.Text,
            CreatedAt = comment.CreatedAt
        };

    public static IReadOnlyDictionary<int, global::User> ToUserDictionary(this IEnumerable<global::User> users) =>
        users.ToDictionary(u => u.Id, u => u);

    public static IReadOnlyList<SelectListItem> ToUserOptions(this IEnumerable<global::User> users, int? selectedId = null) =>
        users.Select(u => new SelectListItem
        {
            Value = u.Id.ToString(),
            Text = $"{u.FullName} ({u.Username})",
            Selected = selectedId.HasValue && selectedId.Value == u.Id
        }).ToList();

    public static IReadOnlyList<SelectListItem> ToCategoryOptions(this IEnumerable<string> categories, string? selected = null) =>
        categories.Select(c => new SelectListItem
        {
            Value = c,
            Text = c,
            Selected = !string.IsNullOrWhiteSpace(selected) && string.Equals(selected, c, StringComparison.OrdinalIgnoreCase)
        }).ToList();
}

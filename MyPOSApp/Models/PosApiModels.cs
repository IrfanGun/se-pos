namespace MyPOSApp.Models;

public sealed record ApiLoginResponse(string AccessToken, string Username, int ExpiresIn,
    IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions);

public sealed record ApiDashboardResponse(DateOnly Date, decimal? SalesToday, int? TransactionsToday,
    int? LowStockProducts, decimal? AverageTransaction, IReadOnlyList<ApiTransactionResponse> RecentTransactions,
    bool DataAvailable, string Username);

public sealed record ApiTransactionResponse(string Number, string Time, string Customer, string Total, string Status);

public sealed record DashboardViewModel(
    DateOnly Date,
    decimal? SalesToday,
    int? TransactionsToday,
    int? LowStockProducts,
    decimal? AverageTransaction,
    bool DataAvailable,
    IReadOnlyList<TransactionViewModel> RecentTransactions);

public sealed record TransactionViewModel(string Number, string Time, string Customer, string Total, string Status);

public sealed record ApiProductResponse(Guid Id, string Name, decimal Price, DateTime CreatedAt, DateTime UpdatedAt);

public sealed record ApiUserResponse(Guid Id, string Username, string DisplayName, bool IsActive,
    DateTime CreatedAt, DateTime UpdatedAt, IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions);

public sealed record ApiRoleResponse(Guid Id, string Name, string Description);

public sealed class UserFormModel
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Email wajib diisi.")]
    [System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessage = "Format email tidak valid.")]
    public string Username { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Nama wajib diisi.")]
    public string DisplayName { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.MinLength(8, ErrorMessage = "Password minimal 8 karakter.")]
    [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
    public string? Password { get; set; }

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Role wajib dipilih.")]
    public string Role { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public sealed record UsersPageViewModel(IReadOnlyList<ApiUserResponse> Users,
    IReadOnlyList<ApiRoleResponse> Roles);

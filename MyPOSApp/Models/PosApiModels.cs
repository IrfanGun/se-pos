namespace MyPOSApp.Models;

public sealed record ApiLoginResponse(string AccessToken, string Username, int ExpiresIn);

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

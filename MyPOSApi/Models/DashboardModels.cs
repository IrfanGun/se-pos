namespace MyPOSApi.Models;

public sealed record TransactionSummary(string Number, string Time, string Customer, string Total, string Status);

public sealed record DashboardResponse(
    DateOnly Date,
    decimal? SalesToday,
    int? TransactionsToday,
    int? LowStockProducts,
    decimal? AverageTransaction,
    IReadOnlyList<TransactionSummary> RecentTransactions,
    bool DataAvailable,
    string Username);

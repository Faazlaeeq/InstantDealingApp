using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;

namespace InstantDealingApp
{
    public class CustomerBalanceItem
    {
        public string CustomerName { get; set; } = string.Empty;
        public double RemainingBalance { get; set; }
        public int UnpaidCount { get; set; }
        public string LastPaymentDate { get; set; } = "-";

        public string BalanceDisplay => $"Rs. {RemainingBalance:N0}";
    }

    public class LedgerTransactionItem
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string ReceiptNumber { get; set; } = string.Empty;
        public string ItemDescription { get; set; } = string.Empty;
        public double Amount { get; set; }
        public bool IsPaid { get; set; }
        public string PaymentDate { get; set; } = string.Empty;
        public string PaymentBatchId { get; set; } = string.Empty;

        public string AmountDisplay => $"Rs. {Amount:N0}";
    }

    public class PaymentRecordItem
    {
        public int Id { get; set; }
        public string PaymentBatchId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string PaymentDate { get; set; } = string.Empty;
        public double AmountPaid { get; set; }
        public int ReceiptsCount { get; set; }
        public string ReceiptsSummary { get; set; } = string.Empty;

        public string AmountDisplay => $"Rs. {AmountPaid:N0}";
    }

    public static class LedgerService
    {
        private static readonly string _dbPath;
        private static readonly string _connectionString;

        static LedgerService()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appDir = Path.Combine(localAppData, "InstantDealingApp");
            if (!Directory.Exists(appDir))
            {
                Directory.CreateDirectory(appDir);
            }
            _dbPath = Path.Combine(appDir, "ledger.db");
            _connectionString = $"Data Source={_dbPath}";
            InitializeDatabase();
        }

        public static string GetDatabasePath() => _dbPath;

        public static void InitializeDatabase()
        {
            try
            {
                using var conn = new SqliteConnection(_connectionString);
                conn.Open();

                string createTransactionsTable = @"
                    CREATE TABLE IF NOT EXISTS CustomerTransactions (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        CustomerName TEXT NOT NULL,
                        Date TEXT NOT NULL,
                        ReceiptNumber TEXT,
                        ItemDescription TEXT,
                        Amount REAL NOT NULL DEFAULT 200.0,
                        IsPaid INTEGER NOT NULL DEFAULT 0,
                        PaymentDate TEXT,
                        PaymentBatchId TEXT
                    );
                    CREATE INDEX IF NOT EXISTS idx_customer ON CustomerTransactions(CustomerName, IsPaid);
                ";

                string createPaymentsTable = @"
                    CREATE TABLE IF NOT EXISTS PaymentRecords (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        PaymentBatchId TEXT NOT NULL,
                        CustomerName TEXT NOT NULL,
                        PaymentDate TEXT NOT NULL,
                        AmountPaid REAL NOT NULL,
                        ReceiptsCount INTEGER NOT NULL,
                        ReceiptsSummary TEXT
                    );
                    CREATE INDEX IF NOT EXISTS idx_payment_customer ON PaymentRecords(CustomerName);
                ";

                using var cmd1 = new SqliteCommand(createTransactionsTable, conn);
                cmd1.ExecuteNonQuery();

                using var cmd2 = new SqliteCommand(createPaymentsTable, conn);
                cmd2.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LedgerService] Init DB Error: {ex.Message}");
            }
        }

        public static void RecordReportFee(string customerName, DateTime date, string receiptNumber, string itemDescription, double fee = 200.0, bool isPaid = false)
        {
            if (string.IsNullOrWhiteSpace(customerName)) customerName = "Walk-in Customer";
            try
            {
                using var conn = new SqliteConnection(_connectionString);
                conn.Open();

                string batchId = isPaid ? "PAID_" + Guid.NewGuid().ToString("N").Substring(0, 8) : "";
                string sql = @"
                    INSERT INTO CustomerTransactions 
                    (CustomerName, Date, ReceiptNumber, ItemDescription, Amount, IsPaid, PaymentDate, PaymentBatchId) 
                    VALUES (@customer, @date, @receipt, @item, @amount, @isPaid, @payDate, @batchId);
                ";

                using var cmd = new SqliteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@customer", customerName.Trim());
                cmd.Parameters.AddWithValue("@date", date.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@receipt", receiptNumber ?? string.Empty);
                cmd.Parameters.AddWithValue("@item", itemDescription ?? "Gold Test");
                cmd.Parameters.AddWithValue("@amount", fee);
                cmd.Parameters.AddWithValue("@isPaid", isPaid ? 1 : 0);
                cmd.Parameters.AddWithValue("@payDate", isPaid ? date.ToString("yyyy-MM-dd HH:mm:ss") : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@batchId", isPaid ? batchId : (object)DBNull.Value);
                cmd.ExecuteNonQuery();

                if (isPaid)
                {
                    string paySql = @"
                        INSERT INTO PaymentRecords 
                        (PaymentBatchId, CustomerName, PaymentDate, AmountPaid, ReceiptsCount, ReceiptsSummary) 
                        VALUES (@batchId, @customer, @date, @amount, 1, @summary);
                    ";
                    using var payCmd = new SqliteCommand(paySql, conn);
                    payCmd.Parameters.AddWithValue("@batchId", batchId);
                    payCmd.Parameters.AddWithValue("@customer", customerName.Trim());
                    payCmd.Parameters.AddWithValue("@date", date.ToString("yyyy-MM-dd HH:mm:ss"));
                    payCmd.Parameters.AddWithValue("@amount", fee);
                    payCmd.Parameters.AddWithValue("@summary", $"Paid on Print: {receiptNumber}");
                    payCmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LedgerService] RecordReportFee Error: {ex.Message}");
            }
        }

        public static List<CustomerBalanceItem> GetCustomersWithBalances(string? filter = null)
        {
            var result = new List<CustomerBalanceItem>();
            try
            {
                using var conn = new SqliteConnection(_connectionString);
                conn.Open();

                string sql = @"
                    SELECT 
                        CustomerName, 
                        SUM(CASE WHEN IsPaid = 0 THEN Amount ELSE 0 END) AS RemainingBalance,
                        SUM(CASE WHEN IsPaid = 0 THEN 1 ELSE 0 END) AS UnpaidCount,
                        (SELECT MAX(PaymentDate) FROM PaymentRecords p WHERE p.CustomerName = t.CustomerName) AS LastPayment
                    FROM CustomerTransactions t
                    GROUP BY CustomerName
                    ORDER BY RemainingBalance DESC, CustomerName ASC;
                ";

                using var cmd = new SqliteCommand(sql, conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string name = reader.GetString(0);
                    if (!string.IsNullOrWhiteSpace(filter) && !name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    double balance = reader.IsDBNull(1) ? 0 : reader.GetDouble(1);
                    int count = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                    string lastPay = reader.IsDBNull(3) ? "-" : reader.GetString(3);

                    result.Add(new CustomerBalanceItem
                    {
                        CustomerName = name,
                        RemainingBalance = balance,
                        UnpaidCount = count,
                        LastPaymentDate = lastPay
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LedgerService] GetCustomersWithBalances Error: {ex.Message}");
            }
            return result;
        }

        public static List<LedgerTransactionItem> GetUnpaidTransactions(string customerName)
        {
            var list = new List<LedgerTransactionItem>();
            if (string.IsNullOrWhiteSpace(customerName)) return list;

            try
            {
                using var conn = new SqliteConnection(_connectionString);
                conn.Open();

                string sql = @"
                    SELECT Id, CustomerName, Date, ReceiptNumber, ItemDescription, Amount, IsPaid 
                    FROM CustomerTransactions 
                    WHERE CustomerName = @customer AND IsPaid = 0 
                    ORDER BY Id ASC;
                ";

                using var cmd = new SqliteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@customer", customerName.Trim());
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new LedgerTransactionItem
                    {
                        Id = reader.GetInt32(0),
                        CustomerName = reader.GetString(1),
                        Date = reader.GetString(2),
                        ReceiptNumber = reader.IsDBNull(3) ? "-" : reader.GetString(3),
                        ItemDescription = reader.IsDBNull(4) ? "-" : reader.GetString(4),
                        Amount = reader.GetDouble(5),
                        IsPaid = false
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LedgerService] GetUnpaidTransactions Error: {ex.Message}");
            }
            return list;
        }

        public static List<PaymentRecordItem> GetPaymentRecords(string customerName)
        {
            var list = new List<PaymentRecordItem>();
            if (string.IsNullOrWhiteSpace(customerName)) return list;

            try
            {
                using var conn = new SqliteConnection(_connectionString);
                conn.Open();

                string sql = @"
                    SELECT Id, PaymentBatchId, CustomerName, PaymentDate, AmountPaid, ReceiptsCount, ReceiptsSummary 
                    FROM PaymentRecords 
                    WHERE CustomerName = @customer 
                    ORDER BY Id DESC;
                ";

                using var cmd = new SqliteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@customer", customerName.Trim());
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new PaymentRecordItem
                    {
                        Id = reader.GetInt32(0),
                        PaymentBatchId = reader.GetString(1),
                        CustomerName = reader.GetString(2),
                        PaymentDate = reader.GetString(3),
                        AmountPaid = reader.GetDouble(4),
                        ReceiptsCount = reader.GetInt32(5),
                        ReceiptsSummary = reader.IsDBNull(6) ? "-" : reader.GetString(6)
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LedgerService] GetPaymentRecords Error: {ex.Message}");
            }
            return list;
        }

        public static double ClearCustomerBalance(string customerName)
        {
            if (string.IsNullOrWhiteSpace(customerName)) return 0;
            double totalCleared = 0;

            try
            {
                using var conn = new SqliteConnection(_connectionString);
                conn.Open();
                using var tx = conn.BeginTransaction();

                // 1. Get all unpaid transactions
                string selectSql = @"
                    SELECT Id, ReceiptNumber, Amount 
                    FROM CustomerTransactions 
                    WHERE CustomerName = @customer AND IsPaid = 0;
                ";

                var ids = new List<int>();
                var receiptNumbers = new List<string>();

                using (var selectCmd = new SqliteCommand(selectSql, conn, tx))
                {
                    selectCmd.Parameters.AddWithValue("@customer", customerName.Trim());
                    using var reader = selectCmd.ExecuteReader();
                    while (reader.Read())
                    {
                        ids.Add(reader.GetInt32(0));
                        string rcpt = reader.IsDBNull(1) ? "" : reader.GetString(1);
                        if (!string.IsNullOrWhiteSpace(rcpt)) receiptNumbers.Add(rcpt);
                        totalCleared += reader.GetDouble(2);
                    }
                }

                if (ids.Count == 0 || totalCleared <= 0)
                {
                    tx.Rollback();
                    return 0;
                }

                string batchId = Guid.NewGuid().ToString("N");
                string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // 2. Mark transactions as paid
                string updateSql = @"
                    UPDATE CustomerTransactions 
                    SET IsPaid = 1, PaymentDate = @now, PaymentBatchId = @batch 
                    WHERE CustomerName = @customer AND IsPaid = 0;
                ";
                using (var updateCmd = new SqliteCommand(updateSql, conn, tx))
                {
                    updateCmd.Parameters.AddWithValue("@now", now);
                    updateCmd.Parameters.AddWithValue("@batch", batchId);
                    updateCmd.Parameters.AddWithValue("@customer", customerName.Trim());
                    updateCmd.ExecuteNonQuery();
                }

                // 3. Record in PaymentRecords
                string summary = receiptNumbers.Count > 0 
                    ? string.Join(", ", receiptNumbers) 
                    : $"{ids.Count} Test Report(s)";

                string insertPaymentSql = @"
                    INSERT INTO PaymentRecords 
                    (PaymentBatchId, CustomerName, PaymentDate, AmountPaid, ReceiptsCount, ReceiptsSummary) 
                    VALUES (@batch, @customer, @now, @amount, @count, @summary);
                ";
                using (var insertCmd = new SqliteCommand(insertPaymentSql, conn, tx))
                {
                    insertCmd.Parameters.AddWithValue("@batch", batchId);
                    insertCmd.Parameters.AddWithValue("@customer", customerName.Trim());
                    insertCmd.Parameters.AddWithValue("@now", now);
                    insertCmd.Parameters.AddWithValue("@amount", totalCleared);
                    insertCmd.Parameters.AddWithValue("@count", ids.Count);
                    insertCmd.Parameters.AddWithValue("@summary", summary);
                    insertCmd.ExecuteNonQuery();
                }

                tx.Commit();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LedgerService] ClearCustomerBalance Error: {ex.Message}");
                return 0;
            }

            return totalCleared;
        }
    }
}

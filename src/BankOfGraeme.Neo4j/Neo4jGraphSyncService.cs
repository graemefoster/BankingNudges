using System.Data;
using BankOfGraeme.Api.Data;
using BankOfGraeme.Api.Models;
using Microsoft.EntityFrameworkCore;
using Neo4j.Driver;

namespace BankOfGraeme.Neo4j;

/// <summary>
/// Builds a point-in-time Neo4j graph snapshot from PostgreSQL.
/// Designed to be idempotent: clears the graph, then reloads everything.
/// Uses UNWIND for bulk operations to keep Cypher round-trips low.
/// </summary>
public class Neo4jGraphSyncService(BankDbContext db, IDriver neo4jDriver)
{
    private const int BatchSize = 1000;

    public async Task SyncAsync(CancellationToken ct = default)
    {
        await using var session = neo4jDriver.AsyncSession();

        // Load data FIRST so a failed PG read doesn't leave Neo4j empty
        Console.WriteLine("📦 Loading data from PostgreSQL...");
        var (customers, accounts, transactions, branches, atms) = await LoadDataAsync(ct);

        Console.WriteLine("🔄 Clearing existing graph...");
        await ClearGraphAsync(session);

        Console.WriteLine("📐 Creating constraints and indexes...");
        await CreateConstraintsAsync(session);

        Console.WriteLine($"  Customers:    {customers.Count}");
        Console.WriteLine($"  Accounts:     {accounts.Count}");
        Console.WriteLine($"  Transactions: {transactions.Count}");
        Console.WriteLine($"  Branches:     {branches.Count}");
        Console.WriteLine($"  ATMs:         {atms.Count}");

        Console.WriteLine("🏗️  Creating nodes...");
        await SyncBranchNodesAsync(session, branches);
        await SyncAtmNodesAsync(session, atms);
        await SyncCustomerNodesAsync(session, customers);
        await SyncAccountNodesAsync(session, accounts);
        await SyncTransactionNodesAsync(session, transactions);

        Console.WriteLine("🔗 Creating relationships...");
        await SyncRelationshipsAsync(session, accounts, transactions, atms);

        Console.WriteLine("✅ Graph sync complete.");
    }

    private static async Task ClearGraphAsync(IAsyncSession session)
    {
        // CALL {} IN TRANSACTIONS requires auto-commit mode (not inside ExecuteWriteAsync)
        await session.RunAsync(
            "CALL { MATCH (n) DETACH DELETE n } IN TRANSACTIONS OF 10000 ROWS");
    }

    private static async Task CreateConstraintsAsync(IAsyncSession session)
    {
        var constraints = new[]
        {
            "CREATE CONSTRAINT customer_id IF NOT EXISTS FOR (c:Customer) REQUIRE c.id IS UNIQUE",
            "CREATE CONSTRAINT account_id IF NOT EXISTS FOR (a:Account) REQUIRE a.id IS UNIQUE",
            "CREATE CONSTRAINT transaction_id IF NOT EXISTS FOR (t:Transaction) REQUIRE t.id IS UNIQUE",
            "CREATE CONSTRAINT branch_id IF NOT EXISTS FOR (b:Branch) REQUIRE b.id IS UNIQUE",
            "CREATE CONSTRAINT atm_id IF NOT EXISTS FOR (a:Atm) REQUIRE a.id IS UNIQUE",
        };

        foreach (var constraint in constraints)
        {
            await session.ExecuteWriteAsync(async tx => await tx.RunAsync(constraint));
        }

        // Additional indexes for common query patterns
        var indexes = new[]
        {
            "CREATE INDEX customer_persona IF NOT EXISTS FOR (c:Customer) ON (c.persona)",
            "CREATE INDEX account_type IF NOT EXISTS FOR (a:Account) ON (a.accountType)",
            "CREATE INDEX transaction_type IF NOT EXISTS FOR (t:Transaction) ON (t.transactionType)",
            "CREATE INDEX transaction_created IF NOT EXISTS FOR (t:Transaction) ON (t.createdAt)",
            "CREATE INDEX transaction_transfer_id IF NOT EXISTS FOR (t:Transaction) ON (t.transferId)",
            "CREATE INDEX branch_state IF NOT EXISTS FOR (b:Branch) ON (b.state)",
        };

        foreach (var index in indexes)
        {
            await session.ExecuteWriteAsync(async tx => await tx.RunAsync(index));
        }
    }

    private async Task<(
        List<Customer> customers,
        List<Account> accounts,
        List<Transaction> transactions,
        List<Branch> branches,
        List<Atm> atms
    )> LoadDataAsync(CancellationToken ct)
    {
        // RepeatableRead ensures a consistent point-in-time snapshot across all queries
        await using var pgTransaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.RepeatableRead, ct);

        var customers = await db.Customers
            .AsNoTracking()
            .ToListAsync(ct);

        var accounts = await db.Accounts
            .AsNoTracking()
            .ToListAsync(ct);

        var transactions = await db.Transactions
            .AsNoTracking()
            .ToListAsync(ct);

        var branches = await db.Branches
            .AsNoTracking()
            .ToListAsync(ct);

        var atms = await db.Atms
            .AsNoTracking()
            .ToListAsync(ct);

        await pgTransaction.CommitAsync(ct);

        return (customers, accounts, transactions, branches, atms);
    }

    private static async Task SyncBranchNodesAsync(IAsyncSession session, List<Branch> branches)
    {
        foreach (var batch in Batch(branches))
        {
            var data = batch.Select(b => new Dictionary<string, object?>
            {
                ["id"] = b.Id,
                ["name"] = b.Name,
                ["address"] = b.Address,
                ["suburb"] = b.Suburb,
                ["state"] = b.State,
                ["postcode"] = b.Postcode,
                ["latitude"] = (double)b.Latitude,
                ["longitude"] = (double)b.Longitude,
            }).ToList();

            await session.ExecuteWriteAsync(async tx =>
            {
                await tx.RunAsync(
                    """
                    UNWIND $batch AS row
                    CREATE (b:Branch {
                        id: row.id, name: row.name, address: row.address,
                        suburb: row.suburb, state: row.state, postcode: row.postcode,
                        latitude: row.latitude, longitude: row.longitude
                    })
                    """,
                    new { batch = data });
            });
        }
        Console.WriteLine($"  ✓ Branches");
    }

    private static async Task SyncAtmNodesAsync(IAsyncSession session, List<Atm> atms)
    {
        foreach (var batch in Batch(atms))
        {
            var data = batch.Select(a => new Dictionary<string, object?>
            {
                ["id"] = a.Id,
                ["locationName"] = a.LocationName,
                ["address"] = a.Address,
                ["suburb"] = a.Suburb,
                ["state"] = a.State,
                ["postcode"] = a.Postcode,
                ["latitude"] = (double)a.Latitude,
                ["longitude"] = (double)a.Longitude,
            }).ToList();

            await session.ExecuteWriteAsync(async tx =>
            {
                await tx.RunAsync(
                    """
                    UNWIND $batch AS row
                    CREATE (a:Atm {
                        id: row.id, locationName: row.locationName, address: row.address,
                        suburb: row.suburb, state: row.state, postcode: row.postcode,
                        latitude: row.latitude, longitude: row.longitude
                    })
                    """,
                    new { batch = data });
            });
        }
        Console.WriteLine($"  ✓ ATMs");
    }

    private static async Task SyncCustomerNodesAsync(IAsyncSession session, List<Customer> customers)
    {
        foreach (var batch in Batch(customers))
        {
            var data = batch.Select(c => new Dictionary<string, object?>
            {
                ["id"] = c.Id,
                ["firstName"] = c.FirstName,
                ["lastName"] = c.LastName,
                ["email"] = c.Email,
                ["phone"] = c.Phone ?? "",
                ["dateOfBirth"] = c.DateOfBirth.ToString("yyyy-MM-dd"),
                ["persona"] = c.Persona ?? "Unknown",
            }).ToList();

            await session.ExecuteWriteAsync(async tx =>
            {
                await tx.RunAsync(
                    """
                    UNWIND $batch AS row
                    CREATE (c:Customer {
                        id: row.id, firstName: row.firstName, lastName: row.lastName,
                        email: row.email, phone: row.phone, dateOfBirth: row.dateOfBirth,
                        persona: row.persona
                    })
                    """,
                    new { batch = data });
            });
        }
        Console.WriteLine($"  ✓ Customers");
    }

    private static async Task SyncAccountNodesAsync(IAsyncSession session, List<Account> accounts)
    {
        foreach (var batch in Batch(accounts))
        {
            var data = batch.Select(a => new Dictionary<string, object?>
            {
                ["id"] = a.Id,
                ["accountType"] = a.AccountType.ToString(),
                ["bsb"] = a.Bsb,
                ["accountNumber"] = a.AccountNumber,
                ["name"] = a.Name,
                ["balance"] = a.Balance.ToString("G"),
                ["isActive"] = a.IsActive,
                ["loanAmount"] = a.LoanAmount?.ToString("G"),
                ["interestRate"] = a.InterestRate?.ToString("G"),
                ["loanTermMonths"] = a.LoanTermMonths,
                ["bonusInterestRate"] = a.BonusInterestRate?.ToString("G"),
            }).ToList();

            await session.ExecuteWriteAsync(async tx =>
            {
                await tx.RunAsync(
                    """
                    UNWIND $batch AS row
                    CREATE (a:Account {
                        id: row.id, accountType: row.accountType, bsb: row.bsb,
                        accountNumber: row.accountNumber, name: row.name, balance: row.balance,
                        isActive: row.isActive, loanAmount: row.loanAmount,
                        interestRate: row.interestRate, loanTermMonths: row.loanTermMonths,
                        bonusInterestRate: row.bonusInterestRate
                    })
                    """,
                    new { batch = data });
            });
        }
        Console.WriteLine($"  ✓ Accounts");
    }

    private static async Task SyncTransactionNodesAsync(IAsyncSession session, List<Transaction> transactions)
    {
        foreach (var batch in Batch(transactions))
        {
            var data = batch.Select(t => new Dictionary<string, object?>
            {
                ["id"] = t.Id,
                ["amount"] = t.Amount.ToString("G"),
                ["description"] = t.Description,
                ["transactionType"] = t.TransactionType.ToString(),
                ["status"] = t.Status.ToString(),
                ["createdAt"] = t.CreatedAt.ToString("o"),
                ["settledAt"] = t.SettledAt?.ToString("o"),
                ["transferId"] = t.TransferId?.ToString(),
                ["originalCurrency"] = t.OriginalCurrency,
                ["originalAmount"] = t.OriginalAmount?.ToString("G"),
                ["exchangeRate"] = t.ExchangeRate?.ToString("G"),
                ["feeAmount"] = t.FeeAmount?.ToString("G"),
            }).ToList();

            await session.ExecuteWriteAsync(async tx =>
            {
                await tx.RunAsync(
                    """
                    UNWIND $batch AS row
                    CREATE (t:Transaction {
                        id: row.id, amount: row.amount, description: row.description,
                        transactionType: row.transactionType, status: row.status,
                        createdAt: row.createdAt, settledAt: row.settledAt, transferId: row.transferId,
                        originalCurrency: row.originalCurrency, originalAmount: row.originalAmount,
                        exchangeRate: row.exchangeRate, feeAmount: row.feeAmount
                    })
                    """,
                    new { batch = data });
            });
        }
        Console.WriteLine($"  ✓ Transactions");
    }

    private static async Task SyncRelationshipsAsync(
        IAsyncSession session,
        List<Account> accounts,
        List<Transaction> transactions,
        List<Atm> atms)
    {
        // Customer -[:OWNS]-> Account
        {
            var data = accounts.Select(a => new Dictionary<string, object>
            {
                ["customerId"] = a.CustomerId,
                ["accountId"] = a.Id,
            }).ToList();

            foreach (var batch in Batch(data))
            {
                await session.ExecuteWriteAsync(async tx =>
                {
                    await tx.RunAsync(
                        """
                        UNWIND $batch AS row
                        MATCH (c:Customer {id: row.customerId})
                        MATCH (a:Account {id: row.accountId})
                        CREATE (c)-[:OWNS]->(a)
                        """,
                        new { batch });
                });
            }
            Console.WriteLine($"  ✓ OWNS ({data.Count})");
        }

        // Account -[:RECORDED]-> Transaction
        {
            var data = transactions.Select(t => new Dictionary<string, object>
            {
                ["accountId"] = t.AccountId,
                ["transactionId"] = t.Id,
            }).ToList();

            foreach (var batch in Batch(data))
            {
                await session.ExecuteWriteAsync(async tx =>
                {
                    await tx.RunAsync(
                        """
                        UNWIND $batch AS row
                        MATCH (a:Account {id: row.accountId})
                        MATCH (t:Transaction {id: row.transactionId})
                        CREATE (a)-[:RECORDED]->(t)
                        """,
                        new { batch });
                });
            }
            Console.WriteLine($"  ✓ RECORDED ({data.Count})");
        }

        // Transaction -[:COUNTERPART_OF]-> Transaction for exact in-bank paired movements.
        // This is deterministic source-of-truth pairing, not heuristic downstream funds tracing.
        {
            var data = transactions
                .Where(t => t.TransferId.HasValue)
                .GroupBy(t => t.TransferId!.Value)
                .Where(g => g.Count() == 2)
                .Select(g =>
                {
                    var pair = g.OrderBy(t => t.Amount).ToList();
                    return new Dictionary<string, object>
                    {
                        ["fromTransactionId"] = pair[0].Id,
                        ["toTransactionId"] = pair[1].Id,
                        ["transferId"] = g.Key.ToString(),
                    };
                })
                .ToList();

            foreach (var batch in Batch(data))
            {
                await session.ExecuteWriteAsync(async tx =>
                {
                    await tx.RunAsync(
                        """
                        UNWIND $batch AS row
                        MATCH (from:Transaction {id: row.fromTransactionId})
                        MATCH (to:Transaction {id: row.toTransactionId})
                        CREATE (from)-[:COUNTERPART_OF {transferId: row.transferId}]->(to)
                        CREATE (to)-[:COUNTERPART_OF {transferId: row.transferId}]->(from)
                        """,
                        new { batch });
                });
            }
            Console.WriteLine($"  ✓ COUNTERPART_OF ({data.Count * 2})");
        }

        // Transaction -[:AT_BRANCH]-> Branch
        {
            var data = transactions
                .Where(t => t.BranchId.HasValue)
                .Select(t => new Dictionary<string, object>
                {
                    ["transactionId"] = t.Id,
                    ["branchId"] = t.BranchId!.Value,
                }).ToList();

            foreach (var batch in Batch(data))
            {
                await session.ExecuteWriteAsync(async tx =>
                {
                    await tx.RunAsync(
                        """
                        UNWIND $batch AS row
                        MATCH (t:Transaction {id: row.transactionId})
                        MATCH (b:Branch {id: row.branchId})
                        CREATE (t)-[:AT_BRANCH]->(b)
                        """,
                        new { batch });
                });
            }
            Console.WriteLine($"  ✓ AT_BRANCH ({data.Count})");
        }

        // Transaction -[:AT_ATM]-> Atm
        {
            var data = transactions
                .Where(t => t.AtmId.HasValue)
                .Select(t => new Dictionary<string, object>
                {
                    ["transactionId"] = t.Id,
                    ["atmId"] = t.AtmId!.Value,
                }).ToList();

            foreach (var batch in Batch(data))
            {
                await session.ExecuteWriteAsync(async tx =>
                {
                    await tx.RunAsync(
                        """
                        UNWIND $batch AS row
                        MATCH (t:Transaction {id: row.transactionId})
                        MATCH (a:Atm {id: row.atmId})
                        CREATE (t)-[:AT_ATM]->(a)
                        """,
                        new { batch });
                });
            }
            Console.WriteLine($"  ✓ AT_ATM ({data.Count})");
        }

        // Atm -[:LOCATED_AT]-> Branch
        {
            var data = atms
                .Where(a => a.BranchId.HasValue)
                .Select(a => new Dictionary<string, object>
                {
                    ["atmId"] = a.Id,
                    ["branchId"] = a.BranchId!.Value,
                }).ToList();

            foreach (var batch in Batch(data))
            {
                await session.ExecuteWriteAsync(async tx =>
                {
                    await tx.RunAsync(
                        """
                        UNWIND $batch AS row
                        MATCH (a:Atm {id: row.atmId})
                        MATCH (b:Branch {id: row.branchId})
                        CREATE (a)-[:LOCATED_AT]->(b)
                        """,
                        new { batch });
                });
            }
            Console.WriteLine($"  ✓ LOCATED_AT ({data.Count})");
        }

        // Account -[:OFFSETS]-> Account (offset account linked to home loan)
        {
            var data = accounts
                .Where(a => a.HomeLoanAccountId.HasValue)
                .Select(a => new Dictionary<string, object>
                {
                    ["offsetAccountId"] = a.Id,
                    ["homeLoanAccountId"] = a.HomeLoanAccountId!.Value,
                }).ToList();

            foreach (var batch in Batch(data))
            {
                await session.ExecuteWriteAsync(async tx =>
                {
                    await tx.RunAsync(
                        """
                        UNWIND $batch AS row
                        MATCH (offset:Account {id: row.offsetAccountId})
                        MATCH (loan:Account {id: row.homeLoanAccountId})
                        CREATE (offset)-[:OFFSETS]->(loan)
                        """,
                        new { batch });
                });
            }
            Console.WriteLine($"  ✓ OFFSETS ({data.Count})");
        }
    }

    private static IEnumerable<List<T>> Batch<T>(IEnumerable<T> source)
    {
        var batch = new List<T>(BatchSize);
        foreach (var item in source)
        {
            batch.Add(item);
            if (batch.Count >= BatchSize)
            {
                yield return batch;
                batch = new List<T>(BatchSize);
            }
        }
        if (batch.Count > 0)
            yield return batch;
    }
}

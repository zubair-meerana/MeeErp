using Mee.Erp.Finance.Core.Contracts.Interfaces;
using Mee.Erp.Finance.Core.Domain.Entities;
using Mee.Erp.Finance.Core.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Mee.Erp.Finance.Core.Tests.Services;

public class AdvancedReconciliationServiceTests
{
    private readonly Mock<FinanceDbContext> _mockContext;
    private readonly AdvancedReconciliationService _service;

    public AdvancedReconciliationServiceTests()
    {
        _mockContext = new Mock<FinanceDbContext>();
        _service = new AdvancedReconciliationService(_mockContext.Object);
    }

    [Fact]
    public async Task PerformAutomatedBankReconciliationAsync_ValidRequest_ReturnsReconciliationResult()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var bankAccountId = Guid.NewGuid();

        var request = new BankReconciliationRequest
        {
            CompanyId = companyId,
            BankAccountId = bankAccountId,
            StatementDate = DateTime.Today,
            StatementBalance = 10000,
            AsOfDate = DateTime.Today,
            ToleranceAmount = 0.01m,
            BankStatementTransactions = new List<BankTransaction>
            {
                new BankTransaction { Id = Guid.NewGuid(), Amount = 5000, TransactionDate = DateTime.Today.AddDays(-5), Description = "Deposit", TransactionType = "Deposit" },
                new BankTransaction { Id = Guid.NewGuid(), Amount = 2000, TransactionDate = DateTime.Today.AddDays(-3), Description = "Check", TransactionType = "Check" }
            },
            SystemTransactions = new List<BankTransaction>
            {
                new BankTransaction { Id = Guid.NewGuid(), Amount = 5000, TransactionDate = DateTime.Today.AddDays(-5), Description = "Deposit", TransactionType = "Deposit" },
                new BankTransaction { Id = Guid.NewGuid(), Amount = 2000, TransactionDate = DateTime.Today.AddDays(-3), Description = "Check", TransactionType = "Check" }
            }
        };

        var bankAccount = new BankAccount
        {
            Id = bankAccountId,
            CompanyId = companyId,
            AccountName = "Main Checking",
            CurrentBalance = 8000
        };

        var ledgerEntries = new List<Domain.Entities.LedgerEntry>
        {
            new Domain.Entities.LedgerEntry { Id = Guid.NewGuid(), AccountId = bankAccountId, Debit = 5000, Credit = 0, EntryDate = DateTime.Today.AddDays(-5) },
            new Domain.Entities.LedgerEntry { Id = Guid.NewGuid(), AccountId = bankAccountId, Debit = 0, Credit = 2000, EntryDate = DateTime.Today.AddDays(-3) }
        };

        _mockContext.Setup(c => c.BankAccounts.FindAsync(It.IsAny<object[]>())).ReturnsAsync(bankAccount);
        _mockContext.Setup(c => c.LedgerEntries.Where(It.IsAny<System.Linq.Expressions.Expression<Func<Domain.Entities.LedgerEntry, bool>>>()))
            .Returns(ledgerEntries.AsQueryable());

        // Act
        var result = await _service.PerformAutomatedBankReconciliationAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.BankAccountId, result.BankAccountId);
        Assert.Equal(request.StatementBalance, result.StatementBalance);
        Assert.Equal(8000, result.SystemBalance); // Based on ledger entries
        Assert.Equal(0, result.Difference); // Perfect match
        Assert.Equal(2, result.Matches.Count);
        Assert.Empty(result.Exceptions);
        Assert.Equal("Reconciled", result.Status);
        Assert.Equal(0, result.OutstandingChecks);
        Assert.Equal(0, result.DepositsInTransit);
    }

    [Fact]
    public async Task PerformAutomatedBankReconciliationAsync_MismatchedTransactions_ReturnsException()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var bankAccountId = Guid.NewGuid();

        var request = new BankReconciliationRequest
        {
            CompanyId = companyId,
            BankAccountId = bankAccountId,
            StatementDate = DateTime.Today,
            StatementBalance = 10000,
            AsOfDate = DateTime.Today,
            ToleranceAmount = 0.01m,
            BankStatementTransactions = new List<BankTransaction>
            {
                new BankTransaction { Id = Guid.NewGuid(), Amount = 5000, TransactionDate = DateTime.Today.AddDays(-5), Description = "Deposit", TransactionType = "Deposit" },
                new BankTransaction { Id = Guid.NewGuid(), Amount = 2000, TransactionDate = DateTime.Today.AddDays(-3), Description = "Check", TransactionType = "Check" }
            },
            SystemTransactions = new List<BankTransaction>
            {
                new BankTransaction { Id = Guid.NewGuid(), Amount = 4000, TransactionDate = DateTime.Today.AddDays(-5), Description = "Deposit", TransactionType = "Deposit" }, // Different amount
                new BankTransaction { Id = Guid.NewGuid(), Amount = 2000, TransactionDate = DateTime.Today.AddDays(-3), Description = "Check", TransactionType = "Check" }
            }
        };

        var bankAccount = new BankAccount
        {
            Id = bankAccountId,
            CompanyId = companyId,
            AccountName = "Main Checking",
            CurrentBalance = 7000
        };

        var ledgerEntries = new List<Domain.Entities.LedgerEntry>
        {
            new Domain.Entities.LedgerEntry { Id = Guid.NewGuid(), AccountId = bankAccountId, Debit = 4000, Credit = 0, EntryDate = DateTime.Today.AddDays(-5) },
            new Domain.Entities.LedgerEntry { Id = Guid.NewGuid(), AccountId = bankAccountId, Debit = 0, Credit = 2000, EntryDate = DateTime.Today.AddDays(-3) }
        };

        _mockContext.Setup(c => c.BankAccounts.FindAsync(It.IsAny<object[]>())).ReturnsAsync(bankAccount);
        _mockContext.Setup(c => c.LedgerEntries.Where(It.IsAny<System.Linq.Expressions.Expression<Func<Domain.Entities.LedgerEntry, bool>>>()))
            .Returns(ledgerEntries.AsQueryable());

        // Act
        var result = await _service.PerformAutomatedBankReconciliationAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.BankAccountId, result.BankAccountId);
        Assert.Equal(request.StatementBalance, result.StatementBalance);
        Assert.Equal(2000, result.Difference); // Due to mismatched deposit
        Assert.Single(result.Matches); // Only the check matches
        Assert.NotEmpty(result.Exceptions);
        Assert.Equal("NotReconciled", result.Status);
        Assert.Contains(result.Exceptions, e => e.ExceptionType == "AmountMismatch");
    }

    [Fact]
    public async Task PerformIntercompanyReconciliationAsync_ValidRequest_ReturnsReconciliationResult()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var relatedCompanyId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var request = new IntercompanyReconciliationRequest
        {
            CompanyId = companyId,
            IntercompanyAccountIds = new List<Guid> { accountId },
            AsOfDate = DateTime.Today,
            RelatedCompanyIds = new List<Guid> { relatedCompanyId }
        };

        var account = new Account
        {
            Id = accountId,
            AccountNumber = "2000",
            Name = "Intercompany Payable",
            AccountType = Domain.Enums.AccountType.Liability,
            CompanyId = companyId
        };

        var ledgerEntries = new List<Domain.Entities.LedgerEntry>
        {
            new Domain.Entities.LedgerEntry { Id = Guid.NewGuid(), AccountId = accountId, Debit = 0, Credit = 10000, EntryDate = DateTime.Today.AddDays(-10) },
            new Domain.Entities.LedgerEntry { Id = Guid.NewGuid(), AccountId = accountId, Debit = 5000, Credit = 0, EntryDate = DateTime.Today.AddDays(-5) }
        };

        _mockContext.Setup(c => c.Accounts.FindAsync(It.IsAny<object[]>())).ReturnsAsync(account);
        _mockContext.Setup(c => c.LedgerEntries.Where(It.IsAny<System.Linq.Expressions.Expression<Func<Domain.Entities.LedgerEntry, bool>>>()))
            .Returns(ledgerEntries.AsQueryable());

        // Act
        var result = await _service.PerformIntercompanyReconciliationAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.CompanyId, result.RequestId);
        Assert.NotEmpty(result.ReconciliationLines);
        Assert.Equal(1, result.ReconciliationLines.Count);
        Assert.Equal(accountId, result.ReconciliationLines.First().AccountId);
        Assert.Equal(relatedCompanyId, result.ReconciliationLines.First().RelatedCompanyId);
        Assert.Equal(-5000, result.ReconciliationLines.First().OurBalance); // Credit 10000 - Debit 5000 = -5000 (liability balance)
        Assert.Equal(5000, result.ReconciliationLines.First().TheirBalance); // Opposite balance
        Assert.Equal(0, result.ReconciliationLines.First().Difference); // Assuming balanced intercompany
        Assert.Equal("Matched", result.ReconciliationLines.First().Status);
        Assert.Equal("Balanced", result.Status);
        Assert.Empty(result.Discrepancies);
    }

    [Fact]
    public async Task PerformBalanceSheetReconciliationAsync_ValidRequest_ReturnsReconciliationResult()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var assetAccountId = Guid.NewGuid();
        var liabilityAccountId = Guid.NewGuid();

        var request = new BalanceSheetReconciliationRequest
        {
            CompanyId = companyId,
            BalanceSheetAccountIds = new List<Guid> { assetAccountId, liabilityAccountId },
            AsOfDate = DateTime.Today,
            SupportingSchedules = new List<SupportingSchedule>
            {
                new SupportingSchedule
                {
                    ScheduleId = Guid.NewGuid(),
                    ScheduleName = "Cash Schedule",
                    ScheduleType = "Detail",
                    ScheduleTotal = 50000,
                    Details = new List<ScheduleDetail>
                    {
                        new ScheduleDetail { DetailId = Guid.NewGuid(), Description = "Bank Account 1", Amount = 30000, Category = "1000" },
                        new ScheduleDetail { DetailId = Guid.NewGuid(), Description = "Petty Cash", Amount = 20000, Category = "1000" }
                    }
                },
                new SupportingSchedule
                {
                    ScheduleId = Guid.NewGuid(),
                    ScheduleName = "Payables Schedule",
                    ScheduleType = "Detail",
                    ScheduleTotal = 25000,
                    Details = new List<ScheduleDetail>
                    {
                        new ScheduleDetail { DetailId = Guid.NewGuid(), Description = "Vendor A", Amount = 15000, Category = "2000" },
                        new ScheduleDetail { DetailId = Guid.NewGuid(), Description = "Vendor B", Amount = 10000, Category = "2000" }
                    }
                }
            }
        };

        var accounts = new List<Account>
        {
            new Account { Id = assetAccountId, AccountNumber = "1000", Name = "Cash", AccountType = Domain.Enums.AccountType.Asset, CompanyId = companyId },
            new Account { Id = liabilityAccountId, AccountNumber = "2000", Name = "Accounts Payable", AccountType = Domain.Enums.AccountType.Liability, CompanyId = companyId }
        };

        var ledgerEntries = new List<Domain.Entities.LedgerEntry>
        {
            new Domain.Entities.LedgerEntry { Id = Guid.NewGuid(), AccountId = assetAccountId, Debit = 50000, Credit = 0, EntryDate = DateTime.Today.AddDays(-1) },
            new Domain.Entities.LedgerEntry { Id = Guid.NewGuid(), AccountId = liabilityAccountId, Debit = 0, Credit = 25000, EntryDate = DateTime.Today.AddDays(-1) }
        };

        _mockContext.Setup(c => c.Accounts.FindAsync(It.IsAny<object[]>())).ReturnsAsync(accounts.First);
        _mockContext.Setup(c => c.Accounts.FindAsync(It.IsAny<object[]>())).ReturnsAsync(accounts.Skip(1).First);
        _mockContext.Setup(c => c.LedgerEntries.Where(It.IsAny<System.Linq.Expressions.Expression<Func<Domain.Entities.LedgerEntry, bool>>>()))
            .Returns(ledgerEntries.AsQueryable());

        // Act
        var result = await _service.PerformBalanceSheetReconciliationAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.CompanyId, result.RequestId);
        Assert.Equal(2, result.ReconciliationLines.Count);
        Assert.Equal(assetAccountId, result.ReconciliationLines.First().AccountId);
        Assert.Equal(50000, result.ReconciliationLines.First().GLBalance);
        Assert.Equal(50000, result.ReconciliationLines.First().ScheduleBalance);
        Assert.Equal(0, result.ReconciliationLines.First().Difference);
        Assert.Equal("Reconciled", result.ReconciliationLines.First().Status);

        Assert.Equal(liabilityAccountId, result.ReconciliationLines.Last().AccountId);
        Assert.Equal(25000, result.ReconciliationLines.Last().GLBalance);
        Assert.Equal(25000, result.ReconciliationLines.Last().ScheduleBalance);
        Assert.Equal(0, result.ReconciliationLines.Last().Difference);
        Assert.Equal("Reconciled", result.ReconciliationLines.Last().Status);

        Assert.Equal(75000, result.TotalGLBalance);
        Assert.Equal(75000, result.TotalScheduleBalance);
        Assert.Equal(0, result.TotalDifference);
        Assert.Equal("Reconciled", result.Status);
        Assert.Empty(result.Exceptions);
    }

    [Fact]
    public async Task PerformInterimReconciliationAsync_ValidRequest_ReturnsReconciliationResult()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var expenseAccountId = Guid.NewGuid();

        var request = new InterimReconciliationRequest
        {
            CompanyId = companyId,
            AccrualAccountIds = new List<Guid> { expenseAccountId },
            AsOfDate = DateTime.Today,
            PeriodEndDate = DateTime.Today.AddMonths(1)
        };

        var account = new Account
        {
            Id = expenseAccountId,
            AccountNumber = "5000",
            Name = "Accrued Expenses",
            AccountType = Domain.Enums.AccountType.Expense,
            CompanyId = companyId
        };

        var ledgerEntries = new List<Domain.Entities.LedgerEntry>
        {
            new Domain.Entities.LedgerEntry { Id = Guid.NewGuid(), AccountId = expenseAccountId, Debit = 10000, Credit = 0, EntryDate = DateTime.Today.AddDays(-5) },
            new Domain.Entities.LedgerEntry { Id = Guid.NewGuid(), AccountId = expenseAccountId, Debit = 5000, Credit = 0, EntryDate = DateTime.Today.AddDays(-2) }
        };

        _mockContext.Setup(c => c.Accounts.FindAsync(It.IsAny<object[]>())).ReturnsAsync(account);
        _mockContext.Setup(c => c.LedgerEntries.Where(It.IsAny<System.Linq.Expressions.Expression<Func<Domain.Entities.LedgerEntry, bool>>>()))
            .Returns(ledgerEntries.AsQueryable());

        // Act
        var result = await _service.PerformInterimReconciliationAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.CompanyId, result.RequestId);
        Assert.NotEmpty(result.ReconciliationLines);
        Assert.Equal(1, result.ReconciliationLines.Count);
        Assert.Equal(expenseAccountId, result.ReconciliationLines.First().AccountId);
        Assert.Equal(15000, result.ReconciliationLines.First().AccruedAmount); // Estimated based on historical
        Assert.Equal(15000, result.ReconciliationLines.First().ActualAmount); // Based on ledger
        Assert.Equal(0, result.ReconciliationLines.First().Variance);
        Assert.Equal(0, result.ReconciliationLines.First().VariancePercentage);
        Assert.Equal("Reconciled", result.ReconciliationLines.First().Status);
        Assert.Equal("Reconciled", result.Status);
        Assert.Empty(result.Exceptions);
    }

    [Fact]
    public async Task PerformVarianceAnalysisAsync_SignificantVariance_ReturnsAnalysisResult()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var revenueAccountId = Guid.NewGuid();
        var expenseAccountId = Guid.NewGuid();

        var request = new VarianceAnalysisRequest
        {
            CompanyId = companyId,
            AccountIds = new List<Guid> { revenueAccountId, expenseAccountId },
            StartDate = DateTime.Today.AddDays(-30),
            EndDate = DateTime.Today,
            VarianceThreshold = 5.0m, // 5% threshold
            VarianceType = "Both",
            IncludeDrillDown = true
        };

        var budget = new Budget
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = "Test Budget",
            FiscalYear = DateTime.Today.Year,
            TotalBudgetAmount = 100000,
            Lines = new List<BudgetLine>
            {
                new BudgetLine { Id = Guid.NewGuid(), BudgetId = Guid.NewGuid(), AccountId = revenueAccountId, BudgetAmount = 60000, ActualAmount = 0, Variance = 0, VariancePercentage = 0 },
                new BudgetLine { Id = Guid.NewGuid(), BudgetId = Guid.NewGuid(), AccountId = expenseAccountId, BudgetAmount = 40000, ActualAmount = 0, Variance = 0, VariancePercentage = 0 }
            }
        };

        var ledgerEntries = new List<Domain.Entities.LedgerEntry>
        {
            new Domain.Entities.LedgerEntry { Id = Guid.NewGuid(), AccountId = revenueAccountId, Debit = 0, Credit = 65000, EntryDate = DateTime.Today.AddDays(-15) }, // 5000 more than budget
            new Domain.Entities.LedgerEntry { Id = Guid.NewGuid(), AccountId = expenseAccountId, Debit = 45000, Credit = 0, EntryDate = DateTime.Today.AddDays(-10) } // 5000 more than budget
        };

        _mockContext.Setup(c => c.Budgets.Include(It.IsAny<string>()).FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Budget, bool>>>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(budget);
        _mockContext.Setup(c => c.LedgerEntries.Where(It.IsAny<System.Linq.Expressions.Expression<Func<Domain.Entities.LedgerEntry, bool>>>()))
            .Returns(ledgerEntries.AsQueryable());

        // Act
        var result = await _service.PerformVarianceAnalysisAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.CompanyId, result.RequestId);
        Assert.NotEmpty(result.VarianceLines);
        Assert.Equal(2, result.VarianceLines.Count); // Both accounts exceed 5% threshold

        var revenueVariance = result.VarianceLines.First(vl => vl.AccountId == revenueAccountId);
        Assert.Equal(60000, revenueVariance.BudgetedAmount);
        Assert.Equal(65000, revenueVariance.ActualAmount);
        Assert.Equal(5000, revenueVariance.Variance);
        Assert.Equal(8.33m, Math.Round(revenueVariance.VariancePercentage, 2));
        Assert.Equal("Favorable", revenueVariance.VarianceType);
        Assert.Equal("Revenue", revenueVariance.VarianceCategory);

        var expenseVariance = result.VarianceLines.First(vl => vl.AccountId == expenseAccountId);
        Assert.Equal(40000, expenseVariance.BudgetedAmount);
        Assert.Equal(45000, expenseVariance.ActualAmount);
        Assert.Equal(5000, expenseVariance.Variance);
        Assert.Equal(12.5m, expenseVariance.VariancePercentage);
        Assert.Equal("Unfavorable", expenseVariance.VarianceType);
        Assert.Equal("Expense", expenseVariance.VarianceCategory);

        Assert.Equal(10000, result.TotalVariance);
        Assert.Equal(10.42m, Math.Round(result.TotalVariancePercentage, 2));
        Assert.Equal("RequiresAttention", result.Status);
        Assert.NotEmpty(result.Investigations);
        Assert.Equal(1, result.Investigations.Count); // Only expense variance exceeds double threshold
    }

    [Fact]
    public async Task ManageReconciliationDocumentsAsync_ValidRequest_ReturnsDocumentResult()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var reconciliationId = Guid.NewGuid();

        var request = new DocumentManagementRequest
        {
            CompanyId = companyId,
            ReconciliationId = reconciliationId,
            ReconciliationType = "Bank",
            Attachments = new List<DocumentAttachment>
            {
                new DocumentAttachment
                {
                    DocumentId = Guid.NewGuid(),
                    FileName = "BankStatement.pdf",
                    FileType = "PDF",
                    FileSize = 102400,
                    FileUrl = "/documents/bankstatement.pdf",
                    Description = "Bank statement for reconciliation",
                    UploadDate = DateTime.Today,
                    UploadedByUserId = Guid.NewGuid(),
                    IsVerified = false
                },
                new DocumentAttachment
                {
                    DocumentId = Guid.NewGuid(),
                    FileName = "Supporting.xlsx",
                    FileType = "Excel",
                    FileSize = 204800,
                    FileUrl = "/documents/supporting.xlsx",
                    Description = "Supporting calculations",
                    UploadDate = DateTime.Today,
                    UploadedByUserId = Guid.NewGuid(),
                    IsVerified = false
                }
            }
        };

        // Act
        var result = await _service.ManageReconciliationDocumentsAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.CompanyId, result.RequestId);
        Assert.Equal(2, result.ProcessedAttachments.Count);
        Assert.Equal("BankStatement.pdf", result.ProcessedAttachments.First().FileName);
        Assert.Equal("Supporting.xlsx", result.ProcessedAttachments.Last().FileName);
        Assert.Equal("Success", result.Status);
        Assert.NotEmpty(result.Messages);
        Assert.Contains(result.Messages, m => m.Contains("Successfully processed 2 document(s)"));
    }
}

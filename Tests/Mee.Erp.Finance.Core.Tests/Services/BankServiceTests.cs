using FluentAssertions;
using Mee.Erp.Finance.Core.Domain.Entities;
using Mee.Erp.Finance.Core.Domain.Enums;
using Mee.Erp.Finance.Core.Persistence;
using Mee.Erp.Finance.Core.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Mee.Erp.Finance.Core.Tests.Services;

public class BankServiceTests
{
    private FinanceDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new FinanceDbContext(options);
    }

    [Fact]
    public async Task CreateBankAccountAsync_Should_Create_Account()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new BankService(context);

        var companyId = Guid.NewGuid();
        var bankAccount = new BankAccount
        {
            AccountNumber = "ACC-001",
            AccountName = "Operating Account",
            BankName = "Test Bank",
            CurrentBalance = 10000,
            AccountType = BankAccountType.Checking,
            CompanyId = companyId
        };

        // 2. ACT
        var result = await service.CreateBankAccountAsync(bankAccount);

        // 3. ASSERT
        result.Should().NotBeNull();
        result.AccountNumber.Should().Be("ACC-001");
        result.AccountName.Should().Be("Operating Account");
        result.CurrentBalance.Should().Be(10000);
        result.AccountType.Should().Be(BankAccountType.Checking);

        var savedAccount = await context.BankAccounts.FindAsync(result.Id);
        savedAccount.Should().NotBeNull();
        savedAccount.AccountNumber.Should().Be("ACC-001");
    }

    [Fact]
    public async Task GetBankAccountByIdAsync_Should_Return_Account_When_Found()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new BankService(context);

        var bankAccount = new BankAccount
        {
            Id = Guid.NewGuid(),
            AccountNumber = "ACC-001",
            AccountName = "Operating Account",
            BankName = "Test Bank",
            CurrentBalance = 10000,
            AccountType = BankAccountType.Checking,
            CompanyId = Guid.NewGuid()
        };

        context.BankAccounts.Add(bankAccount);
        await context.SaveChangesAsync();

        // 2. ACT
        var result = await service.GetBankAccountByIdAsync(bankAccount.Id);

        // 3. ASSERT
        result.Should().NotBeNull();
        result.Id.Should().Be(bankAccount.Id);
        result.AccountNumber.Should().Be("ACC-001");
        result.CurrentBalance.Should().Be(10000);
    }

    [Fact]
    public async Task GetBankAccountByIdAsync_Should_ThrowException_When_Not_Found()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new BankService(context);

        var nonExistentId = Guid.NewGuid();

        // 2. ACT & ASSERT
        await Assert.ThrowsAsync<Exception>(async () =>
            await service.GetBankAccountByIdAsync(nonExistentId)
        );
    }

    [Fact]
    public async Task GetBankAccountsByCompanyAsync_Should_Return_Accounts()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new BankService(context);

        var companyId = Guid.NewGuid();
        var otherCompanyId = Guid.NewGuid();

        var accounts = new List<BankAccount>
        {
            new BankAccount { Id = Guid.NewGuid(), AccountNumber = "ACC-001", AccountName = "Company A Account", BankName = "Bank A", CurrentBalance = 10000, CompanyId = companyId },
            new BankAccount { Id = Guid.NewGuid(), AccountNumber = "ACC-002", AccountName = "Company A Account 2", BankName = "Bank B", CurrentBalance = 20000, CompanyId = companyId },
            new BankAccount { Id = Guid.NewGuid(), AccountNumber = "ACC-003", AccountName = "Company B Account", BankName = "Bank C", CurrentBalance = 30000, CompanyId = otherCompanyId }
        };

        context.BankAccounts.AddRange(accounts);
        await context.SaveChangesAsync();

        // 2. ACT
        var result = await service.GetBankAccountsByCompanyAsync(companyId);

        // 3. ASSERT
        result.Should().HaveCount(2);
        result.Should().ContainSingle(acc => acc.AccountName == "Company A Account");
        result.Should().ContainSingle(acc => acc.AccountName == "Company A Account 2");
        result.Should().NotContain(acc => acc.AccountName == "Company B Account");
    }

    [Fact]
    public async Task ProcessBankTransactionAsync_Should_Process_Transaction()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new BankService(context);

        var companyId = Guid.NewGuid();
        var bankAccountId = Guid.NewGuid();

        var bankAccount = new BankAccount
        {
            Id = bankAccountId,
            AccountNumber = "ACC-001",
            AccountName = "Operating Account",
            BankName = "Test Bank",
            CurrentBalance = 10000,
            AccountType = BankAccountType.Checking,
            CompanyId = companyId
        };

        context.BankAccounts.Add(bankAccount);
        await context.SaveChangesAsync();

        var transaction = new BankTransaction
        {
            BankAccountId = bankAccountId,
            TransactionDate = DateTime.Today,
            Amount = 500,
            TransactionType = "Deposit",
            Description = "Test deposit",
            ReferenceNumber = "REF-001",
            CompanyId = companyId
        };

        // 2. ACT
        var result = await service.ProcessBankTransactionAsync(transaction);

        // 3. ASSERT
        result.Should().NotBeNull();
        result.Amount.Should().Be(500);
        result.TransactionType.Should().Be("Deposit");

        // Verify account balance updated
        var updatedAccount = await context.BankAccounts.FindAsync(bankAccountId);
        updatedAccount.CurrentBalance.Should().Be(10500); // 10000 + 500
    }

    [Fact]
    public async Task ReconcileBankAccountAsync_Should_Perform_Reconciliation()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new BankService(context);

        var companyId = Guid.NewGuid();
        var bankAccountId = Guid.NewGuid();

        var bankAccount = new BankAccount
        {
            Id = bankAccountId,
            AccountNumber = "ACC-001",
            AccountName = "Operating Account",
            BankName = "Test Bank",
            CurrentBalance = 10000,
            AccountType = BankAccountType.Checking,
            CompanyId = companyId
        };

        context.BankAccounts.Add(bankAccount);
        await context.SaveChangesAsync();

        var reconciliation = new BankReconciliation
        {
            BankAccountId = bankAccountId,
            StatementDate = DateTime.Today,
            StatementBalance = 10500,
            ClearedBalance = 10000,
            OutstandingChecks = 0,
            DepositsInTransit = 500,
            CompanyId = companyId
        };

        // 2. ACT
        var result = await service.ReconcileBankAccountAsync(reconciliation);

        // 3. ASSERT
        result.Should().NotBeNull();
        result.StatementBalance.Should().Be(10500);
        result.DepositsInTransit.Should().Be(500);

        var savedReconciliation = await context.BankReconciliations.FindAsync(result.Id);
        savedReconciliation.Should().NotBeNull();
        savedReconciliation.StatementBalance.Should().Be(10500);
    }
}

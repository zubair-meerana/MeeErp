using FluentAssertions;
using Mee.Erp.Finance.Core.Domain.Entities;
using Mee.Erp.Finance.Core.Domain.Enums;
using Mee.Erp.Finance.Core.Persistence;
using Mee.Erp.Finance.Core.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Mee.Erp.Finance.Core.Tests.Services;

public class AccountServiceTests
{
    private FinanceDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new FinanceDbContext(options);
    }

    [Fact]
    public async Task CreateAccountAsync_Should_Create_Account_When_Number_Is_Unique()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new AccountService(context);

        var companyId = Guid.NewGuid();
        var account = new Account
        {
            AccountNumber = "1000",
            Name = "Test Account",
            AccountType = AccountType.Asset,
            CompanyId = companyId
        };

        // 2. ACT
        var result = await service.CreateAccountAsync(account);

        // 3. ASSERT
        result.Should().NotBeNull();
        result.AccountNumber.Should().Be("1000");
        result.Name.Should().Be("Test Account");
        result.AccountType.Should().Be(AccountType.Asset);
        result.CompanyId.Should().Be(companyId);

        var savedAccount = await context.Accounts.FindAsync(result.Id);
        savedAccount.Should().NotBeNull();
        savedAccount.AccountNumber.Should().Be("1000");
    }

    [Fact]
    public async Task CreateAccountAsync_Should_ThrowException_When_Number_Already_Exists()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new AccountService(context);

        var companyId = Guid.NewGuid();
        var existingAccount = new Account
        {
            AccountNumber = "1000",
            Name = "Existing Account",
            AccountType = AccountType.Asset,
            CompanyId = companyId
        };

        context.Accounts.Add(existingAccount);
        await context.SaveChangesAsync();

        var newAccount = new Account
        {
            AccountNumber = "1000", // Same number as existing
            Name = "New Account",
            AccountType = AccountType.Asset,
            CompanyId = companyId
        };

        // 2. ACT & ASSERT
        await Assert.ThrowsAsync<Exception>(async () =>
            await service.CreateAccountAsync(newAccount)
        );
    }

    [Fact]
    public async Task UpdateAccountAsync_Should_Update_Account_When_Exists()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new AccountService(context);

        var account = new Account
        {
            AccountNumber = "1000",
            Name = "Old Name",
            AccountType = AccountType.Asset,
            CompanyId = Guid.NewGuid()
        };

        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        // Update properties
        account.Name = "New Name";
        account.IsActive = false;

        // 2. ACT
        var result = await service.UpdateAccountAsync(account);

        // 3. ASSERT
        result.Should().NotBeNull();
        result.Name.Should().Be("New Name");
        result.IsActive.Should().BeFalse();

        var savedAccount = await context.Accounts.FindAsync(account.Id);
        savedAccount.Name.Should().Be("New Name");
        savedAccount.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAccountAsync_Should_ThrowException_When_Not_Found()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new AccountService(context);

        var account = new Account
        {
            Id = Guid.NewGuid(), // Non-existent ID
            AccountNumber = "1000",
            Name = "Test Account",
            AccountType = AccountType.Asset,
            CompanyId = Guid.NewGuid()
        };

        // 2. ACT & ASSERT
        await Assert.ThrowsAsync<Exception>(async () =>
            await service.UpdateAccountAsync(account)
        );
    }

    [Fact]
    public async Task GetChartOfAccountsAsync_Should_Return_Accounts_For_Company()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new AccountService(context);

        var companyId = Guid.NewGuid();
        var otherCompanyId = Guid.NewGuid();

        var accounts = new List<Account>
        {
            new Account { AccountNumber = "1000", Name = "Company A Asset", AccountType = AccountType.Asset, CompanyId = companyId },
            new Account { AccountNumber = "2000", Name = "Company A Liability", AccountType = AccountType.Liability, CompanyId = companyId },
            new Account { AccountNumber = "1000", Name = "Company B Asset", AccountType = AccountType.Asset, CompanyId = otherCompanyId }
        };

        context.Accounts.AddRange(accounts);
        await context.SaveChangesAsync();

        // 2. ACT
        var result = await service.GetChartOfAccountsAsync(companyId);

        // 3. ASSERT
        result.Should().HaveCount(2);
        result.Should().ContainSingle(a => a.Name == "Company A Asset");
        result.Should().ContainSingle(a => a.Name == "Company A Liability");
        result.Should().NotContain(a => a.Name == "Company B Asset");

        // Verify ordering by account number
        result.First().AccountNumber.Should().Be("1000");
        result.Skip(1).First().AccountNumber.Should().Be("2000");
    }

    [Fact]
    public async Task GetAccountByIdAsync_Should_Return_Account_When_Found()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new AccountService(context);

        var account = new Account
        {
            Id = Guid.NewGuid(),
            AccountNumber = "1000",
            Name = "Test Account",
            AccountType = AccountType.Asset,
            CompanyId = Guid.NewGuid()
        };

        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        // 2. ACT
        var result = await service.GetAccountByIdAsync(account.Id);

        // 3. ASSERT
        result.Should().NotBeNull();
        result.Id.Should().Be(account.Id);
        result.AccountNumber.Should().Be("1000");
        result.Name.Should().Be("Test Account");
    }

    [Fact]
    public async Task GetAccountByIdAsync_Should_ThrowException_When_Not_Found()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new AccountService(context);

        var nonExistentId = Guid.NewGuid();

        // 2. ACT & ASSERT
        await Assert.ThrowsAsync<Exception>(async () =>
            await service.GetAccountByIdAsync(nonExistentId)
        );
    }

    [Fact]
    public async Task GetAccountByNumberAsync_Should_Return_Account_When_Found()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new AccountService(context);

        var companyId = Guid.NewGuid();
        var account = new Account
        {
            Id = Guid.NewGuid(),
            AccountNumber = "1000",
            Name = "Test Account",
            AccountType = AccountType.Asset,
            CompanyId = companyId
        };

        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        // 2. ACT
        var result = await service.GetAccountByNumberAsync("1000", companyId);

        // 3. ASSERT
        result.Should().NotBeNull();
        result.Id.Should().Be(account.Id);
        result.AccountNumber.Should().Be("1000");
        result.Name.Should().Be("Test Account");
    }

    [Fact]
    public async Task GetAccountByNumberAsync_Should_Return_Null_When_Not_Found()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new AccountService(context);

        var companyId = Guid.NewGuid();

        // 2. ACT
        var result = await service.GetAccountByNumberAsync("9999", companyId);

        // 3. ASSERT
        result.Should().BeNull();
    }
}

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

namespace Mee.Erp.Finance.Core.Tests;

public class JournalPostingServiceTests
{
    // Helper to create an In-Memory Database Context
    private FinanceDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique name per test
            .Options;
        return new FinanceDbContext(options);
    }

    [Fact]
    public async Task PostJournalAsync_Should_Post_When_Journal_Is_Balanced()
    {
        // 1. ARRANGE (Setup the data)
        using var context = GetInMemoryDbContext();
        var service = new JournalPostingService(context);

        var journalId = Guid.NewGuid();
        var account1Id = Guid.NewGuid();
        var account2Id = Guid.NewGuid();

        // Create a balanced Draft Journal (100 Debit, 100 Credit)
        var journal = new Journal
        {
            Id = journalId,
            Description = "Test Journal",
            JournalDate = DateTime.UtcNow,
            Status = JournalStatus.Draft,
            CompanyId = Guid.NewGuid(),
            Entries = new List<JournalEntry>
            {
                new JournalEntry { AccountId = account1Id, Debit = 100, Credit = 0 },
                new JournalEntry { AccountId = account2Id, Debit = 0, Credit = 100 }
            }
        };

        context.Journals.Add(journal);
        await context.SaveChangesAsync();

        // 2. ACT (Run the logic)
        await service.PostJournalAsync(journalId, Guid.NewGuid());

        // 3. ASSERT (Verify the results)
        
        // Refetch the journal to check changes
        var savedJournal = await context.Journals.FindAsync(journalId);
        savedJournal.Status.Should().Be(JournalStatus.Posted); // Check Status Change

        // Check Ledger Entries
        var ledgerEntries = await context.LedgerEntries.Where(l => l.JournalId == journalId).ToListAsync();
        ledgerEntries.Should().HaveCount(2); // Should have created 2 ledger rows
        ledgerEntries.Sum(l => l.Debit).Should().Be(100); // Debits should match
        ledgerEntries.Sum(l => l.Credit).Should().Be(100); // Credits should match
    }

    [Fact]
    public async Task PostJournalAsync_Should_ThrowException_When_Unbalanced()
    {
        // 1. ARRANGE
        using var context = GetInMemoryDbContext();
        var service = new JournalPostingService(context);
        var journalId = Guid.NewGuid();

        // Create an UNBALANCED Journal (100 Debit, 50 Credit)
        var journal = new Journal
        {
            Id = journalId,
            Description = "Unbalanced Test",
            Status = JournalStatus.Draft,
            CompanyId = Guid.NewGuid(),
            Entries = new List<JournalEntry>
            {
                new JournalEntry { AccountId = Guid.NewGuid(), Debit = 100, Credit = 0 },
                new JournalEntry { AccountId = Guid.NewGuid(), Debit = 0, Credit = 50 }
            }
        };

        context.Journals.Add(journal);
        await context.SaveChangesAsync();

        // 2. ACT & ASSERT
        // We expect an Exception because 100 != 50
        await Assert.ThrowsAsync<Exception>(async () => 
            await service.PostJournalAsync(journalId, Guid.NewGuid())
        );
    }
}
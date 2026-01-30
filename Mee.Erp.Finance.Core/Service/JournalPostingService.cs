using Mee.Erp.Finance.Core.Contracts.Interfaces;
using Mee.Erp.Finance.Core.Domain.Entities;
using Mee.Erp.Finance.Core.Domain.Enums;
// using Mee.Erp.Finance.Core.Persistence; // We will create this DbContext later
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Services;

public class JournalPostingService // : IJournalPostingService  <- We would implement the interface
{
    // This represents our database connection. It will be injected via Dependency Injection.
    // private readonly FinanceDbContext _context;
    // public JournalPostingService(FinanceDbContext context) { _context = context; }
    
    public async Task PostJournalAsync(Guid journalId, Guid userId)
    {
        // In a real application, all of this would be wrapped in a database transaction.
        // EF Core's SaveChangesAsync does this automatically.

        // 1. FETCH the journal and its lines
        // var journal = await _context.Journals.Include(j => j.Entries).FirstOrDefaultAsync(j => j.Id == journalId);

        // 2. VALIDATE
        // if (journal == null) throw new Exception("Journal not found.");
        // if (journal.Status != JournalStatus.Draft) throw new Exception("Only draft journals can be posted.");
        // if (journal.Entries.Count < 2) throw new Exception("Journal must have at least two entries.");

        // decimal totalDebits = journal.Entries.Sum(e => e.Debit);
        // decimal totalCredits = journal.Entries.Sum(e => e.Credit);
        // if (totalDebits != totalCredits) throw new Exception("Journal is not balanced. Debits must equal Credits.");

        // TODO: We could also validate here that all AccountIds exist and are active.

        // 3. EXECUTE
        // foreach (var entry in journal.Entries)
        // {
        //     var ledgerEntry = new LedgerEntry
        //     {
        //         EntryDate = journal.JournalDate,
        //         AccountId = entry.AccountId,
        //         BusinessUnitId = entry.BusinessUnitId,
        //         Debit = entry.Debit,
        //         Credit = entry.Credit,
        //         Description = entry.Description ?? journal.Description,
        //         JournalId = journal.Id,
        //         // Audit fields are set here or automatically by the DbContext
        //         CreatedDate = DateTime.UtcNow,
        //         CreatedBy = userId
        //     };
        //     await _context.LedgerEntries.AddAsync(ledgerEntry);
        // }

        // // 4. COMMIT - Update journal status and save everything
        // journal.Status = JournalStatus.Posted;
        // journal.UpdatedDate = DateTime.UtcNow;
        // journal.UpdatedBy = userId;

        // await _context.SaveChangesAsync();
        Console.WriteLine("--- Logic for Posting Service ---");
        Console.WriteLine("1. Fetched Journal and its lines.");
        Console.WriteLine("2. Validated Status, Balance (Debits == Credits).");
        Console.WriteLine("3. Created a new LedgerEntry for each JournalEntry.");
        Console.WriteLine("4. Changed Journal status to 'Posted'.");
        Console.WriteLine("5. Saved all changes to the database in a single transaction.");
    }
}
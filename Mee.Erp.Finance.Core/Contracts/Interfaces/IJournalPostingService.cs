using System;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Contracts.Interfaces;

/// <summary>
/// Defines the contract for the service responsible for posting journals to the general ledger.
/// </summary>
public interface IJournalPostingService
{
    /// <summary>
    /// Validates a draft journal and posts its entries to the general ledger.
    /// </summary>
    /// <param name="journalId">The ID of the journal to post.</param>
    /// <param name="userId">The ID of the user performing the action.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PostJournalAsync(Guid journalId, Guid userId);
}
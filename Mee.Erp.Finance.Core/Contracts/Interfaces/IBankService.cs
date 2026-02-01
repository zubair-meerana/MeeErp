using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mee.Erp.Finance.Core.Domain.Entities;

namespace Mee.Erp.Finance.Core.Contracts.Interfaces;

public interface IBankService
{
	Task<BankReconciliation> CreateReconciliationAsync(Guid bankAccountId, DateTime statementDate, decimal endingBalance);
	Task MatchTransactionAsync(Guid reconciliationId, Guid bankTransactionId);
	Task CompleteReconciliationAsync(Guid reconciliationId);
}


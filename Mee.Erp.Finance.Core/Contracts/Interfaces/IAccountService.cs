using Mee.Erp.Finance.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Contracts.Interfaces;

public interface IAccountService
{
	Task<Account> CreateAccountAsync(Account account);
	Task<Account> UpdateAccountAsync(Account account);
	Task<List<Account>> GetChartOfAccountsAsync(Guid companyId);
	Task<Account> GetAccountByIdAsync(Guid id);
}
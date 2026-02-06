using Mee.Erp.Finance.Core.Contracts.Interfaces;
using Mee.Erp.Finance.Core.Domain.Entities;
using Mee.Erp.Finance.Core.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Services;

/// <summary>
/// Implements treasury and cash management
/// </summary>
public class TreasuryCashManagementService : ITreasuryCashManagementService
{
    private readonly FinanceDbContext _context;

    public TreasuryCashManagementService(FinanceDbContext context)
    {
        _context = context;
    }

    public async Task<CashFlowForecastResult> CreateCashFlowForecastAsync(CashFlowForecastRequest request)
    {
        var result = new CashFlowForecastResult
        {
            RequestId = Guid.NewGuid(),
            GeneratedDate = DateTime.UtcNow,
            ForecastPeriods = new List<CashFlowPeriod>(),
            BeginningCashBalance = await GetBeginningCashBalanceAsync(request.CompanyId, request.StartDate),
            ModelUsed = request.ForecastMethod,
            AccuracyRating = 0.85m // Placeholder accuracy
        };

        var currentBalance = result.BeginningCashBalance;

        // Generate forecast periods based on horizon
        var periodIncrement = request.ForecastHorizon.ToLower() switch
        {
            "daily" => TimeSpan.FromDays(1),
            "weekly" => TimeSpan.FromDays(7),
            "monthly" => TimeSpan.FromDays(30),
            "quarterly" => TimeSpan.FromDays(90),
            _ => TimeSpan.FromDays(30) // Default to monthly
        };

        var currentDate = request.StartDate;
        while (currentDate <= request.EndDate)
        {
            var periodEnd = currentDate + periodIncrement;
            if (periodEnd > request.EndDate) periodEnd = request.EndDate;

            // Calculate cash flows based on drivers
            var operatingCashFlow = CalculateOperatingCashFlow(request.Drivers, currentDate);
            var investingCashFlow = CalculateInvestingCashFlow(request.Drivers, currentDate);
            var financingCashFlow = CalculateFinancingCashFlow(request.Drivers, currentDate);
            var netCashFlow = operatingCashFlow + investingCashFlow + financingCashFlow;

            var endingBalance = currentBalance + netCashFlow;

            // Calculate confidence intervals
            var lowerBound = netCashFlow * 0.9m; // 10% lower bound
            var upperBound = netCashFlow * 1.1m; // 10% upper bound

            result.ForecastPeriods.Add(new CashFlowPeriod
            {
                PeriodStart = currentDate,
                PeriodEnd = periodEnd,
                OperatingCashFlow = operatingCashFlow,
                InvestingCashFlow = investingCashFlow,
                FinancingCashFlow = financingCashFlow,
                NetCashFlow = netCashFlow,
                BeginningBalance = currentBalance,
                EndingBalance = endingBalance,
                LowerBound = lowerBound,
                UpperBound = upperBound,
                VarianceExplanation = "Based on historical trends and market drivers"
            });

            currentBalance = endingBalance;
            currentDate = periodEnd;
        }

        result.NetCashFlow = result.ForecastPeriods.Sum(fp => fp.NetCashFlow);
        result.EndingCashBalance = currentBalance;

        return result;
    }

    public async Task<InvestmentPortfolioResult> ManageInvestmentPortfolioAsync(InvestmentPortfolioRequest request)
    {
        var result = new InvestmentPortfolioResult
        {
            RequestId = Guid.NewGuid(),
            ProcessedDate = DateTime.UtcNow,
            Holdings = new List<InvestmentHolding>(),
            Metrics = new List<PortfolioMetric>(),
            AccountingEntries = new List<Journal>()
        };

        // Process each holding
        foreach (var holding in request.Holdings)
        {
            var currentValue = holding.Quantity * holding.UnitPrice;
            var annualIncome = holding.CurrentYield * currentValue;

            var processedHolding = new InvestmentHolding
            {
                InvestmentId = holding.InvestmentId,
                InvestmentType = holding.InvestmentType,
                Issuer = holding.Issuer,
                Ticker = holding.Ticker,
                Quantity = holding.Quantity,
                UnitPrice = holding.UnitPrice,
                TotalValue = currentValue,
                PurchaseDate = holding.PurchaseDate,
                PurchasePrice = holding.PurchasePrice,
                CurrentYield = holding.CurrentYield,
                MaturityDate = holding.MaturityDate,
                CouponRate = holding.CouponRate,
                RiskRating = holding.RiskRating
            };

            result.Holdings.Add(processedHolding);
            result.TotalPortfolioValue += currentValue;
            result.TotalAnnualIncome += annualIncome;
        }

        // Calculate portfolio yield
        result.PortfolioYield = result.TotalPortfolioValue != 0 ?
            (result.TotalAnnualIncome / result.TotalPortfolioValue) * 100 : 0;

        // Calculate risk score based on ratings
        result.PortfolioRiskScore = CalculatePortfolioRiskScore(request.Holdings);

        // Add metrics
        result.Metrics.Add(new PortfolioMetric
        {
            MetricName = "Sharpe Ratio",
            Value = CalculateSharpeRatio(result),
            Unit = "ratio",
            AsOfDate = request.AsOfDate
        });

        result.Metrics.Add(new PortfolioMetric
        {
            MetricName = "Duration",
            Value = CalculatePortfolioDuration(request.Holdings),
            Unit = "years",
            AsOfDate = request.AsOfDate
        });

        // Create accounting entries for portfolio
        result.AccountingEntries = await CreatePortfolioAccountingEntriesAsync(request, result);

        return result;
    }

    public async Task<BankingRelationshipResult> ManageBankingRelationshipsAsync(BankingRelationshipRequest request)
    {
        var result = new BankingRelationshipResult
        {
            RequestId = Guid.NewGuid(),
            ProcessedDate = DateTime.UtcNow,
            Arrangements = new List<BankingArrangement>(),
            TotalBankBalances = 0,
            TotalAvailableCredit = 0,
            TotalAnnualFees = 0
        };

        foreach (var arrangement in request.Arrangements)
        {
            var processedArrangement = new BankingArrangement
            {
                ArrangementId = arrangement.ArrangementId,
                BankName = arrangement.BankName,
                AccountType = arrangement.AccountType,
                AccountNumber = arrangement.AccountNumber,
                CurrentBalance = arrangement.CurrentBalance,
                AvailableCredit = arrangement.AvailableCredit,
                InterestRate = arrangement.InterestRate,
                Fees = arrangement.Fees,
                LastStatementDate = arrangement.LastStatementDate,
                RelationshipManager = arrangement.RelationshipManager,
                ServiceLevel = arrangement.ServiceLevel,
                Services = arrangement.Services
            };

            result.Arrangements.Add(processedArrangement);
            result.TotalBankBalances += arrangement.CurrentBalance;
            result.TotalAvailableCredit += arrangement.AvailableCredit;
            result.TotalAnnualFees += arrangement.Fees;
        }

        result.PrimaryBank = request.PrimaryBank;
        result.RelationshipManager = request.RelationshipManager;

        return result;
    }

    public async Task<CreditFacilityResult> ManageCreditFacilitiesAsync(CreditFacilityRequest request)
    {
        var result = new CreditFacilityResult
        {
            RequestId = Guid.NewGuid(),
            ProcessedDate = DateTime.UtcNow,
            Facilities = new List<CreditFacility>(),
            TotalCreditFacilities = 0,
            TotalAvailableCredit = 0,
            TotalOutstandingCredit = 0,
            ComplianceAlerts = new List<ComplianceAlert>()
        };

        foreach (var facility in request.Facilities)
        {
            var processedFacility = new CreditFacility
            {
                FacilityId = facility.FacilityId,
                FacilityType = facility.FacilityType,
                Lender = facility.Lender,
                FacilityAmount = facility.FacilityAmount,
                AvailableAmount = facility.AvailableAmount,
                OutstandingAmount = facility.OutstandingAmount,
                InterestRate = facility.InterestRate,
                InterestRateType = facility.InterestRateType,
                MaturityDate = facility.MaturityDate,
                NextReviewDate = facility.NextReviewDate,
                Covenants = facility.Covenants,
                Collateral = facility.Collateral,
                CreditRating = facility.CreditRating,
                CovenantCompliance = facility.CovenantCompliance
            };

            result.Facilities.Add(processedFacility);
            result.TotalCreditFacilities += facility.FacilityAmount;
            result.TotalAvailableCredit += facility.AvailableAmount;
            result.TotalOutstandingCredit += facility.OutstandingAmount;

            // Check for covenant compliance
            foreach (var covenant in facility.CovenantCompliance)
            {
                if (!covenant.IsCompliant)
                {
                    result.ComplianceAlerts.Add(new ComplianceAlert
                    {
                        FacilityId = facility.FacilityId,
                        CovenantName = covenant.CovenantName,
                        AlertType = "Breach",
                        Description = $"Covenant '{covenant.CovenantName}' not met: Required {covenant.RequiredValue}, Actual {covenant.ActualValue}",
                        AlertDate = covenant.ComplianceDate,
                        Severity = "High"
                    });
                }
                else if (Math.Abs(covenant.ActualValue - covenant.RequiredValue) / covenant.RequiredValue < 0.05m)
                {
                    // Within 5% of required value - warning
                    result.ComplianceAlerts.Add(new ComplianceAlert
                    {
                        FacilityId = facility.FacilityId,
                        CovenantName = covenant.CovenantName,
                        AlertType = "NearBreach",
                        Description = $"Covenant '{covenant.CovenantName}' near breach: Required {covenant.RequiredValue}, Actual {covenant.ActualValue}",
                        AlertDate = covenant.ComplianceDate,
                        Severity = "Medium"
                    });
                }
            }
        }

        result.OverallComplianceStatus = result.ComplianceAlerts.Any() ? "Non-Compliant" : "Compliant";

        return result;
    }

    public async Task<LiquidityOptimizationResult> OptimizeLiquidityAsync(LiquidityOptimizationRequest request)
    {
        var result = new LiquidityOptimizationResult
        {
            RequestId = Guid.NewGuid(),
            ProcessedDate = DateTime.UtcNow,
            CurrentCashBalance = await GetCurrentCashBalanceAsync(request.CompanyId),
            Actions = new List<LiquidityAction>(),
            OptimizationStrategy = request.OptimizationStrategy
        };

        // Determine recommended actions based on strategy
        switch (request.OptimizationStrategy.ToLower())
        {
            case "maximizeyield":
                result.Actions = RecommendYieldMaximizingActions(request, result.CurrentCashBalance);
                break;
            case "minimizerisk":
                result.Actions = RecommendRiskMinimizingActions(request, result.CurrentCashBalance);
                break;
            case "balance":
                result.Actions = RecommendBalancedActions(request, result.CurrentCashBalance);
                break;
        }

        // Calculate projected balance after actions
        var projectedBalance = result.CurrentCashBalance;
        foreach (var action in result.Actions)
        {
            switch (action.ActionType.ToLower())
            {
                case "invest":
                    projectedBalance -= action.Amount;
                    break;
                case "withdraw":
                    projectedBalance += action.Amount;
                    break;
                case "borrow":
                    projectedBalance += action.Amount;
                    break;
                case "repay":
                    projectedBalance -= action.Amount;
                    break;
            }
        }

        result.ProjectedCashBalanceAfterActions = projectedBalance;

        return result;
    }

    public async Task<CashPoolingResult> ManageCashPoolingAsync(CashPoolingRequest request)
    {
        var result = new CashPoolingResult
        {
            RequestId = Guid.NewGuid(),
            ProcessedDate = DateTime.UtcNow,
            Members = new List<CashPoolMember>(),
            TotalPoolBalance = 0
        };

        // Process each pool member
        foreach (var accountId in request.PoolMemberAccountIds)
        {
            var account = await _context.BankAccounts.FindAsync(accountId);
            if (account != null)
            {
                var openingBalance = await GetAccountBalanceAsync(accountId, request.AsOfDate.AddDays(-1));
                var closingBalance = await GetAccountBalanceAsync(accountId, request.AsOfDate);

                var poolMember = new CashPoolMember
                {
                    AccountId = accountId,
                    AccountName = account.AccountName,
                    OpeningBalance = openingBalance,
                    ClosingBalance = closingBalance,
                    ContributionToPool = openingBalance > request.TargetBalance ? openingBalance - request.TargetBalance : 0,
                    DistributionFromPool = openingBalance < request.TargetBalance ? request.TargetBalance - openingBalance : 0
                };

                result.Members.Add(poolMember);
                result.TotalPoolBalance += closingBalance;
            }
        }

        // Create accounting entries for pooling
        result.AccountingEntries = await CreateCashPoolingAccountingEntriesAsync(request, result);

        return result;
    }

    public async Task<SweepAccountResult> ManageSweepAccountsAsync(SweepAccountRequest request)
    {
        var result = new SweepAccountResult
        {
            RequestId = Guid.NewGuid(),
            ProcessedDate = DateTime.UtcNow,
            SweepAccountId = request.SweepAccountId,
            SweepTransactions = new List<SweepTransaction>(),
            TotalSweptAmount = 0,
            AccountingEntries = new List<Journal>()
        };

        // Process each source account
        foreach (var sourceAccountId in request.SourceAccountIds)
        {
            var sourceBalance = await GetAccountBalanceAsync(sourceAccountId, request.AsOfDate);

            // Check if balance exceeds threshold
            if (sourceBalance > request.SweepThreshold)
            {
                var sweepAmount = sourceBalance - request.MinimumBalance;

                var transaction = new SweepTransaction
                {
                    SourceAccountId = sourceAccountId,
                    Amount = sweepAmount,
                    SweepDate = request.AsOfDate,
                    Status = "Completed"
                };

                result.SweepTransactions.Add(transaction);
                result.TotalSweptAmount += sweepAmount;
            }
        }

        // Create accounting entries for sweeps
        result.AccountingEntries = await CreateSweepAccountingEntriesAsync(request, result);

        return result;
    }

    #region Helper Methods

    private decimal CalculateOperatingCashFlow(List<CashFlowDriver> drivers, DateTime period)
    {
        // Simplified calculation - in reality, this would use complex forecasting models
        var operatingDrivers = drivers.Where(d => d.Type == "Revenue" || d.Type == "Expense");
        decimal baseValue = 50000; // Placeholder base value

        foreach (var driver in operatingDrivers)
        {
            baseValue += (driver.Weight / 100) * 10000; // Apply driver impact
        }

        return baseValue;
    }

    private decimal CalculateInvestingCashFlow(List<CashFlowDriver> drivers, DateTime period)
    {
        var investingDrivers = drivers.Where(d => d.Type == "Investment");
        decimal baseValue = -10000; // Placeholder base value (typically negative for investments)

        foreach (var driver in investingDrivers)
        {
            baseValue += (driver.Weight / 100) * 5000; // Apply driver impact
        }

        return baseValue;
    }

    private decimal CalculateFinancingCashFlow(List<CashFlowDriver> drivers, DateTime period)
    {
        var financingDrivers = drivers.Where(d => d.Type == "Financing");
        decimal baseValue = 0; // Placeholder base value

        foreach (var driver in financingDrivers)
        {
            baseValue += (driver.Weight / 100) * 15000; // Apply driver impact
        }

        return baseValue;
    }

    private async Task<decimal> GetBeginningCashBalanceAsync(Guid companyId, DateTime asOfDate)
    {
        // Get the beginning cash balance for the company
        var cashAccounts = await _context.Accounts
            .Where(a => a.CompanyId == companyId &&
                       (a.Name.ToLower().Contains("cash") ||
                        a.Name.ToLower().Contains("bank") ||
                        a.AccountNumber.StartsWith("10")))
            .ToListAsync();

        decimal totalBalance = 0;
        foreach (var account in cashAccounts)
        {
            totalBalance += await GetAccountBalanceAsync(account.Id, asOfDate.AddDays(-1));
        }

        return totalBalance;
    }

    private async Task<decimal> GetAccountBalanceAsync(Guid accountId, DateTime asOfDate)
    {
        // Calculate account balance up to the specified date
        var ledgerEntries = await _context.LedgerEntries
            .Where(le => le.AccountId == accountId && le.EntryDate <= asOfDate)
            .ToListAsync();

        decimal totalDebits = ledgerEntries.Sum(le => le.Debit);
        decimal totalCredits = ledgerEntries.Sum(le => le.Credit);

        // For asset accounts (cash), the balance is debits minus credits
        return totalDebits - totalCredits;
    }

    private async Task<decimal> GetCurrentCashBalanceAsync(Guid companyId)
    {
        // Get the current cash balance for the company
        var cashAccounts = await _context.Accounts
            .Where(a => a.CompanyId == companyId &&
                       (a.Name.ToLower().Contains("cash") ||
                        a.Name.ToLower().Contains("bank") ||
                        a.AccountNumber.StartsWith("10")))
            .ToListAsync();

        decimal totalBalance = 0;
        foreach (var account in cashAccounts)
        {
            totalBalance += await GetAccountBalanceAsync(account.Id, DateTime.Today);
        }

        return totalBalance;
    }

    private decimal CalculatePortfolioRiskScore(List<InvestmentHolding> holdings)
    {
        // Calculate weighted risk score based on ratings
        decimal totalValue = holdings.Sum(h => h.TotalValue);
        if (totalValue == 0) return 0;

        decimal weightedRisk = 0;
        foreach (var holding in holdings)
        {
            var riskWeight = holding.RiskRating.ToUpper() switch
            {
                "AAA" => 0.1m,
                "AA" => 0.2m,
                "A" => 0.3m,
                "BBB" => 0.4m,
                "BB" => 0.6m,
                "B" => 0.8m,
                "CCC" => 0.9m,
                _ => 1.0m // Below CCC or unknown
            };

            weightedRisk += (holding.TotalValue / totalValue) * riskWeight;
        }

        return weightedRisk;
    }

    private decimal CalculateSharpeRatio(InvestmentPortfolioResult result)
    {
        // Simplified Sharpe ratio calculation
        // In reality, this would require risk-free rate and volatility calculations
        var excessReturn = result.PortfolioYield - 2.0m; // Assume 2% risk-free rate
        var volatility = 0.1m; // Placeholder volatility

        return volatility != 0 ? excessReturn / volatility : 0;
    }

    private decimal CalculatePortfolioDuration(List<InvestmentHolding> holdings)
    {
        // Simplified duration calculation
        // In reality, this would require complex bond mathematics
        return 3.0m; // Placeholder value
    }

    private List<LiquidityAction> RecommendYieldMaximizingActions(LiquidityOptimizationRequest request, decimal currentBalance)
    {
        var actions = new List<LiquidityAction>();

        // If cash is above target, recommend investing excess
        if (currentBalance > request.TargetCashBalance)
        {
            var excessCash = currentBalance - request.TargetCashBalance;

            // Find highest yielding investment opportunity
            var bestOpportunity = request.InvestmentOpportunities
                .OrderByDescending(io => io.InterestRate)
                .FirstOrDefault();

            if (bestOpportunity != null && excessCash >= bestOpportunity.MinimumInvestment)
            {
                actions.Add(new LiquidityAction
                {
                    ActionType = "Invest",
                    Target = bestOpportunity.OpportunityName,
                    Amount = Math.Min(excessCash, bestOpportunity.Amount),
                    Reason = "Maximize yield by investing excess cash",
                    RecommendedDate = DateTime.Today
                });
            }
        }

        return actions;
    }

    private List<LiquidityAction> RecommendRiskMinimizingActions(LiquidityOptimizationRequest request, decimal currentBalance)
    {
        var actions = new List<LiquidityAction>();

        // If cash is below minimum, recommend drawing from credit facilities
        if (currentBalance < request.MinimumCashBalance)
        {
            var shortfall = request.MinimumCashBalance - currentBalance;
            actions.Add(new LiquidityAction
            {
                ActionType = "Borrow",
                Target = "Credit Facility",
                Amount = shortfall,
                Reason = "Maintain minimum cash balance for risk mitigation",
                RecommendedDate = DateTime.Today
            });
        }

        return actions;
    }

    private List<LiquidityAction> RecommendBalancedActions(LiquidityOptimizationRequest request, decimal currentBalance)
    {
        var actions = new List<LiquidityAction>();

        // Balance between yield and risk
        if (currentBalance > request.TargetCashBalance)
        {
            // Invest some excess but keep buffer
            var investableAmount = (currentBalance - request.TargetCashBalance) * 0.7m; // Invest 70% of excess
            var bestOpportunity = request.InvestmentOpportunities
                .OrderByDescending(io => io.InterestRate)
                .FirstOrDefault();

            if (bestOpportunity != null && investableAmount >= bestOpportunity.MinimumInvestment)
            {
                actions.Add(new LiquidityAction
                {
                    ActionType = "Invest",
                    Target = bestOpportunity.OpportunityName,
                    Amount = Math.Min(investableAmount, bestOpportunity.Amount),
                    Reason = "Balance yield and liquidity",
                    RecommendedDate = DateTime.Today
                });
            }
        }

        return actions;
    }

    private async Task<List<Journal>> CreatePortfolioAccountingEntriesAsync(InvestmentPortfolioRequest request, InvestmentPortfolioResult result)
    {
        var entries = new List<Journal>();

        // Create entries for each investment transaction
        foreach (var holding in request.Holdings)
        {
            var journal = new Journal
            {
                JournalDate = DateTime.Today,
                Description = $"Investment in {holding.Issuer} {holding.InvestmentType}",
                CompanyId = request.CompanyId,
                Status = Domain.Enums.JournalStatus.Draft
            };

            // Debit investment account
            journal.Entries.Add(new JournalEntry
            {
                AccountId = await GetInvestmentAccountIdAsync(request.CompanyId),
                Debit = holding.TotalValue,
                Credit = 0,
                Description = $"Purchase of {holding.InvestmentType} from {holding.Issuer}"
            });

            // Credit cash account
            journal.Entries.Add(new JournalEntry
            {
                AccountId = await GetCashAccountIdAsync(request.CompanyId),
                Debit = 0,
                Credit = holding.TotalValue,
                Description = $"Cash paid for investment"
            });

            entries.Add(journal);
        }

        return entries;
    }

    private async Task<List<Journal>> CreateCashPoolingAccountingEntriesAsync(CashPoolingRequest request, CashPoolingResult result)
    {
        var entries = new List<Journal>();

        // Create entries for cash pooling movements
        foreach (var member in result.Members)
        {
            if (member.ContributionToPool > 0)
            {
                // Transfer excess to pool
                var journal = new Journal
                {
                    JournalDate = DateTime.Today,
                    Description = $"Cash pooling contribution from {member.AccountName}",
                    CompanyId = request.CompanyId,
                    Status = Domain.Enums.JournalStatus.Draft
                };

                // Credit member account
                journal.Entries.Add(new JournalEntry
                {
                    AccountId = member.AccountId,
                    Debit = 0,
                    Credit = member.ContributionToPool,
                    Description = $"Cash pooling outflow"
                });

                // Debit pool account
                journal.Entries.Add(new JournalEntry
                {
                    AccountId = await GetCashPoolingAccountIdAsync(request.CompanyId),
                    Debit = member.ContributionToPool,
                    Credit = 0,
                    Description = $"Cash pooling inflow"
                });

                entries.Add(journal);
            }
            else if (member.DistributionFromPool > 0)
            {
                // Transfer from pool to member
                var journal = new Journal
                {
                    JournalDate = DateTime.Today,
                    Description = $"Cash pooling distribution to {member.AccountName}",
                    CompanyId = request.CompanyId,
                    Status = Domain.Enums.JournalStatus.Draft
                };

                // Debit member account
                journal.Entries.Add(new JournalEntry
                {
                    AccountId = member.AccountId,
                    Debit = member.DistributionFromPool,
                    Credit = 0,
                    Description = $"Cash pooling inflow"
                });

                // Credit pool account
                journal.Entries.Add(new JournalEntry
                {
                    AccountId = await GetCashPoolingAccountIdAsync(request.CompanyId),
                    Debit = 0,
                    Credit = member.DistributionFromPool,
                    Description = $"Cash pooling outflow"
                });

                entries.Add(journal);
            }
        }

        return entries;
    }

    private async Task<List<Journal>> CreateSweepAccountingEntriesAsync(SweepAccountRequest request, SweepAccountResult result)
    {
        var entries = new List<Journal>();

        // Create entries for each sweep transaction
        foreach (var transaction in result.SweepTransactions)
        {
            var journal = new Journal
            {
                JournalDate = DateTime.Today,
                Description = $"Sweep from account {transaction.SourceAccountId} to sweep account",
                CompanyId = request.CompanyId,
                Status = Domain.Enums.JournalStatus.Draft
            };

            // Credit source account
            journal.Entries.Add(new JournalEntry
            {
                AccountId = transaction.SourceAccountId,
                Debit = 0,
                Credit = transaction.Amount,
                Description = $"Sweep outflow"
            });

            // Debit sweep account
            journal.Entries.Add(new JournalEntry
            {
                AccountId = request.SweepAccountId,
                Debit = transaction.Amount,
                Credit = 0,
                Description = $"Sweep inflow"
            });

            entries.Add(journal);
        }

        return entries;
    }

    private async Task<Guid> GetInvestmentAccountIdAsync(Guid companyId)
    {
        // In a real implementation, this would look up the appropriate investment account
        return Guid.NewGuid();
    }

    private async Task<Guid> GetCashAccountIdAsync(Guid companyId)
    {
        // In a real implementation, this would look up the appropriate cash account
        return Guid.NewGuid();
    }

    private async Task<Guid> GetCashPoolingAccountIdAsync(Guid companyId)
    {
        // In a real implementation, this would look up the appropriate cash pooling account
        return Guid.NewGuid();
    }

    #endregion
}

using Mee.Erp.Finance.Core.Contracts.Interfaces;
using Mee.Erp.Finance.Core.Domain.Entities;
using Mee.Erp.Finance.Core.Domain.Enums;
using Mee.Erp.Finance.Core.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Mee.Erp.Finance.Core.Tests.Services;

public class AdvancedJournalProcessingServiceTests
{
    private readonly Mock<FinanceDbContext> _mockContext;
    private readonly AdvancedJournalProcessingService _service;

    public AdvancedJournalProcessingServiceTests()
    {
        _mockContext = new Mock<FinanceDbContext>();
        _service = new AdvancedJournalProcessingService(_mockContext.Object);
    }

    [Fact]
    public async Task CreateRecurringJournalEntriesAsync_MonthlyFrequency_ReturnsGeneratedJournals()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var request = new RecurringJournalRequest
        {
            CompanyId = companyId,
            JournalTemplateName = "Monthly Rent",
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(2),
            Frequency = "Monthly",
            Description = "Monthly rent payment",
            AccountIds = new List<Guid> { accountId },
            BusinessUnitIds = new List<Guid> { Guid.NewGuid() },
            AutoPost = false,
            Formulas = new List<RecurringFormula>
            {
                new RecurringFormula
                {
                    FormulaName = "Rent Formula",
                    FormulaExpression = "10000",
                    FormulaType = "FixedAmount",
                    DataSource = "Ledger"
                }
            }
        };

        var account = new Account
        {
            Id = accountId,
            AccountNumber = "2000",
            Name = "Rent Expense",
            AccountType = AccountType.Expense,
            CompanyId = companyId
        };

        _mockContext.Setup(c => c.Accounts.FindAsync(It.IsAny<object[]>())).ReturnsAsync(account);

        // Act
        var result = await _service.CreateRecurringJournalEntriesAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.CompanyId, result.RequestId);
        Assert.Equal(3, result.GeneratedJournals.Count); // Monthly for 3 months
        Assert.Equal(3, result.TotalGenerated);
        Assert.Equal(0, result.TotalPosted); // AutoPost is false
        Assert.Equal(0, result.TotalFailed);
        Assert.Equal("Success", result.Status);

        // Verify each journal
        for (int i = 0; i < result.GeneratedJournals.Count; i++)
        {
            var journal = result.GeneratedJournals[i];
            Assert.Equal(JournalStatus.Draft, journal.Status);
            Assert.Contains(request.JournalTemplateName, journal.Description);
            Assert.Equal(1, journal.Entries.Count);
            Assert.Equal(accountId, journal.Entries.First().AccountId);
            Assert.Equal(10000, journal.Entries.First().Debit);
            Assert.Equal(0, journal.Entries.First().Credit);
        }

        // Verify dates are monthly
        var expectedDates = new[]
        {
            DateTime.Today,
            DateTime.Today.AddMonths(1),
            DateTime.Today.AddMonths(2)
        };

        foreach (var date in expectedDates)
        {
            Assert.Contains(result.GeneratedJournals, j => j.JournalDate.Date == date.Date);
        }
    }

    [Fact]
    public async Task ProcessStatisticalJournalEntriesAsync_HeadcountType_ReturnsStatisticalJournal()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var request = new StatisticalJournalRequest
        {
            CompanyId = companyId,
            StatisticalType = "Headcount",
            AsOfDate = DateTime.Today,
            BusinessUnitIds = new List<Guid> { Guid.NewGuid() },
            ReportingDimension = "Department",
            Metrics = new List<StatisticalMetric>
            {
                new StatisticalMetric
                {
                    MetricName = "Total Employees",
                    MetricType = "Count",
                    Value = 50,
                    UnitOfMeasure = "People",
                    MeasurementDate = DateTime.Today,
                    DataSource = "HR System",
                    Formula = "COUNT(Employee)"
                },
                new StatisticalMetric
                {
                    MetricName = "Avg Salary",
                    MetricType = "Ratio",
                    Value = 75000,
                    UnitOfMeasure = "USD",
                    MeasurementDate = DateTime.Today,
                    DataSource = "HR System",
                    Formula = "SUM(Salary)/COUNT(Employee)"
                }
            }
        };

        var account = new Account
        {
            Id = accountId,
            AccountNumber = "5000",
            Name = "Personnel Expense",
            AccountType = AccountType.Expense,
            CompanyId = companyId
        };

        _mockContext.Setup(c => c.Accounts.Where(It.IsAny<System.Linq.Expressions.Expression<Func<Account, bool>>>()))
            .Returns(new List<Account> { account }.AsQueryable());

        // Act
        var result = await _service.ProcessStatisticalJournalEntriesAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.CompanyId, result.RequestId);
        Assert.Equal(1, result.StatisticalJournals.Count);
        Assert.Equal(1, result.TotalGenerated);
        Assert.Equal("Success", result.Status);
        Assert.Empty(result.ValidationErrors);

        var journal = result.StatisticalJournals.First();
        Assert.Equal(JournalStatus.Draft, journal.Status);
        Assert.Contains("Statistical Journal", journal.Description);
        Assert.Equal(2, journal.Entries.Count); // One for each metric

        // Verify entries
        var entries = journal.Entries.ToList();
        Assert.Equal(accountId, entries[0].AccountId);
        Assert.Equal(50, entries[0].Debit); // Headcount value
        Assert.Equal(75000, entries[1].Debit); // Avg salary value
    }

    [Fact]
    public async Task CreateCompositeJournalEntriesAsync_MultiPeriod_ReturnsCompositeJournal()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var accountId1 = Guid.NewGuid();
        var accountId2 = Guid.NewGuid();

        var request = new CompositeJournalRequest
        {
            CompanyId = companyId,
            CompositeType = "MultiPeriod",
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(1),
            EntityIds = new List<Guid> { Guid.NewGuid() },
            BookIds = new List<Guid> { Guid.NewGuid() },
            ConsolidationMethod = "Full",
            Segments = new List<CompositeJournalSegment>
            {
                new CompositeJournalSegment
                {
                    SegmentName = "Opening Balance",
                    SegmentDate = DateTime.Today.AddDays(-1),
                    SegmentType = "Opening",
                    SegmentDescription = "Opening balance for the period",
                    Entries = new List<JournalEntry>
                    {
                        new JournalEntry
                        {
                            AccountId = accountId1,
                            Debit = 10000,
                            Credit = 0,
                            Description = "Opening balance - Cash"
                        }
                    }
                },
                new CompositeJournalSegment
                {
                    SegmentName = "Activity",
                    SegmentDate = DateTime.Today,
                    SegmentType = "Activity",
                    SegmentDescription = "Activities during the period",
                    Entries = new List<JournalEntry>
                    {
                        new JournalEntry
                        {
                            AccountId = accountId1,
                            Debit = 5000,
                            Credit = 0,
                            Description = "Cash receipt"
                        },
                        new JournalEntry
                        {
                            AccountId = accountId2,
                            Debit = 0,
                            Credit = 5000,
                            Description = "Revenue recognition"
                        }
                    }
                }
            }
        };

        var accounts = new List<Account>
        {
            new Account { Id = accountId1, AccountNumber = "1000", Name = "Cash", AccountType = AccountType.Asset, CompanyId = companyId },
            new Account { Id = accountId2, AccountNumber = "4000", Name = "Revenue", AccountType = AccountType.Revenue, CompanyId = companyId }
        };

        _mockContext.Setup(c => c.Accounts.FindAsync(It.IsAny<object[]>())).ReturnsAsync(accounts.First);
        _mockContext.Setup(c => c.Accounts.FindAsync(It.IsAny<object[]>())).ReturnsAsync(accounts.Skip(1).First);

        // Act
        var result = await _service.CreateCompositeJournalEntriesAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.CompanyId, result.RequestId);
        Assert.Equal(1, result.CompositeJournals.Count);
        Assert.Equal(2, result.TotalSegments); // Two segments
        Assert.Equal("Success", result.Status);
        Assert.Empty(result.ValidationErrors);

        var journal = result.CompositeJournals.First();
        Assert.Equal(JournalStatus.Draft, journal.Status);
        Assert.Contains("Composite Journal", journal.Description);
        Assert.Equal(3, journal.Entries.Count); // Total from both segments

        // Verify the journal is balanced
        var totalDebits = journal.Entries.Sum(e => e.Debit);
        var totalCredits = journal.Entries.Sum(e => e.Credit);
        Assert.Equal(totalDebits, totalCredits);
    }

    [Fact]
    public async Task ManageJournalTemplatesAsync_ValidTemplate_ReturnsTemplateResult()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var request = new JournalTemplateRequest
        {
            CompanyId = companyId,
            TemplateName = "Payroll Template",
            TemplateDescription = "Template for payroll journal entries",
            TemplateType = "Standard",
            TemplateLines = new List<JournalTemplateLine>
            {
                new JournalTemplateLine
                {
                    LineNumber = 1,
                    AccountId = accountId,
                    AccountNumber = "5000",
                    AccountName = "Salaries Expense",
                    AmountType = "Fixed",
                    FixedAmount = 50000,
                    Description = "Salaries for employees"
                },
                new JournalTemplateLine
                {
                    LineNumber = 2,
                    AccountId = Guid.NewGuid(),
                    AccountNumber = "2000",
                    AccountName = "Accrued Payroll Liabilities",
                    AmountType = "Fixed",
                    FixedAmount = 10000,
                    Description = "Employer portion of payroll taxes"
                }
            },
            ValidationRules = new List<JournalValidationRule>
            {
                new JournalValidationRule
                {
                    RuleName = "Balance Check",
                    RuleType = "Balance",
                    RuleExpression = "Debits = Credits",
                    ErrorMessage = "Journal must be balanced",
                    Severity = "Error",
                    IsActive = true
                }
            },
            AllowedUserIds = new List<Guid> { Guid.NewGuid() },
            AllowedRoleIds = new List<Guid> { Guid.NewGuid() }
        };

        var account = new Account
        {
            Id = accountId,
            AccountNumber = "5000",
            Name = "Salaries Expense",
            AccountType = AccountType.Expense,
            CompanyId = companyId
        };

        _mockContext.Setup(c => c.Accounts.FindAsync(It.IsAny<object[]>())).ReturnsAsync(account);

        // Act
        var result = await _service.ManageJournalTemplatesAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.CompanyId, result.RequestId);
        Assert.Equal("Payroll Template", result.TemplateName);
        Assert.Equal("Created", result.Status);
        Assert.Empty(result.ValidationMessages.Where(vm => vm.MessageType == "Error"));
        Assert.Contains(result.ValidationMessages, vm => vm.Message.Contains("created successfully"));
    }

    [Fact]
    public async Task ProcessJournalApprovalWorkflowsAsync_SequentialApproval_ReturnsApprovalResult()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var journalId = Guid.NewGuid();
        var approverId1 = Guid.NewGuid();
        var approverId2 = Guid.NewGuid();

        var request = new JournalApprovalRequest
        {
            CompanyId = companyId,
            JournalIds = new List<Guid> { journalId },
            ApprovalType = "Standard",
            ApprovalWorkflow = "Sequential",
            RequireAllApprovals = true,
            ApproverIds = new List<Guid> { approverId1, approverId2 },
            ApprovalLevels = new List<ApprovalLevel>
            {
                new ApprovalLevel
                {
                    LevelNumber = 1,
                    ApproverIds = new List<Guid> { approverId1 },
                    AmountThreshold = 10000,
                    Role = "Manager",
                    Department = "Finance",
                    IsRequired = true
                },
                new ApprovalLevel
                {
                    LevelNumber = 2,
                    ApproverIds = new List<Guid> { approverId2 },
                    AmountThreshold = 50000,
                    Role = "Director",
                    Department = "Finance",
                    IsRequired = true
                }
            }
        };

        var journal = new Journal
        {
            Id = journalId,
            CompanyId = companyId,
            Description = "Test journal for approval",
            JournalDate = DateTime.Today,
            Status = JournalStatus.Draft,
            Entries = new List<JournalEntry>
            {
                new JournalEntry { Id = Guid.NewGuid(), AccountId = Guid.NewGuid(), Debit = 25000, Credit = 0 }
            }
        };

        _mockContext.Setup(c => c.Journals.FindAsync(It.IsAny<object[]>())).ReturnsAsync(journal);

        // Act
        var result = await _service.ProcessJournalApprovalWorkflowsAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.CompanyId, result.RequestId);
        Assert.Equal(1, result.ApprovalStatuses.Count);
        Assert.Equal(journalId, result.ApprovalStatuses.First().JournalId);
        Assert.Equal("Pending", result.ApprovalStatuses.First().Status); // Sequential approval requires both levels
        Assert.Equal("Pending", result.OverallStatus);
        Assert.Empty(result.ValidationErrors);

        // Verify the approval levels are set correctly
        Assert.Equal("Level 1", result.ApprovalStatuses.First().CurrentApprovalLevel);
    }

    [Fact]
    public async Task HandleJournalReversalAndReclassificationAsync_FullReversal_ReturnsReversalResult()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var originalJournalId = Guid.NewGuid();
        var originalAccountId = Guid.NewGuid();
        var newAccountId = Guid.NewGuid();

        // Create original journal
        var originalJournal = new Journal
        {
            Id = originalJournalId,
            CompanyId = companyId,
            Description = "Original journal to reverse",
            JournalDate = DateTime.Today.AddDays(-5),
            Status = JournalStatus.Posted,
            Entries = new List<JournalEntry>
            {
                new JournalEntry
                {
                    Id = Guid.NewGuid(),
                    AccountId = originalAccountId,
                    Debit = 10000,
                    Credit = 0,
                    Description = "Original expense"
                },
                new JournalEntry
                {
                    Id = Guid.NewGuid(),
                    AccountId = Guid.NewGuid(),
                    Debit = 0,
                    Credit = 10000,
                    Description = "Original funding"
                }
            }
        };

        var request = new JournalReversalRequest
        {
            CompanyId = companyId,
            JournalIdsToReverse = new List<Guid> { originalJournalId },
            ReversalDate = DateTime.Today,
            ReversalReason = "Error correction",
            ReversalType = "Full",
            AccountIdsToReclassify = new List<Guid> { originalAccountId },
            Reclassifications = new List<AccountReclassification>
            {
                new AccountReclassification
                {
                    OriginalAccountId = originalAccountId,
                    NewAccountId = newAccountId,
                    Amount = 10000,
                    Description = "Moving from wrong account to correct account",
                    EffectiveDate = DateTime.Today
                }
            },
            AutoPost = true
        };

        var accounts = new List<Account>
        {
            new Account { Id = originalAccountId, AccountNumber = "5000", Name = "Wrong Expense Account", AccountType = AccountType.Expense, CompanyId = companyId },
            new Account { Id = newAccountId, AccountNumber = "5001", Name = "Correct Expense Account", AccountType = AccountType.Expense, CompanyId = companyId }
        };

        _mockContext.Setup(c => c.Journals.FindAsync(It.IsAny<object[]>())).ReturnsAsync(originalJournal);
        _mockContext.Setup(c => c.Accounts.FindAsync(It.IsAny<object[]>())).ReturnsAsync(accounts.First);
        _mockContext.Setup(c => c.Accounts.FindAsync(It.IsAny<object[]>())).ReturnsAsync(accounts.Skip(1).First);

        // Act
        var result = await _service.HandleJournalReversalAndReclassificationAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.CompanyId, result.RequestId);
        Assert.Equal(1, result.ReversalJournals.Count);
        Assert.Equal(1, result.ReclassificationJournals.Count);
        Assert.Equal(1, result.TotalReversed);
        Assert.Equal(1, result.TotalReclassified);
        Assert.Equal("Success", result.Status);
        Assert.Empty(result.ValidationErrors);

        // Verify reversal journal
        var reversalJournal = result.ReversalJournals.First();
        Assert.Contains("REVERSAL", reversalJournal.Description);
        Assert.Equal(DateTime.Today, reversalJournal.JournalDate);
        Assert.Equal(2, reversalJournal.Entries.Count); // Should reverse both original entries

        // Verify reclassification journal
        var reclassificationJournal = result.ReclassificationJournals.First();
        Assert.Contains("ACCOUNT RECLASSIFICATION", reclassificationJournal.Description);
        Assert.Equal(DateTime.Today, reclassificationJournal.JournalDate);
        Assert.Equal(2, reclassificationJournal.Entries.Count); // Debit new account, credit original
    }

    [Fact]
    public async Task ValidateMassJournalUploadsAsync_ValidJournals_ReturnsValidationResult()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var accountId1 = Guid.NewGuid();
        var accountId2 = Guid.NewGuid();

        var journals = new List<Journal>
        {
            new Journal
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Description = "Valid journal 1",
                JournalDate = DateTime.Today,
                Status = JournalStatus.Draft,
                Entries = new List<JournalEntry>
                {
                    new JournalEntry { Id = Guid.NewGuid(), AccountId = accountId1, Debit = 5000, Credit = 0 },
                    new JournalEntry { Id = Guid.NewGuid(), AccountId = accountId2, Debit = 0, Credit = 5000 }
                }
            },
            new Journal
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Description = "Valid journal 2",
                JournalDate = DateTime.Today,
                Status = JournalStatus.Draft,
                Entries = new List<JournalEntry>
                {
                    new JournalEntry { Id = Guid.NewGuid(), AccountId = accountId1, Debit = 3000, Credit = 0 },
                    new JournalEntry { Id = Guid.NewGuid(), AccountId = accountId2, Debit = 0, Credit = 3000 }
                }
            }
        };

        var request = new MassJournalUploadRequest
        {
            CompanyId = companyId,
            Journals = journals,
            UploadFormat = "JSON",
            ValidateOnly = false,
            AutoPost = true,
            ValidationProfile = "Standard",
            BusinessUnitIds = new List<Guid> { Guid.NewGuid() }
        };

        var accounts = new List<Account>
        {
            new Account { Id = accountId1, AccountNumber = "1000", Name = "Cash", AccountType = AccountType.Asset, CompanyId = companyId },
            new Account { Id = accountId2, AccountNumber = "2000", Name = "Revenue", AccountType = AccountType.Revenue, CompanyId = companyId }
        };

        _mockContext.Setup(c => c.Accounts.FindAsync(It.IsAny<object[]>())).ReturnsAsync(accounts.First);
        _mockContext.Setup(c => c.Accounts.FindAsync(It.IsAny<object[]>())).ReturnsAsync(accounts.Skip(1).First);

        // Act
        var result = await _service.ValidateMassJournalUploadsAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.CompanyId, result.RequestId);
        Assert.Equal(2, result.TotalJournals);
        Assert.Equal(2, result.ValidJournals);
        Assert.Equal(0, result.InvalidJournals);
        Assert.Equal(100.0m, result.SuccessRate);
        Assert.Equal("Valid", result.OverallStatus);
        Assert.Empty(result.ValidationErrors);

        // Verify validation statuses
        Assert.Equal(2, result.ValidationStatuses.Count);
        Assert.All(result.ValidationStatuses, status => Assert.Equal("Valid", status.Status));
        Assert.All(result.ValidationStatuses, status => Assert.Empty(status.ValidationMessages));
    }

    [Fact]
    public async Task ValidateMassJournalUploadsAsync_InvalidJournals_ReturnsValidationResultWithErrors()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var accountId1 = Guid.NewGuid();
        var accountId2 = Guid.NewGuid();

        // Create an unbalanced journal (invalid)
        var invalidJournal = new Journal
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Description = "Invalid journal - unbalanced",
            JournalDate = DateTime.Today,
            Status = JournalStatus.Draft,
            Entries = new List<JournalEntry>
            {
                new JournalEntry { Id = Guid.NewGuid(), AccountId = accountId1, Debit = 5000, Credit = 0 }, // 5000 debit
                new JournalEntry { Id = Guid.NewGuid(), AccountId = accountId2, Debit = 0, Credit = 3000 } // Only 3000 credit - unbalanced
            }
        };

        var validJournal = new Journal
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Description = "Valid journal",
            JournalDate = DateTime.Today,
            Status = JournalStatus.Draft,
            Entries = new List<JournalEntry>
            {
                new JournalEntry { Id = Guid.NewGuid(), AccountId = accountId1, Debit = 2000, Credit = 0 },
                new JournalEntry { Id = Guid.NewGuid(), AccountId = accountId2, Debit = 0, Credit = 2000 }
            }
        };

        var request = new MassJournalUploadRequest
        {
            CompanyId = companyId,
            Journals = new List<Journal> { invalidJournal, validJournal },
            UploadFormat = "JSON",
            ValidateOnly = true, // Only validate, don't post
            AutoPost = false,
            ValidationProfile = "Standard",
            BusinessUnitIds = new List<Guid> { Guid.NewGuid() }
        };

        var accounts = new List<Account>
        {
            new Account { Id = accountId1, AccountNumber = "1000", Name = "Cash", AccountType = AccountType.Asset, CompanyId = companyId },
            new Account { Id = accountId2, AccountNumber = "2000", Name = "Revenue", AccountType = AccountType.Revenue, CompanyId = companyId }
        };

        _mockContext.Setup(c => c.Accounts.FindAsync(It.IsAny<object[]>())).ReturnsAsync(accounts.First);
        _mockContext.Setup(c => c.Accounts.FindAsync(It.IsAny<object[]>())).ReturnsAsync(accounts.Skip(1).First);

        // Act
        var result = await _service.ValidateMassJournalUploadsAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.CompanyId, result.RequestId);
        Assert.Equal(2, result.TotalJournals);
        Assert.Equal(1, result.ValidJournals);
        Assert.Equal(1, result.InvalidJournals);
        Assert.Equal(50.0m, result.SuccessRate); // 50% success rate
        Assert.Equal("PartiallyValid", result.OverallStatus);
        Assert.Single(result.ValidationErrors);
        Assert.Contains(result.ValidationErrors, error => error.ValidationErrorType == "Balance");

        // Verify validation statuses
        Assert.Equal(2, result.ValidationStatuses.Count);
        var invalidStatus = result.ValidationStatuses.First(s => s.JournalId == invalidJournal.Id);
        var validStatus = result.ValidationStatuses.First(s => s.JournalId == validJournal.Id);

        Assert.Equal("Invalid", invalidStatus.Status);
        Assert.Equal("Valid", validStatus.Status);
        Assert.NotEmpty(invalidStatus.ValidationMessages);
        Assert.Contains(invalidStatus.ValidationMessages, msg => msg.Contains("not balanced"));
        Assert.Empty(validStatus.ValidationMessages);
    }
}

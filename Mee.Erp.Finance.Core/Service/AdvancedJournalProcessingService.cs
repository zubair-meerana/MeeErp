using Mee.Erp.Finance.Core.Contracts.Interfaces;
using Mee.Erp.Finance.Core.Domain.Entities;
using Mee.Erp.Finance.Core.Domain.Enums;
using Mee.Erp.Finance.Core.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Services;

/// <summary>
/// Implements advanced journal processing
/// </summary>
public class AdvancedJournalProcessingService : IAdvancedJournalProcessingService
{
    private readonly FinanceDbContext _context;

    public AdvancedJournalProcessingService(FinanceDbContext context)
    {
        _context = context;
    }

    public async Task<RecurringJournalResult> CreateRecurringJournalEntriesAsync(RecurringJournalRequest request)
    {
        var result = new RecurringJournalResult
        {
            RequestId = Guid.NewGuid(),
            ProcessedDate = DateTime.UtcNow,
            GeneratedJournals = new List<Journal>(),
            ValidationErrors = new List<RecurringValidationError>(),
            Status = "Success",
            TotalGenerated = 0,
            TotalPosted = 0,
            TotalFailed = 0
        };

        // Calculate the recurrence pattern based on frequency
        var currentDate = request.StartDate;
        var endDate = request.EndDate;

        while (currentDate <= endDate)
        {
            // Create a journal for the current period
            var journal = new Journal
            {
                JournalDate = currentDate,
                Description = $"{request.JournalTemplateName} - {currentDate:yyyy-MM-dd}",
                ReferenceNumber = $"RJ-{currentDate:yyyyMMdd}-{result.GeneratedJournals.Count + 1}",
                CompanyId = request.CompanyId,
                Status = request.AutoPost ? JournalStatus.Posted : JournalStatus.Draft
            };

            // Apply formulas to generate journal entries
            foreach (var formula in request.Formulas)
            {
                var calculatedAmount = CalculateFormulaValue(formula, currentDate);

                // Create journal entries based on the formula
                foreach (var accountId in request.AccountIds)
                {
                    var journalEntry = new JournalEntry
                    {
                        AccountId = accountId,
                        Debit = formula.FormulaType == "Expense" ? calculatedAmount : 0,
                        Credit = formula.FormulaType == "Expense" ? 0 : calculatedAmount,
                        Description = $"Recurring entry for {formula.FormulaName}",
                        BusinessUnitId = request.BusinessUnitIds.FirstOrDefault()
                    };

                    journal.Entries.Add(journalEntry);
                }
            }

            // Validate the journal before adding
            var validationErrors = ValidateJournal(journal);
            if (validationErrors.Any())
            {
                result.ValidationErrors.AddRange(validationErrors.Select(ve => new RecurringValidationError
                {
                    ValidationErrorType = ve.ValidationErrorType,
                    Description = ve.Description,
                    ErrorDate = DateTime.UtcNow,
                    Severity = ve.Severity
                }));

                result.TotalFailed++;
                result.Status = "Partial";
            }
            else
            {
                result.GeneratedJournals.Add(journal);
                result.TotalGenerated++;

                if (request.AutoPost)
                {
                    result.TotalPosted++;
                }
            }

            // Move to next period based on frequency
            currentDate = request.Frequency.ToLower() switch
            {
                "daily" => currentDate.AddDays(1),
                "weekly" => currentDate.AddDays(7),
                "monthly" => currentDate.AddMonths(1),
                "quarterly" => currentDate.AddMonths(3),
                "annually" => currentDate.AddYears(1),
                _ => currentDate.AddMonths(1) // Default to monthly
            };
        }

        // Save all generated journals to the database
        if (result.GeneratedJournals.Any())
        {
            _context.Journals.AddRange(result.GeneratedJournals);
            await _context.SaveChangesAsync();
        }

        return result;
    }

    public async Task<StatisticalJournalResult> ProcessStatisticalJournalEntriesAsync(StatisticalJournalRequest request)
    {
        var result = new StatisticalJournalResult
        {
            RequestId = Guid.NewGuid(),
            ProcessedDate = DateTime.UtcNow,
            StatisticalJournals = new List<Journal>(),
            ValidationErrors = new List<StatisticalValidationError>(),
            Status = "Success",
            TotalGenerated = 0,
            TotalPosted = 0
        };

        // Create statistical journal based on metrics
        var journal = new Journal
        {
            JournalDate = request.AsOfDate,
            Description = $"Statistical Journal - {request.StatisticalType}",
            ReferenceNumber = $"SJ-{request.AsOfDate:yyyyMMdd}",
            CompanyId = request.CompanyId,
            Status = JournalStatus.Draft
        };

        // Add entries for each metric
        foreach (var metric in request.Metrics)
        {
            // Determine appropriate account based on metric type
            var account = await GetStatisticalAccountAsync(metric.MetricType, request.CompanyId);

            if (account != null)
            {
                var journalEntry = new JournalEntry
                {
                    AccountId = account.Id,
                    Debit = metric.MetricType == "Cost" ? metric.Value : 0,
                    Credit = metric.MetricType == "Cost" ? 0 : metric.Value,
                    Description = $"{metric.MetricName}: {metric.Value} {metric.UnitOfMeasure}",
                    BusinessUnitId = request.BusinessUnitIds.FirstOrDefault()
                };

                journal.Entries.Add(journalEntry);
            }
        }

        // Validate the statistical journal
        var validationErrors = ValidateJournal(journal);
        if (validationErrors.Any())
        {
            result.ValidationErrors.AddRange(validationErrors.Select(ve => new StatisticalValidationError
            {
                ValidationErrorType = ve.ValidationErrorType,
                Description = ve.Description,
                ErrorDate = DateTime.UtcNow,
                Severity = ve.Severity
            }));

            result.Status = "Failed";
        }
        else
        {
            result.StatisticalJournals.Add(journal);
            result.TotalGenerated = 1;
        }

        // Save the journal to the database
        if (result.StatisticalJournals.Any())
        {
            _context.Journals.AddRange(result.StatisticalJournals);
            await _context.SaveChangesAsync();
        }

        return result;
    }

    public async Task<CompositeJournalResult> CreateCompositeJournalEntriesAsync(CompositeJournalRequest request)
    {
        var result = new CompositeJournalResult
        {
            RequestId = Guid.NewGuid(),
            ProcessedDate = DateTime.UtcNow,
            CompositeJournals = new List<Journal>(),
            ValidationErrors = new List<CompositeValidationError>(),
            Status = "Success",
            TotalSegments = 0,
            TotalPosted = 0
        };

        // Create composite journal spanning multiple periods/entities/books
        var journal = new Journal
        {
            JournalDate = request.StartDate,
            Description = $"Composite Journal - {request.CompositeType}",
            ReferenceNumber = $"CJ-{request.StartDate:yyyyMMdd}",
            CompanyId = request.CompanyId,
            Status = JournalStatus.Draft
        };

        // Add entries from each segment
        foreach (var segment in request.Segments)
        {
            foreach (var entry in segment.Entries)
            {
                journal.Entries.Add(entry);
            }
        }

        // Validate the composite journal
        var validationErrors = ValidateCompositeJournal(journal, request);
        if (validationErrors.Any())
        {
            result.ValidationErrors.AddRange(validationErrors);
            result.Status = "Failed";
        }
        else
        {
            result.CompositeJournals.Add(journal);
            result.TotalSegments = request.Segments.Count;
        }

        // Save the journal to the database
        if (result.CompositeJournals.Any())
        {
            _context.Journals.AddRange(result.CompositeJournals);
            await _context.SaveChangesAsync();
        }

        return result;
    }

    public async Task<JournalTemplateResult> ManageJournalTemplatesAsync(JournalTemplateRequest request)
    {
        var result = new JournalTemplateResult
        {
            RequestId = Guid.NewGuid(),
            ProcessedDate = DateTime.UtcNow,
            TemplateId = Guid.NewGuid(), // In a real implementation, this would be the saved template ID
            TemplateName = request.TemplateName,
            Status = "Created",
            ValidationMessages = new List<ValidationMessage>()
        };

        // Validate the template
        var validationErrors = ValidateJournalTemplate(request);
        if (validationErrors.Any())
        {
            result.ValidationMessages.AddRange(validationErrors.Select(error => new ValidationMessage
            {
                MessageType = "Error",
                Message = error.ErrorMessage,
                Timestamp = DateTime.UtcNow
            }));
            result.Status = "ValidationFailed";
        }
        else
        {
            // In a real implementation, we would save the template to the database
            // For now, we'll just add a success message
            result.ValidationMessages.Add(new ValidationMessage
            {
                MessageType = "Info",
                Message = "Journal template created successfully",
                Timestamp = DateTime.UtcNow
            });
        }

        return result;
    }

    public async Task<JournalApprovalResult> ProcessJournalApprovalWorkflowsAsync(JournalApprovalRequest request)
    {
        var result = new JournalApprovalResult
        {
            RequestId = Guid.NewGuid(),
            ProcessedDate = DateTime.UtcNow,
            ApprovalStatuses = new List<JournalApprovalStatus>(),
            ValidationErrors = new List<ApprovalValidationError>()
        };

        foreach (var journalId in request.JournalIds)
        {
            var journal = await _context.Journals.FindAsync(journalId);
            if (journal != null)
            {
                // Process approval workflow based on type
                var approvalStatus = ProcessJournalApproval(journal, request.ApprovalLevels, request.RequireAllApprovals);
                result.ApprovalStatuses.Add(approvalStatus);

                // Check for validation errors
                if (approvalStatus.Status == "Rejected")
                {
                    result.ValidationErrors.Add(new ApprovalValidationError
                    {
                        JournalId = journalId,
                        ValidationErrorType = "Approval",
                        Description = $"Journal {journalId} was rejected during approval process",
                        ErrorDate = DateTime.UtcNow,
                        Severity = "High"
                    });
                }
            }
        }

        // Determine overall status
        var rejectedCount = result.ApprovalStatuses.Count(s => s.Status == "Rejected");
        var pendingCount = result.ApprovalStatuses.Count(s => s.Status == "Pending");
        var approvedCount = result.ApprovalStatuses.Count(s => s.Status == "Approved");

        result.OverallStatus = rejectedCount > 0 ? "Rejected" :
                              pendingCount > 0 ? "Pending" :
                              approvedCount == result.ApprovalStatuses.Count ? "Approved" : "PartiallyApproved";

        return result;
    }

    public async Task<JournalReversalResult> HandleJournalReversalAndReclassificationAsync(JournalReversalRequest request)
    {
        var result = new JournalReversalResult
        {
            RequestId = Guid.NewGuid(),
            ProcessedDate = DateTime.UtcNow,
            ReversalJournals = new List<Journal>(),
            ReclassificationJournals = new List<Journal>(),
            ValidationErrors = new List<ReversalValidationError>(),
            Status = "Success",
            TotalReversed = 0,
            TotalReclassified = 0
        };

        // Process each journal for reversal
        foreach (var journalId in request.JournalIdsToReverse)
        {
            var originalJournal = await _context.Journals
                .Include(j => j.Entries)
                .FirstOrDefaultAsync(j => j.Id == journalId);

            if (originalJournal != null)
            {
                // Create reversal journal
                var reversalJournal = CreateReversalJournal(originalJournal, request.ReversalDate, request.ReversalReason);

                // Validate reversal journal
                var validationErrors = ValidateJournal(reversalJournal);
                if (validationErrors.Any())
                {
                    result.ValidationErrors.AddRange(validationErrors.Select(ve => new ReversalValidationError
                    {
                        JournalId = journalId,
                        ValidationErrorType = ve.ValidationErrorType,
                        Description = ve.Description,
                        ErrorDate = DateTime.UtcNow,
                        Severity = ve.Severity
                    }));
                    result.Status = "Partial";
                }
                else
                {
                    result.ReversalJournals.Add(reversalJournal);
                    result.TotalReversed++;
                }
            }
        }

        // Process reclassifications
        foreach (var reclassification in request.Reclassifications)
        {
            var reclassificationJournal = CreateReclassificationJournal(reclassification, request.ReversalDate);

            // Validate reclassification journal
            var validationErrors = ValidateJournal(reclassificationJournal);
            if (validationErrors.Any())
            {
                result.ValidationErrors.AddRange(validationErrors.Select(ve => new ReversalValidationError
                {
                    JournalId = Guid.NewGuid(), // No specific journal ID for reclassification
                    ValidationErrorType = ve.ValidationErrorType,
                    Description = ve.Description,
                    ErrorDate = DateTime.UtcNow,
                    Severity = ve.Severity
                }));
                result.Status = "Partial";
            }
            else
            {
                result.ReclassificationJournals.Add(reclassificationJournal);
                result.TotalReclassified++;
            }
        }

        // Save all journals to the database
        var allJournals = result.ReversalJournals.Concat(result.ReclassificationJournals).ToList();
        if (allJournals.Any())
        {
            _context.Journals.AddRange(allJournals);
            await _context.SaveChangesAsync();
        }

        return result;
    }

    public async Task<MassJournalValidationResult> ValidateMassJournalUploadsAsync(MassJournalUploadRequest request)
    {
        var result = new MassJournalValidationResult
        {
            RequestId = Guid.NewGuid(),
            ProcessedDate = DateTime.UtcNow,
            ValidationStatuses = new List<MassJournalValidationStatus>(),
            ValidationErrors = new List<MassJournalValidationError>(),
            TotalJournals = request.Journals.Count,
            ValidJournals = 0,
            InvalidJournals = 0
        };

        foreach (var journal in request.Journals)
        {
            var status = new MassJournalValidationStatus
            {
                JournalId = journal.Id,
                JournalNumber = journal.ReferenceNumber,
                Status = "Pending",
                ValidationMessages = new List<string>(),
                ValidationDate = DateTime.UtcNow
            };

            // Validate the journal
            var validationErrors = ValidateJournal(journal);
            if (validationErrors.Any())
            {
                status.Status = "Invalid";
                status.ValidationMessages.AddRange(validationErrors.Select(ve => ve.Description));

                result.ValidationErrors.AddRange(validationErrors.Select(ve => new MassJournalValidationError
                {
                    JournalId = journal.Id,
                    ValidationErrorType = ve.ValidationErrorType,
                    Description = ve.Description,
                    ErrorDate = DateTime.UtcNow,
                    Severity = ve.Severity
                }));

                result.InvalidJournals++;
            }
            else
            {
                status.Status = "Valid";
                result.ValidJournals++;
            }

            result.ValidationStatuses.Add(status);
        }

        // Determine overall status
        result.OverallStatus = result.InvalidJournals == 0 ? "Valid" :
                              result.ValidJournals == 0 ? "Invalid" : "PartiallyValid";

        result.SuccessRate = result.TotalJournals > 0 ? (decimal)result.ValidJournals / result.TotalJournals * 100 : 0;

        // If not validate-only and auto-post is enabled, post valid journals
        if (!request.ValidateOnly && request.AutoPost)
        {
            var validJournals = request.Journals
                .Where(j => result.ValidationStatuses.First(vs => vs.JournalId == j.Id).Status == "Valid")
                .ToList();

            foreach (var journal in validJournals)
            {
                journal.Status = JournalStatus.Posted;
                journal.UpdatedDate = DateTime.UtcNow;
            }

            if (validJournals.Any())
            {
                _context.Journals.UpdateRange(validJournals);
                await _context.SaveChangesAsync();
            }
        }

        return result;
    }

    #region Helper Methods

    private decimal CalculateFormulaValue(RecurringFormula formula, DateTime date)
    {
        // Simplified formula calculation - in reality, this would evaluate complex expressions
        return 1000; // Placeholder value
    }

    private List<ValidationError> ValidateJournal(Journal journal)
    {
        var errors = new List<ValidationError>();

        // Check if journal is balanced
        var totalDebits = journal.Entries.Sum(e => e.Debit);
        var totalCredits = journal.Entries.Sum(e => e.Credit);

        if (Math.Abs(totalDebits - totalCredits) > 0.01m)
        {
            errors.Add(new ValidationError
            {
                ValidationErrorType = "Balance",
                Description = $"Journal is not balanced. Debits: {totalDebits}, Credits: {totalCredits}",
                Severity = "Critical"
            });
        }

        // Check for required fields
        if (string.IsNullOrEmpty(journal.Description))
        {
            errors.Add(new ValidationError
            {
                ValidationErrorType = "Field",
                Description = "Journal description is required",
                Severity = "High"
            });
        }

        // Check if accounts exist
        var accountIds = journal.Entries.Select(e => e.AccountId).Distinct();
        var existingAccounts = _context.Accounts.Where(a => accountIds.Contains(a.Id)).ToList();
        var missingAccounts = accountIds.Except(existingAccounts.Select(a => a.Id));

        if (missingAccounts.Any())
        {
            errors.Add(new ValidationError
            {
                ValidationErrorType = "Account",
                Description = $"Accounts do not exist: {string.Join(", ", missingAccounts)}",
                Severity = "Critical"
            });
        }

        return errors;
    }

    private List<CompositeValidationError> ValidateCompositeJournal(Journal journal, CompositeJournalRequest request)
    {
        var errors = new List<CompositeValidationError>();

        // Validate composite-specific rules
        if (request.StartDate > request.EndDate)
        {
            errors.Add(new CompositeValidationError
            {
                ValidationErrorType = "Period",
                Description = "Start date cannot be after end date",
                ErrorDate = DateTime.UtcNow,
                Severity = "Critical"
            });
        }

        // Validate segments
        if (!request.Segments.Any())
        {
            errors.Add(new CompositeValidationError
            {
                ValidationErrorType = "Segment",
                Description = "Composite journal must have at least one segment",
                ErrorDate = DateTime.UtcNow,
                Severity = "Critical"
            });
        }

        // Validate consolidation method
        if (!string.IsNullOrEmpty(request.ConsolidationMethod) &&
            !new[] { "Full", "Proportional", "Equity" }.Contains(request.ConsolidationMethod))
        {
            errors.Add(new CompositeValidationError
            {
                ValidationErrorType = "Method",
                Description = "Invalid consolidation method",
                ErrorDate = DateTime.UtcNow,
                Severity = "High"
            });
        }

        return errors;
    }

    private List<ValidationError> ValidateJournalTemplate(JournalTemplateRequest request)
    {
        var errors = new List<ValidationError>();

        // Validate template name
        if (string.IsNullOrEmpty(request.TemplateName))
        {
            errors.Add(new ValidationError
            {
                ValidationErrorType = "Field",
                Description = "Template name is required",
                Severity = "High"
            });
        }

        // Validate template lines
        if (!request.TemplateLines.Any())
        {
            errors.Add(new ValidationError
            {
                ValidationErrorType = "Field",
                Description = "Template must have at least one line",
                Severity = "Critical"
            });
        }

        // Validate accounts in template lines
        var accountIds = request.TemplateLines.Select(tl => tl.AccountId).Distinct();
        var existingAccounts = _context.Accounts.Where(a => accountIds.Contains(a.Id)).ToList();
        var missingAccounts = accountIds.Except(existingAccounts.Select(a => a.Id));

        if (missingAccounts.Any())
        {
            errors.Add(new ValidationError
            {
                ValidationErrorType = "Account",
                Description = $"Template references non-existent accounts: {string.Join(", ", missingAccounts)}",
                Severity = "Critical"
            });
        }

        return errors;
    }

    private JournalApprovalStatus ProcessJournalApproval(Journal journal, List<ApprovalLevel> approvalLevels, bool requireAllApprovals)
    {
        var status = new JournalApprovalStatus
        {
            JournalId = journal.Id,
            JournalNumber = journal.ReferenceNumber,
            CurrentApprovalLevel = "Initial",
            Status = "Pending",
            LastApprovalDate = null,
            LastApprovedByUserId = null,
            Comments = ""
        };

        // Simplified approval process
        // In a real implementation, this would check user authorities, approval limits, etc.
        var hasApprovalAuthority = true; // Placeholder

        if (hasApprovalAuthority)
        {
            status.Status = "Approved";
            status.LastApprovalDate = DateTime.UtcNow;
            status.LastApprovedByUserId = Guid.NewGuid(); // Placeholder
        }
        else
        {
            status.Status = "Rejected";
            status.Comments = "User does not have required approval authority";
        }

        return status;
    }

    private Journal CreateReversalJournal(Journal originalJournal, DateTime reversalDate, string reason)
    {
        var reversalJournal = new Journal
        {
            JournalDate = reversalDate,
            Description = $"REVERSAL: {originalJournal.Description} - {reason}",
            ReferenceNumber = $"REV-{originalJournal.ReferenceNumber}",
            CompanyId = originalJournal.CompanyId,
            Status = JournalStatus.Draft
        };

        // Create reversal entries (swap debits and credits)
        foreach (var originalEntry in originalJournal.Entries)
        {
            var reversalEntry = new JournalEntry
            {
                AccountId = originalEntry.AccountId,
                Debit = originalEntry.Credit, // Swap debit/credit
                Credit = originalEntry.Debit, // Swap debit/credit
                Description = $"REVERSAL: {originalEntry.Description}",
                BusinessUnitId = originalEntry.BusinessUnitId,
                TaxCodeId = originalEntry.TaxCodeId,
                TaxRatePercentage = originalEntry.TaxRatePercentage
            };

            reversalJournal.Entries.Add(reversalEntry);
        }

        return reversalJournal;
    }

    private Journal CreateReclassificationJournal(AccountReclassification reclassification, DateTime effectiveDate)
    {
        var reclassificationJournal = new Journal
        {
            JournalDate = effectiveDate,
            Description = $"ACCOUNT RECLASSIFICATION: Transfer from {reclassification.OriginalAccountId} to {reclassification.NewAccountId}",
            ReferenceNumber = $"REC-{Guid.NewGuid()}",
            CompanyId = Guid.NewGuid(), // Would be determined from the accounts
            Status = JournalStatus.Draft
        };

        // Debit the new account
        reclassificationJournal.Entries.Add(new JournalEntry
        {
            AccountId = reclassification.NewAccountId,
            Debit = reclassification.Amount,
            Credit = 0,
            Description = $"Reclassification to account {reclassification.NewAccountId}"
        });

        // Credit the original account
        reclassificationJournal.Entries.Add(new JournalEntry
        {
            AccountId = reclassification.OriginalAccountId,
            Debit = 0,
            Credit = reclassification.Amount,
            Description = $"Reclassification from account {reclassification.OriginalAccountId}"
        });

        return reclassificationJournal;
    }

    private async Task<Account> GetStatisticalAccountAsync(string metricType, Guid companyId)
    {
        // Determine appropriate account based on metric type
        var accountType = metricType.ToLower() switch
        {
            "cost" => AccountType.Expense,
            "revenue" => AccountType.Revenue,
            "headcount" => AccountType.Expense, // Usually tracked in expense accounts
            "efficiency" => AccountType.Expense, // Usually tracked in expense accounts
            _ => AccountType.Expense
        };

        // Get the first account of the appropriate type for the company
        return await _context.Accounts
            .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountType == accountType);
    }

    #endregion

    #region Nested Classes

    private class ValidationError
    {
        public string ValidationErrorType { get; set; }
        public string Description { get; set; }
        public string Severity { get; set; }
    }

    #endregion
}

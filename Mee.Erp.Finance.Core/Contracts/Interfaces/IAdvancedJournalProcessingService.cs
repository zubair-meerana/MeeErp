using Mee.Erp.Finance.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Contracts.Interfaces;

/// <summary>
/// Defines the contract for advanced journal processing
/// </summary>
public interface IAdvancedJournalProcessingService
{
    /// <summary>
    /// Creates recurring journal entries with complex formulas
    /// </summary>
    Task<RecurringJournalResult> CreateRecurringJournalEntriesAsync(RecurringJournalRequest request);

    /// <summary>
    /// Processes statistical journal entries
    /// </summary>
    Task<StatisticalJournalResult> ProcessStatisticalJournalEntriesAsync(StatisticalJournalRequest request);

    /// <summary>
    /// Creates composite journal entries spanning periods
    /// </summary>
    Task<CompositeJournalResult> CreateCompositeJournalEntriesAsync(CompositeJournalRequest request);

    /// <summary>
    /// Manages journal templates with validation rules
    /// </summary>
    Task<JournalTemplateResult> ManageJournalTemplatesAsync(JournalTemplateRequest request);

    /// <summary>
    /// Processes journal approval workflows with multiple levels
    /// </summary>
    Task<JournalApprovalResult> ProcessJournalApprovalWorkflowsAsync(JournalApprovalRequest request);

    /// <summary>
    /// Handles journal reversal and reclassification procedures
    /// </summary>
    Task<JournalReversalResult> HandleJournalReversalAndReclassificationAsync(JournalReversalRequest request);

    /// <summary>
    /// Validates mass journal uploads
    /// </summary>
    Task<MassJournalValidationResult> ValidateMassJournalUploadsAsync(MassJournalUploadRequest request);
}

public class RecurringJournalRequest
{
    public Guid CompanyId { get; set; }
    public string JournalTemplateName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Frequency { get; set; } // "Daily", "Weekly", "Monthly", "Quarterly", "Annually"
    public List<RecurringFormula> Formulas { get; set; } = new List<RecurringFormula>();
    public List<Guid> AccountIds { get; set; } = new List<Guid>();
    public List<Guid> BusinessUnitIds { get; set; } = new List<Guid>();
    public bool AutoPost { get; set; }
    public string Description { get; set; }
}

public class RecurringFormula
{
    public string FormulaName { get; set; }
    public string FormulaExpression { get; set; } // Mathematical expression
    public string FormulaType { get; set; } // "Percentage", "FixedAmount", "Calculation"
    public List<FormulaParameter> Parameters { get; set; } = new List<FormulaParameter>();
    public string DataSource { get; set; } // "Ledger", "Budget", "Forecast", "External"
}

public class FormulaParameter
{
    public string ParameterName { get; set; }
    public string ParameterType { get; set; } // "Decimal", "Integer", "String", "Date"
    public string ParameterValue { get; set; }
    public string DefaultValue { get; set; }
}

public class RecurringJournalResult
{
    public Guid RequestId { get; set; }
    public DateTime ProcessedDate { get; set; }
    public List<Journal> GeneratedJournals { get; set; } = new List<Journal>();
    public List<RecurringValidationError> ValidationErrors { get; set; } = new List<RecurringValidationError>();
    public string Status { get; set; } // "Success", "Partial", "Failed"
    public int TotalGenerated { get; set; }
    public int TotalPosted { get; set; }
    public int TotalFailed { get; set; }
}

public class RecurringValidationError
{
    public string ValidationErrorType { get; set; } // "Formula", "Balance", "Account", "Period"
    public string Description { get; set; }
    public DateTime ErrorDate { get; set; }
    public string Severity { get; set; } // "Low", "Medium", "High", "Critical"
}

public class StatisticalJournalRequest
{
    public Guid CompanyId { get; set; }
    public string StatisticalType { get; set; } // "Headcount", "RevenuePerEmployee", "CostPerUnit", "Efficiency"
    public DateTime AsOfDate { get; set; }
    public List<StatisticalMetric> Metrics { get; set; } = new List<StatisticalMetric>();
    public List<Guid> BusinessUnitIds { get; set; } = new List<Guid>();
    public string ReportingDimension { get; set; } // "Department", "Project", "Product", "Customer"
}

public class StatisticalMetric
{
    public string MetricName { get; set; }
    public string MetricType { get; set; } // "Count", "Ratio", "Percentage", "Index"
    public decimal Value { get; set; }
    public string UnitOfMeasure { get; set; } // "Hours", "Units", "People", "Percentage"
    public DateTime MeasurementDate { get; set; }
    public string DataSource { get; set; }
    public string Formula { get; set; }
}

public class StatisticalJournalResult
{
    public Guid RequestId { get; set; }
    public DateTime ProcessedDate { get; set; }
    public List<Journal> StatisticalJournals { get; set; } = new List<Journal>();
    public List<StatisticalValidationError> ValidationErrors { get; set; } = new List<StatisticalValidationError>();
    public string Status { get; set; } // "Success", "Partial", "Failed"
    public int TotalGenerated { get; set; }
    public int TotalPosted { get; set; }
}

public class StatisticalValidationError
{
    public string ValidationErrorType { get; set; } // "DataQuality", "Formula", "Range", "Consistency"
    public string Description { get; set; }
    public DateTime ErrorDate { get; set; }
    public string Severity { get; set; } // "Low", "Medium", "High", "Critical"
}

public class CompositeJournalRequest
{
    public Guid CompanyId { get; set; }
    public string CompositeType { get; set; } // "MultiPeriod", "MultiEntity", "MultiBook", "ComplexTransaction"
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<CompositeJournalSegment> Segments { get; set; } = new List<CompositeJournalSegment>();
    public List<Guid> EntityIds { get; set; } = new List<Guid>();
    public List<Guid> BookIds { get; set; } = new List<Guid>();
    public string ConsolidationMethod { get; set; } // "Full", "Proportional", "Equity"
}

public class CompositeJournalSegment
{
    public string SegmentName { get; set; }
    public DateTime SegmentDate { get; set; }
    public List<JournalEntry> Entries { get; set; } = new List<JournalEntry>();
    public string SegmentType { get; set; } // "Opening", "Activity", "Closing"
    public string SegmentDescription { get; set; }
}

public class CompositeJournalResult
{
    public Guid RequestId { get; set; }
    public DateTime ProcessedDate { get; set; }
    public List<Journal> CompositeJournals { get; set; } = new List<Journal>();
    public List<CompositeValidationError> ValidationErrors { get; set; } = new List<CompositeValidationError>();
    public string Status { get; set; } // "Success", "Partial", "Failed"
    public int TotalSegments { get; set; }
    public int TotalPosted { get; set; }
}

public class CompositeValidationError
{
    public string ValidationErrorType { get; set; } // "Segment", "Balance", "Period", "Consistency"
    public string Description { get; set; }
    public DateTime ErrorDate { get; set; }
    public string Severity { get; set; } // "Low", "Medium", "High", "Critical"
}

public class JournalTemplateRequest
{
    public Guid CompanyId { get; set; }
    public string TemplateName { get; set; }
    public string TemplateDescription { get; set; }
    public List<JournalTemplateLine> TemplateLines { get; set; } = new List<JournalTemplateLine>();
    public List<JournalValidationRule> ValidationRules { get; set; } = new List<JournalValidationRule>();
    public string TemplateType { get; set; } // "Standard", "Recurring", "Statistical", "Composite"
    public List<Guid> AllowedUserIds { get; set; } = new List<Guid>();
    public List<Guid> AllowedRoleIds { get; set; } = new List<Guid>();
}

public class JournalTemplateLine
{
    public int LineNumber { get; set; }
    public Guid AccountId { get; set; }
    public string AccountNumber { get; set; }
    public string AccountName { get; set; }
    public string AmountType { get; set; } // "Fixed", "Formula", "Percentage", "Variable"
    public decimal FixedAmount { get; set; }
    public string Formula { get; set; }
    public decimal Percentage { get; set; }
    public string Description { get; set; }
    public Guid? BusinessUnitId { get; set; }
    public Guid? CostCenterId { get; set; }
    public Guid? ProjectId { get; set; }
}

public class JournalValidationRule
{
    public string RuleName { get; set; }
    public string RuleType { get; set; } // "Balance", "Account", "Amount", "Period", "User"
    public string RuleExpression { get; set; }
    public string ErrorMessage { get; set; }
    public string Severity { get; set; } // "Warning", "Error", "Critical"
    public bool IsActive { get; set; }
}

public class JournalTemplateResult
{
    public Guid RequestId { get; set; }
    public DateTime ProcessedDate { get; set; }
    public Guid TemplateId { get; set; }
    public string TemplateName { get; set; }
    public string Status { get; set; } // "Created", "Updated", "Activated", "Deactivated"
    public List<ValidationMessage> ValidationMessages { get; set; } = new List<ValidationMessage>();
}

public class ValidationMessage
{
    public string MessageType { get; set; } // "Info", "Warning", "Error"
    public string Message { get; set; }
    public DateTime Timestamp { get; set; }
}

public class JournalApprovalRequest
{
    public Guid CompanyId { get; set; }
    public List<Guid> JournalIds { get; set; } = new List<Guid>();
    public string ApprovalType { get; set; } // "Standard", "Emergency", "Batch"
    public List<ApprovalLevel> ApprovalLevels { get; set; } = new List<ApprovalLevel>();
    public string ApprovalWorkflow { get; set; } // "Sequential", "Parallel", "Hybrid"
    public bool RequireAllApprovals { get; set; }
    public List<Guid> ApproverIds { get; set; } = new List<Guid>();
}

public class ApprovalLevel
{
    public int LevelNumber { get; set; }
    public List<Guid> ApproverIds { get; set; } = new List<Guid>();
    public decimal AmountThreshold { get; set; }
    public string Role { get; set; }
    public string Department { get; set; }
    public bool IsRequired { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public string Status { get; set; } // "Pending", "Approved", "Rejected", "Escalated"
}

public class JournalApprovalResult
{
    public Guid RequestId { get; set; }
    public DateTime ProcessedDate { get; set; }
    public List<JournalApprovalStatus> ApprovalStatuses { get; set; } = new List<JournalApprovalStatus>();
    public string OverallStatus { get; set; } // "Approved", "Rejected", "Pending", "PartiallyApproved"
    public List<ApprovalValidationError> ValidationErrors { get; set; } = new List<ApprovalValidationError>();
}

public class JournalApprovalStatus
{
    public Guid JournalId { get; set; }
    public string JournalNumber { get; set; }
    public string CurrentApprovalLevel { get; set; }
    public string Status { get; set; } // "Pending", "Approved", "Rejected", "Escalated"
    public DateTime? LastApprovalDate { get; set; }
    public Guid? LastApprovedByUserId { get; set; }
    public string Comments { get; set; }
}

public class ApprovalValidationError
{
    public Guid JournalId { get; set; }
    public string ValidationErrorType { get; set; } // "User", "Authority", "Workflow", "Data"
    public string Description { get; set; }
    public DateTime ErrorDate { get; set; }
    public string Severity { get; set; } // "Low", "Medium", "High", "Critical"
}

public class JournalReversalRequest
{
    public Guid CompanyId { get; set; }
    public List<Guid> JournalIdsToReverse { get; set; } = new List<Guid>();
    public DateTime ReversalDate { get; set; }
    public string ReversalReason { get; set; }
    public string ReversalType { get; set; } // "Full", "Partial", "Adjusting"
    public List<Guid> AccountIdsToReclassify { get; set; } = new List<Guid>();
    public List<AccountReclassification> Reclassifications { get; set; } = new List<AccountReclassification>();
    public bool AutoPost { get; set; }
}

public class AccountReclassification
{
    public Guid OriginalAccountId { get; set; }
    public Guid NewAccountId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; }
    public DateTime EffectiveDate { get; set; }
}

public class JournalReversalResult
{
    public Guid RequestId { get; set; }
    public DateTime ProcessedDate { get; set; }
    public List<Journal> ReversalJournals { get; set; } = new List<Journal>();
    public List<Journal> ReclassificationJournals { get; set; } = new List<Journal>();
    public List<ReversalValidationError> ValidationErrors { get; set; } = new List<ReversalValidationError>();
    public string Status { get; set; } // "Success", "Partial", "Failed"
    public int TotalReversed { get; set; }
    public int TotalReclassified { get; set; }
}

public class ReversalValidationError
{
    public Guid JournalId { get; set; }
    public string ValidationErrorType { get; set; } // "Status", "Authority", "Balance", "Period"
    public string Description { get; set; }
    public DateTime ErrorDate { get; set; }
    public string Severity { get; set; } // "Low", "Medium", "High", "Critical"
}

public class MassJournalUploadRequest
{
    public Guid CompanyId { get; set; }
    public List<Journal> Journals { get; set; } = new List<Journal>();
    public string UploadFormat { get; set; } // "CSV", "Excel", "JSON", "XML"
    public bool ValidateOnly { get; set; }
    public bool AutoPost { get; set; }
    public string ValidationProfile { get; set; } // "Strict", "Standard", "Relaxed"
    public List<Guid> BusinessUnitIds { get; set; } = new List<Guid>();
}

public class MassJournalValidationResult
{
    public Guid RequestId { get; set; }
    public DateTime ProcessedDate { get; set; }
    public List<MassJournalValidationStatus> ValidationStatuses { get; set; } = new List<MassJournalValidationStatus>();
    public List<MassJournalValidationError> ValidationErrors { get; set; } = new List<MassJournalValidationError>();
    public string OverallStatus { get; set; } // "Valid", "PartiallyValid", "Invalid"
    public int TotalJournals { get; set; }
    public int ValidJournals { get; set; }
    public int InvalidJournals { get; set; }
    public decimal SuccessRate { get; set; }
}

public class MassJournalValidationStatus
{
    public Guid JournalId { get; set; }
    public string JournalNumber { get; set; }
    public string Status { get; set; } // "Valid", "Invalid", "Pending"
    public List<string> ValidationMessages { get; set; } = new List<string>();
    public DateTime ValidationDate { get; set; }
}

public class MassJournalValidationError
{
    public Guid JournalId { get; set; }
    public string ValidationErrorType { get; set; } // "Format", "Balance", "Account", "Period", "User"
    public string Description { get; set; }
    public DateTime ErrorDate { get; set; }
    public string Severity { get; set; } // "Low", "Medium", "High", "Critical"
}

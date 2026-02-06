using Mee.Erp.Finance.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Contracts.Interfaces;

/// <summary>
/// Defines the contract for advanced reconciliation management
/// </summary>
public interface IAdvancedReconciliationService
{
    /// <summary>
    /// Performs automated bank reconciliations with exception handling
    /// </summary>
    Task<BankReconciliationResult> PerformAutomatedBankReconciliationAsync(BankReconciliationRequest request);

    /// <summary>
    /// Performs intercompany reconciliations with dispute resolution
    /// </summary>
    Task<IntercompanyReconciliationResult> PerformIntercompanyReconciliationAsync(IntercompanyReconciliationRequest request);

    /// <summary>
    /// Performs balance sheet reconciliations with supporting schedules
    /// </summary>
    Task<BalanceSheetReconciliationResult> PerformBalanceSheetReconciliationAsync(BalanceSheetReconciliationRequest request);

    /// <summary>
    /// Performs interim reconciliations for accruals
    /// </summary>
    Task<InterimReconciliationResult> PerformInterimReconciliationAsync(InterimReconciliationRequest request);

    /// <summary>
    /// Performs variance analysis and investigation workflows
    /// </summary>
    Task<VarianceAnalysisResult> PerformVarianceAnalysisAsync(VarianceAnalysisRequest request);

    /// <summary>
    /// Manages supporting document management for reconciliations
    /// </summary>
    Task<DocumentManagementResult> ManageReconciliationDocumentsAsync(DocumentManagementRequest request);
}

public class BankReconciliationRequest
{
    public Guid CompanyId { get; set; }
    public Guid BankAccountId { get; set; }
    public DateTime StatementDate { get; set; }
    public decimal StatementBalance { get; set; }
    public DateTime AsOfDate { get; set; }
    public List<BankTransaction> BankStatementTransactions { get; set; } = new List<BankTransaction>();
    public List<BankTransaction> SystemTransactions { get; set; } = new List<BankTransaction>();
    public decimal ToleranceAmount { get; set; } // Small difference tolerance
}

public class BankReconciliationResult
{
    public Guid RequestId { get; set; }
    public DateTime ProcessedDate { get; set; }
    public Guid BankAccountId { get; set; }
    public decimal StatementBalance { get; set; }
    public decimal SystemBalance { get; set; }
    public decimal Difference { get; set; }
    public List<ReconciliationMatch> Matches { get; set; } = new List<ReconciliationMatch>();
    public List<ReconciliationException> Exceptions { get; set; } = new List<ReconciliationException>();
    public List<Journal> AccountingEntries { get; set; } = new List<Journal>();
    public string Status { get; set; } // "Reconciled", "Partial", "NotReconciled"
    public decimal OutstandingChecks { get; set; }
    public decimal DepositsInTransit { get; set; }
}

public class ReconciliationMatch
{
    public Guid SystemTransactionId { get; set; }
    public Guid BankStatementTransactionId { get; set; }
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; }
    public string Description { get; set; }
    public DateTime MatchDate { get; set; }
    public string MatchMethod { get; set; } // "Amount", "Reference", "Manual"
}

public class ReconciliationException
{
    public Guid TransactionId { get; set; }
    public string TransactionType { get; set; } // "SystemOnly", "BankOnly"
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; }
    public string Description { get; set; }
    public string ExceptionType { get; set; } // "AmountMismatch", "DateMismatch", "Missing"
    public string ResolutionStatus { get; set; } // "Open", "Investigating", "Resolved", "Ignored"
    public string Notes { get; set; }
}

public class IntercompanyReconciliationRequest
{
    public Guid CompanyId { get; set; }
    public List<Guid> IntercompanyAccountIds { get; set; } = new List<Guid>();
    public DateTime AsOfDate { get; set; }
    public List<Guid> RelatedCompanyIds { get; set; } = new List<Guid>();
}

public class IntercompanyReconciliationResult
{
    public Guid RequestId { get; set; }
    public DateTime ProcessedDate { get; set; }
    public List<IntercompanyReconciliationLine> ReconciliationLines { get; set; } = new List<IntercompanyReconciliationLine>();
    public decimal TotalIntercompanyBalance { get; set; }
    public decimal TotalDiscrepancy { get; set; }
    public List<IntercompanyDiscrepancy> Discrepancies { get; set; } = new List<IntercompanyDiscrepancy>();
    public List<IntercompanyDispute> Disputes { get; set; } = new List<IntercompanyDispute>();
    public List<Journal> AccountingEntries { get; set; } = new List<Journal>();
    public string Status { get; set; } // "Balanced", "Unbalanced", "Partial"
}

public class IntercompanyReconciliationLine
{
    public Guid AccountId { get; set; }
    public string AccountName { get; set; }
    public Guid RelatedCompanyId { get; set; }
    public string RelatedCompanyName { get; set; }
    public decimal OurBalance { get; set; }
    public decimal TheirBalance { get; set; }
    public decimal Difference { get; set; }
    public DateTime LastReconciledDate { get; set; }
    public string Status { get; set; } // "Matched", "Unmatched", "Disputed"
}

public class IntercompanyDiscrepancy
{
    public Guid AccountId { get; set; }
    public Guid RelatedCompanyId { get; set; }
    public decimal OurBalance { get; set; }
    public decimal TheirBalance { get; set; }
    public decimal Difference { get; set; }
    public string DiscrepancyType { get; set; } // "Amount", "Timing", "Classification"
    public string ResolutionStatus { get; set; } // "Open", "Investigating", "Resolved"
    public string Notes { get; set; }
}

public class IntercompanyDispute
{
    public Guid DisputeId { get; set; }
    public Guid AccountId { get; set; }
    public Guid RelatedCompanyId { get; set; }
    public decimal Amount { get; set; }
    public string DisputeReason { get; set; }
    public DateTime DisputeDate { get; set; }
    public string Status { get; set; } // "Open", "InReview", "Resolved", "Escalated"
    public string ResolutionNotes { get; set; }
    public DateTime? ResolutionDate { get; set; }
    public Guid? ResolvedByUserId { get; set; }
}

public class BalanceSheetReconciliationRequest
{
    public Guid CompanyId { get; set; }
    public List<Guid> BalanceSheetAccountIds { get; set; } = new List<Guid>();
    public DateTime AsOfDate { get; set; }
    public List<SupportingSchedule> SupportingSchedules { get; set; } = new List<SupportingSchedule>();
}

public class SupportingSchedule
{
    public Guid ScheduleId { get; set; }
    public string ScheduleName { get; set; }
    public string ScheduleType { get; set; } // "Subledger", "Detail", "Analysis"
    public decimal ScheduleTotal { get; set; }
    public List<ScheduleDetail> Details { get; set; } = new List<ScheduleDetail>();
}

public class ScheduleDetail
{
    public Guid DetailId { get; set; }
    public string Description { get; set; }
    public decimal Amount { get; set; }
    public string Reference { get; set; }
    public DateTime Date { get; set; }
    public string Category { get; set; }
}

public class BalanceSheetReconciliationResult
{
    public Guid RequestId { get; set; }
    public DateTime ProcessedDate { get; set; }
    public List<BalanceSheetReconciliationLine> ReconciliationLines { get; set; } = new List<BalanceSheetReconciliationLine>();
    public decimal TotalGLBalance { get; set; }
    public decimal TotalScheduleBalance { get; set; }
    public decimal TotalDifference { get; set; }
    public List<ReconciliationException> Exceptions { get; set; } = new List<ReconciliationException>();
    public List<Journal> AccountingEntries { get; set; } = new List<Journal>();
    public List<SupportingSchedule> SupportingSchedules { get; set; } = new List<SupportingSchedule>();
    public string Status { get; set; } // "Reconciled", "Partial", "NotReconciled"
}

public class BalanceSheetReconciliationLine
{
    public Guid AccountId { get; set; }
    public string AccountNumber { get; set; }
    public string AccountName { get; set; }
    public decimal GLBalance { get; set; }
    public decimal ScheduleBalance { get; set; }
    public decimal Difference { get; set; }
    public List<SupportingSchedule> SupportingSchedules { get; set; } = new List<SupportingSchedule>();
    public string Status { get; set; } // "Reconciled", "Unreconciled", "Pending"
    public DateTime LastReconciledDate { get; set; }
}

public class InterimReconciliationRequest
{
    public Guid CompanyId { get; set; }
    public List<Guid> AccrualAccountIds { get; set; } = new List<Guid>();
    public DateTime AsOfDate { get; set; }
    public DateTime PeriodEndDate { get; set; }
}

public class InterimReconciliationResult
{
    public Guid RequestId { get; set; }
    public DateTime ProcessedDate { get; set; }
    public List<InterimReconciliationLine> ReconciliationLines { get; set; } = new List<InterimReconciliationLine>();
    public decimal TotalAccruedAmount { get; set; }
    public decimal TotalActualAmount { get; set; }
    public decimal TotalVariance { get; set; }
    public List<ReconciliationException> Exceptions { get; set; } = new List<ReconciliationException>();
    public List<Journal> AccountingEntries { get; set; } = new List<Journal>();
    public string Status { get; set; } // "Reconciled", "Partial", "NotReconciled"
}

public class InterimReconciliationLine
{
    public Guid AccountId { get; set; }
    public string AccountName { get; set; }
    public decimal AccruedAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal Variance { get; set; }
    public decimal VariancePercentage { get; set; }
    public string VarianceType { get; set; } // "Favorable", "Unfavorable"
    public string Status { get; set; } // "Reconciled", "Unreconciled", "Pending"
}

public class VarianceAnalysisRequest
{
    public Guid CompanyId { get; set; }
    public List<Guid> AccountIds { get; set; } = new List<Guid>();
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal VarianceThreshold { get; set; } // Percentage threshold for investigation
    public string VarianceType { get; set; } // "Favorable", "Unfavorable", "Both"
}

public class VarianceAnalysisResult
{
    public Guid RequestId { get; set; }
    public DateTime ProcessedDate { get; set; }
    public List<VarianceAnalysisLine> VarianceLines { get; set; } = new List<VarianceAnalysisLine>();
    public List<VarianceInvestigation> Investigations { get; set; } = new List<VarianceInvestigation>();
    public decimal TotalVariance { get; set; }
    public decimal TotalVariancePercentage { get; set; }
    public string Status { get; set; } // "Complete", "Partial", "RequiresAttention"
}

public class VarianceAnalysisLine
{
    public Guid AccountId { get; set; }
    public string AccountNumber { get; set; }
    public string AccountName { get; set; }
    public decimal BudgetedAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal Variance { get; set; }
    public decimal VariancePercentage { get; set; }
    public string VarianceType { get; set; } // "Favorable", "Unfavorable"
    public string VarianceCategory { get; set; } // "Operational", "Timing", "OneTime", "Structural"
    public string InvestigationStatus { get; set; } // "NotStarted", "InProcess", "Completed"
    public string RootCause { get; set; }
    public string ActionPlan { get; set; }
}

public class VarianceInvestigation
{
    public Guid InvestigationId { get; set; }
    public Guid AccountId { get; set; }
    public decimal VarianceAmount { get; set; }
    public decimal VariancePercentage { get; set; }
    public string InvestigationStatus { get; set; } // "Open", "InReview", "Completed"
    public string InvestigatorNotes { get; set; }
    public string RootCause { get; set; }
    public string CorrectiveAction { get; set; }
    public DateTime? InvestigationDate { get; set; }
    public DateTime? ResolutionDate { get; set; }
    public Guid? InvestigatorUserId { get; set; }
}

public class DocumentManagementRequest
{
    public Guid CompanyId { get; set; }
    public Guid ReconciliationId { get; set; }
    public string ReconciliationType { get; set; } // "Bank", "Intercompany", "BalanceSheet", "Interim"
    public List<DocumentAttachment> Attachments { get; set; } = new List<DocumentAttachment>();
}

public class DocumentAttachment
{
    public Guid DocumentId { get; set; }
    public string FileName { get; set; }
    public string FileType { get; set; } // "PDF", "Excel", "Image", etc.
    public long FileSize { get; set; }
    public string FileUrl { get; set; }
    public string Description { get; set; }
    public DateTime UploadDate { get; set; }
    public Guid UploadedByUserId { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? VerificationDate { get; set; }
    public Guid? VerifiedByUserId { get; set; }
}

public class DocumentManagementResult
{
    public Guid RequestId { get; set; }
    public DateTime ProcessedDate { get; set; }
    public List<DocumentAttachment> ProcessedAttachments { get; set; } = new List<DocumentAttachment>();
    public string Status { get; set; } // "Success", "Partial", "Failed"
    public List<string> Messages { get; set; } = new List<string>();
}

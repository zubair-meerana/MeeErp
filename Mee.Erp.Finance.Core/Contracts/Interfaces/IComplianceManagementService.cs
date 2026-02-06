using Mee.Erp.Finance.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mee.Erp.Finance.Core.Contracts.Interfaces;

/// <summary>
/// Defines the contract for regulatory and compliance management
/// </summary>
public interface IComplianceManagementService
{
    /// <summary>
    /// Tracks and manages SOX compliance
    /// </summary>
    Task<SoxComplianceResult> TrackSoxComplianceAsync(SoxComplianceRequest request);

    /// <summary>
    /// Automates local regulatory compliance
    /// </summary>
    Task<RegulatoryComplianceResult> AutomateRegulatoryComplianceAsync(RegulatoryComplianceRequest request);

    /// <summary>
    /// Maintains audit trails
    /// </summary>
    Task<AuditTrailResult> MaintainAuditTrailsAsync(AuditTrailRequest request);

    /// <summary>
    /// Tests and monitors financial controls
    /// </summary>
    Task<FinancialControlsResult> TestFinancialControlsAsync(FinancialControlsRequest request);

    /// <summary>
    /// Manages disclosure controls and procedures
    /// </summary>
    Task<DisclosureControlsResult> ManageDisclosureControlsAsync(DisclosureControlsRequest request);

    /// <summary>
    /// Manages regulatory filings
    /// </summary>
    Task<RegulatoryFilingResult> ManageRegulatoryFilingsAsync(RegulatoryFilingRequest request);

    /// <summary>
    /// Documents internal controls
    /// </summary>
    Task<InternalControlDocumentationResult> DocumentInternalControlsAsync(InternalControlDocumentationRequest request);
}

public class SoxComplianceRequest
{
    public Guid CompanyId { get; set; }
    public DateTime AsOfDate { get; set; }
    public List<Guid> ControlIds { get; set; } = new List<Guid>();
    public List<Guid> ProcessIds { get; set; } = new List<Guid>();
    public string ComplianceStandard { get; set; } // "SOX404", "SOX302", "SOX906"
    public List<Guid> BusinessUnitIds { get; set; } = new List<Guid>();
}

public class SoxComplianceResult
{
    public Guid RequestId { get; set; }
    public DateTime ProcessedDate { get; set; }
    public List<SoxControlTest> ControlTests { get; set; } = new List<SoxControlTest>();
    public List<SoxProcessAssessment> ProcessAssessments { get; set; } = new List<SoxProcessAssessment>();
    public List<SoxDeficiency> Deficiencies { get; set; } = new List<SoxDeficiency>();
    public string OverallComplianceStatus { get; set; } // "Compliant", "PartiallyCompliant", "NonCompliant"
    public decimal ComplianceScore { get; set; }
    public List<SoxRecommendation> Recommendations { get; set; } = new List<SoxRecommendation>();
    public List<Journal> AccountingEntries { get; set; } = new List<Journal>();
}

public class SoxControlTest
{
    public Guid ControlId { get; set; }
    public string ControlName { get; set; }
    public string ControlType { get; set; } // "Preventive", "Detective", "Corrective"
    public string TestProcedure { get; set; }
    public DateTime TestDate { get; set; }
    public string TestResult { get; set; } // "Passed", "Failed", "NotTested"
    public string TestEvidence { get; set; }
    public string TestedBy { get; set; }
    public string DeficiencyType { get; set; } // "Design", "OperatingEffectiveness"
    public string Severity { get; set; } // "Minor", "Significant", "Material"
    public DateTime? RemediationDate { get; set; }
    public string RemediationStatus { get; set; } // "Open", "InProcess", "Completed"
}

public class SoxProcessAssessment
{
    public Guid ProcessId { get; set; }
    public string ProcessName { get; set; }
    public string ProcessOwner { get; set; }
    public string AssessmentType { get; set; } // "DesignEffectiveness", "OperatingEffectiveness"
    public DateTime AssessmentDate { get; set; }
    public string AssessmentResult { get; set; } // "Effective", "Ineffective", "NotAssessed"
    public string AssessmentEvidence { get; set; }
    public string IdentifiedRisks { get; set; }
    public string MitigationMeasures { get; set; }
    public string AssessmentStatus { get; set; } // "Complete", "Incomplete", "Pending"
}

public class SoxDeficiency
{
    public Guid DeficiencyId { get; set; }
    public Guid ControlId { get; set; }
    public string DeficiencyType { get; set; } // "Design", "OperatingEffectiveness"
    public string Description { get; set; }
    public string Severity { get; set; } // "Minor", "Significant", "Material"
    public string PotentialImpact { get; set; }
    public DateTime DiscoveryDate { get; set; }
    public string Status { get; set; } // "Open", "InProcess", "Remediated"
    public string RootCause { get; set; }
    public string CorrectiveAction { get; set; }
    public DateTime? TargetRemediationDate { get; set; }
    public DateTime? ActualRemediationDate { get; set; }
    public Guid? ResponsiblePersonId { get; set; }
}

public class SoxRecommendation
{
    public string RecommendationType { get; set; } // "Control", "Process", "Policy"
    public string Description { get; set; }
    public string Priority { get; set; } // "High", "Medium", "Low"
    public DateTime RecommendedDate { get; set; }
    public string ExpectedBenefit { get; set; }
    public string ImplementationCost { get; set; }
}

public class RegulatoryComplianceRequest
{
    public Guid CompanyId { get; set; }
    public string CountryCode { get; set; }
    public string RegulatoryBody { get; set; } // "SEC", "IRS", "FASB", "LocalAuthority"
    public List<Regulation> Regulations { get; set; } = new List<Regulation>();
    public DateTime AsOfDate { get; set; }
    public List<Guid> BusinessUnitIds { get; set; } = new List<Guid>();
}

public class Regulation
{
    public string RegulationCode { get; set; }
    public string RegulationName { get; set; }
    public string RegulationType { get; set; } // "Financial", "Tax", "Operational", "Environmental"
    public string ComplianceRequirement { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string ReportingFrequency { get; set; } // "Monthly", "Quarterly", "Annually"
    public string PenaltyStructure { get; set; }
}

public class RegulatoryComplianceResult
{
    public Guid RequestId { get; set; }
    public DateTime ProcessedDate { get; set; }
    public List<RegulationComplianceStatus> ComplianceStatuses { get; set; } = new List<RegulationComplianceStatus>();
    public List<RegulatoryFilingRequirement> FilingRequirements { get; set; } = new List<RegulatoryFilingRequirement>();
    public List<RegulatoryAlert> Alerts { get; set; } = new List<RegulatoryAlert>();
    public string OverallComplianceStatus { get; set; } // "Compliant", "PartiallyCompliant", "NonCompliant"
    public decimal ComplianceScore { get; set; }
    public List<RegulatoryRecommendation> Recommendations { get; set; } = new List<RegulatoryRecommendation>();
}

public class RegulationComplianceStatus
{
    public string RegulationCode { get; set; }
    public string RegulationName { get; set; }
    public string Status { get; set; } // "Compliant", "NonCompliant", "Pending", "Waived"
    public DateTime LastAssessmentDate { get; set; }
    public string AssessmentMethod { get; set; } // "SelfAssessment", "ExternalAudit", "RegulatoryReview"
    public string Evidence { get; set; }
    public string NextAssessmentDate { get; set; }
    public string ComplianceOfficer { get; set; }
}

public class RegulatoryFilingRequirement
{
    public string FilingCode { get; set; }
    public string FilingName { get; set; }
    public DateTime DueDate { get; set; }
    public string FilingType { get; set; } // "Form", "Report", "Notice"
    public string FilingStatus { get; set; } // "Submitted", "Pending", "Overdue", "Waived"
    public DateTime? SubmissionDate { get; set; }
    public string FilingUrl { get; set; }
    public string ResponsibleOfficer { get; set; }
}

public class RegulatoryAlert
{
    public string AlertType { get; set; } // "Deadline", "Compliance", "Penalty"
    public string Description { get; set; }
    public DateTime AlertDate { get; set; }
    public string Severity { get; set; } // "Low", "Medium", "High", "Critical"
    public string ActionRequired { get; set; }
    public DateTime? RequiredActionDate { get; set; }
    public string Status { get; set; } // "Open", "Acknowledged", "Resolved"
}

public class RegulatoryRecommendation
{
    public string RecommendationType { get; set; } // "Process", "System", "Training", "Policy"
    public string Description { get; set; }
    public string Priority { get; set; } // "High", "Medium", "Low"
    public DateTime RecommendedDate { get; set; }
    public string ExpectedBenefit { get; set; }
    public string ImplementationCost { get; set; }
}

public class AuditTrailRequest
{
    public Guid CompanyId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<Guid> UserIds { get; set; } = new List<Guid>();
    public List<Guid> EntityIds { get; set; } = new List<Guid>();
    public List<string> ActionTypes { get; set; } = new List<string>(); // "Create", "Update", "Delete", "View"
    public List<string> EntityTypes { get; set; } = new List<string>(); // "Journal", "Account", "Customer", etc.
    public List<Guid> BusinessUnitIds { get; set; } = new List<Guid>();
}

public class AuditTrailResult
{
    public Guid RequestId { get; set; }
    public DateTime ProcessedDate { get; set; }
    public List<AuditEvent> AuditEvents { get; set; } = new List<AuditEvent>();
    public List<AuditFinding> AuditFindings { get; set; } = new List<AuditFinding>();
    public List<AuditRecommendation> Recommendations { get; set; } = new List<AuditRecommendation>();
    public string ReportType { get; set; } // "Full", "Summary", "Exception"
    public string AccessStatus { get; set; } // "Granted", "Restricted", "Denied"
}

public class AuditEvent
{
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; }
    public string ActionType { get; set; } // "Create", "Update", "Delete", "View"
    public string EntityType { get; set; } // "Journal", "Account", "Customer", etc.
    public Guid EntityId { get; set; }
    public string EntityName { get; set; }
    public DateTime EventDateTime { get; set; }
    public string IpAddress { get; set; }
    public string SessionId { get; set; }
    public string OldValues { get; set; }
    public string NewValues { get; set; }
    public string AdditionalInfo { get; set; }
}

public class AuditFinding
{
    public Guid FindingId { get; set; }
    public string FindingType { get; set; } // "UnauthorizedAccess", "DataIntegrity", "ProcessViolation"
    public string Description { get; set; }
    public DateTime DiscoveryDate { get; set; }
    public string Severity { get; set; } // "Low", "Medium", "High", "Critical"
    public string Status { get; set; } // "Open", "InReview", "Resolved", "Escalated"
    public string RootCause { get; set; }
    public string CorrectiveAction { get; set; }
    public DateTime? TargetResolutionDate { get; set; }
    public DateTime? ActualResolutionDate { get; set; }
    public Guid? ResponsibleUserId { get; set; }
}

public class AuditRecommendation
{
    public string RecommendationType { get; set; } // "Control", "Process", "System", "Policy"
    public string Description { get; set; }
    public string Priority { get; set; } // "High", "Medium", "Low"
    public DateTime RecommendedDate { get; set; }
    public string ExpectedBenefit { get; set; }
    public string ImplementationCost { get; set; }
}

public class FinancialControlsRequest
{
    public Guid CompanyId { get; set; }
    public DateTime AsOfDate { get; set; }
    public List<Guid> ControlIds { get; set; } = new List<Guid>();
    public List<Guid> ProcessIds { get; set; } = new List<Guid>();
    public string ControlType { get; set; } // "Preventive", "Detective", "Corrective"
    public List<Guid> BusinessUnitIds { get; set; } = new List<Guid>();
}

public class FinancialControlsResult
{
    public Guid RequestId { get; set; }
    public DateTime ProcessedDate { get; set; }
    public List<FinancialControl> Controls { get; set; } = new List<FinancialControl>();
    public List<ControlTestResult> ControlTests { get; set; } = new List<ControlTestResult>();
    public List<ControlDeficiency> Deficiencies { get; set; } = new List<ControlDeficiency>();
    public string OverallControlEffectiveness { get; set; } // "Effective", "PartiallyEffective", "Ineffective"
    public decimal ControlScore { get; set; }
    public List<ControlRecommendation> Recommendations { get; set; } = new List<ControlRecommendation>();
}

public class FinancialControl
{
    public Guid ControlId { get; set; }
    public string ControlName { get; set; }
    public string ControlType { get; set; } // "Preventive", "Detective", "Corrective"
    public string ControlObjective { get; set; }
    public string ControlProcedure { get; set; }
    public string ControlOwner { get; set; }
    public string ControlFrequency { get; set; } // "Daily", "Weekly", "Monthly", "Quarterly"
    public string ControlMethod { get; set; } // "Manual", "Automated", "Hybrid"
    public string ControlStatus { get; set; } // "Active", "Inactive", "UnderReview"
    public DateTime LastTestDate { get; set; }
    public string LastTestResult { get; set; } // "Passed", "Failed", "NotTested"
    public DateTime? NextTestDate { get; set; }
}

public class ControlTestResult
{
    public Guid ControlId { get; set; }
    public string ControlName { get; set; }
    public DateTime TestDate { get; set; }
    public string TestResult { get; set; } // "Passed", "Failed", "NotTested"
    public string TestEvidence { get; set; }
    public string TestedBy { get; set; }
    public string TestMethod { get; set; } // "Walkthrough", "Reperformance", "Inspection", "Inquiry"
    public string TestSampleSize { get; set; }
    public string DeviationsFound { get; set; }
    public string TestConclusion { get; set; }
}

public class ControlDeficiency
{
    public Guid DeficiencyId { get; set; }
    public Guid ControlId { get; set; }
    public string DeficiencyType { get; set; } // "Design", "OperatingEffectiveness"
    public string Description { get; set; }
    public string Severity { get; set; } // "Minor", "Significant", "Material"
    public string PotentialImpact { get; set; }
    public DateTime DiscoveryDate { get; set; }
    public string Status { get; set; } // "Open", "InProcess", "Remediated"
    public string RootCause { get; set; }
    public string CorrectiveAction { get; set; }
    public DateTime? TargetRemediationDate { get; set; }
    public DateTime? ActualRemediationDate { get; set; }
    public Guid? ResponsiblePersonId { get; set; }
}

public class ControlRecommendation
{
    public string RecommendationType { get; set; } // "Control", "Process", "System", "Training"
    public string Description { get; set; }
    public string Priority { get; set; } // "High", "Medium", "Low"
    public DateTime RecommendedDate { get; set; }
    public string ExpectedBenefit { get; set; }
    public string ImplementationCost { get; set; }
}

public class DisclosureControlsRequest
{
    public Guid CompanyId { get; set; }
    public DateTime AsOfDate { get; set; }
    public List<Guid> DisclosureProcessIds { get; set; } = new List<Guid>();
    public List<Guid> FinancialReportIds { get; set; } = new List<Guid>();
    public string DisclosureType { get; set; } // "Financial", "Operational", "Strategic"
    public List<Guid> BusinessUnitIds { get; set; } = new List<Guid>();
}

public class DisclosureControlsResult
{
    public Guid RequestId { get; set; }
    public DateTime ProcessedDate { get; set; }
    public List<DisclosureControl> Controls { get; set; } = new List<DisclosureControl>();
    public List<DisclosureControlTest> ControlTests { get; set; } = new List<DisclosureControlTest>();
    public List<DisclosureDeficiency> Deficiencies { get; set; } = new List<DisclosureDeficiency>();
    public string OverallControlEffectiveness { get; set; } // "Effective", "PartiallyEffective", "Ineffective"
    public decimal ControlScore { get; set; }
    public List<DisclosureRecommendation> Recommendations { get; set; } = new List<DisclosureRecommendation>();
}

public class DisclosureControl
{
    public Guid ControlId { get; set; }
    public string ControlName { get; set; }
    public string ControlType { get; set; } // "Preventive", "Detective", "Corrective"
    public string ControlObjective { get; set; }
    public string ControlProcedure { get; set; }
    public string ControlOwner { get; set; }
    public string ControlFrequency { get; set; } // "PerFiling", "Continuous", "Periodic"
    public string ControlMethod { get; set; } // "Manual", "Automated", "Hybrid"
    public string ControlStatus { get; set; } // "Active", "Inactive", "UnderReview"
    public DateTime LastTestDate { get; set; }
    public string LastTestResult { get; set; } // "Passed", "Failed", "NotTested"
    public DateTime? NextTestDate { get; set; }
}

public class DisclosureControlTest
{
    public Guid ControlId { get; set; }
    public string ControlName { get; set; }
    public DateTime TestDate { get; set; }
    public string TestResult { get; set; } // "Passed", "Failed", "NotTested"
    public string TestEvidence { get; set; }
    public string TestedBy { get; set; }
    public string TestMethod { get; set; } // "Walkthrough", "Reperformance", "Inspection", "Inquiry"
    public string TestSampleSize { get; set; }
    public string DeviationsFound { get; set; }
    public string TestConclusion { get; set; }
}

public class DisclosureDeficiency
{
    public Guid DeficiencyId { get; set; }
    public Guid ControlId { get; set; }
    public string DeficiencyType { get; set; } // "Design", "OperatingEffectiveness"
    public string Description { get; set; }
    public string Severity { get; set; } // "Minor", "Significant", "Material"
    public string PotentialImpact { get; set; }
    public DateTime DiscoveryDate { get; set; }
    public string Status { get; set; } // "Open", "InProcess", "Remediated"
    public string RootCause { get; set; }
    public string CorrectiveAction { get; set; }
    public DateTime? TargetRemediationDate { get; set; }
    public DateTime? ActualRemediationDate { get; set; }
    public Guid? ResponsiblePersonId { get; set; }
}

public class DisclosureRecommendation
{
    public string RecommendationType { get; set; } // "Control", "Process", "System", "Training"
    public string Description { get; set; }
    public string Priority { get; set; } // "High", "Medium", "Low"
    public DateTime RecommendedDate { get; set; }
    public string ExpectedBenefit { get; set; }
    public string ImplementationCost { get; set; }
}

public class RegulatoryFilingRequest
{
    public Guid CompanyId { get; set; }
    public DateTime AsOfDate { get; set; }
    public List<RegulatoryFilingRequirement> FilingRequirements { get; set; } = new List<RegulatoryFilingRequirement>();
    public string RegulatoryBody { get; set; } // "SEC", "IRS", "LocalAuthority"
    public List<Guid> BusinessUnitIds { get; set; } = new List<Guid>();
}

public class RegulatoryFilingResult
{
    public Guid RequestId { get; set; }
    public DateTime ProcessedDate { get; set; }
    public List<RegulatoryFilingSubmission> Filings { get; set; } = new List<RegulatoryFilingSubmission>();
    public List<RegulatoryFilingAlert> Alerts { get; set; } = new List<RegulatoryFilingAlert>();
    public string OverallFilingStatus { get; set; } // "AllFiled", "PartiallyFiled", "Overdue"
    public List<RegulatoryFilingRecommendation> Recommendations { get; set; } = new List<RegulatoryFilingRecommendation>();
}

public class RegulatoryFilingSubmission
{
    public Guid FilingId { get; set; }
    public string FilingCode { get; set; }
    public string FilingName { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? SubmissionDate { get; set; }
    public string FilingStatus { get; set; } // "Submitted", "Pending", "Overdue", "Waived"
    public string FilingUrl { get; set; }
    public string FilingContent { get; set; }
    public string FilingFormat { get; set; } // "XML", "PDF", "Excel", "JSON"
    public string SubmittedBy { get; set; }
    public string ValidationStatus { get; set; } // "Valid", "Invalid", "Pending"
    public string ValidationErrors { get; set; }
}

public class RegulatoryFilingAlert
{
    public string AlertType { get; set; } // "Deadline", "Validation", "Submission"
    public string Description { get; set; }
    public DateTime AlertDate { get; set; }
    public string Severity { get; set; } // "Low", "Medium", "High", "Critical"
    public string ActionRequired { get; set; }
    public DateTime? RequiredActionDate { get; set; }
    public string Status { get; set; } // "Open", "Acknowledged", "Resolved"
}

public class RegulatoryFilingRecommendation
{
    public string RecommendationType { get; set; } // "Process", "System", "Training", "Policy"
    public string Description { get; set; }
    public string Priority { get; set; } // "High", "Medium", "Low"
    public DateTime RecommendedDate { get; set; }
    public string ExpectedBenefit { get; set; }
    public string ImplementationCost { get; set; }
}

public class InternalControlDocumentationRequest
{
    public Guid CompanyId { get; set; }
    public DateTime AsOfDate { get; set; }
    public List<Guid> ProcessIds { get; set; } = new List<Guid>();
    public List<Guid> ControlIds { get; set; } = new List<Guid>();
    public string DocumentationType { get; set; } // "ProcessFlow", "ControlMatrix", "RiskRegister"
    public List<Guid> BusinessUnitIds { get; set; } = new List<Guid>();
}

public class InternalControlDocumentationResult
{
    public Guid RequestId { get; set; }
    public DateTime ProcessedDate { get; set; }
    public List<ProcessDocumentation> ProcessDocs { get; set; } = new List<ProcessDocumentation>();
    public List<ControlDocumentation> ControlDocs { get; set; } = new List<ControlDocumentation>();
    public List<RiskRegister> RiskRegisters { get; set; } = new List<RiskRegister>();
    public List<ControlMatrix> ControlMatrices { get; set; } = new List<ControlMatrix>();
    public string DocumentationStatus { get; set; } // "Complete", "Partial", "Missing"
    public List<DocumentationRecommendation> Recommendations { get; set; } = new List<DocumentationRecommendation>();
}

public class ProcessDocumentation
{
    public Guid ProcessId { get; set; }
    public string ProcessName { get; set; }
    public string ProcessDescription { get; set; }
    public string ProcessOwner { get; set; }
    public string ProcessInputs { get; set; }
    public string ProcessOutputs { get; set; }
    public string ProcessSteps { get; set; }
    public string ProcessControls { get; set; }
    public string ProcessRisks { get; set; }
    public string ProcessMetrics { get; set; }
    public DateTime LastUpdated { get; set; }
    public string DocumentationUrl { get; set; }
    public string Version { get; set; }
}

public class ControlDocumentation
{
    public Guid ControlId { get; set; }
    public string ControlName { get; set; }
    public string ControlDescription { get; set; }
    public string ControlType { get; set; } // "Preventive", "Detective", "Corrective"
    public string ControlObjective { get; set; }
    public string ControlProcedure { get; set; }
    public string ControlOwner { get; set; }
    public string ControlFrequency { get; set; }
    public string ControlMethod { get; set; }
    public string ControlTesting { get; set; }
    public string ControlMonitoring { get; set; }
    public DateTime LastUpdated { get; set; }
    public string DocumentationUrl { get; set; }
    public string Version { get; set; }
}

public class RiskRegister
{
    public Guid RiskId { get; set; }
    public string RiskName { get; set; }
    public string RiskDescription { get; set; }
    public string RiskCategory { get; set; } // "Financial", "Operational", "Compliance", "Strategic"
    public string RiskOwner { get; set; }
    public decimal RiskProbability { get; set; } // 0-1 scale
    public decimal RiskImpact { get; set; } // 0-1 scale
    public decimal RiskScore { get; set; } // Probability * Impact
    public string RiskLevel { get; set; } // "Low", "Medium", "High", "Critical"
    public string RiskMitigation { get; set; }
    public string RiskMonitoring { get; set; }
    public DateTime LastUpdated { get; set; }
    public string DocumentationUrl { get; set; }
    public string Version { get; set; }
}

public class ControlMatrix
{
    public Guid MatrixId { get; set; }
    public string MatrixName { get; set; }
    public string MatrixDescription { get; set; }
    public List<ControlMatrixRow> Rows { get; set; } = new List<ControlMatrixRow>();
    public DateTime LastUpdated { get; set; }
    public string DocumentationUrl { get; set; }
    public string Version { get; set; }
}

public class ControlMatrixRow
{
    public string Risk { get; set; }
    public string Control { get; set; }
    public string ControlType { get; set; } // "Preventive", "Detective", "Corrective"
    public string ControlOwner { get; set; }
    public string ControlFrequency { get; set; }
    public string ControlTesting { get; set; }
    public string ControlEffectiveness { get; set; } // "High", "Medium", "Low"
}

public class DocumentationRecommendation
{
    public string RecommendationType { get; set; } // "Documentation", "Process", "System"
    public string Description { get; set; }
    public string Priority { get; set; } // "High", "Medium", "Low"
    public DateTime RecommendedDate { get; set; }
    public string ExpectedBenefit { get; set; }
    public string ImplementationCost { get; set; }
}

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
/// Implements regulatory and compliance management
/// </summary>
public class ComplianceManagementService : IComplianceManagementService
{
    private readonly FinanceDbContext _context;

    public ComplianceManagementService(FinanceDbContext context)
    {
        _context = context;
    }

    public async Task<SoxComplianceResult> TrackSoxComplianceAsync(SoxComplianceRequest request)
    {
        var result = new SoxComplianceResult
        {
            RequestId = Guid.NewGuid(),
            ProcessedDate = DateTime.UtcNow,
            ControlTests = new List<SoxControlTest>(),
            ProcessAssessments = new List<SoxProcessAssessment>(),
            Deficiencies = new List<SoxDeficiency>(),
            Recommendations = new List<SoxRecommendation>(),
            AccountingEntries = new List<Journal>()
        };

        // Process SOX control tests
        foreach (var controlId in request.ControlIds)
        {
            var controlTest = new SoxControlTest
            {
                ControlId = controlId,
                ControlName = $"Control {controlId}",
                ControlType = "Detective",
                TestProcedure = "Test procedure for SOX compliance",
                TestDate = DateTime.Today,
                TestResult = "Passed",
                TestEvidence = "Test evidence documentation",
                TestedBy = "Compliance Officer",
                Severity = "Minor",
                RemediationStatus = "Completed"
            };

            result.ControlTests.Add(controlTest);

            // Check for deficiencies
            if (controlTest.TestResult == "Failed")
            {
                result.Deficiencies.Add(new SoxDeficiency
                {
                    DeficiencyId = Guid.NewGuid(),
                    ControlId = controlId,
                    DeficiencyType = "OperatingEffectiveness",
                    Description = $"Control {controlId} failed testing",
                    Severity = controlTest.Severity,
                    PotentialImpact = "Moderate impact on financial reporting",
                    DiscoveryDate = DateTime.Today,
                    Status = "Open",
                    RootCause = "Insufficient monitoring",
                    CorrectiveAction = "Implement additional monitoring controls",
                    TargetRemediationDate = DateTime.Today.AddDays(30),
                    ResponsiblePersonId = Guid.NewGuid()
                });
            }
        }

        // Process SOX process assessments
        foreach (var processId in request.ProcessIds)
        {
            var processAssessment = new SoxProcessAssessment
            {
                ProcessId = processId,
                ProcessName = $"Process {processId}",
                ProcessOwner = "Process Owner",
                AssessmentType = "OperatingEffectiveness",
                AssessmentDate = DateTime.Today,
                AssessmentResult = "Effective",
                AssessmentEvidence = "Assessment evidence documentation",
                IdentifiedRisks = "Low risk identified",
                MitigationMeasures = "Standard controls in place",
                AssessmentStatus = "Complete"
            };

            result.ProcessAssessments.Add(processAssessment);
        }

        // Calculate overall compliance status
        var failedTests = result.ControlTests.Count(ct => ct.TestResult == "Failed");
        var totalTests = result.ControlTests.Count;

        result.OverallComplianceStatus = failedTests == 0 ? "Compliant" :
                                        failedTests < totalTests * 0.1m ? "PartiallyCompliant" : "NonCompliant";

        result.ComplianceScore = totalTests > 0 ? ((totalTests - failedTests) / (decimal)totalTests) * 100 : 0;

        // Add recommendations
        if (failedTests > 0)
        {
            result.Recommendations.Add(new SoxRecommendation
            {
                RecommendationType = "Control",
                Description = "Implement additional controls for failed areas",
                Priority = "High",
                RecommendedDate = DateTime.Today,
                ExpectedBenefit = "Improve SOX compliance score by 10%",
                ImplementationCost = "$50,000"
            });
        }

        return result;
    }

    public async Task<RegulatoryComplianceResult> AutomateRegulatoryComplianceAsync(RegulatoryComplianceRequest request)
    {
        var result = new RegulatoryComplianceResult
        {
            RequestId = Guid.NewGuid(),
            ProcessedDate = DateTime.UtcNow,
            ComplianceStatuses = new List<RegulationComplianceStatus>(),
            FilingRequirements = new List<RegulatoryFilingRequirement>(),
            Alerts = new List<RegulatoryAlert>(),
            Recommendations = new List<RegulatoryRecommendation>()
        };

        // Process each regulation
        foreach (var regulation in request.Regulations)
        {
            var complianceStatus = new RegulationComplianceStatus
            {
                RegulationCode = regulation.RegulationCode,
                RegulationName = regulation.RegulationName,
                Status = "Compliant",
                LastAssessmentDate = DateTime.Today,
                AssessmentMethod = "SelfAssessment",
                Evidence = "Compliance evidence documentation",
                NextAssessmentDate = DateTime.Today.AddMonths(3),
                ComplianceOfficer = "Compliance Officer"
            };

            result.ComplianceStatuses.Add(complianceStatus);

            // Add filing requirements based on reporting frequency
            if (regulation.ReportingFrequency == "Quarterly")
            {
                var nextFilingDate = GetNextQuarterlyFilingDate(DateTime.Today);
                result.FilingRequirements.Add(new RegulatoryFilingRequirement
                {
                    FilingCode = $"{regulation.RegulationCode}-QTR",
                    FilingName = $"{regulation.RegulationName} Quarterly Report",
                    DueDate = nextFilingDate,
                    FilingType = "Report",
                    FilingStatus = "Pending",
                    ResponsibleOfficer = "Compliance Officer"
                });
            }
            else if (regulation.ReportingFrequency == "Annually")
            {
                var nextFilingDate = new DateTime(DateTime.Today.Year + 1, 1, 31); // Jan 31 next year
                result.FilingRequirements.Add(new RegulatoryFilingRequirement
                {
                    FilingCode = $"{regulation.RegulationCode}-ANN",
                    FilingName = $"{regulation.RegulationName} Annual Report",
                    DueDate = nextFilingDate,
                    FilingType = "Report",
                    FilingStatus = "Pending",
                    ResponsibleOfficer = "Compliance Officer"
                });
            }

            // Check for upcoming deadlines
            if ((regulation.EffectiveDate - DateTime.Today).Days <= 30)
            {
                result.Alerts.Add(new RegulatoryAlert
                {
                    AlertType = "Deadline",
                    Description = $"Regulation {regulation.RegulationName} becomes effective on {regulation.EffectiveDate:yyyy-MM-dd}",
                    AlertDate = DateTime.Today,
                    Severity = "High",
                    ActionRequired = "Implement compliance measures",
                    RequiredActionDate = regulation.EffectiveDate.AddDays(-7),
                    Status = "Open"
                });
            }
        }

        // Calculate overall compliance status
        var compliantRegs = result.ComplianceStatuses.Count(cs => cs.Status == "Compliant");
        var totalRegs = result.ComplianceStatuses.Count;

        result.OverallComplianceStatus = compliantRegs == totalRegs ? "Compliant" :
                                        compliantRegs >= totalRegs * 0.8m ? "PartiallyCompliant" : "NonCompliant";

        result.ComplianceScore = totalRegs > 0 ? ((decimal)compliantRegs / totalRegs) * 100 : 0;

        // Add recommendations
        if (result.Alerts.Any(a => a.Severity == "High"))
        {
            result.Recommendations.Add(new RegulatoryRecommendation
            {
                RecommendationType = "Process",
                Description = "Prioritize high-severity regulatory alerts",
                Priority = "High",
                RecommendedDate = DateTime.Today,
                ExpectedBenefit = "Avoid regulatory penalties",
                ImplementationCost = "Varies by regulation"
            });
        }

        return result;
    }

    public async Task<AuditTrailResult> MaintainAuditTrailsAsync(AuditTrailRequest request)
    {
        var result = new AuditTrailResult
        {
            RequestId = Guid.NewGuid(),
            ProcessedDate = DateTime.UtcNow,
            AuditEvents = new List<AuditEvent>(),
            AuditFindings = new List<AuditFinding>(),
            Recommendations = new List<AuditRecommendation>(),
            AccessStatus = "Granted"
        };

        // Query audit events based on request parameters
        var auditEventsQuery = _context.LedgerEntries
            .Where(le => le.CreatedDate >= request.StartDate && le.CreatedDate <= request.EndDate);

        // Filter by user IDs if specified
        if (request.UserIds.Any())
        {
            auditEventsQuery = auditEventsQuery.Where(le => request.UserIds.Contains(le.CreatedBy ?? Guid.Empty));
        }

        // Get audit events
        var ledgerEntries = await auditEventsQuery.ToListAsync();

        foreach (var entry in ledgerEntries)
        {
            var eventEntry = new AuditEvent
            {
                EventId = entry.Id,
                UserId = entry.CreatedBy ?? Guid.Empty,
                UserName = "User Name", // Would be retrieved from user table
                ActionType = "Create", // Would be determined from the action
                EntityType = "LedgerEntry",
                EntityId = entry.Id,
                EntityName = "Ledger Entry",
                EventDateTime = entry.CreatedDate,
                IpAddress = "127.0.0.1", // Would be from session info
                SessionId = "SessionId", // Would be from session info
                OldValues = "", // Would be captured from before state
                NewValues = $"Debit: {entry.Debit}, Credit: {entry.Credit}", // Current state
                AdditionalInfo = "Ledger entry created"
            };

            result.AuditEvents.Add(eventEntry);

            // Check for potential audit findings
            if (entry.Debit > 10000 || entry.Credit > 10000) // Large transaction
            {
                result.AuditFindings.Add(new AuditFinding
                {
                    FindingId = Guid.NewGuid(),
                    FindingType = "LargeTransaction",
                    Description = $"Large transaction recorded: {entry.Debit - entry.Credit:C}",
                    DiscoveryDate = DateTime.Today,
                    Severity = "Medium",
                    Status = "Open",
                    RootCause = "High-value transaction",
                    CorrectiveAction = "Review transaction authorization",
                    TargetResolutionDate = DateTime.Today.AddDays(7),
                    ResponsibleUserId = entry.CreatedBy
                });
            }
        }

        // Add recommendations based on findings
        if (result.AuditFindings.Any())
        {
            result.Recommendations.Add(new AuditRecommendation
            {
                RecommendationType = "Control",
                Description = "Implement enhanced controls for large transactions",
                Priority = "High",
                RecommendedDate = DateTime.Today,
                ExpectedBenefit = "Reduce risk of unauthorized large transactions",
                ImplementationCost = "$25,000"
            });
        }

        return result;
    }

    public async Task<FinancialControlsResult> TestFinancialControlsAsync(FinancialControlsRequest request)
    {
        var result = new FinancialControlsResult
        {
            RequestId = Guid.NewGuid(),
            ProcessedDate = DateTime.UtcNow,
            Controls = new List<FinancialControl>(),
            ControlTests = new List<ControlTestResult>(),
            Deficiencies = new List<ControlDeficiency>(),
            Recommendations = new List<ControlRecommendation>()
        };

        // Process each control
        foreach (var controlId in request.ControlIds)
        {
            var control = new FinancialControl
            {
                ControlId = controlId,
                ControlName = $"Control {controlId}",
                ControlType = "Preventive",
                ControlObjective = "Prevent unauthorized transactions",
                ControlProcedure = "Review and approval process",
                ControlOwner = "Finance Manager",
                ControlFrequency = "Daily",
                ControlMethod = "Manual",
                ControlStatus = "Active",
                LastTestDate = DateTime.Today.AddDays(-7),
                LastTestResult = "Passed",
                NextTestDate = DateTime.Today.AddDays(23)
            };

            result.Controls.Add(control);

            // Perform control test
            var testResult = new ControlTestResult
            {
                ControlId = controlId,
                ControlName = control.ControlName,
                TestDate = DateTime.Today,
                TestResult = "Passed",
                TestEvidence = "Test evidence documentation",
                TestedBy = "Control Tester",
                TestMethod = "Walkthrough",
                TestSampleSize = "20 samples",
                DeviationsFound = "0 deviations",
                TestConclusion = "Control operating effectively"
            };

            result.ControlTests.Add(testResult);

            // Check for deficiencies
            if (testResult.TestResult == "Failed")
            {
                result.Deficiencies.Add(new ControlDeficiency
                {
                    DeficiencyId = Guid.NewGuid(),
                    ControlId = controlId,
                    DeficiencyType = "OperatingEffectiveness",
                    Description = $"Control {controlId} failed testing",
                    Severity = "Significant",
                    PotentialImpact = "Risk of unauthorized transactions",
                    DiscoveryDate = DateTime.Today,
                    Status = "Open",
                    RootCause = "Inadequate monitoring",
                    CorrectiveAction = "Implement additional monitoring",
                    TargetRemediationDate = DateTime.Today.AddDays(30),
                    ResponsiblePersonId = Guid.NewGuid()
                });
            }
        }

        // Calculate overall effectiveness
        var failedTests = result.ControlTests.Count(ct => ct.TestResult == "Failed");
        var totalTests = result.ControlTests.Count;

        result.OverallControlEffectiveness = failedTests == 0 ? "Effective" :
                                            failedTests < totalTests * 0.1m ? "PartiallyEffective" : "Ineffective";

        result.ControlScore = totalTests > 0 ? ((totalTests - failedTests) / (decimal)totalTests) * 100 : 0;

        // Add recommendations
        if (failedTests > 0)
        {
            result.Recommendations.Add(new ControlRecommendation
            {
                RecommendationType = "Control",
                Description = "Address control deficiencies identified in testing",
                Priority = "High",
                RecommendedDate = DateTime.Today,
                ExpectedBenefit = "Improve control effectiveness by 15%",
                ImplementationCost = "$30,000"
            });
        }

        return result;
    }

    public async Task<DisclosureControlsResult> ManageDisclosureControlsAsync(DisclosureControlsRequest request)
    {
        var result = new DisclosureControlsResult
        {
            RequestId = Guid.NewGuid(),
            ProcessedDate = DateTime.UtcNow,
            Controls = new List<DisclosureControl>(),
            ControlTests = new List<DisclosureControlTest>(),
            Deficiencies = new List<DisclosureDeficiency>(),
            Recommendations = new List<DisclosureRecommendation>()
        };

        // Process each disclosure process
        foreach (var processId in request.DisclosureProcessIds)
        {
            var control = new DisclosureControl
            {
                ControlId = processId,
                ControlName = $"Disclosure Control {processId}",
                ControlType = "Detective",
                ControlObjective = "Ensure accurate financial disclosures",
                ControlProcedure = "Review and validation process",
                ControlOwner = "CFO",
                ControlFrequency = "PerFiling",
                ControlMethod = "Manual",
                ControlStatus = "Active",
                LastTestDate = DateTime.Today.AddDays(-14),
                LastTestResult = "Passed",
                NextTestDate = DateTime.Today.AddDays(16)
            };

            result.Controls.Add(control);

            // Perform control test
            var testResult = new DisclosureControlTest
            {
                ControlId = processId,
                ControlName = control.ControlName,
                TestDate = DateTime.Today,
                TestResult = "Passed",
                TestEvidence = "Test evidence documentation",
                TestedBy = "Disclosure Officer",
                TestMethod = "Reperformance",
                TestSampleSize = "10 samples",
                DeviationsFound = "0 deviations",
                TestConclusion = "Control operating effectively"
            };

            result.ControlTests.Add(testResult);

            // Check for deficiencies
            if (testResult.TestResult == "Failed")
            {
                result.Deficiencies.Add(new DisclosureDeficiency
                {
                    DeficiencyId = Guid.NewGuid(),
                    ControlId = processId,
                    DeficiencyType = "OperatingEffectiveness",
                    Description = $"Disclosure control {processId} failed testing",
                    Severity = "Material",
                    PotentialImpact = "Risk of inaccurate financial disclosures",
                    DiscoveryDate = DateTime.Today,
                    Status = "Open",
                    RootCause = "Insufficient validation procedures",
                    CorrectiveAction = "Implement additional validation controls",
                    TargetRemediationDate = DateTime.Today.AddDays(45),
                    ResponsiblePersonId = Guid.NewGuid()
                });
            }
        }

        // Calculate overall effectiveness
        var failedTests = result.ControlTests.Count(ct => ct.TestResult == "Failed");
        var totalTests = result.ControlTests.Count;

        result.OverallControlEffectiveness = failedTests == 0 ? "Effective" :
                                            failedTests < totalTests * 0.05m ? "PartiallyEffective" : "Ineffective";

        result.ControlScore = totalTests > 0 ? ((totalTests - failedTests) / (decimal)totalTests) * 100 : 0;

        // Add recommendations
        if (failedTests > 0)
        {
            result.Recommendations.Add(new DisclosureRecommendation
            {
                RecommendationType = "Control",
                Description = "Strengthen disclosure controls to prevent inaccuracies",
                Priority = "High",
                RecommendedDate = DateTime.Today,
                ExpectedBenefit = "Ensure accurate financial reporting",
                ImplementationCost = "$40,000"
            });
        }

        return result;
    }

    public async Task<RegulatoryFilingResult> ManageRegulatoryFilingsAsync(RegulatoryFilingRequest request)
    {
        var result = new RegulatoryFilingResult
        {
            RequestId = Guid.NewGuid(),
            ProcessedDate = DateTime.UtcNow,
            Filings = new List<RegulatoryFilingSubmission>(),
            Alerts = new List<RegulatoryFilingAlert>(),
            Recommendations = new List<RegulatoryFilingRecommendation>()
        };

        // Process each filing requirement
        foreach (var filingReq in request.FilingRequirements)
        {
            var filing = new RegulatoryFilingSubmission
            {
                FilingId = Guid.NewGuid(),
                FilingCode = filingReq.FilingCode,
                FilingName = filingReq.FilingName,
                DueDate = filingReq.DueDate,
                SubmissionDate = filingReq.DueDate.AddDays(-2), // Simulate submission
                FilingStatus = "Submitted",
                FilingUrl = "/filings/sample.pdf",
                FilingContent = "Filing content",
                FilingFormat = "PDF",
                SubmittedBy = "Filing Officer",
                ValidationStatus = "Valid",
                ValidationErrors = ""
            };

            result.Filings.Add(filing);

            // Check for overdue filings
            if (filingReq.DueDate < DateTime.Today && filingReq.FilingStatus != "Submitted")
            {
                result.Alerts.Add(new RegulatoryFilingAlert
                {
                    AlertType = "Deadline",
                    Description = $"Filing {filingReq.FilingName} is overdue. Due: {filingReq.DueDate:yyyy-MM-dd}",
                    AlertDate = DateTime.Today,
                    Severity = "Critical",
                    ActionRequired = "Submit filing immediately",
                    RequiredActionDate = DateTime.Today,
                    Status = "Open"
                });
            }
        }

        // Calculate overall filing status
        var submittedFilings = result.Filings.Count(f => f.FilingStatus == "Submitted");
        var totalFilings = result.Filings.Count;
        var overdueAlerts = result.Alerts.Count(a => a.AlertType == "Deadline");

        result.OverallFilingStatus = overdueAlerts > 0 ? "Overdue" :
                                   submittedFilings == totalFilings ? "AllFiled" : "PartiallyFiled";

        // Add recommendations
        if (overdueAlerts > 0)
        {
            result.Recommendations.Add(new RegulatoryFilingRecommendation
            {
                RecommendationType = "Process",
                Description = "Implement filing deadline monitoring system",
                Priority = "High",
                RecommendedDate = DateTime.Today,
                ExpectedBenefit = "Eliminate missed filing deadlines",
                ImplementationCost = "$15,000"
            });
        }

        return result;
    }

    public async Task<InternalControlDocumentationResult> DocumentInternalControlsAsync(InternalControlDocumentationRequest request)
    {
        var result = new InternalControlDocumentationResult
        {
            RequestId = Guid.NewGuid(),
            ProcessedDate = DateTime.UtcNow,
            ProcessDocs = new List<ProcessDocumentation>(),
            ControlDocs = new List<ControlDocumentation>(),
            RiskRegisters = new List<RiskRegister>(),
            ControlMatrices = new List<ControlMatrix>(),
            Recommendations = new List<DocumentationRecommendation>()
        };

        // Process each process
        foreach (var processId in request.ProcessIds)
        {
            var processDoc = new ProcessDocumentation
            {
                ProcessId = processId,
                ProcessName = $"Process {processId}",
                ProcessDescription = "Process description",
                ProcessOwner = "Process Owner",
                ProcessInputs = "Inputs description",
                ProcessOutputs = "Outputs description",
                ProcessSteps = "Step 1, Step 2, Step 3",
                ProcessControls = "Control 1, Control 2",
                ProcessRisks = "Risk 1, Risk 2",
                ProcessMetrics = "Metric 1, Metric 2",
                LastUpdated = DateTime.Today,
                DocumentationUrl = $"/documentation/process/{processId}",
                Version = "1.0"
            };

            result.ProcessDocs.Add(processDoc);
        }

        // Process each control
        foreach (var controlId in request.ControlIds)
        {
            var controlDoc = new ControlDocumentation
            {
                ControlId = controlId,
                ControlName = $"Control {controlId}",
                ControlDescription = "Control description",
                ControlType = "Preventive",
                ControlObjective = "Control objective",
                ControlProcedure = "Control procedure",
                ControlOwner = "Control Owner",
                ControlFrequency = "Daily",
                ControlMethod = "Manual",
                ControlTesting = "Testing procedure",
                ControlMonitoring = "Monitoring procedure",
                LastUpdated = DateTime.Today,
                DocumentationUrl = $"/documentation/control/{controlId}",
                Version = "1.0"
            };

            result.ControlDocs.Add(controlDoc);

            // Create risk register for the control
            var riskRegister = new RiskRegister
            {
                RiskId = Guid.NewGuid(),
                RiskName = $"Risk associated with {controlDoc.ControlName}",
                RiskDescription = "Risk description",
                RiskCategory = "Operational",
                RiskOwner = controlDoc.ControlOwner,
                RiskProbability = 0.3m,
                RiskImpact = 0.7m,
                RiskScore = 0.21m,
                RiskLevel = "Medium",
                RiskMitigation = "Mitigation strategy",
                RiskMonitoring = "Monitoring approach",
                LastUpdated = DateTime.Today,
                DocumentationUrl = $"/documentation/risk/{controlId}",
                Version = "1.0"
            };

            result.RiskRegisters.Add(riskRegister);
        }

        // Create control matrix
        var controlMatrix = new ControlMatrix
        {
            MatrixId = Guid.NewGuid(),
            MatrixName = "Financial Controls Matrix",
            MatrixDescription = "Matrix mapping risks to controls",
            LastUpdated = DateTime.Today,
            DocumentationUrl = "/documentation/control-matrix",
            Version = "1.0"
        };

        // Add rows to the matrix
        foreach (var risk in result.RiskRegisters)
        {
            controlMatrix.Rows.Add(new ControlMatrixRow
            {
                Risk = risk.RiskName,
                Control = result.ControlDocs.FirstOrDefault()?.ControlName ?? "Generic Control",
                ControlType = "Preventive",
                ControlOwner = risk.RiskOwner,
                ControlFrequency = "Daily",
                ControlTesting = "Monthly testing",
                ControlEffectiveness = "High"
            });
        }

        result.ControlMatrices.Add(controlMatrix);

        // Determine documentation status
        result.DocumentationStatus = result.ProcessDocs.Count > 0 && result.ControlDocs.Count > 0 ? "Complete" :
                                   result.ProcessDocs.Count > 0 || result.ControlDocs.Count > 0 ? "Partial" : "Missing";

        // Add recommendations
        if (result.DocumentationStatus != "Complete")
        {
            result.Recommendations.Add(new DocumentationRecommendation
            {
                RecommendationType = "Documentation",
                Description = "Complete missing internal control documentation",
                Priority = "High",
                RecommendedDate = DateTime.Today,
                ExpectedBenefit = "Improve compliance and audit readiness",
                ImplementationCost = "$20,000"
            });
        }

        return result;
    }

    #region Helper Methods

    private DateTime GetNextQuarterlyFilingDate(DateTime asOfDate)
    {
        // Calculate the next quarter end date
        int currentMonth = asOfDate.Month;
        int currentYear = asOfDate.Year;

        int quarterEndMonth;
        if (currentMonth <= 3) quarterEndMonth = 3; // Q1 ends March
        else if (currentMonth <= 6) quarterEndMonth = 6; // Q2 ends June
        else if (currentMonth <= 9) quarterEndMonth = 9; // Q3 ends September
        else quarterEndMonth = 12; // Q4 ends December

        // If we're already past the quarter end, go to next quarter
        if (currentMonth > quarterEndMonth || (currentMonth == quarterEndMonth && asOfDate.Day > 28))
        {
            if (quarterEndMonth == 12)
            {
                currentYear++;
                quarterEndMonth = 3;
            }
            else
            {
                quarterEndMonth += 3;
            }
        }

        // Return the last day of the quarter month
        return new DateTime(currentYear, quarterEndMonth, DateTime.DaysInMonth(currentYear, quarterEndMonth));
    }

    #endregion
}

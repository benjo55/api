namespace api.Dtos.Cmdb;

public class ConfigurationItemListDto
{
    public int Id { get; set; }
    public string ExternalCiNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Label { get; set; }
    public string Model { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Status { get; set; }
    public string? ApplicationDomain { get; set; }
    public string? EntityPath { get; set; }
    public string? ResponsibleEmployer { get; set; }
    public bool IsPlaceholder { get; set; }
    public bool IsCurrent { get; set; }
    public bool Locked { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

public sealed class ConfigurationItemDetailDto : ConfigurationItemListDto
{
    public string? ApplicationCode { get; set; }
    public string? Version { get; set; }
    public string? DatabaseCode { get; set; }
    public string? PlatformType { get; set; }
    public string? PlatformName { get; set; }
    public string? BudgetCode { get; set; }
    public string? OwnerName { get; set; }
    public string? Rto { get; set; }
    public string? Rpo { get; set; }
    public int IncomingRelationshipCount { get; set; }
    public int OutgoingRelationshipCount { get; set; }
    public List<CiAttributeDto> Attributes { get; set; } = [];
    public List<CiSupportAssignmentDto> SupportAssignments { get; set; } = [];
    public ConfigurationItemApplicationProfileDto ApplicationProfile { get; set; } = new();
}

public class ConfigurationItemApplicationProfileWriteDto
{
    public string? ShortDescription { get; set; }
    public string? DetailedDescription { get; set; }
    public string? MainFunctionalProcesses { get; set; }
    public string? GeneralTechnicalFramework { get; set; }
    public string? OverallArchitecture { get; set; }
    public string? ApplicationCriticality { get; set; }
    public string? ApplicationNature { get; set; }
    public bool? InternetExposed { get; set; }
    public string? LegalOwnerEntity { get; set; }
    public string? OtherStakeholders { get; set; }
    public bool? SourceCodeAvailable { get; set; }
    public string? HostingMode { get; set; }
    public string? HostingProvider { get; set; }
    public string? CloudServiceModel { get; set; }
    public string? HostingNetworkZone { get; set; }
    public string? AuthenticationMode { get; set; }
    public string? IamSolution { get; set; }
    public string? StandalonePasswordRules { get; set; }
    public bool? MfaEnabled { get; set; }
    public int? InternalTechnicalAdminCount { get; set; }
    public int? ExternalTechnicalAdminCount { get; set; }
    public DateTime? LastAccessRecertificationDate { get; set; }
    public decimal? LastAccessRemediationPercentage { get; set; }
    public DateTime? PreviousAccessRecertificationDate { get; set; }
    public decimal? PreviousAccessRemediationPercentage { get; set; }
    public bool? CodeScanEnabled { get; set; }
    public DateTime? LastPentestDate { get; set; }
    public DateTime? PreviousPentestDate { get; set; }
    public DateTime? LastRedTeamDate { get; set; }
    public DateTime? LastBugBountyDate { get; set; }
    public int? OpenRecommendationsLow { get; set; }
    public int? OpenRecommendationsMedium { get; set; }
    public int? OpenRecommendationsHigh { get; set; }
    public int? OverdueRecommendationsLow { get; set; }
    public int? OverdueRecommendationsMedium { get; set; }
    public int? OverdueRecommendationsHigh { get; set; }
    public string? SecurityComments { get; set; }
    public bool? RestorationTestedWithinYear { get; set; }
    public string? LastRestorationTestResult { get; set; }
    public bool? FailoverTestPerformed { get; set; }
    public DateTime? LastFailoverTestDate { get; set; }
    public DateTime? PreviousFailoverTestDate { get; set; }
    public int? PendingTestActionsCount { get; set; }
    public bool? ProcessesPersonalData { get; set; }
    public bool? NonProductionPersonalData { get; set; }
    public bool? NonProductionBusinessData { get; set; }
    public string? PersonalDataPseudonymization { get; set; }
}

public sealed class ConfigurationItemApplicationProfileDto :
    ConfigurationItemApplicationProfileWriteDto
{
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

public sealed class ConfigurationItemEnrichmentWriteDto
{
    public ConfigurationItemApplicationProfileWriteDto ApplicationProfile { get; set; } = new();
}

public sealed class CiAttributeDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Value { get; set; }
}

public sealed class CiSupportAssignmentDto
{
    public string GroupName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string? ManagerName { get; set; }
}

public sealed class ExchangePatternDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Family { get; set; } = string.Empty;
    public string InteractionMode { get; set; } = string.Empty;
    public string TriggerMode { get; set; } = string.Empty;
    public int? DefaultTechnologyId { get; set; }
    public string? DefaultTechnologyName { get; set; }
    public string? Description { get; set; }
    public string? TypicalUses { get; set; }
    public bool IsActive { get; set; }
    public bool IsSystem { get; set; }
    public bool Locked { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

public sealed class ExchangePatternWriteDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Family { get; set; } = string.Empty;
    public string InteractionMode { get; set; } = "Asynchronous";
    public string TriggerMode { get; set; } = "Scheduled";
    public int? DefaultTechnologyId { get; set; }
    public string? Description { get; set; }
    public string? TypicalUses { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class IntegrationFlowDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SourceCiId { get; set; }
    public string SourceCiName { get; set; } = string.Empty;
    public string SourceCiNumber { get; set; } = string.Empty;
    public int TargetCiId { get; set; }
    public string TargetCiName { get; set; } = string.Empty;
    public string TargetCiNumber { get; set; } = string.Empty;
    public int? BrokerCiId { get; set; }
    public string? BrokerCiName { get; set; }
    public int ExchangePatternId { get; set; }
    public string ExchangePatternName { get; set; } = string.Empty;
    public string PatternFamily { get; set; } = string.Empty;
    public int? TechnologyId { get; set; }
    public string? TechnologyName { get; set; }
    public string? FlowGroupCode { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Criticality { get; set; }
    public string? TransportProtocol { get; set; }
    public string? ChannelName { get; set; }
    public string? EndpointReference { get; set; }
    public long? AverageMessagesPerDay { get; set; }
    public int? PeakMessagesPerMinute { get; set; }
    public decimal? AveragePayloadKb { get; set; }
    public int? ExpectedLatencyMs { get; set; }
    public string? DataClassification { get; set; }
    public bool ContainsPersonalData { get; set; }
    public bool? IsEncryptedInTransit { get; set; }
    public DateTime? ValidFromDate { get; set; }
    public DateTime? ValidToDate { get; set; }
    public bool Locked { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

public sealed class IntegrationFlowWriteDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SourceCiId { get; set; }
    public int TargetCiId { get; set; }
    public int? BrokerCiId { get; set; }
    public int ExchangePatternId { get; set; }
    public int? TechnologyId { get; set; }
    public string? FlowGroupCode { get; set; }
    public string Status { get; set; } = "Draft";
    public string? Criticality { get; set; }
    public string? TransportProtocol { get; set; }
    public string? ChannelName { get; set; }
    public string? EndpointReference { get; set; }
    public long? AverageMessagesPerDay { get; set; }
    public int? PeakMessagesPerMinute { get; set; }
    public decimal? AveragePayloadKb { get; set; }
    public int? ExpectedLatencyMs { get; set; }
    public string? DataClassification { get; set; }
    public bool ContainsPersonalData { get; set; }
    public bool? IsEncryptedInTransit { get; set; }
    public DateTime? ValidFromDate { get; set; }
    public DateTime? ValidToDate { get; set; }
}

public sealed class CartographyGraphDto
{
    public List<CartographyNodeDto> Nodes { get; set; } = [];
    public List<CartographyEdgeDto> Edges { get; set; } = [];
    public bool Truncated { get; set; }
    public int Depth { get; set; }
}

public sealed class CartographyEmployerEntityDto
{
    public string Name { get; set; } = string.Empty;
    public int ConfigurationItemCount { get; set; }
    public decimal FlowCount { get; set; }
    public decimal CmdbRelationshipCount { get; set; }
    public List<CartographyEmployerEntityTypeCountDto> TypeCounts { get; set; } = [];
}

public sealed class CartographyEmployerEntityTypeCountDto
{
    public string Type { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class CartographyNodeDto
{
    public int Id { get; set; }
    public string ExternalCiNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Label { get; set; }
    public string Model { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Status { get; set; }
    public string? ApplicationDomain { get; set; }
    public string? EntityPath { get; set; }
    public string? ResponsibleEmployer { get; set; }
    public string? EmployerEntity { get; set; }
    public string? OwnerName { get; set; }
    public string? PlatformType { get; set; }
    public string? PlatformName { get; set; }
    public bool IsPlaceholder { get; set; }
    public bool IsRoot { get; set; }
}

public sealed class CartographyEdgeDto
{
    public string Id { get; set; } = string.Empty;
    public int Source { get; set; }
    public int Target { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Family { get; set; }
    public string? InteractionMode { get; set; }
    public bool IsBlocking { get; set; }
}

public sealed class CartographyLayoutDto
{
    public string ScopeType { get; set; } = string.Empty;
    public string ScopeKey { get; set; } = string.Empty;
    public List<CartographyNodePositionDto> Nodes { get; set; } = [];
}

public sealed class CartographyNodePositionDto
{
    public int ConfigurationItemId { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
}

public sealed class CartographyDomainDocumentDto
{
    public int Id { get; set; }
    public string EmployerEntity { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public List<CartographyDomainDocumentSectionDto> Sections { get; set; } = [];
}

public sealed class CartographyDomainDocumentSectionDto
{
    public int Id { get; set; }
    public int CartographyDomainDocumentId { get; set; }
    public string SectionKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int HeadingLevel { get; set; }
    public int SortOrder { get; set; }
    public string? ContentHtml { get; set; }
    public string? PlainText { get; set; }
    public string? EditorJson { get; set; }
    public bool IsSystem { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

public sealed class UpdateCartographyDomainDocumentSectionDto
{
    public string? Title { get; set; }
    public int? HeadingLevel { get; set; }
    public string? ContentHtml { get; set; }
    public string? PlainText { get; set; }
    public string? EditorJson { get; set; }
}

public sealed class CreateCartographyDomainDocumentSectionDto
{
    public string Title { get; set; } = string.Empty;
    public int HeadingLevel { get; set; } = 2;
    public string? ContentHtml { get; set; }
    public string? PlainText { get; set; }
    public string? EditorJson { get; set; }
    public int? AfterSectionId { get; set; }
    public int? AnchorSectionId { get; set; }
    public string InsertPosition { get; set; } = "After";
}

public sealed class CmdbImportResultDto
{
    public long ImportRunId { get; set; }
    public int ConfigurationItemCount { get; set; }
    public int RelationshipCount { get; set; }
    public int AttributeCount { get; set; }
    public int SupportAssignmentCount { get; set; }
    public int RejectedCount { get; set; }
}

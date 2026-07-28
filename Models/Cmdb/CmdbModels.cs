namespace api.Models.Cmdb;

public class CmdbImportRun
{
    public long Id { get; set; }
    public string SourceSystem { get; set; } = "EasyVista";
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = "Running";
    public int InsertedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int RelationshipCount { get; set; }
    public int AttributeCount { get; set; }
    public int SupportAssignmentCount { get; set; }
    public int RejectedCount { get; set; }
    public string? ErrorSummary { get; set; }
}

public class ConfigurationItem
{
    public int Id { get; set; }
    public string ExternalCiNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Label { get; set; }
    public string Model { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Status { get; set; }
    public string? ApplicationCode { get; set; }
    public string? Version { get; set; }
    public string? DatabaseCode { get; set; }
    public string? EntityPath { get; set; }
    public string? ResponsibleEmployer { get; set; }
    public string? ApplicationDomain { get; set; }
    public string? PlatformType { get; set; }
    public string? PlatformName { get; set; }
    public string? BudgetCode { get; set; }
    public string? OwnerName { get; set; }
    public string? Rto { get; set; }
    public string? Rpo { get; set; }
    public bool IsPlaceholder { get; set; }
    public bool IsCurrent { get; set; } = true;
    public DateTime? SourceUpdatedAt { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }
    public bool Locked { get; set; }

    public ICollection<CiAttributeValue> AttributeValues { get; set; } = [];
    public ICollection<CmdbRelationship> OutgoingRelationships { get; set; } = [];
    public ICollection<CmdbRelationship> IncomingRelationships { get; set; } = [];
    public ICollection<CiSupportAssignment> SupportAssignments { get; set; } = [];
    public ICollection<CartographyNodeLayout> CartographyLayouts { get; set; } = [];
    public ConfigurationItemApplicationProfile? ApplicationProfile { get; set; }
}

public class CartographyNodeLayout
{
    public long Id { get; set; }
    public string ScopeType { get; set; } = string.Empty;
    public string ScopeKey { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public int ConfigurationItemId { get; set; }
    public double PositionX { get; set; }
    public double PositionY { get; set; }
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

    public ConfigurationItem ConfigurationItem { get; set; } = null!;
}

public class CartographyDomainDocument
{
    public int Id { get; set; }
    public string EmployerEntity { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }

    public ICollection<CartographyDomainDocumentSection> Sections { get; set; } = [];
}

public class CartographyDomainDocumentSection
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
    public bool IsSystem { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }

    public CartographyDomainDocument CartographyDomainDocument { get; set; } = null!;
}

public class ConfigurationItemApplicationProfile
{
    public int ConfigurationItemId { get; set; }
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
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }

    public ConfigurationItem ConfigurationItem { get; set; } = null!;
}

public class CiAttributeDefinition
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DataType { get; set; } = "String";
    public string? Unit { get; set; }
    public bool IsFacet { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<CiAttributeValue> Values { get; set; } = [];
}

public class CiAttributeValue
{
    public long Id { get; set; }
    public int ConfigurationItemId { get; set; }
    public int AttributeDefinitionId { get; set; }
    public string? RawValue { get; set; }
    public string? StringValue { get; set; }
    public decimal? NumberValue { get; set; }
    public bool? BooleanValue { get; set; }
    public DateTime? DateTimeValue { get; set; }
    public ConfigurationItem ConfigurationItem { get; set; } = null!;
    public CiAttributeDefinition AttributeDefinition { get; set; } = null!;
}

public class CmdbRelationshipType
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Family { get; set; }
    public bool IsDirectional { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public ICollection<CmdbRelationship> Relationships { get; set; } = [];
}

public class CmdbRelationship
{
    public long Id { get; set; }
    public int SourceCiId { get; set; }
    public int TargetCiId { get; set; }
    public int RelationshipTypeId { get; set; }
    public bool IsBlocking { get; set; }
    public bool IsCurrent { get; set; } = true;
    public string SourceSystem { get; set; } = "EasyVista";
    public ConfigurationItem SourceCi { get; set; } = null!;
    public ConfigurationItem TargetCi { get; set; } = null!;
    public CmdbRelationshipType RelationshipType { get; set; } = null!;
}

public class CiSupportAssignment
{
    public long Id { get; set; }
    public int ConfigurationItemId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string? ManagerName { get; set; }
    public string? ManagerEntity { get; set; }
    public string? ManagerTeam { get; set; }
    public ConfigurationItem ConfigurationItem { get; set; } = null!;
}

public class IntegrationTechnology
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Family { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<ExchangePattern> ExchangePatterns { get; set; } = [];
    public ICollection<IntegrationFlow> IntegrationFlows { get; set; } = [];
}

public class ExchangePattern
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Family { get; set; } = string.Empty;
    public string InteractionMode { get; set; } = "Asynchronous";
    public string TriggerMode { get; set; } = "Scheduled";
    public int? DefaultTechnologyId { get; set; }
    public string? Description { get; set; }
    public string? TypicalUses { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsSystem { get; set; }
    public bool Locked { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }
    public IntegrationTechnology? DefaultTechnology { get; set; }
    public ICollection<IntegrationFlow> IntegrationFlows { get; set; } = [];
}

public class IntegrationFlow
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SourceCiId { get; set; }
    public int TargetCiId { get; set; }
    public int? BrokerCiId { get; set; }
    public int ExchangePatternId { get; set; }
    public int? TechnologyId { get; set; }
    public long? CmdbRelationshipId { get; set; }
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
    public bool Locked { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }

    public ConfigurationItem SourceCi { get; set; } = null!;
    public ConfigurationItem TargetCi { get; set; } = null!;
    public ConfigurationItem? BrokerCi { get; set; }
    public ExchangePattern ExchangePattern { get; set; } = null!;
    public IntegrationTechnology? Technology { get; set; }
    public CmdbRelationship? CmdbRelationship { get; set; }
    public ICollection<FlowRouteStep> RouteSteps { get; set; } = [];
}

public class FlowRouteStep
{
    public int Id { get; set; }
    public int IntegrationFlowId { get; set; }
    public int StepOrder { get; set; }
    public string StepKind { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int? ConfigurationItemId { get; set; }
    public int? TechnologyId { get; set; }
    public string? ConfigurationJson { get; set; }
    public IntegrationFlow IntegrationFlow { get; set; } = null!;
    public ConfigurationItem? ConfigurationItem { get; set; }
    public IntegrationTechnology? Technology { get; set; }
}

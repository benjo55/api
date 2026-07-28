using api.Dtos.LegalDocuments;
using api.Models;
using api.Models.Enum;

namespace api.Interfaces
{
    public interface IDocumentStructureService
    {
        Task<IReadOnlyList<LegalDocumentDefinitionListDto>> GetDefinitionsAsync(bool? isLibrary, CancellationToken cancellationToken = default);
        Task<LegalDocumentDefinitionDto?> GetDefinitionAsync(int definitionId, CancellationToken cancellationToken = default);
        Task<LegalDocumentDefinitionDto> CreateDefinitionAsync(CreateLegalDocumentDefinitionDto dto, string? userName, CancellationToken cancellationToken = default);
        Task<LegalDocumentRevisionDto?> GetRevisionAsync(int revisionId, CancellationToken cancellationToken = default);
        Task<LegalDocumentNodeDto> AddNodeAsync(int revisionId, CreateLegalDocumentNodeDto dto, string? userName, CancellationToken cancellationToken = default);
        Task<LegalDocumentNodeDto> UpdateNodeAsync(int nodeId, UpdateLegalDocumentNodeDto dto, string? userName, CancellationToken cancellationToken = default);
        Task MoveNodeAsync(int nodeId, MoveLegalDocumentNodeDto dto, string? userName, CancellationToken cancellationToken = default);
        Task<LegalDocumentNodeDto> DuplicateSubtreeAsync(int nodeId, string? userName, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ReusableDocumentNodeDto>> GetReusableNodesAsync(int excludeRevisionId, DocumentNodeType? type, string? search, CancellationToken cancellationToken = default);
        Task<LegalDocumentNodeDto> ImportSubtreeAsync(int revisionId, ImportDocumentNodeDto dto, string? userName, CancellationToken cancellationToken = default);
        Task DeleteNodeAsync(int nodeId, string rowVersion, string? userName, CancellationToken cancellationToken = default);
    }

    public interface ILegalDocumentImportService
    {
        Task<LegalDocumentImportResult> ImportAsync(
            string filePath,
            string? userName,
            CancellationToken cancellationToken = default);
    }

    public interface IDocumentNumberingService
    {
        IReadOnlyDictionary<int, string> GenerateNumbers(IEnumerable<LegalDocumentNode> nodes);
    }

    public interface IDocumentVersioningService
    {
        Task<LegalDocumentRevisionDto> CreateVersionAsync(int definitionId, CreateDocumentVersionDto dto, string? userName, CancellationToken cancellationToken = default);
    }

    public interface IDocumentWorkflowService
    {
        Task<LegalDocumentRevisionDto> SubmitForReviewAsync(int revisionId, WorkflowTransitionDto dto, string? userName, CancellationToken cancellationToken = default);
        Task<DocumentValidationResultDto> ValidateAsync(int revisionId, WorkflowTransitionDto dto, string? userName, CancellationToken cancellationToken = default);
        Task<LegalDocumentRevisionDto> PublishAsync(int revisionId, WorkflowTransitionDto dto, string? userName, CancellationToken cancellationToken = default);
    }

    public interface IDocumentValidationService
    {
        Task<DocumentValidationResultDto> ValidateRevisionAsync(int revisionId, bool includePdfGeneration, CancellationToken cancellationToken = default);
    }

    public interface IDocumentRenderService
    {
        Task<DocumentRenderModel> BuildRenderModelAsync(int revisionId, CancellationToken cancellationToken = default);
        string RenderCanonicalHtml(DocumentRenderModel model);
        Task<DocumentPreviewDto> GeneratePreviewAsync(int revisionId, string revisionStamp, string? userName, CancellationToken cancellationToken = default);
    }

    public interface IPdfGenerationService
    {
        Task<byte[]> GeneratePdfAsync(string html, string pageFormat, CancellationToken cancellationToken = default);
    }

    public interface IDocumentVariableResolver
    {
        IReadOnlySet<string> GetKnownVariables();
        IReadOnlyList<DocumentVariableDefinitionDto> GetVariableDefinitions();
    }

    public interface IDocumentConditionEvaluator
    {
        bool IsValidConditionJson(string? conditionJson);
    }

    public interface IClauseCatalogService
    {
        Task<IReadOnlyList<ClauseDefinition>> GetClausesAsync(CancellationToken cancellationToken = default);
    }

    public interface IDocumentComparisonService
    {
        Task<RevisionComparisonDto> CompareAsync(int leftRevisionId, int rightRevisionId, CancellationToken cancellationToken = default);
    }

    public interface IDocumentAuditService
    {
        Task AddAsync(DocumentAuditAction action, int? definitionId, int? revisionId, int? nodeId, object? details, string? userName, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<DocumentAuditEventDto>> GetHistoryAsync(int revisionId, CancellationToken cancellationToken = default);
    }

    public interface IDocumentBinaryStorage
    {
        Task<(string StorageKey, string Hash, long Size)> SaveAsync(byte[] content, string extension, CancellationToken cancellationToken = default);
        Task<byte[]> ReadAsync(string storageKey, CancellationToken cancellationToken = default);
    }

    public interface IProductDocumentAssignmentService
    {
        Task<IReadOnlyList<ProductDocumentAssignmentDto>> GetProductAssignmentsAsync(int productId, CancellationToken cancellationToken = default);
        Task<ProductDocumentAssignmentDto> AssignAsync(CreateProductDocumentAssignmentDto dto, string? userName, CancellationToken cancellationToken = default);
        Task DeleteAsync(int assignmentId, string rowVersion, string? userName, CancellationToken cancellationToken = default);
    }
}

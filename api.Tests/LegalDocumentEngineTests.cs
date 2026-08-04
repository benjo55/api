using api.Data;
using api.Dtos.LegalDocuments;
using api.Interfaces;
using api.Models;
using api.Models.Enum;
using api.Services.LegalDocuments;
using Microsoft.EntityFrameworkCore;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Annotations;
using Xunit;

namespace api.Tests
{
    public class LegalDocumentEngineTests
    {
        [Fact]
        public void Numbering_generates_hierarchical_numbers()
        {
            var service = new DocumentNumberingService();
            var nodes = new[]
            {
                new LegalDocumentNode { Id = 1, Type = DocumentNodeType.Document, SortOrder = 1000 },
                new LegalDocumentNode { Id = 2, ParentNodeId = 1, Type = DocumentNodeType.Chapter, SortOrder = 1000 },
                new LegalDocumentNode { Id = 3, ParentNodeId = 2, Type = DocumentNodeType.Article, SortOrder = 1000 },
                new LegalDocumentNode { Id = 4, ParentNodeId = 2, Type = DocumentNodeType.Article, SortOrder = 2000 }
            };

            var result = service.GenerateNumbers(nodes);

            Assert.Equal("1", result[2]);
            Assert.Equal("1.1", result[3]);
            Assert.Equal("1.2", result[4]);
        }

        [Fact]
        public void Numbering_preserves_manual_legal_references_and_unnumbered_sections()
        {
            var service = new DocumentNumberingService();
            var nodes = new[]
            {
                new LegalDocumentNode { Id = 1, Type = DocumentNodeType.Document, SortOrder = 1000 },
                new LegalDocumentNode
                {
                    Id = 2,
                    ParentNodeId = 1,
                    Type = DocumentNodeType.Chapter,
                    NumberingStyle = "none",
                    SortOrder = 1000
                },
                new LegalDocumentNode
                {
                    Id = 3,
                    ParentNodeId = 1,
                    Type = DocumentNodeType.Chapter,
                    BusinessCode = "1",
                    NumberingStyle = "manual",
                    SortOrder = 2000
                },
                new LegalDocumentNode
                {
                    Id = 4,
                    ParentNodeId = 3,
                    Type = DocumentNodeType.Article,
                    BusinessCode = "1.1",
                    NumberingStyle = "manual",
                    SortOrder = 1000
                },
                new LegalDocumentNode
                {
                    Id = 5,
                    ParentNodeId = 4,
                    Type = DocumentNodeType.Article,
                    BusinessCode = "1.1.1",
                    NumberingStyle = "manual",
                    SortOrder = 1000
                }
            };

            var result = service.GenerateNumbers(nodes);

            Assert.False(result.ContainsKey(2));
            Assert.Equal("1", result[3]);
            Assert.Equal("1.1", result[4]);
            Assert.Equal("1.1.1", result[5]);
        }

        [Fact]
        public async Task Rendering_includes_unnumbered_toc_entries_without_paragraph_headings()
        {
            await using var db = CreateContext();
            var service = new DocumentRenderService(
                db,
                new DocumentNumberingService(),
                new FakePdfGenerationService(),
                new FakeBinaryStorage(),
                new FakeAuditService());
            var model = new DocumentRenderModel(
                1,
                "DOC",
                "Document",
                "1.0",
                "hash",
                new DocumentLayoutModel("A4", 10, 10, 10, 10, string.Empty, null, null, 1),
                [
                    new DocumentRenderNode(
                        1,
                        "chapter",
                        DocumentNodeType.Chapter,
                        null,
                        "Lexique",
                        null,
                        true,
                        false,
                        false,
                        [
                            new DocumentRenderNode(
                                2,
                                "paragraph",
                                DocumentNodeType.Paragraph,
                                null,
                                "Paragraphe",
                                "<p>Contenu</p>",
                                false,
                                false,
                                false,
                                [])
                        ])
                ]);

            var html = service.RenderCanonicalHtml(model);

            Assert.Contains("Table des matières", html);
            Assert.Contains("href=\"#document-node-1\"", html);
            Assert.Contains("data-toc-target=\"1\"", html);
            Assert.Contains("id=\"document-node-1\"", html);
            Assert.Contains("[[PDF_TARGET_1]]", html);
            Assert.Contains(">Lexique</span>", html);
            Assert.Contains("<p>Contenu</p>", html);
            Assert.DoesNotContain(">Paragraphe</h", html);
        }

        [Fact]
        public async Task Pdf_generation_resolves_toc_pages_and_numbers_every_page()
        {
            const string html = """
                <!doctype html>
                <html>
                  <head>
                    <meta charset="utf-8">
                    <style>
                      @page { size: A4; margin: 20mm; }
                      body { font-family: Arial, sans-serif; }
                      .cover, .toc { page-break-after: always; }
                      .pdf-page-marker { position: absolute; color: #fff; font-size: 1px; }
                    </style>
                  </head>
                  <body>
                    <section class="cover"><h1>Couverture</h1></section>
                    <nav class="toc">
                      <a href="#document-node-7">
                        Section cible <span data-toc-target="7">000</span>
                      </a>
                    </nav>
                    <section id="document-node-7">
                      <span class="pdf-page-marker">[[PDF_TARGET_7]]</span>
                      <h1>Section cible</h1>
                    </section>
                  </body>
                </html>
                """;

            var pdf = await new PdfGenerationService().GeneratePdfAsync(html, "A4");
            using var document = PdfDocument.Open(pdf);

            Assert.Equal(3, document.NumberOfPages);
            Assert.Contains("Section cible 3", document.GetPage(2).Text);
            Assert.Contains(
                document.GetPage(2).GetAnnotations(),
                annotation => annotation.Type == AnnotationType.Link);
            Assert.Contains("Page 1 / 3", document.GetPage(1).Text);
            Assert.Contains("Page 2 / 3", document.GetPage(2).Text);
            Assert.Contains("Page 3 / 3", document.GetPage(3).Text);
        }

        [Fact]
        public async Task AddNode_rejects_invalid_parent_child_relation()
        {
            await using var db = CreateContext();
            var revision = await SeedDraftRevisionAsync(db);
            var root = revision.Nodes.Single(x => x.Type == DocumentNodeType.Document);
            var service = new DocumentStructureService(db, new DocumentNumberingService(), new FakeAuditService());

            await Assert.ThrowsAsync<api.Exceptions.BusinessException>(() =>
                service.AddNodeAsync(
                    revision.Id,
                    new CreateLegalDocumentNodeDto(root.Id, DocumentNodeType.Paragraph, "Invalid", null, null, null),
                    "test"));
        }

        [Fact]
        public async Task AddNode_uses_french_default_title()
        {
            await using var db = CreateContext();
            var revision = await SeedDraftRevisionAsync(db);
            var root = revision.Nodes.Single(x => x.Type == DocumentNodeType.Document);
            var service = new DocumentStructureService(db, new DocumentNumberingService(), new FakeAuditService());

            var node = await service.AddNodeAsync(
                revision.Id,
                new CreateLegalDocumentNodeDto(root.Id, DocumentNodeType.Chapter, " ", null, null, null),
                "test");

            Assert.Equal("Chapitre", node.Title);
        }

        [Fact]
        public async Task Validation_reports_duplicate_codes_and_dangerous_html()
        {
            await using var db = CreateContext();
            var revision = await SeedDraftRevisionAsync(db);
            var root = revision.Nodes.Single(x => x.Type == DocumentNodeType.Document);
            db.LegalDocumentNodes.AddRange(
                new LegalDocumentNode
                {
                    LegalDocumentRevisionId = revision.Id,
                    ParentNodeId = root.Id,
                    Type = DocumentNodeType.Chapter,
                    StableKey = "chapter-a",
                    Title = "A",
                    BusinessCode = "ART-1",
                    ContentHtml = "<script>alert(1)</script>",
                    SortOrder = 1000
                },
                new LegalDocumentNode
                {
                    LegalDocumentRevisionId = revision.Id,
                    ParentNodeId = root.Id,
                    Type = DocumentNodeType.Chapter,
                    StableKey = "chapter-b",
                    Title = "B",
                    BusinessCode = "ART-1",
                    SortOrder = 2000
                });
            await db.SaveChangesAsync();

            var service = new DocumentValidationService(
                db,
                new DocumentNumberingService(),
                new DocumentVariableResolver(),
                new DocumentConditionEvaluator(),
                new FakeRenderService(),
                new FakePdfGenerationService());

            var result = await service.ValidateRevisionAsync(revision.Id, includePdfGeneration: false);

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, x => x.Code == "DANGEROUS_HTML");
            Assert.Contains(result.Issues, x => x.Code == "DUPLICATE_BUSINESS_CODE");
        }

        [Fact]
        public async Task CreateVersion_clones_tree_and_preserves_stable_keys()
        {
            await using var db = CreateContext();
            var revision = await SeedDraftRevisionAsync(db);
            revision.Status = DocumentRevisionStatus.Published;
            revision.LegalDocumentDefinition.CurrentDraftRevisionId = null;
            revision.LegalDocumentDefinition.CurrentPublishedRevisionId = revision.Id;
            await db.SaveChangesAsync();

            var service = new DocumentVersioningService(db, new DocumentNumberingService(), new FakeAuditService());
            var cloned = await service.CreateVersionAsync(
                revision.LegalDocumentDefinitionId,
                new CreateDocumentVersionDto(revision.Id, VersionBumpType.Minor, "Minor update"),
                "test");

            Assert.Equal(DocumentRevisionStatus.Draft, cloned.Status);
            Assert.Contains(cloned.Nodes, x => x.StableKey == revision.Nodes.Single().StableKey);
            Assert.NotEqual(revision.Id, cloned.Id);
        }

        [Fact]
        public async Task Reusable_catalog_and_import_copy_a_complete_subtree()
        {
            await using var db = CreateContext();
            var sourceRevision = await SeedDraftRevisionAsync(db, "SRC", "CG source", isLibrary: true);
            var sourceRoot = sourceRevision.Nodes.Single(x => x.Type == DocumentNodeType.Document);
            var sourceChapter = new LegalDocumentNode
            {
                LegalDocumentRevisionId = sourceRevision.Id,
                ParentNodeId = sourceRoot.Id,
                Type = DocumentNodeType.Chapter,
                StableKey = "source-chapter",
                Title = "Garanties",
                SortOrder = 1000
            };
            var sourceArticle = new LegalDocumentNode
            {
                LegalDocumentRevisionId = sourceRevision.Id,
                ParentNode = sourceChapter,
                Type = DocumentNodeType.Article,
                StableKey = "source-article",
                Title = "Étendue",
                SortOrder = 1000
            };
            var sourceParagraph = new LegalDocumentNode
            {
                LegalDocumentRevisionId = sourceRevision.Id,
                ParentNode = sourceArticle,
                Type = DocumentNodeType.Paragraph,
                StableKey = "source-paragraph",
                Title = "Paragraphe",
                PlainText = "Le contrat couvre les garanties définies.",
                ContentHtml = "<p>Le contrat couvre les garanties définies.</p>",
                SortOrder = 1000
            };
            db.AddRange(sourceChapter, sourceArticle, sourceParagraph);
            await db.SaveChangesAsync();

            var destinationRevision = await SeedDraftRevisionAsync(db, "DST", "CG destination");
            var destinationRoot = destinationRevision.Nodes.Single(x => x.Type == DocumentNodeType.Document);
            var service = new DocumentStructureService(db, new DocumentNumberingService(), new FakeAuditService());

            var catalog = await service.GetReusableNodesAsync(
                destinationRevision.Id,
                DocumentNodeType.Chapter,
                "Garanties");
            var reusableChapter = Assert.Single(catalog);
            Assert.Equal(2, reusableChapter.DescendantCount);
            Assert.Equal("SRC", reusableChapter.SourceDocumentCode);

            var imported = await service.ImportSubtreeAsync(
                destinationRevision.Id,
                new ImportDocumentNodeDto(sourceChapter.Id, destinationRoot.Id),
                "test");

            Assert.Equal("Garanties", imported.Title);
            Assert.NotEqual(sourceChapter.StableKey, imported.StableKey);
            var importedArticle = Assert.Single(imported.Children);
            var importedParagraph = Assert.Single(importedArticle.Children);
            Assert.Equal("Le contrat couvre les garanties définies.", importedParagraph.PlainText);
            Assert.NotEqual(sourceParagraph.StableKey, importedParagraph.StableKey);
        }

        [Fact]
        public async Task DeleteNode_removes_subtree_and_detaches_audit_events()
        {
            await using var db = CreateContext();
            var revision = await SeedDraftRevisionAsync(db);
            var root = revision.Nodes.Single(x => x.Type == DocumentNodeType.Document);
            var chapter = new LegalDocumentNode
            {
                LegalDocumentRevisionId = revision.Id,
                ParentNodeId = root.Id,
                Type = DocumentNodeType.Chapter,
                StableKey = "chapter",
                Title = "Chapter",
                SortOrder = 1000
            };
            var article = new LegalDocumentNode
            {
                LegalDocumentRevisionId = revision.Id,
                ParentNode = chapter,
                Type = DocumentNodeType.Article,
                StableKey = "article",
                Title = "Article",
                SortOrder = 1000
            };
            db.LegalDocumentNodes.AddRange(chapter, article);
            db.DocumentAuditEvents.Add(new DocumentAuditEvent
            {
                LegalDocumentDefinitionId = revision.LegalDocumentDefinitionId,
                LegalDocumentRevisionId = revision.Id,
                LegalDocumentNode = article,
                Action = DocumentAuditAction.Updated
            });
            await db.SaveChangesAsync();

            var service = new DocumentStructureService(db, new DocumentNumberingService(), new DocumentAuditService(db));
            await service.DeleteNodeAsync(chapter.Id, string.Empty, "test");

            Assert.DoesNotContain(await db.LegalDocumentNodes.ToListAsync(), x => x.Id == chapter.Id || x.Id == article.Id);
            var auditEvents = await db.DocumentAuditEvents.OrderBy(x => x.CreatedAt).ToListAsync();
            Assert.Equal(2, auditEvents.Count);
            Assert.All(auditEvents, x => Assert.Null(x.LegalDocumentNodeId));
            Assert.Contains(auditEvents, x =>
                x.Action == DocumentAuditAction.Deleted
                && x.DetailJson != null
                && x.DetailJson.Contains($"\"deletedNodeId\":{chapter.Id}", StringComparison.Ordinal));
        }

        [Fact]
        public async Task ProductAssignment_requires_published_revision()
        {
            await using var db = CreateContext();
            var revision = await SeedDraftRevisionAsync(db);
            var product = new Product { ProductCode = "P1", ProductName = "Product 1" };
            db.Products.Add(product);
            await db.SaveChangesAsync();

            var service = new ProductDocumentAssignmentService(db, new FakeAuditService());

            await Assert.ThrowsAsync<api.Exceptions.BusinessException>(() =>
                service.AssignAsync(
                    new CreateProductDocumentAssignmentDto(
                        product.Id,
                        revision.Id,
                        ProductDocumentRole.GeneralTerms,
                        new DateTime(2026, 1, 1),
                        null,
                        true),
                    "test"));
        }

        [Fact]
        public async Task ProductAssignment_rejects_overlapping_periods_for_same_role()
        {
            await using var db = CreateContext();
            var revision = await SeedDraftRevisionAsync(db);
            revision.Status = DocumentRevisionStatus.Published;
            var product = new Product { ProductCode = "P1", ProductName = "Product 1" };
            db.Products.Add(product);
            await db.SaveChangesAsync();

            var service = new ProductDocumentAssignmentService(db, new FakeAuditService());
            await service.AssignAsync(
                new CreateProductDocumentAssignmentDto(
                    product.Id,
                    revision.Id,
                    ProductDocumentRole.GeneralTerms,
                    new DateTime(2026, 1, 1),
                    new DateTime(2026, 12, 31),
                    true),
                "test");

            await Assert.ThrowsAsync<api.Exceptions.BusinessException>(() =>
                service.AssignAsync(
                    new CreateProductDocumentAssignmentDto(
                        product.Id,
                        revision.Id,
                        ProductDocumentRole.GeneralTerms,
                        new DateTime(2026, 6, 1),
                        null,
                        false),
                    "test"));
        }

        private static ApplicationDBContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDBContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDBContext(options);
        }

        private static async Task<LegalDocumentRevision> SeedDraftRevisionAsync(
            ApplicationDBContext db,
            string code = "GT",
            string name = "General terms",
            bool isLibrary = false)
        {
            var definition = new LegalDocumentDefinition
            {
                Code = code,
                Name = name,
                Type = LegalDocumentType.ProductGeneralTerms,
                IsLibrary = isLibrary
            };
            var revision = new LegalDocumentRevision
            {
                LegalDocumentDefinition = definition,
                MajorVersion = 1,
                MinorVersion = 0,
                Status = DocumentRevisionStatus.Draft
            };
            var root = new LegalDocumentNode
            {
                LegalDocumentRevision = revision,
                Type = DocumentNodeType.Document,
                StableKey = "root",
                Title = name,
                SortOrder = 1000
            };

            db.AddRange(definition, revision, root);
            await db.SaveChangesAsync();
            definition.CurrentDraftRevisionId = revision.Id;
            await db.SaveChangesAsync();
            return await db.LegalDocumentRevisions
                .Include(x => x.LegalDocumentDefinition)
                .Include(x => x.Nodes)
                .FirstAsync(x => x.Id == revision.Id);
        }
    }

    internal sealed class FakeAuditService : IDocumentAuditService
    {
        public Task AddAsync(DocumentAuditAction action, int? definitionId, int? revisionId, int? nodeId, object? details, string? userName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<DocumentAuditEventDto>> GetHistoryAsync(int revisionId, CancellationToken cancellationToken = default) =>
            Task.FromResult((IReadOnlyList<DocumentAuditEventDto>)Array.Empty<DocumentAuditEventDto>());
    }

    internal sealed class FakePdfGenerationService : IPdfGenerationService
    {
        public Task<byte[]> GeneratePdfAsync(string html, string pageFormat, CancellationToken cancellationToken = default) =>
            Task.FromResult(new byte[] { 1, 2, 3 });
    }

    internal sealed class FakeBinaryStorage : IDocumentBinaryStorage
    {
        public Task<(string StorageKey, string Hash, long Size)> SaveAsync(
            byte[] content,
            string extension,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(("document.pdf", "hash", (long)content.Length));

        public Task<byte[]> ReadAsync(
            string storageKey,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Array.Empty<byte>());
    }

    internal sealed class FakeRenderService : IDocumentRenderService
    {
        public Task<DocumentRenderModel> BuildRenderModelAsync(int revisionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new DocumentRenderModel(
                revisionId,
                "GT",
                "General terms",
                "1.0",
                "hash",
                new DocumentLayoutModel("A4", 10, 10, 10, 10, string.Empty, null, null, 1),
                Array.Empty<DocumentRenderNode>()));

        public string RenderCanonicalHtml(DocumentRenderModel model) => "<html><body></body></html>";

        public Task<DocumentPreviewDto> GeneratePreviewAsync(int revisionId, string revisionStamp, string? userName, CancellationToken cancellationToken = default) =>
            Task.FromResult(new DocumentPreviewDto(1, "preview.pdf", "application/pdf", "hash", revisionStamp, true));
    }
}

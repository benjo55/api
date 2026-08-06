using System.Text;
using api.Controllers;
using api.Data;
using api.Dtos.Cmdb;
using api.Models.Cmdb;
using api.Services.Cmdb;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace api.Tests;

public sealed class CmdbCartographyTests
{
    [Fact]
    public void CsvReader_HandlesUtf8QuotedSemicolonValues()
    {
        const string csv = "\uFEFF\"Nom\";\"N° de CI\";\"Libellé\"\r\n\"APP;ONE\";\"APPL00001\";\"Texte accentué\"\r\n";
        var bytes = Encoding.UTF8.GetBytes(csv);
        using var stream = new MemoryStream(bytes);
        var file = new FormFile(stream, 0, bytes.Length, "files", "ARAP Complet.csv");

        var rows = CmdbImportService.ReadCsv(file);

        Assert.Single(rows);
        Assert.Equal("APP;ONE", rows[0]["Nom"]);
        Assert.Equal("APPL00001", rows[0]["N° de CI"]);
        Assert.Equal("Texte accentué", rows[0]["Libellé"]);
    }

    [Fact]
    public async Task Graph_ReturnsCmdbAndFunctionalEdgesAroundRoot()
    {
        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase($"cmdb-{Guid.NewGuid()}")
            .Options;
        await using var db = new ApplicationDBContext(options);
        await db.Database.EnsureCreatedAsync();

        var application = new ConfigurationItem
        {
            ExternalCiNumber = "APPL00001", Name = "Application A", Model = "Application",
        };
        var server = new ConfigurationItem
        {
            ExternalCiNumber = "SERV00001", Name = "Serveur A", Model = "VMWARE",
        };
        var target = new ConfigurationItem
        {
            ExternalCiNumber = "APPL00002", Name = "Application B", Model = "Application",
        };
        var relationshipType = new CmdbRelationshipType { Code = "TECHNIQUE", Name = "Technique" };
        db.AddRange(application, server, target, relationshipType);
        await db.SaveChangesAsync();
        db.CmdbRelationships.Add(new CmdbRelationship
        {
            SourceCiId = server.Id,
            TargetCiId = application.Id,
            RelationshipTypeId = relationshipType.Id,
            IsBlocking = true,
        });
        db.IntegrationFlows.Add(new IntegrationFlow
        {
            Code = "FLOW_001",
            Name = "Flux de test",
            SourceCiId = application.Id,
            TargetCiId = target.Id,
            ExchangePatternId = 1,
            Status = "Active",
        });
        await db.SaveChangesAsync();

        var controller = new CartographyController(db, CreateMemoryCache());
        var result = await controller.GetGraph(application.Id, 1, "Both", true, true, 300);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var graph = Assert.IsType<CartographyGraphDto>(ok.Value);

        Assert.Equal(3, graph.Nodes.Count);
        Assert.Contains(graph.Edges, edge => edge.Kind == "cmdb" && edge.IsBlocking);
        Assert.Contains(graph.Edges, edge => edge.Kind == "flow" && edge.Label == "Flux de test");
    }

    [Fact]
    public async Task EmployerEntityGraph_ReturnsWholeDomainAndInternalEdges()
    {
        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase($"cmdb-employer-{Guid.NewGuid()}")
            .Options;
        await using var db = new ApplicationDBContext(options);
        await db.Database.EnsureCreatedAsync();

        var application = new ConfigurationItem
        {
            ExternalCiNumber = "APPL00010",
            Name = "Application Epargne",
            Model = "Application",
            EntityPath = "COMMUNAUTAIRE",
            ResponsibleEmployer = "APICIL EPARGNE",
        };
        var database = new ConfigurationItem
        {
            ExternalCiNumber = "DB000010",
            Name = "Base Epargne",
            Model = "Base de données",
            EntityPath = "APICIL EPARGNE/DSI/DOMAINE",
        };
        var otherEntity = new ConfigurationItem
        {
            ExternalCiNumber = "APPL00020",
            Name = "Application Banque",
            Model = "Application",
            EntityPath = "GRESHAM BANQUE/DSI",
            ResponsibleEmployer = "APICIL EPARGNE",
        };
        var relationshipType = new CmdbRelationshipType
        {
            Code = "HOSTS",
            Name = "Héberge",
        };
        db.AddRange(application, database, otherEntity, relationshipType);
        await db.SaveChangesAsync();
        db.CmdbRelationships.Add(new CmdbRelationship
        {
            SourceCiId = database.Id,
            TargetCiId = application.Id,
            RelationshipTypeId = relationshipType.Id,
        });
        await db.SaveChangesAsync();

        var controller = new CartographyController(db, CreateMemoryCache());
        var entitiesResult = await controller.GetEmployerEntities();
        var entitiesOk = Assert.IsType<OkObjectResult>(entitiesResult.Result);
        var entities = Assert.IsType<List<CartographyEmployerEntityDto>>(entitiesOk.Value);
        Assert.Contains(entities, x =>
            x.Name == "APICIL EPARGNE" && x.ConfigurationItemCount == 2);
        Assert.DoesNotContain(entities, x => x.Name == "COMMUNAUTAIRE");
        Assert.Contains(entities, x =>
            x.Name == "GRESHAM BANQUE" && x.ConfigurationItemCount == 1);

        var graphResult = await controller.GetEntityGraph(
            ["APICIL EPARGNE"],
            depth: 0,
            includeCmdb: true,
            includeFlows: false);
        var graphOk = Assert.IsType<OkObjectResult>(graphResult.Result);
        var graph = Assert.IsType<CartographyGraphDto>(graphOk.Value);

        Assert.Equal(2, graph.Nodes.Count);
        Assert.All(graph.Nodes, node => Assert.True(node.IsRoot));
        Assert.Contains(graph.Nodes, node =>
            node.Id == application.Id &&
            node.ResponsibleEmployer == "APICIL EPARGNE" &&
            node.EmployerEntity == "APICIL EPARGNE");
        Assert.Single(graph.Edges);
        Assert.DoesNotContain(graph.Nodes, node => node.Id == otherEntity.Id);

        var bankGraphResult = await controller.GetEntityGraph(
            ["GRESHAM BANQUE"],
            depth: 0,
            includeCmdb: false,
            includeFlows: false);
        var bankGraphOk = Assert.IsType<OkObjectResult>(bankGraphResult.Result);
        var bankGraph = Assert.IsType<CartographyGraphDto>(bankGraphOk.Value);
        Assert.Single(bankGraph.Nodes);
        Assert.Contains(bankGraph.Nodes, node =>
            node.Id == otherEntity.Id &&
            node.EmployerEntity == "GRESHAM BANQUE");

        var combinedGraphResult = await controller.GetEntityGraph(
            ["APICIL EPARGNE", "GRESHAM BANQUE"],
            depth: 0,
            includeCmdb: false,
            includeFlows: false);
        var combinedGraphOk =
            Assert.IsType<OkObjectResult>(combinedGraphResult.Result);
        var combinedGraph =
            Assert.IsType<CartographyGraphDto>(combinedGraphOk.Value);
        Assert.Equal(3, combinedGraph.Nodes.Count);
        Assert.All(combinedGraph.Nodes, node => Assert.True(node.IsRoot));
    }

    [Theory]
    [InlineData(
        "GRESHAM BANQUE/GB DIRECTION/DIRECTION GRESHAM BANQUE",
        "APICIL EPARGNE",
        "GRESHAM BANQUE")]
    [InlineData("COMMUNAUTAIRE", "APICIL EPARGNE", "APICIL EPARGNE")]
    [InlineData(null, "APICIL TRANSVERSE", "APICIL TRANSVERSE")]
    [InlineData("TERRITORIA/DIRECTION", null, "TERRITORIA")]
    public void EmployerResolver_PrioritizesMeaningfulEntityThenResponsibleEmployer(
        string? entityPath,
        string? responsibleEmployer,
        string expected)
    {
        Assert.Equal(
            expected,
            CmdbEmployerResolver.Resolve(entityPath, responsibleEmployer));
    }

    [Fact]
    public void ResponsibleEmployer_PrefersDedicatedEasyVistaColumn()
    {
        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Entité (complète)"] = "COMMUNAUTAIRE",
            ["Employeur_CI"] =
                "APICIL EPARGNE,GRESHAM BANQUE,INTENCIAL PATRIMOINE",
            ["Employeur Responsable CMDB"] = "APICIL EPARGNE",
        };

        Assert.Equal(
            "APICIL EPARGNE",
            CmdbImportService.ResolveResponsibleEmployer(row));
    }

    [Fact]
    public async Task IntegrationFlowCreate_GeneratesCodeFromEndpointsAndAddsSequence()
    {
        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase($"cmdb-flow-code-{Guid.NewGuid()}")
            .Options;
        await using var db = new ApplicationDBContext(options);
        await db.Database.EnsureCreatedAsync();

        var source = new ConfigurationItem
        {
            ExternalCiNumber = "CMDB00791",
            Name = "SUNSHINE - GESTION",
            Model = "Application",
        };
        var target = new ConfigurationItem
        {
            ExternalCiNumber = "CMDB20326",
            Name = "SOLIAM",
            Model = "Application",
        };
        db.AddRange(source, target);
        await db.SaveChangesAsync();

        var controller = new IntegrationFlowsController(db);
        var firstResult = await controller.Create(
            new IntegrationFlowWriteDto
            {
                Code = "CODE_SAISI_IGNORE",
                Name = "Premier flux",
                SourceCiId = source.Id,
                TargetCiId = target.Id,
                ExchangePatternId = 1,
            },
            CancellationToken.None);
        var firstCreated = Assert.IsType<CreatedAtActionResult>(firstResult.Result);
        var first = Assert.IsType<IntegrationFlowDto>(firstCreated.Value);

        var secondResult = await controller.Create(
            new IntegrationFlowWriteDto
            {
                Name = "Second flux",
                SourceCiId = source.Id,
                TargetCiId = target.Id,
                ExchangePatternId = 1,
            },
            CancellationToken.None);
        var secondCreated = Assert.IsType<CreatedAtActionResult>(secondResult.Result);
        var second = Assert.IsType<IntegrationFlowDto>(secondCreated.Value);

        Assert.Equal("FLUX_CMDB00791_CMDB20326", first.Code);
        Assert.Equal("FLUX_CMDB00791_CMDB20326_02", second.Code);
    }

    [Fact]
    public async Task ConfigurationItemTypeahead_PrioritizesBusinessApplications()
    {
        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase($"cmdb-typeahead-{Guid.NewGuid()}")
            .Options;
        await using var db = new ApplicationDBContext(options);
        await db.Database.EnsureCreatedAsync();

        db.ConfigurationItems.AddRange(
            new ConfigurationItem
            {
                ExternalCiNumber = "CMDB00001",
                Name = "Apache TOMCAT",
                Label = "Serveur SUNSHINE",
                Model = "Application",
                Category = "Application Technique",
            },
            new ConfigurationItem
            {
                ExternalCiNumber = "CMDB00002",
                Name = "SUNSHINE - GESTION",
                Model = "Application",
                Category = "Application Métier",
            },
            new ConfigurationItem
            {
                ExternalCiNumber = "CMDB00003",
                Name = "SUNSHINE - DOCUMENTATION",
                Model = "Application",
                Category = "Application ArchiMate",
            },
            new ConfigurationItem
            {
                ExternalCiNumber = "CMDB00004",
                Name = "Serveur SUNSHINE",
                Model = "VMWARE",
                Category = "Serveur Virtuel",
            });
        await db.SaveChangesAsync();

        var controller = new ConfigurationItemsController(
            db,
            new StubCmdbImportService());
        var result = await controller.Typeahead("SUNSHINE", 20);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var items = Assert.IsType<List<ConfigurationItemListDto>>(ok.Value);

        Assert.Equal("SUNSHINE - GESTION", items[0].Name);
        Assert.Equal("Application Métier", items[0].Category);
        Assert.Equal("SUNSHINE - DOCUMENTATION", items[1].Name);
        Assert.Equal("Apache TOMCAT", items[2].Name);
        Assert.Equal("Serveur SUNSHINE", items[3].Name);
    }

    [Fact]
    public async Task ConfigurationItemEnrichment_IsStoredSeparatelyFromImportedFields()
    {
        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase($"cmdb-profile-{Guid.NewGuid()}")
            .Options;
        await using var db = new ApplicationDBContext(options);
        await db.Database.EnsureCreatedAsync();

        var application = new ConfigurationItem
        {
            ExternalCiNumber = "CMDB00001",
            Name = "Application importée",
            Model = "Application",
            IsCurrent = true,
        };
        db.ConfigurationItems.Add(application);
        await db.SaveChangesAsync();

        var controller = new ConfigurationItemsController(db, new StubCmdbImportService());
        var update = await controller.UpdateEnrichment(
            application.Id,
            new ConfigurationItemEnrichmentWriteDto
            {
                ApplicationProfile = new ConfigurationItemApplicationProfileWriteDto
                {
                    ApplicationNature = "InternalDevelopment",
                    InternetExposed = true,
                    HostingMode = "Cloud",
                    CloudServiceModel = "PaaS",
                    MfaEnabled = true,
                    InternalTechnicalAdminCount = 3,
                    LastAccessRemediationPercentage = 12.5m,
                    PersonalDataPseudonymization = "Yes",
                },
            },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(update.Result);
        var detail = Assert.IsType<ConfigurationItemDetailDto>(ok.Value);
        Assert.Equal("InternalDevelopment", detail.ApplicationProfile.ApplicationNature);
        Assert.True(detail.ApplicationProfile.InternetExposed);
        Assert.Equal(3, detail.ApplicationProfile.InternalTechnicalAdminCount);

        application.Name = "Application renommée par EasyVista";
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var persisted = await db.ConfigurationItemApplicationProfiles.SingleAsync();
        Assert.Equal("PaaS", persisted.CloudServiceModel);
        Assert.Equal(12.5m, persisted.LastAccessRemediationPercentage);
    }

    private sealed class StubCmdbImportService : ICmdbImportService
    {
        public Task<CmdbImportResultDto> ImportAsync(
            IReadOnlyCollection<IFormFile> files,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<CmdbImportResultDto> ImportDirectoryAsync(
            string directoryPath,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private static IMemoryCache CreateMemoryCache() =>
        new MemoryCache(new MemoryCacheOptions());
}

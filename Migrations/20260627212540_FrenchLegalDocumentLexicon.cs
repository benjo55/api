using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class FrenchLegalDocumentLexicon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DECLARE @AffectedRevisions TABLE (Id int PRIMARY KEY);

                INSERT INTO @AffectedRevisions (Id)
                SELECT DISTINCT [LegalDocumentRevisionId]
                FROM [LegalDocumentNodes]
                WHERE ([Type] = 'Part' AND [Title] = 'Part')
                   OR ([Type] = 'Title' AND [Title] = 'Title')
                   OR ([Type] = 'Chapter' AND [Title] = 'Chapter')
                   OR ([Type] = 'Paragraph' AND [Title] = 'Paragraph')
                   OR ([Type] = 'Table' AND [Title] = 'Table')
                   OR ([Type] = 'Callout' AND [Title] = 'Callout')
                   OR ([Type] = 'PageBreak' AND [Title] = 'PageBreak');

                UPDATE [LegalDocumentNodes]
                SET [Title] = CASE [Type]
                    WHEN 'Part' THEN N'Partie'
                    WHEN 'Title' THEN N'Titre'
                    WHEN 'Chapter' THEN N'Chapitre'
                    WHEN 'Paragraph' THEN N'Paragraphe'
                    WHEN 'Table' THEN N'Tableau'
                    WHEN 'Callout' THEN N'Encadré'
                    WHEN 'PageBreak' THEN N'Saut de page'
                    ELSE [Title]
                END
                WHERE [LegalDocumentRevisionId] IN (SELECT [Id] FROM @AffectedRevisions)
                  AND [Title] = [Type];

                UPDATE revision
                SET [ContentHash] = CONVERT(varchar(64), HASHBYTES(
                    'SHA2_256',
                    CONCAT(COALESCE(revision.[ContentHash], ''), '|FrenchLegalDocumentLexicon')
                ), 2)
                FROM [LegalDocumentRevisions] revision
                INNER JOIN @AffectedRevisions affected ON affected.[Id] = revision.[Id];
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DECLARE @AffectedRevisions TABLE (Id int PRIMARY KEY);

                INSERT INTO @AffectedRevisions (Id)
                SELECT DISTINCT [LegalDocumentRevisionId]
                FROM [LegalDocumentNodes]
                WHERE ([Type] = 'Part' AND [Title] = N'Partie')
                   OR ([Type] = 'Title' AND [Title] = N'Titre')
                   OR ([Type] = 'Chapter' AND [Title] = N'Chapitre')
                   OR ([Type] = 'Paragraph' AND [Title] = N'Paragraphe')
                   OR ([Type] = 'Table' AND [Title] = N'Tableau')
                   OR ([Type] = 'Callout' AND [Title] = N'Encadré')
                   OR ([Type] = 'PageBreak' AND [Title] = N'Saut de page');

                UPDATE [LegalDocumentNodes]
                SET [Title] = CASE [Type]
                    WHEN 'Part' THEN 'Part'
                    WHEN 'Title' THEN 'Title'
                    WHEN 'Chapter' THEN 'Chapter'
                    WHEN 'Paragraph' THEN 'Paragraph'
                    WHEN 'Table' THEN 'Table'
                    WHEN 'Callout' THEN 'Callout'
                    WHEN 'PageBreak' THEN 'PageBreak'
                    ELSE [Title]
                END
                WHERE [LegalDocumentRevisionId] IN (SELECT [Id] FROM @AffectedRevisions)
                  AND (([Type] = 'Part' AND [Title] = N'Partie')
                    OR ([Type] = 'Title' AND [Title] = N'Titre')
                    OR ([Type] = 'Chapter' AND [Title] = N'Chapitre')
                    OR ([Type] = 'Paragraph' AND [Title] = N'Paragraphe')
                    OR ([Type] = 'Table' AND [Title] = N'Tableau')
                    OR ([Type] = 'Callout' AND [Title] = N'Encadré')
                    OR ([Type] = 'PageBreak' AND [Title] = N'Saut de page'));

                UPDATE revision
                SET [ContentHash] = CONVERT(varchar(64), HASHBYTES(
                    'SHA2_256',
                    CONCAT(COALESCE(revision.[ContentHash], ''), '|RollbackFrenchLegalDocumentLexicon')
                ), 2)
                FROM [LegalDocumentRevisions] revision
                INNER JOIN @AffectedRevisions affected ON affected.[Id] = revision.[Id];
                """);
        }
    }
}

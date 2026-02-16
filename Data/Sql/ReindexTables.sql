USE Life;
GO

SET QUOTED_IDENTIFIER ON;
GO

EXEC sp_MSforeachtable 
  @command1 = 'PRINT ''Reindexing ?''; 
               BEGIN TRY 
                 ALTER INDEX ALL ON ? REBUILD; 
               END TRY 
               BEGIN CATCH 
                 PRINT ''Erreur sur ? : '' + ERROR_MESSAGE(); 
               END CATCH';

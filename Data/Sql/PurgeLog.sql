-- Vérifie le mode de récupération
Use Life;

SELECT name, recovery_model_desc
FROM sys.databases
WHERE name = 'Life';

ALTER DATABASE Life SET RECOVERY SIMPLE;
GO

DBCC SHRINKFILE ('Life_log', 1);
GO

-- Remet le mode de récupération initial si nécessaire
ALTER DATABASE Life SET RECOVERY FULL;
GO
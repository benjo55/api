BEGIN TRANSACTION;

-- 1️⃣ Rendre négatifs les montants des opérations de type PartialWithdrawal
UPDATE Life.dbo.Operations
SET Amount = -ABS(Amount)
WHERE Type IN (6)  -- 6 = PartialWithdrawal
  AND ContractId = 4256;

-- 2️⃣ Rendre négatifs les montants dans les allocations associées à ces opérations
UPDATE Life.dbo.OperationSupportAllocations
SET Amount = -ABS(Amount)
WHERE OperationId IN (
    SELECT Id FROM Life.dbo.Operations
    WHERE Type IN (6) AND ContractId = 4256
);

-- 3️⃣ Purger les anciennes valorisations pour repartir d’un recalcul propre
DELETE FROM Life.dbo.FinancialSupportAllocations
WHERE ContractId = 4256;

COMMIT TRANSACTION;

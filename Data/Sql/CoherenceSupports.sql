USE Life;
GO

PRINT '========================================================';
PRINT '🧩 Vérification de cohérence des opérations et allocations';
PRINT '========================================================';

-- 1️⃣ Vérifier les opérations sans allocations
PRINT '🔹 Étape 1 : Opérations sans allocations correspondantes';
SELECT o.Id AS OperationId, o.ContractId, o.Type, o.Amount, o.OperationDate
FROM dbo.Operations AS o
LEFT JOIN dbo.OperationSupportAllocations AS a ON a.OperationId = o.Id
WHERE a.Id IS NULL;
PRINT '--------------------------------------------------------';

-- 2️⃣ Vérifier les allocations sans opération existante
PRINT '🔹 Étape 2 : Allocations orphelines (sans opération)';
SELECT a.Id AS AllocationId, a.OperationId, a.SupportId, s.ISIN, s.Label
FROM dbo.OperationSupportAllocations AS a
LEFT JOIN dbo.Operations AS o ON o.Id = a.OperationId
LEFT JOIN dbo.FinancialSupports AS s ON s.Id = a.SupportId
WHERE o.Id IS NULL;
PRINT '--------------------------------------------------------';

-- 3️⃣ Vérifier les allocations à zéro (erreur logique)
PRINT '🔹 Étape 3 : Allocations à zéro (montant et pourcentage = 0)';
SELECT a.Id, a.OperationId, a.SupportId, s.ISIN, a.Amount, a.Percentage
FROM dbo.OperationSupportAllocations AS a
INNER JOIN dbo.FinancialSupports AS s ON s.Id = a.SupportId
WHERE ISNULL(a.Amount,0)=0 AND ISNULL(a.Percentage,0)=0;
PRINT '--------------------------------------------------------';

-- 4️⃣ Vérifier cohérence contrat / allocations (ContractId ≠ contrat réel)
PRINT '🔹 Étape 4 : Allocations liées à un contrat incohérent';
SELECT a.Id, a.OperationId, o.ContractId AS OperationContractId, 
       c.Id AS ContractId, c.ContractNumber, s.Label
FROM dbo.OperationSupportAllocations a
INNER JOIN dbo.Operations o ON o.Id = a.OperationId
LEFT JOIN dbo.Contracts c ON c.Id = o.ContractId
LEFT JOIN dbo.FinancialSupports s ON s.Id = a.SupportId
WHERE o.ContractId <> c.Id;
PRINT '--------------------------------------------------------';

-- 5️⃣ Vérifier cohérence FinancialSupportAllocations (compartiments et montants)
PRINT '🔹 Étape 5 : Supports financiers avec CurrentShares négatifs ou incohérents';
SELECT fsa.Id, fsa.ContractId, fsa.CompartmentId, fs.Label, fs.ISIN,
       fsa.CurrentShares, fsa.CurrentAmount, fs.LastValuationAmount,
       ROUND(ISNULL(fsa.CurrentShares * fs.LastValuationAmount,0),2) AS ExpectedAmount
FROM dbo.FinancialSupportAllocations fsa
INNER JOIN dbo.FinancialSupports fs ON fs.Id = fsa.SupportId
WHERE fsa.CurrentShares < 0 
   OR ABS(ISNULL(fsa.CurrentAmount,0) - ISNULL(fsa.CurrentShares * fs.LastValuationAmount,0)) > 0.01;
PRINT '--------------------------------------------------------';

-- 6️⃣ Vérifier cohérence entre OperationSupportAllocations et FinancialSupportAllocations
PRINT '🔹 Étape 6 : Vérification de la présence des supports dans les deux tables';
SELECT DISTINCT a.SupportId, fs.Label, fs.ISIN
FROM dbo.OperationSupportAllocations a
LEFT JOIN dbo.FinancialSupportAllocations fsa ON fsa.SupportId = a.SupportId
INNER JOIN dbo.FinancialSupports fs ON fs.Id = a.SupportId
WHERE fsa.SupportId IS NULL;
PRINT '--------------------------------------------------------';

-- 7️⃣ Synthèse
PRINT '✅ Vérification terminée.';
PRINT '   → 0 ligne = base cohérente';
PRINT '   → 1+ ligne = incohérence détectée à corriger.';

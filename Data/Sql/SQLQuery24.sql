-- Total réel (identique au cartouche)
SELECT CurrentValue FROM Contracts WHERE Id = 4256;

-- Total agrégé (identique à ContractSituationView)
SELECT SUM(CurrentAmount) 
FROM FinancialSupportAllocations 
WHERE ContractId = 4256;
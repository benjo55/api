--use Life
--go
--SELECT 
--    SUM(CurrentShares * (SELECT LastValuationAmount FROM FinancialSupports WHERE Id = fsa.SupportId))
--FROM FinancialSupportAllocations fsa
--WHERE fsa.ContractId = 4256;

SELECT 
    SUM((a.Shares * a.NavAtOperation))
FROM OperationSupportAllocations a
JOIN Operations o ON o.Id = a.OperationId
WHERE o.ContractId = 4256;
SELECT Id, Type, Amount, ContractId, CompartmentId, OperationDate
FROM Operations
WHERE ContractId = 4256
ORDER BY OperationDate
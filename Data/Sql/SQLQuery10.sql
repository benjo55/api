use Life
Go
SELECT TOP (1000) [Id]
      ,[ContractId]
      ,[SupportId]
      ,[AllocationPercentage]
      ,[CompartmentId]
      ,[CurrentAmount]
      ,[CurrentShares]
  FROM [Life].[dbo].[FinancialSupportAllocations] order by SupportId

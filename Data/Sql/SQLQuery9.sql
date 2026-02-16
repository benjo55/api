Use Life
Go
SELECT TOP (1000) [Id]
      ,[OperationId]
      ,[SupportId]
      ,[Amount]
      ,[Percentage]
      ,[NavAtOperation]
      ,[NavDateAtOperation]
      ,[CompartmentId]
  FROM [Life].[dbo].[OperationSupportAllocations] order by SupportId

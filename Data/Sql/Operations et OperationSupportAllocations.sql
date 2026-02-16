Use Life
Go

SELECT TOP (1000) [Id]
      ,[ContractId]
      ,[CompartmentId]
      ,[Type]
      ,[Status]
      ,[OperationDate]
      ,[Amount]
      ,[Currency]
        
  FROM [Life].[dbo].[Operations] 

SELECT TOP (1000) [Id]
      ,[OperationId]
      ,[SupportId]
      ,[Amount]
      ,[Percentage]
      ,[NavAtOperation]
      ,[NavDateAtOperation]
      ,[CompartmentId]
  FROM [Life].[dbo].[OperationSupportAllocations] order by SupportId

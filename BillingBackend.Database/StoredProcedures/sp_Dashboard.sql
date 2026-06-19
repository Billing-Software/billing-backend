-- ============================================================
-- Dashboard Aggregations Stored Procedure
-- ============================================================

IF OBJECT_ID('dbo.sp_GetDashboardData', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetDashboardData;
GO

CREATE PROCEDURE dbo.sp_GetDashboardData
    @BusinessId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Result 1: Key Metrics Summary
    DECLARE @TotalRevenue DECIMAL(18,2) = 0;
    DECLARE @TotalBills INT = 0;
    DECLARE @TotalCustomers INT = 0;
    DECLARE @LowStockCount INT = 0;
    
    SELECT @TotalRevenue = ISNULL(SUM([TotalAmount]), 0), @TotalBills = COUNT(*)
    FROM [dbo].[Bills]
    WHERE [BusinessId] = @BusinessId;
    
    SELECT @TotalCustomers = COUNT(*)
    FROM [dbo].[Customers]
    WHERE [BusinessId] = @BusinessId;
    
    SELECT @LowStockCount = COUNT(*)
    FROM [dbo].[InventoryItems]
    WHERE [BusinessId] = @BusinessId AND [CurrentStock] <= [ReorderLevel];
    
    SELECT 
        @TotalRevenue AS TotalRevenue,
        @TotalBills AS TotalBills,
        @TotalCustomers AS TotalCustomers,
        @LowStockCount AS LowStockCount;
        
    -- Result 2: Recent Bills (Top 5)
    SELECT TOP 5 
        b.[Id],
        b.[BillNumber],
        b.[TotalAmount],
        b.[CreatedAt],
        b.[Status],
        c.[Name] AS CustomerName
    FROM [dbo].[Bills] b
    INNER JOIN [dbo].[Customers] c ON b.[CustomerId] = c.[Id]
    WHERE b.[BusinessId] = @BusinessId
    ORDER BY b.[CreatedAt] DESC;
    
    -- Result 3: Top Selling Services (Top 5)
    SELECT TOP 5
        bi.[ServiceId],
        bi.[ServiceName],
        SUM(bi.[Quantity]) AS TotalQuantity,
        SUM(bi.[LineTotal]) AS TotalRevenue
    FROM [dbo].[BillItems] bi
    INNER JOIN [dbo].[Bills] b ON bi.[BillId] = b.[Id]
    WHERE b.[BusinessId] = @BusinessId
    GROUP BY bi.[ServiceId], bi.[ServiceName]
    ORDER BY TotalRevenue DESC;
    
    -- Result 4: Low Stock Items (Top 5)
    SELECT TOP 5
        [Id],
        [Name],
        [SKU],
        [CurrentStock],
        [ReorderLevel],
        [Unit]
    FROM [dbo].[InventoryItems]
    WHERE [BusinessId] = @BusinessId AND [CurrentStock] <= [ReorderLevel]
    ORDER BY [CurrentStock] ASC;
    
    -- Result 5: Sales Trend (Last 30 Days)
    SELECT 
        CAST(b.[CreatedAt] AS DATE) AS SalesDate,
        COUNT(*) AS BillsCount,
        SUM(b.[TotalAmount]) AS DailyRevenue
    FROM [dbo].[Bills] b
    WHERE b.[BusinessId] = @BusinessId 
      AND b.[CreatedAt] >= DATEADD(day, -30, GETUTCDATE())
    GROUP BY CAST(b.[CreatedAt] AS DATE)
    ORDER BY SalesDate ASC;
END;
GO

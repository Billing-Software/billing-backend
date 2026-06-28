-- ============================================================
-- SmartBilling Extended Database: Expenses and Purchases Stored Procedures
-- ============================================================

-- 1. Create Expense
IF OBJECT_ID('dbo.sp_CreateExpense', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_CreateExpense;
GO

CREATE PROCEDURE dbo.sp_CreateExpense
    @BusinessId INT,
    @Description NVARCHAR(500),
    @Amount DECIMAL(18,2),
    @Category NVARCHAR(100),
    @ExpenseDate DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO [dbo].[Expenses] (
        [BusinessId], [Description], [Amount], [Category], [ExpenseDate], [CreatedAt]
    )
    VALUES (
        @BusinessId, @Description, @Amount, @Category, ISNULL(@ExpenseDate, GETUTCDATE()), GETUTCDATE()
    );
    
    SELECT * FROM [dbo].[Expenses] WHERE [Id] = SCOPE_IDENTITY();
END;
GO

-- 2. Get Expenses By Business Id
IF OBJECT_ID('dbo.sp_GetExpensesByBusinessId', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetExpensesByBusinessId;
GO

CREATE PROCEDURE dbo.sp_GetExpensesByBusinessId
    @BusinessId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT * FROM [dbo].[Expenses]
    WHERE [BusinessId] = @BusinessId
    ORDER BY [ExpenseDate] DESC;
END;
GO

-- 3. Delete Expense
IF OBJECT_ID('dbo.sp_DeleteExpense', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_DeleteExpense;
GO

CREATE PROCEDURE dbo.sp_DeleteExpense
    @BusinessId INT,
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DELETE FROM [dbo].[Expenses]
    WHERE [Id] = @Id AND [BusinessId] = @BusinessId;
    
    SELECT @@ROWCOUNT AS RowsDeleted;
END;
GO

-- 4. Create Purchase
IF OBJECT_ID('dbo.sp_CreatePurchase', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_CreatePurchase;
GO

CREATE PROCEDURE dbo.sp_CreatePurchase
    @BusinessId INT,
    @VendorName NVARCHAR(200),
    @InvoiceNumber NVARCHAR(100) = NULL,
    @Subtotal DECIMAL(18,2),
    @TaxAmount DECIMAL(18,2),
    @TotalAmount DECIMAL(18,2),
    @Status NVARCHAR(50),
    @PurchaseDate DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO [dbo].[Purchases] (
        [BusinessId], [VendorName], [InvoiceNumber], [Subtotal], [TaxAmount], [TotalAmount], [Status], [PurchaseDate], [CreatedAt]
    )
    VALUES (
        @BusinessId, @VendorName, @InvoiceNumber, @Subtotal, @TaxAmount, @TotalAmount, @Status, ISNULL(@PurchaseDate, GETUTCDATE()), GETUTCDATE()
    );
    
    SELECT * FROM [dbo].[Purchases] WHERE [Id] = SCOPE_IDENTITY();
END;
GO

-- 5. Create Purchase Item
IF OBJECT_ID('dbo.sp_CreatePurchaseItem', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_CreatePurchaseItem;
GO

CREATE PROCEDURE dbo.sp_CreatePurchaseItem
    @PurchaseId INT,
    @InventoryItemId INT = NULL,
    @ItemName NVARCHAR(200),
    @UnitPrice DECIMAL(18,2),
    @Quantity INT,
    @LineTotal DECIMAL(18,2)
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO [dbo].[PurchaseItems] (
        [PurchaseId], [InventoryItemId], [ItemName], [UnitPrice], [Quantity], [LineTotal]
    )
    VALUES (
        @PurchaseId, @InventoryItemId, @ItemName, @UnitPrice, @Quantity, @LineTotal
    );
    
    SELECT * FROM [dbo].[PurchaseItems] WHERE [Id] = SCOPE_IDENTITY();
END;
GO

-- 6. Get Purchases By Business Id
IF OBJECT_ID('dbo.sp_GetPurchasesByBusinessId', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetPurchasesByBusinessId;
GO

CREATE PROCEDURE dbo.sp_GetPurchasesByBusinessId
    @BusinessId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT * FROM [dbo].[Purchases]
    WHERE [BusinessId] = @BusinessId
    ORDER BY [PurchaseDate] DESC;
END;
GO

-- 7. Get Purchase By Id (Header + Items)
IF OBJECT_ID('dbo.sp_GetPurchaseById', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetPurchaseById;
GO

CREATE PROCEDURE dbo.sp_GetPurchaseById
    @BusinessId INT,
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Result Set 1: Purchase Header
    SELECT * FROM [dbo].[Purchases]
    WHERE [Id] = @Id AND [BusinessId] = @BusinessId;
    
    -- Result Set 2: Purchase Line Items
    SELECT pi.* 
    FROM [dbo].[PurchaseItems] pi
    INNER JOIN [dbo].[Purchases] p ON pi.[PurchaseId] = p.[Id]
    WHERE p.[Id] = @Id AND p.[BusinessId] = @BusinessId;
END;
GO

-- 8. Delete Purchase
IF OBJECT_ID('dbo.sp_DeletePurchase', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_DeletePurchase;
GO

CREATE PROCEDURE dbo.sp_DeletePurchase
    @BusinessId INT,
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DELETE FROM [dbo].[Purchases]
    WHERE [Id] = @Id AND [BusinessId] = @BusinessId;
    
    SELECT @@ROWCOUNT AS RowsDeleted;
END;
GO

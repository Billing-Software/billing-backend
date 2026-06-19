-- ============================================================
-- InventoryItems CRUD Stored Procedures
-- ============================================================

-- 1. Create InventoryItem
IF OBJECT_ID('dbo.sp_CreateInventoryItem', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_CreateInventoryItem;
GO

CREATE PROCEDURE dbo.sp_CreateInventoryItem
    @BusinessId INT,
    @Name NVARCHAR(200),
    @SKU NVARCHAR(50),
    @Category NVARCHAR(100),
    @CurrentStock INT,
    @Unit NVARCHAR(50),
    @ReorderLevel INT,
    @ImageUrl NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO [dbo].[InventoryItems] (
        [BusinessId], [Name], [SKU], [Category], [CurrentStock], [Unit], [ReorderLevel], [ImageUrl], [CreatedAt]
    )
    VALUES (
        @BusinessId, @Name, @SKU, @Category, @CurrentStock, @Unit, @ReorderLevel, @ImageUrl, GETUTCDATE()
    );
    
    SELECT * FROM [dbo].[InventoryItems] WHERE [Id] = SCOPE_IDENTITY();
END;
GO

-- 2. Read All InventoryItems for a Business
IF OBJECT_ID('dbo.sp_GetInventoryItemsByBusinessId', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetInventoryItemsByBusinessId;
GO

CREATE PROCEDURE dbo.sp_GetInventoryItemsByBusinessId
    @BusinessId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT * FROM [dbo].[InventoryItems] 
    WHERE [BusinessId] = @BusinessId
    ORDER BY [Name];
END;
GO

-- 3. Read Single InventoryItem
IF OBJECT_ID('dbo.sp_GetInventoryItemById', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetInventoryItemById;
GO

CREATE PROCEDURE dbo.sp_GetInventoryItemById
    @BusinessId INT,
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT * FROM [dbo].[InventoryItems] 
    WHERE [Id] = @Id AND [BusinessId] = @BusinessId;
END;
GO

-- 4. Update InventoryItem
IF OBJECT_ID('dbo.sp_UpdateInventoryItem', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_UpdateInventoryItem;
GO

CREATE PROCEDURE dbo.sp_UpdateInventoryItem
    @BusinessId INT,
    @Id INT,
    @Name NVARCHAR(200),
    @SKU NVARCHAR(50),
    @Category NVARCHAR(100),
    @CurrentStock INT,
    @Unit NVARCHAR(50),
    @ReorderLevel INT,
    @ImageUrl NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE [dbo].[InventoryItems]
    SET [Name] = @Name,
        [SKU] = @SKU,
        [Category] = @Category,
        [CurrentStock] = @CurrentStock,
        [Unit] = @Unit,
        [ReorderLevel] = @ReorderLevel,
        [ImageUrl] = @ImageUrl,
        [UpdatedAt] = GETUTCDATE()
    WHERE [Id] = @Id AND [BusinessId] = @BusinessId;
    
    SELECT * FROM [dbo].[InventoryItems] WHERE [Id] = @Id AND [BusinessId] = @BusinessId;
END;
GO

-- 5. Delete InventoryItem
IF OBJECT_ID('dbo.sp_DeleteInventoryItem', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_DeleteInventoryItem;
GO

CREATE PROCEDURE dbo.sp_DeleteInventoryItem
    @BusinessId INT,
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DELETE FROM [dbo].[InventoryItems]
    WHERE [Id] = @Id AND [BusinessId] = @BusinessId;
    
    SELECT @@ROWCOUNT AS RowsDeleted;
END;
GO

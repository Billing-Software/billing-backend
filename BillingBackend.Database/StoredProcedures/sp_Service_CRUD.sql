-- ============================================================
-- Services CRUD Stored Procedures
-- ============================================================

-- 1. Create Service
IF OBJECT_ID('dbo.sp_CreateService', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_CreateService;
GO

CREATE PROCEDURE dbo.sp_CreateService
    @BusinessId INT,
    @Name NVARCHAR(200),
    @SKU NVARCHAR(50),
    @Category NVARCHAR(100),
    @BasePrice DECIMAL(18,2),
    @TaxRate DECIMAL(5,2),
    @Status NVARCHAR(20),
    @IconName NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO [dbo].[Services] (
        [BusinessId], [Name], [SKU], [Category], [BasePrice], [TaxRate], [Status], [IconName], [CreatedAt]
    )
    VALUES (
        @BusinessId, @Name, @SKU, @Category, @BasePrice, @TaxRate, ISNULL(@Status, 'Active'), @IconName, GETUTCDATE()
    );
    
    SELECT * FROM [dbo].[Services] WHERE [Id] = SCOPE_IDENTITY();
END;
GO

-- 2. Read All Services for a Business
IF OBJECT_ID('dbo.sp_GetServicesByBusinessId', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetServicesByBusinessId;
GO

CREATE PROCEDURE dbo.sp_GetServicesByBusinessId
    @BusinessId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT * FROM [dbo].[Services] 
    WHERE [BusinessId] = @BusinessId
    ORDER BY [Name];
END;
GO

-- 3. Read Single Service
IF OBJECT_ID('dbo.sp_GetServiceById', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetServiceById;
GO

CREATE PROCEDURE dbo.sp_GetServiceById
    @BusinessId INT,
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT * FROM [dbo].[Services] 
    WHERE [Id] = @Id AND [BusinessId] = @BusinessId;
END;
GO

-- 4. Update Service
IF OBJECT_ID('dbo.sp_UpdateService', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_UpdateService;
GO

CREATE PROCEDURE dbo.sp_UpdateService
    @BusinessId INT,
    @Id INT,
    @Name NVARCHAR(200),
    @SKU NVARCHAR(50),
    @Category NVARCHAR(100),
    @BasePrice DECIMAL(18,2),
    @TaxRate DECIMAL(5,2),
    @Status NVARCHAR(20),
    @IconName NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE [dbo].[Services]
    SET [Name] = @Name,
        [SKU] = @SKU,
        [Category] = @Category,
        [BasePrice] = @BasePrice,
        [TaxRate] = @TaxRate,
        [Status] = @Status,
        [IconName] = @IconName,
        [UpdatedAt] = GETUTCDATE()
    WHERE [Id] = @Id AND [BusinessId] = @BusinessId;
    
    SELECT * FROM [dbo].[Services] WHERE [Id] = @Id AND [BusinessId] = @BusinessId;
END;
GO

-- 5. Delete Service
IF OBJECT_ID('dbo.sp_DeleteService', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_DeleteService;
GO

CREATE PROCEDURE dbo.sp_DeleteService
    @BusinessId INT,
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DELETE FROM [dbo].[Services]
    WHERE [Id] = @Id AND [BusinessId] = @BusinessId;
    
    SELECT @@ROWCOUNT AS RowsDeleted;
END;
GO

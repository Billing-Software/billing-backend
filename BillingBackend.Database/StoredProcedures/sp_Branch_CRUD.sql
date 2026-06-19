-- ============================================================
-- Branches CRUD Stored Procedures
-- ============================================================

-- 1. Create Branch
IF OBJECT_ID('dbo.sp_CreateBranch', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_CreateBranch;
GO

CREATE PROCEDURE dbo.sp_CreateBranch
    @BusinessId INT,
    @Name NVARCHAR(100),
    @Address NVARCHAR(500) = NULL,
    @City NVARCHAR(100) = NULL,
    @PostalCode NVARCHAR(20) = NULL,
    @Phone NVARCHAR(20) = NULL,
    @IsActive BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO [dbo].[Branches] (
        [BusinessId], [Name], [Address], [City], [PostalCode], [Phone], [IsActive], [CreatedAt]
    )
    VALUES (
        @BusinessId, @Name, @Address, @City, @PostalCode, @Phone, @IsActive, GETUTCDATE()
    );
    
    SELECT * FROM [dbo].[Branches] WHERE [Id] = SCOPE_IDENTITY();
END;
GO

-- 2. Read All Branches for a Business
IF OBJECT_ID('dbo.sp_GetBranchesByBusinessId', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetBranchesByBusinessId;
GO

CREATE PROCEDURE dbo.sp_GetBranchesByBusinessId
    @BusinessId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT * FROM [dbo].[Branches] 
    WHERE [BusinessId] = @BusinessId
    ORDER BY [Name];
END;
GO

-- 3. Read Single Branch
IF OBJECT_ID('dbo.sp_GetBranchById', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetBranchById;
GO

CREATE PROCEDURE dbo.sp_GetBranchById
    @BusinessId INT,
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT * FROM [dbo].[Branches] 
    WHERE [Id] = @Id AND [BusinessId] = @BusinessId;
END;
GO

-- 4. Update Branch
IF OBJECT_ID('dbo.sp_UpdateBranch', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_UpdateBranch;
GO

CREATE PROCEDURE dbo.sp_UpdateBranch
    @BusinessId INT,
    @Id INT,
    @Name NVARCHAR(100),
    @Address NVARCHAR(500) = NULL,
    @City NVARCHAR(100) = NULL,
    @PostalCode NVARCHAR(20) = NULL,
    @Phone NVARCHAR(20) = NULL,
    @IsActive BIT
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE [dbo].[Branches]
    SET [Name] = @Name,
        [Address] = @Address,
        [City] = @City,
        [PostalCode] = @PostalCode,
        [Phone] = @Phone,
        [IsActive] = @IsActive
    WHERE [Id] = @Id AND [BusinessId] = @BusinessId;
    
    SELECT * FROM [dbo].[Branches] WHERE [Id] = @Id AND [BusinessId] = @BusinessId;
END;
GO

-- 5. Delete Branch
IF OBJECT_ID('dbo.sp_DeleteBranch', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_DeleteBranch;
GO

CREATE PROCEDURE dbo.sp_DeleteBranch
    @BusinessId INT,
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DELETE FROM [dbo].[Branches]
    WHERE [Id] = @Id AND [BusinessId] = @BusinessId;
    
    SELECT @@ROWCOUNT AS RowsDeleted;
END;
GO

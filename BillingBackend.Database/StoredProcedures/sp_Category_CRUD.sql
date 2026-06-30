-- ============================================================
-- Categories CRUD Stored Procedures
-- ============================================================

-- 1. Create Category
IF OBJECT_ID('dbo.sp_CreateCategory', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_CreateCategory;
GO

CREATE PROCEDURE dbo.sp_CreateCategory
    @BusinessId INT,
    @Name NVARCHAR(100),
    @Type NVARCHAR(50),
    @ParentId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO [dbo].[Categories] (
        [BusinessId], [Name], [Type], [ParentId], [CreatedAt]
    )
    VALUES (
        @BusinessId, @Name, @Type, @ParentId, GETUTCDATE()
    );
    
    SELECT * FROM [dbo].[Categories] WHERE [Id] = SCOPE_IDENTITY();
END;
GO

-- 2. Read All Categories for a Business
IF OBJECT_ID('dbo.sp_GetCategoriesByBusinessId', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetCategoriesByBusinessId;
GO

CREATE PROCEDURE dbo.sp_GetCategoriesByBusinessId
    @BusinessId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT * FROM [dbo].[Categories] 
    WHERE [BusinessId] = @BusinessId
    ORDER BY [Type], [Name];
END;
GO

-- 3. Delete Category
IF OBJECT_ID('dbo.sp_DeleteCategory', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_DeleteCategory;
GO

CREATE PROCEDURE dbo.sp_DeleteCategory
    @BusinessId INT,
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DELETE FROM [dbo].[Categories]
    WHERE [Id] = @Id AND [BusinessId] = @BusinessId;
    
    SELECT @@ROWCOUNT AS RowsDeleted;
END;
GO

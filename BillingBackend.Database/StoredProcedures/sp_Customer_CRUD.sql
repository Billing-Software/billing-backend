-- ============================================================
-- Customers CRUD Stored Procedures
-- ============================================================

-- 1. Create Customer
IF OBJECT_ID('dbo.sp_CreateCustomer', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_CreateCustomer;
GO

CREATE PROCEDURE dbo.sp_CreateCustomer
    @BusinessId INT,
    @Name NVARCHAR(200),
    @Phone NVARCHAR(20) = NULL,
    @Email NVARCHAR(256) = NULL,
    @IsWalkIn BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO [dbo].[Customers] (
        [BusinessId], [Name], [Phone], [Email], [IsWalkIn], [CreatedAt]
    )
    VALUES (
        @BusinessId, @Name, @Phone, @Email, @IsWalkIn, GETUTCDATE()
    );
    
    SELECT * FROM [dbo].[Customers] WHERE [Id] = SCOPE_IDENTITY();
END;
GO

-- 2. Read All Customers for a Business
IF OBJECT_ID('dbo.sp_GetCustomersByBusinessId', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetCustomersByBusinessId;
GO

CREATE PROCEDURE dbo.sp_GetCustomersByBusinessId
    @BusinessId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT * FROM [dbo].[Customers] 
    WHERE [BusinessId] = @BusinessId
    ORDER BY [Name];
END;
GO

-- 3. Read Single Customer
IF OBJECT_ID('dbo.sp_GetCustomerById', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetCustomerById;
GO

CREATE PROCEDURE dbo.sp_GetCustomerById
    @BusinessId INT,
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT * FROM [dbo].[Customers] 
    WHERE [Id] = @Id AND [BusinessId] = @BusinessId;
END;
GO

-- 4. Update Customer
IF OBJECT_ID('dbo.sp_UpdateCustomer', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_UpdateCustomer;
GO

CREATE PROCEDURE dbo.sp_UpdateCustomer
    @BusinessId INT,
    @Id INT,
    @Name NVARCHAR(200),
    @Phone NVARCHAR(20) = NULL,
    @Email NVARCHAR(256) = NULL,
    @IsWalkIn BIT
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE [dbo].[Customers]
    SET [Name] = @Name,
        [Phone] = @Phone,
        [Email] = @Email,
        [IsWalkIn] = @IsWalkIn,
        [UpdatedAt] = GETUTCDATE()
    WHERE [Id] = @Id AND [BusinessId] = @BusinessId;
    
    SELECT * FROM [dbo].[Customers] WHERE [Id] = @Id AND [BusinessId] = @BusinessId;
END;
GO

-- 5. Delete Customer
IF OBJECT_ID('dbo.sp_DeleteCustomer', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_DeleteCustomer;
GO

CREATE PROCEDURE dbo.sp_DeleteCustomer
    @BusinessId INT,
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DELETE FROM [dbo].[Customers]
    WHERE [Id] = @Id AND [BusinessId] = @BusinessId;
    
    SELECT @@ROWCOUNT AS RowsDeleted;
END;
GO

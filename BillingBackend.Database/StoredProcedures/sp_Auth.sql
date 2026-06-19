-- ============================================================
-- Authentication & Signup Stored Procedures
-- ============================================================

-- 1. Register User and Business (in a Transaction)
IF OBJECT_ID('dbo.sp_RegisterUserAndBusiness', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_RegisterUserAndBusiness;
GO

CREATE PROCEDURE dbo.sp_RegisterUserAndBusiness
    @Username NVARCHAR(100),
    @Email NVARCHAR(256),
    @PasswordHash VARBINARY(MAX),
    @PasswordSalt VARBINARY(MAX),
    @Role NVARCHAR(50),
    @LegalName NVARCHAR(200),
    @TradingName NVARCHAR(200) = NULL,
    @LogoUrl NVARCHAR(500) = NULL,
    @Address NVARCHAR(500) = NULL,
    @City NVARCHAR(100) = NULL,
    @State NVARCHAR(100) = NULL,
    @PostalCode NVARCHAR(20) = NULL,
    @Country NVARCHAR(100) = 'India',
    @Phone NVARCHAR(20) = NULL,
    @BusinessEmail NVARCHAR(256) = NULL,
    @Website NVARCHAR(500) = NULL,
    @GstIn NVARCHAR(50) = NULL,
    @DefaultTaxRate DECIMAL(5,2) = 18.00,
    @PricesIncludeTax BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        -- Insert User
        INSERT INTO [dbo].[Users] ([Username], [Email], [PasswordHash], [PasswordSalt], [Role])
        VALUES (@Username, @Email, @PasswordHash, @PasswordSalt, ISNULL(@Role, 'Owner'));
        
        DECLARE @UserId INT = SCOPE_IDENTITY();

        -- Insert Business
        INSERT INTO [dbo].[Businesses] (
            [OwnerId], [LegalName], [TradingName], [LogoUrl], [Address], [City], [State], 
            [PostalCode], [Country], [Phone], [Email], [Website], [GstIn], [DefaultTaxRate], [PricesIncludeTax]
        )
        VALUES (
            @UserId, @LegalName, @TradingName, @LogoUrl, @Address, @City, @State, 
            @PostalCode, @Country, @Phone, ISNULL(@BusinessEmail, @Email), @Website, @GstIn, ISNULL(@DefaultTaxRate, 18.00), ISNULL(@PricesIncludeTax, 1)
        );

        DECLARE @BusinessId INT = SCOPE_IDENTITY();

        -- Insert Default Branch
        INSERT INTO [dbo].[Branches] ([BusinessId], [Name], [Address], [City], [PostalCode], [Phone], [IsActive])
        VALUES (@BusinessId, 'Main Branch', @Address, @City, @PostalCode, @Phone, 1);

        DECLARE @BranchId INT = SCOPE_IDENTITY();

        -- Insert Default Walk-In Customer
        INSERT INTO [dbo].[Customers] ([BusinessId], [Name], [Phone], [Email], [IsWalkIn])
        VALUES (@BusinessId, 'Walk-In Customer', 'N/A', NULL, 1);

        -- Insert WhatsApp Settings
        INSERT INTO [dbo].[WhatsAppSettings] ([BusinessId], [ApiKey], [IsConnected])
        VALUES (@BusinessId, NULL, 0);

        COMMIT TRANSACTION;

        -- Return the newly created user and business details
        SELECT 
            u.[Id] AS UserId,
            u.[Username],
            u.[Email],
            u.[Role],
            b.[Id] AS BusinessId,
            b.[LegalName] AS BusinessName,
            @BranchId AS DefaultBranchId
        FROM [dbo].[Users] u
        INNER JOIN [dbo].[Businesses] b ON u.[Id] = b.[OwnerId]
        WHERE u.[Id] = @UserId;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- 2. Get User By Email (for Authentication)
IF OBJECT_ID('dbo.sp_GetUserByEmail', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetUserByEmail;
GO

CREATE PROCEDURE dbo.sp_GetUserByEmail
    @Email NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        u.[Id] AS UserId,
        u.[Username],
        u.[Email],
        u.[PasswordHash],
        u.[PasswordSalt],
        u.[Role],
        b.[Id] AS BusinessId,
        b.[LegalName] AS BusinessName
    FROM [dbo].[Users] u
    LEFT JOIN [dbo].[Businesses] b ON u.[Id] = b.[OwnerId]
    WHERE u.[Email] = @Email;
END;
GO

-- 3. Get User By Id
IF OBJECT_ID('dbo.sp_GetUserById', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetUserById;
GO

CREATE PROCEDURE dbo.sp_GetUserById
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        u.[Id] AS UserId,
        u.[Username],
        u.[Email],
        u.[Role],
        b.[Id] AS BusinessId,
        b.[LegalName] AS BusinessName
    FROM [dbo].[Users] u
    LEFT JOIN [dbo].[Businesses] b ON u.[Id] = b.[OwnerId]
    WHERE u.[Id] = @UserId;
END;
GO

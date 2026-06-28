-- ============================================================
-- Settings & Business Profile CRUD Stored Procedures
-- ============================================================

-- 1. Get Business Profile
IF OBJECT_ID('dbo.sp_GetBusinessProfile', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetBusinessProfile;
GO

CREATE PROCEDURE dbo.sp_GetBusinessProfile
    @BusinessId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT * FROM [dbo].[Businesses] 
    WHERE [Id] = @BusinessId;
END;
GO

-- 2. Update Business Profile
IF OBJECT_ID('dbo.sp_UpdateBusinessProfile', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_UpdateBusinessProfile;
GO

CREATE PROCEDURE dbo.sp_UpdateBusinessProfile
    @BusinessId INT,
    @LegalName NVARCHAR(200),
    @TradingName NVARCHAR(200) = NULL,
    @LogoUrl NVARCHAR(500) = NULL,
    @Address NVARCHAR(500) = NULL,
    @City NVARCHAR(100) = NULL,
    @State NVARCHAR(100) = NULL,
    @PostalCode NVARCHAR(20) = NULL,
    @Country NVARCHAR(100) = NULL,
    @Phone NVARCHAR(20) = NULL,
    @Email NVARCHAR(256) = NULL,
    @Website NVARCHAR(500) = NULL,
    @GstIn NVARCHAR(50) = NULL,
    @DefaultTaxRate DECIMAL(5,2),
    @PricesIncludeTax BIT,
    @ReceiptHeader NVARCHAR(500) = NULL,
    @ReceiptFooter NVARCHAR(500) = NULL,
    @ShowLogoOnReceipt BIT = 1,
    @ReceiptTemplateType NVARCHAR(50) = 'Thermal80mm'
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE [dbo].[Businesses]
    SET [LegalName] = @LegalName,
        [TradingName] = @TradingName,
        [LogoUrl] = @LogoUrl,
        [Address] = @Address,
        [City] = @City,
        [State] = @State,
        [PostalCode] = @PostalCode,
        [Country] = ISNULL(@Country, [Country]),
        [Phone] = @Phone,
        [Email] = @Email,
        [Website] = @Website,
        [GstIn] = @GstIn,
        [DefaultTaxRate] = @DefaultTaxRate,
        [PricesIncludeTax] = @PricesIncludeTax,
        [ReceiptHeader] = @ReceiptHeader,
        [ReceiptFooter] = @ReceiptFooter,
        [ShowLogoOnReceipt] = @ShowLogoOnReceipt,
        [ReceiptTemplateType] = @ReceiptTemplateType,
        [UpdatedAt] = GETUTCDATE()
    WHERE [Id] = @BusinessId;
    
    SELECT * FROM [dbo].[Businesses] WHERE [Id] = @BusinessId;
END;
GO

-- 3. Get WhatsApp Settings and Templates
IF OBJECT_ID('dbo.sp_GetWhatsAppSettings', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetWhatsAppSettings;
GO

CREATE PROCEDURE dbo.sp_GetWhatsAppSettings
    @BusinessId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Result 1: Settings
    SELECT * FROM [dbo].[WhatsAppSettings] 
    WHERE [BusinessId] = @BusinessId;
    
    -- Result 2: Templates
    SELECT t.* 
    FROM [dbo].[WhatsAppTemplates] t
    INNER JOIN [dbo].[WhatsAppSettings] s ON t.[WhatsAppSettingsId] = s.[Id]
    WHERE s.[BusinessId] = @BusinessId;
END;
GO

-- 4. Update WhatsApp Settings
IF OBJECT_ID('dbo.sp_UpdateWhatsAppSettings', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_UpdateWhatsAppSettings;
GO

CREATE PROCEDURE dbo.sp_UpdateWhatsAppSettings
    @BusinessId INT,
    @ApiKey NVARCHAR(256) = NULL,
    @IsConnected BIT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Ensure settings row exists
    IF NOT EXISTS (SELECT 1 FROM [dbo].[WhatsAppSettings] WHERE [BusinessId] = @BusinessId)
    BEGIN
        INSERT INTO [dbo].[WhatsAppSettings] ([BusinessId], [ApiKey], [IsConnected], [UpdatedAt])
        VALUES (@BusinessId, @ApiKey, @IsConnected, GETUTCDATE());
    END
    ELSE
    BEGIN
        UPDATE [dbo].[WhatsAppSettings]
        SET [ApiKey] = @ApiKey,
            [IsConnected] = @IsConnected,
            [UpdatedAt] = GETUTCDATE()
        WHERE [BusinessId] = @BusinessId;
    END
    
    SELECT * FROM [dbo].[WhatsAppSettings] WHERE [BusinessId] = @BusinessId;
END;
GO

-- 5. Add WhatsApp Template
IF OBJECT_ID('dbo.sp_AddWhatsAppTemplate', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_AddWhatsAppTemplate;
GO

CREATE PROCEDURE dbo.sp_AddWhatsAppTemplate
    @BusinessId INT,
    @TemplateName NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @SettingsId INT;
    SELECT @SettingsId = [Id] FROM [dbo].[WhatsAppSettings] WHERE [BusinessId] = @BusinessId;
    
    IF @SettingsId IS NULL
    BEGIN
        -- Auto-create settings if not exists
        INSERT INTO [dbo].[WhatsAppSettings] ([BusinessId], [ApiKey], [IsConnected], [UpdatedAt])
        VALUES (@BusinessId, NULL, 0, GETUTCDATE());
        SET @SettingsId = SCOPE_IDENTITY();
    END
    
    INSERT INTO [dbo].[WhatsAppTemplates] ([WhatsAppSettingsId], [TemplateName])
    VALUES (@SettingsId, @TemplateName);
    
    SELECT * FROM [dbo].[WhatsAppTemplates] WHERE [Id] = SCOPE_IDENTITY();
END;
GO

-- 6. Delete WhatsApp Template
IF OBJECT_ID('dbo.sp_DeleteWhatsAppTemplate', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_DeleteWhatsAppTemplate;
GO

CREATE PROCEDURE dbo.sp_DeleteWhatsAppTemplate
    @BusinessId INT,
    @TemplateId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DELETE t
    FROM [dbo].[WhatsAppTemplates] t
    INNER JOIN [dbo].[WhatsAppSettings] s ON t.[WhatsAppSettingsId] = s.[Id]
    WHERE t.[Id] = @TemplateId AND s.[BusinessId] = @BusinessId;
    
    SELECT @@ROWCOUNT AS RowsDeleted;
END;
GO

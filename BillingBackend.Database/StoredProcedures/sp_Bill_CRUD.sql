-- ============================================================
-- Bills CRUD Stored Procedures (uses JSON for transactional bulk inserts)
-- ============================================================

-- 1. Create Bill and Line Items in a Transaction
IF OBJECT_ID('dbo.sp_CreateBill', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_CreateBill;
GO

CREATE PROCEDURE dbo.sp_CreateBill
    @BusinessId INT,
    @BranchId INT,
    @CustomerId INT,
    @CreatedByStaffId INT = NULL,
    @BillNumber NVARCHAR(50),
    @Subtotal DECIMAL(18,2),
    @DiscountCode NVARCHAR(50) = NULL,
    @DiscountAmount DECIMAL(18,2) = 0,
    @TaxAmount DECIMAL(18,2) = 0,
    @TotalAmount DECIMAL(18,2),
    @PaymentMethod NVARCHAR(20) = 'Cash',
    @Status NVARCHAR(20) = 'Pending',
    @ItemsJson NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        -- Insert Bill Header
        INSERT INTO [dbo].[Bills] (
            [BusinessId], [BranchId], [CustomerId], [CreatedByStaffId], 
            [BillNumber], [Subtotal], [DiscountCode], [DiscountAmount], 
            [TaxAmount], [TotalAmount], [PaymentMethod], [Status], [CreatedAt]
        )
        VALUES (
            @BusinessId, @BranchId, @CustomerId, @CreatedByStaffId, 
            @BillNumber, @Subtotal, @DiscountCode, @DiscountAmount, 
            @TaxAmount, @TotalAmount, @PaymentMethod, @Status, GETUTCDATE()
        );
        
        DECLARE @BillId INT = SCOPE_IDENTITY();

        -- Insert Bill Line Items using OPENJSON
        INSERT INTO [dbo].[BillItems] ([BillId], [ServiceId], [ServiceName], [UnitPrice], [Quantity], [LineTotal])
        SELECT 
            @BillId, 
            [ServiceId], 
            [ServiceName], 
            [UnitPrice], 
            [Quantity], 
            [LineTotal]
        FROM OPENJSON(@ItemsJson)
        WITH (
            [ServiceId] INT '$.serviceId',
            [ServiceName] NVARCHAR(200) '$.serviceName',
            [UnitPrice] DECIMAL(18,2) '$.unitPrice',
            [Quantity] INT '$.quantity',
            [LineTotal] DECIMAL(18,2) '$.lineTotal'
        );

        -- Update Staff stats if a staff member was associated with this bill
        IF @CreatedByStaffId IS NOT NULL AND EXISTS (SELECT 1 FROM [dbo].[StaffMembers] WHERE [Id] = @CreatedByStaffId AND [BusinessId] = @BusinessId)
        BEGIN
            UPDATE [dbo].[StaffMembers]
            SET [TotalBills] = [TotalBills] + 1,
                [RevenueGenerated] = [RevenueGenerated] + @TotalAmount,
                [UpdatedAt] = GETUTCDATE()
            WHERE [Id] = @CreatedByStaffId AND [BusinessId] = @BusinessId;
        END

        COMMIT TRANSACTION;
        
        -- Return the created Bill details
        SELECT * FROM [dbo].[Bills] WHERE [Id] = @BillId;
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- 2. Read All Bills for a Business
IF OBJECT_ID('dbo.sp_GetBillsByBusinessId', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetBillsByBusinessId;
GO

CREATE PROCEDURE dbo.sp_GetBillsByBusinessId
    @BusinessId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        b.*,
        c.[Name] AS CustomerName,
        c.[Phone] AS CustomerPhone,
        s.[Name] AS StaffName,
        br.[Name] AS BranchName
    FROM [dbo].[Bills] b
    INNER JOIN [dbo].[Customers] c ON b.[CustomerId] = c.[Id]
    INNER JOIN [dbo].[Branches] br ON b.[BranchId] = br.[Id]
    LEFT JOIN [dbo].[StaffMembers] s ON b.[CreatedByStaffId] = s.[Id]
    WHERE b.[BusinessId] = @BusinessId
    ORDER BY b.[CreatedAt] DESC;
END;
GO

-- 3. Read Single Bill (Header & Details)
IF OBJECT_ID('dbo.sp_GetBillById', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetBillById;
GO

CREATE PROCEDURE dbo.sp_GetBillById
    @BusinessId INT,
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Result 1: Bill Header details
    SELECT 
        b.*,
        c.[Name] AS CustomerName,
        c.[Phone] AS CustomerPhone,
        c.[Email] AS CustomerEmail,
        s.[Name] AS StaffName,
        br.[Name] AS BranchName
    FROM [dbo].[Bills] b
    INNER JOIN [dbo].[Customers] c ON b.[CustomerId] = c.[Id]
    INNER JOIN [dbo].[Branches] br ON b.[BranchId] = br.[Id]
    LEFT JOIN [dbo].[StaffMembers] s ON b.[CreatedByStaffId] = s.[Id]
    WHERE b.[Id] = @Id AND b.[BusinessId] = @BusinessId;
    
    -- Result 2: Bill Line Items
    SELECT 
        bi.*
    FROM [dbo].[BillItems] bi
    INNER JOIN [dbo].[Bills] b ON bi.[BillId] = b.[Id]
    WHERE b.[Id] = @Id AND b.[BusinessId] = @BusinessId;
END;
GO

-- 4. Delete Bill (automatically cascades to BillItems if configured, otherwise manually delete)
IF OBJECT_ID('dbo.sp_DeleteBill', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_DeleteBill;
GO

CREATE PROCEDURE dbo.sp_DeleteBill
    @BusinessId INT,
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRANSACTION;
    BEGIN TRY
        -- Adjust staff stats back down
        DECLARE @CreatedByStaffId INT;
        DECLARE @TotalAmount DECIMAL(18,2);
        
        SELECT 
            @CreatedByStaffId = [CreatedByStaffId], 
            @TotalAmount = [TotalAmount]
        FROM [dbo].[Bills]
        WHERE [Id] = @Id AND [BusinessId] = @BusinessId;
        
        -- Delete Bill Items (cascade handles this if FK ON DELETE CASCADE is there, but let's be safe)
        DELETE FROM [dbo].[BillItems] WHERE [BillId] = @Id;

        -- Delete Bill
        DELETE FROM [dbo].[Bills] WHERE [Id] = @Id AND [BusinessId] = @BusinessId;
        DECLARE @Deleted INT = @@ROWCOUNT;

        -- Revert staff stats
        IF @Deleted > 0 AND @CreatedByStaffId IS NOT NULL
        BEGIN
            UPDATE [dbo].[StaffMembers]
            SET [TotalBills] = CASE WHEN [TotalBills] > 0 THEN [TotalBills] - 1 ELSE 0 END,
                [RevenueGenerated] = CASE WHEN [RevenueGenerated] >= @TotalAmount THEN [RevenueGenerated] - @TotalAmount ELSE 0.00 END,
                [UpdatedAt] = GETUTCDATE()
            WHERE [Id] = @CreatedByStaffId AND [BusinessId] = @BusinessId;
        END

        COMMIT TRANSACTION;
        SELECT @Deleted AS RowsDeleted;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

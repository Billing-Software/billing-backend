-- ============================================================
-- StaffMembers CRUD Stored Procedures
-- ============================================================

-- 1. Create StaffMember
IF OBJECT_ID('dbo.sp_CreateStaffMember', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_CreateStaffMember;
GO

CREATE PROCEDURE dbo.sp_CreateStaffMember
    @BusinessId INT,
    @UserId INT = NULL,
    @Name NVARCHAR(200),
    @EmpCode NVARCHAR(50),
    @Contact NVARCHAR(256) = NULL,
    @Role NVARCHAR(50),
    @Status NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO [dbo].[StaffMembers] (
        [BusinessId], [UserId], [Name], [EmpCode], [Contact], [Role], [TotalBills], [RevenueGenerated], [Status], [CreatedAt]
    )
    VALUES (
        @BusinessId, @UserId, @Name, @EmpCode, @Contact, ISNULL(@Role, 'Staff'), 0, 0.00, ISNULL(@Status, 'Active'), GETUTCDATE()
    );
    
    SELECT * FROM [dbo].[StaffMembers] WHERE [Id] = SCOPE_IDENTITY();
END;
GO

-- 2. Read All StaffMembers for a Business
IF OBJECT_ID('dbo.sp_GetStaffMembersByBusinessId', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetStaffMembersByBusinessId;
GO

CREATE PROCEDURE dbo.sp_GetStaffMembersByBusinessId
    @BusinessId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT * FROM [dbo].[StaffMembers] 
    WHERE [BusinessId] = @BusinessId
    ORDER BY [Name];
END;
GO

-- 3. Read Single StaffMember
IF OBJECT_ID('dbo.sp_GetStaffMemberById', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetStaffMemberById;
GO

CREATE PROCEDURE dbo.sp_GetStaffMemberById
    @BusinessId INT,
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT * FROM [dbo].[StaffMembers] 
    WHERE [Id] = @Id AND [BusinessId] = @BusinessId;
END;
GO

-- 4. Update StaffMember
IF OBJECT_ID('dbo.sp_UpdateStaffMember', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_UpdateStaffMember;
GO

CREATE PROCEDURE dbo.sp_UpdateStaffMember
    @BusinessId INT,
    @Id INT,
    @UserId INT = NULL,
    @Name NVARCHAR(200),
    @EmpCode NVARCHAR(50),
    @Contact NVARCHAR(256) = NULL,
    @Role NVARCHAR(50),
    @Status NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE [dbo].[StaffMembers]
    SET [UserId] = @UserId,
        [Name] = @Name,
        [EmpCode] = @EmpCode,
        [Contact] = @Contact,
        [Role] = @Role,
        [Status] = @Status,
        [UpdatedAt] = GETUTCDATE()
    WHERE [Id] = @Id AND [BusinessId] = @BusinessId;
    
    SELECT * FROM [dbo].[StaffMembers] WHERE [Id] = @Id AND [BusinessId] = @BusinessId;
END;
GO

-- 5. Delete StaffMember
IF OBJECT_ID('dbo.sp_DeleteStaffMember', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_DeleteStaffMember;
GO

CREATE PROCEDURE dbo.sp_DeleteStaffMember
    @BusinessId INT,
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DELETE FROM [dbo].[StaffMembers]
    WHERE [Id] = @Id AND [BusinessId] = @BusinessId;
    
    SELECT @@ROWCOUNT AS RowsDeleted;
END;
GO

-- stored-procedures.sql
-- Stored procedures for Expense Management System
-- Run by run-sql-stored-procs.py
-- Uses CREATE OR ALTER PROCEDURE to be idempotent

SET NOCOUNT ON;
GO

-- =============================================
-- GetAllExpenses: Returns all expenses with joined data
-- =============================================
CREATE OR ALTER PROCEDURE dbo.GetAllExpenses
    @StatusId INT = NULL,
    @UserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        e.ExpenseId,
        e.UserId,
        u.UserName,
        u.Email,
        e.CategoryId,
        c.CategoryName,
        e.StatusId,
        s.StatusName,
        e.AmountMinor,
        e.Currency,
        e.ExpenseDate,
        e.Description,
        e.ReceiptFile,
        e.SubmittedAt,
        e.ReviewedBy,
        rb.UserName AS ReviewedByName,
        e.ReviewedAt,
        e.CreatedAt
    FROM dbo.Expenses e
    JOIN dbo.Users u ON e.UserId = u.UserId
    JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    LEFT JOIN dbo.Users rb ON e.ReviewedBy = rb.UserId
    WHERE (@StatusId IS NULL OR e.StatusId = @StatusId)
      AND (@UserId IS NULL OR e.UserId = @UserId)
    ORDER BY e.CreatedAt DESC;
END
GO

-- =============================================
-- GetExpenseById: Returns single expense by ID
-- =============================================
CREATE OR ALTER PROCEDURE dbo.GetExpenseById
    @ExpenseId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        e.ExpenseId,
        e.UserId,
        u.UserName,
        u.Email,
        e.CategoryId,
        c.CategoryName,
        e.StatusId,
        s.StatusName,
        e.AmountMinor,
        e.Currency,
        e.ExpenseDate,
        e.Description,
        e.ReceiptFile,
        e.SubmittedAt,
        e.ReviewedBy,
        rb.UserName AS ReviewedByName,
        e.ReviewedAt,
        e.CreatedAt
    FROM dbo.Expenses e
    JOIN dbo.Users u ON e.UserId = u.UserId
    JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    LEFT JOIN dbo.Users rb ON e.ReviewedBy = rb.UserId
    WHERE e.ExpenseId = @ExpenseId;
END
GO

-- =============================================
-- CreateExpense: Creates a new expense record
-- =============================================
CREATE OR ALTER PROCEDURE dbo.CreateExpense
    @UserId INT,
    @CategoryId INT,
    @AmountMinor INT,
    @Currency NVARCHAR(3) = 'GBP',
    @ExpenseDate DATE,
    @Description NVARCHAR(1000) = NULL,
    @ReceiptFile NVARCHAR(500) = NULL,
    @StatusId INT = 1   -- Default: Draft
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Expenses (UserId, CategoryId, StatusId, AmountMinor, Currency, ExpenseDate, Description, ReceiptFile, CreatedAt)
    VALUES (@UserId, @CategoryId, @StatusId, @AmountMinor, @Currency, @ExpenseDate, @Description, @ReceiptFile, SYSUTCDATETIME());
    SELECT SCOPE_IDENTITY() AS ExpenseId;
END
GO

-- =============================================
-- UpdateExpense: Updates an existing expense
-- =============================================
CREATE OR ALTER PROCEDURE dbo.UpdateExpense
    @ExpenseId INT,
    @CategoryId INT,
    @AmountMinor INT,
    @Currency NVARCHAR(3) = 'GBP',
    @ExpenseDate DATE,
    @Description NVARCHAR(1000) = NULL,
    @ReceiptFile NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Expenses
    SET CategoryId = @CategoryId,
        AmountMinor = @AmountMinor,
        Currency = @Currency,
        ExpenseDate = @ExpenseDate,
        Description = @Description,
        ReceiptFile = @ReceiptFile
    WHERE ExpenseId = @ExpenseId;
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- =============================================
-- SubmitExpense: Marks expense as submitted
-- =============================================
CREATE OR ALTER PROCEDURE dbo.SubmitExpense
    @ExpenseId INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @SubmittedStatusId INT;
    SELECT @SubmittedStatusId = StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Submitted';
    UPDATE dbo.Expenses
    SET StatusId = @SubmittedStatusId,
        SubmittedAt = SYSUTCDATETIME()
    WHERE ExpenseId = @ExpenseId;
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- =============================================
-- ApproveExpense: Approves an expense
-- =============================================
CREATE OR ALTER PROCEDURE dbo.ApproveExpense
    @ExpenseId INT,
    @ReviewedBy INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @ApprovedStatusId INT;
    SELECT @ApprovedStatusId = StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Approved';
    UPDATE dbo.Expenses
    SET StatusId = @ApprovedStatusId,
        ReviewedBy = @ReviewedBy,
        ReviewedAt = SYSUTCDATETIME()
    WHERE ExpenseId = @ExpenseId;
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- =============================================
-- RejectExpense: Rejects an expense
-- =============================================
CREATE OR ALTER PROCEDURE dbo.RejectExpense
    @ExpenseId INT,
    @ReviewedBy INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @RejectedStatusId INT;
    SELECT @RejectedStatusId = StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Rejected';
    UPDATE dbo.Expenses
    SET StatusId = @RejectedStatusId,
        ReviewedBy = @ReviewedBy,
        ReviewedAt = SYSUTCDATETIME()
    WHERE ExpenseId = @ExpenseId;
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- =============================================
-- DeleteExpense: Deletes a draft expense
-- =============================================
CREATE OR ALTER PROCEDURE dbo.DeleteExpense
    @ExpenseId INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.Expenses WHERE ExpenseId = @ExpenseId;
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- =============================================
-- GetAllUsers: Returns all active users
-- =============================================
CREATE OR ALTER PROCEDURE dbo.GetAllUsers
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        u.UserId,
        u.UserName,
        u.Email,
        u.RoleId,
        r.RoleName,
        u.ManagerId,
        m.UserName AS ManagerName,
        u.IsActive,
        u.CreatedAt
    FROM dbo.Users u
    JOIN dbo.Roles r ON u.RoleId = r.RoleId
    LEFT JOIN dbo.Users m ON u.ManagerId = m.UserId
    WHERE u.IsActive = 1
    ORDER BY u.UserName;
END
GO

-- =============================================
-- GetAllCategories: Returns all active categories
-- =============================================
CREATE OR ALTER PROCEDURE dbo.GetAllCategories
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CategoryId, CategoryName, IsActive
    FROM dbo.ExpenseCategories
    WHERE IsActive = 1
    ORDER BY CategoryName;
END
GO

-- =============================================
-- GetAllStatuses: Returns all expense statuses
-- =============================================
CREATE OR ALTER PROCEDURE dbo.GetAllStatuses
AS
BEGIN
    SET NOCOUNT ON;
    SELECT StatusId, StatusName
    FROM dbo.ExpenseStatus
    ORDER BY StatusId;
END
GO

-- =============================================
-- GetExpensesSummary: Returns summary stats
-- =============================================
CREATE OR ALTER PROCEDURE dbo.GetExpensesSummary
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        s.StatusName,
        COUNT(e.ExpenseId) AS Count,
        ISNULL(SUM(e.AmountMinor), 0) AS TotalAmountMinor
    FROM dbo.ExpenseStatus s
    LEFT JOIN dbo.Expenses e ON e.StatusId = s.StatusId
    GROUP BY s.StatusId, s.StatusName
    ORDER BY s.StatusId;
END
GO

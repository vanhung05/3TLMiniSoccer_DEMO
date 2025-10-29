-- =====================================================
-- 3TL MINI SOCCER MANAGEMENT SYSTEM - DATABASE SCHEMA
-- Version: 2.0 (Dynamic Booking Support)
-- Created: 2024
-- =====================================================

CREATE DATABASE MiniSoccerManagement;
GO

USE MiniSoccerManagement;
GO

-- =====================================================
-- 1. BẢNG NGƯỜI DÙNG & PHÂN QUYỀN
-- =====================================================

-- Bảng vai trò
CREATE TABLE Roles (
    RoleId INT IDENTITY(1,1) PRIMARY KEY,
    RoleName NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(200),
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE()
);

-- Bảng người dùng
CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    PhoneNumber NVARCHAR(20),
    DateOfBirth DATE,
    Gender NVARCHAR(10),
    Avatar NVARCHAR(255),
    Address NVARCHAR(200),
    RoleId INT NOT NULL DEFAULT 3, -- Mặc định là User 
    IsActive BIT DEFAULT 1,
    EmailConfirmed BIT DEFAULT 0,
    PhoneConfirmed BIT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    LastLoginAt DATETIME2,
    AssignedAt DATETIME2 DEFAULT GETDATE(),
    AssignedBy INT,
    FOREIGN KEY (RoleId) REFERENCES Roles(RoleId),
    FOREIGN KEY (AssignedBy) REFERENCES Users(UserId)
);

-- Bảng xác thực xã hội
CREATE TABLE SocialLogins (
    SocialLoginId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    Provider NVARCHAR(50) NOT NULL, -- Google, Facebook
    ProviderKey NVARCHAR(255) NOT NULL,
    ProviderDisplayName NVARCHAR(100),
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE,
    UNIQUE(Provider, ProviderKey)
);

-- =====================================================
-- 2. DANH MỤC SÂN & LỊCH (DYNAMIC BOOKING SUPPORT)
-- =====================================================

-- Bảng loại sân
CREATE TABLE FieldTypes (
    FieldTypeId INT IDENTITY(1,1) PRIMARY KEY,
    TypeName NVARCHAR(50) NOT NULL,
    Description NVARCHAR(200),
    PlayerCount INT NOT NULL,
    BasePrice DECIMAL(10,2) NOT NULL,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE()
);

-- Bảng sân bóng
CREATE TABLE Fields (
    FieldId INT IDENTITY(1,1) PRIMARY KEY,
    FieldName NVARCHAR(100) NOT NULL,
    FieldTypeId INT NOT NULL,
    Location NVARCHAR(200) NOT NULL,
    Status NVARCHAR(20) DEFAULT 'Active',
    Description NVARCHAR(500),
    ImageUrl NVARCHAR(255),
    -- Dynamic booking support - Operating hours
    OpeningTime TIME NOT NULL DEFAULT '06:00:00',
    ClosingTime TIME NOT NULL DEFAULT '23:00:00',
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (FieldTypeId) REFERENCES FieldTypes(FieldTypeId)
);


-- Bảng lịch sân (Dynamic booking support)
CREATE TABLE FieldSchedules (
    ScheduleId INT IDENTITY(1,1) PRIMARY KEY,
    FieldId INT NOT NULL,
    Date DATE NOT NULL,
    -- Dynamic time support
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,
    Status NVARCHAR(20) DEFAULT 'Available', -- Available, Booked, Maintenance, Closed
    BookingId INT NULL,
    Notes NVARCHAR(500),
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (FieldId) REFERENCES Fields(FieldId),
    FOREIGN KEY (BookingId) REFERENCES Bookings(BookingId)
);

-- Bảng quy tắc giá (theo giờ và ngày trong tuần)
CREATE TABLE PricingRules (
    PricingRuleId INT IDENTITY(1,1) PRIMARY KEY,
    FieldTypeId INT NOT NULL,
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,
    DayOfWeek INT NOT NULL, -- 1=Monday, 7=Sunday
    Price DECIMAL(10,2) NOT NULL,
    IsPeakHour BIT DEFAULT 0,
    PeakMultiplier DECIMAL(3,2) DEFAULT 1.0,
    EffectiveFrom DATETIME2 NOT NULL,
    EffectiveTo DATETIME2 NULL,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (FieldTypeId) REFERENCES FieldTypes(FieldTypeId)
);

-- =====================================================
-- 3. QUẢN LÝ ĐẶT SÂN (DYNAMIC BOOKING SUPPORT)
-- =====================================================

-- Bảng đặt sân
CREATE TABLE Bookings (
    BookingId INT IDENTITY(1,1) PRIMARY KEY,
    BookingCode NVARCHAR(20) NOT NULL UNIQUE,
    UserId INT NULL, -- NULL for guest bookings
    GuestName NVARCHAR(100),
    GuestPhone NVARCHAR(20),
    GuestEmail NVARCHAR(100),
    FieldId INT NOT NULL,
    BookingDate DATE NOT NULL,
    -- Dynamic time support
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,
    Duration INT NOT NULL DEFAULT 60, -- Duration in minutes
    Status NVARCHAR(20) DEFAULT 'Pending', -- Pending, Confirmed, Cancelled, Completed
    TotalPrice DECIMAL(10,2) NOT NULL,
    PaymentStatus NVARCHAR(20) DEFAULT 'Pending', -- Pending, Paid, Failed, Refunded
    PaymentMethod NVARCHAR(50),
    PaymentReference NVARCHAR(100),
    Notes NVARCHAR(500),
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    ConfirmedAt DATETIME2 NULL,
    ConfirmedBy INT NULL,
    CancelledAt DATETIME2 NULL,
    CancelledBy INT NULL,
    FOREIGN KEY (UserId) REFERENCES Users(UserId),
    FOREIGN KEY (FieldId) REFERENCES Fields(FieldId),
    FOREIGN KEY (ConfirmedBy) REFERENCES Users(UserId),
    FOREIGN KEY (CancelledBy) REFERENCES Users(UserId)
);

-- Bảng phiên đặt sân
CREATE TABLE BookingSessions (
    SessionId INT IDENTITY(1,1) PRIMARY KEY,
    BookingId INT NOT NULL,
    CheckInTime DATETIME2 NULL,           -- Thời gian check-in thực tế
    CheckOutTime DATETIME2 NULL,          -- Thời gian check-out thực tế
    StaffId INT NULL,                     -- Nhân viên check-in/out
    Status NVARCHAR(20) NOT NULL DEFAULT 'Active',
    Notes NVARCHAR(500) NULL,             -- Ghi chú
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (BookingId) REFERENCES Bookings(BookingId) ON DELETE CASCADE,
    FOREIGN KEY (StaffId) REFERENCES Users(UserId)
);

-- Bảng các đơn hàng trong phiên thuê
CREATE TABLE SessionOrders (
    SessionOrderId INT IDENTITY(1,1) PRIMARY KEY,
    SessionId INT NOT NULL,
    OrderId INT NULL,                     -- NULL nếu thanh toán ngay
    PaymentType NVARCHAR(20) NOT NULL,   -- 'Immediate' hoặc 'Consolidated'
    TotalAmount DECIMAL(10,2) NOT NULL,
    PaymentStatus NVARCHAR(20) DEFAULT 'Pending',
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (SessionId) REFERENCES BookingSessions(SessionId),
    FOREIGN KEY (OrderId) REFERENCES Orders(OrderId)
);

CREATE TABLE SessionOrderItems (
    SessionOrderItemId INT IDENTITY(1,1) PRIMARY KEY,
    SessionOrderId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(10,2) NOT NULL,
    TotalPrice DECIMAL(10,2) NOT NULL,
    FOREIGN KEY (SessionOrderId) REFERENCES SessionOrders(SessionOrderId),
    FOREIGN KEY (ProductId) REFERENCES Products(ProductId)
);

-- =====================================================
-- 4. QUẢN LÝ THANH TOÁN
-- =====================================================

-- Bảng phương thức thanh toán
CREATE TABLE PaymentMethods (
    PaymentMethodId INT IDENTITY(1,1) PRIMARY KEY,
    MethodName NVARCHAR(50) NOT NULL,
    MethodType NVARCHAR(20) NOT NULL, -- SEPay, VietQR, BankTransfer, Cash
    IsActive BIT DEFAULT 1,
    ConfigData NVARCHAR(MAX), -- JSON configuration
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE()
);

-- Bảng giao dịch
CREATE TABLE Transactions (
    TransactionId INT IDENTITY(1,1) PRIMARY KEY,
    BookingId INT NOT NULL,
    PaymentMethodId INT NOT NULL,
    Amount DECIMAL(10,2) NOT NULL,
    TransactionCode NVARCHAR(100),
    Status NVARCHAR(20) DEFAULT 'Pending', -- Pending, Success, Failed, Cancelled
    PaymentData NVARCHAR(MAX), -- JSON data from payment gateway
    ProcessedAt DATETIME2 NULL,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (BookingId) REFERENCES Bookings(BookingId),
    FOREIGN KEY (PaymentMethodId) REFERENCES PaymentMethods(PaymentMethodId)
);

-- Bảng đơn hàng thanh toán
CREATE TABLE PaymentOrders (
    PaymentOrderId INT IDENTITY(1,1) PRIMARY KEY,
    OrderCode NVARCHAR(50) NOT NULL UNIQUE,
    BookingId INT NOT NULL,
    UserId INT NOT NULL,
    Amount DECIMAL(10,2) NOT NULL,
    Status NVARCHAR(20) DEFAULT 'Pending', -- Pending, Success, Failed, Cancelled
    PaymentMethod NVARCHAR(50) NOT NULL, -- SEPay, VietQR, BankTransfer, Cash
    ExpiredAt DATETIME2 NULL,
    ProcessedAt DATETIME2 NULL,
    PaymentReference NVARCHAR(100),
    PaymentData NVARCHAR(MAX), -- JSON data from payment gateway
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (BookingId) REFERENCES Bookings(BookingId),
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

-- =====================================================
-- 5. QUẢN LÝ SẢN PHẨM & ĐỐN HÀNG
-- =====================================================

-- Bảng sản phẩm
CREATE TABLE Products (
    ProductId INT IDENTITY(1,1) PRIMARY KEY,
    ProductName NVARCHAR(200) NOT NULL,
    Description NVARCHAR(500),
    Price DECIMAL(10,2) NOT NULL,
    ProductTypeId INT,
    ImageUrl NVARCHAR(255),
    IsAvailable BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    CreatedBy INT NOT NULL,
    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId),
    FOREIGN KEY (ProductTypeId) REFERENCES ProductTypes(ProductTypeId)
);

CREATE TABLE ProductTypes (
    ProductTypeId INT IDENTITY(1,1) PRIMARY KEY,
    TypeName NVARCHAR(50) NOT NULL UNIQUE,  -- Tên loại: Food, Drink, Equipment, v.v.
    Description NVARCHAR(200),              -- Mô tả loại (ví dụ: "Thực phẩm và đồ ăn nhẹ")
    IsActive BIT DEFAULT 1,                 
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE()
);

-- Bảng đơn hàng
CREATE TABLE Orders (
    OrderId INT IDENTITY(1,1) PRIMARY KEY,
    OrderCode NVARCHAR(20) NOT NULL UNIQUE,
    BookingId INT NOT NULL,
    UserId INT NOT NULL,
    TotalAmount DECIMAL(10,2) NOT NULL,
    Status NVARCHAR(20) DEFAULT 'Pending', -- Pending, Confirmed, Shipped, Delivered, Cancelled
    PaymentStatus NVARCHAR(20) DEFAULT 'Pending', -- Pending, Paid, Failed, Refunded
    Notes NVARCHAR(500),
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (BookingId) REFERENCES Bookings(BookingId),
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

-- Bảng chi tiết đơn hàng
CREATE TABLE OrderItems (
    OrderItemId INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(10,2) NOT NULL,
    TotalPrice DECIMAL(10,2) NOT NULL,
    FOREIGN KEY (OrderId) REFERENCES Orders(OrderId) ON DELETE CASCADE,
    FOREIGN KEY (ProductId) REFERENCES Products(ProductId)
);

-- =====================================================
-- 6. QUẢN LÝ GIẢM GIÁ & LOYALTY
-- =====================================================

-- Bảng mã giảm giá
CREATE TABLE DiscountCodes (
    DiscountCodeId INT IDENTITY(1,1) PRIMARY KEY,
    Code NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(200),
    DiscountType NVARCHAR(20) NOT NULL, -- Percentage, FixedAmount
    DiscountValue DECIMAL(10,2) NOT NULL,
    MinOrderAmount DECIMAL(10,2) DEFAULT 0,
    MaxDiscountAmount DECIMAL(10,2) NULL,
    UsageLimit INT NULL, -- NULL = unlimited
    UsedCount INT DEFAULT 0,
    ValidFrom DATETIME2 NOT NULL,
    ValidTo DATETIME2 NOT NULL,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    CreatedBy INT NOT NULL,
    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
);

-- Bảng sử dụng mã giảm giá
CREATE TABLE DiscountCodeUsages (
    UsageId INT IDENTITY(1,1) PRIMARY KEY,
    DiscountCodeId INT NOT NULL,
    UserId INT NOT NULL,
    BookingId INT NOT NULL,
    DiscountAmount DECIMAL(10,2) NOT NULL,
    UsedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (DiscountCodeId) REFERENCES DiscountCodes(DiscountCodeId),
    FOREIGN KEY (UserId) REFERENCES Users(UserId),
    FOREIGN KEY (BookingId) REFERENCES Bookings(BookingId)
);

-- Bảng điểm thưởng
CREATE TABLE LoyaltyPoints (
    PointId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    Points INT NOT NULL,
    PointType NVARCHAR(20) NOT NULL, -- Earned, Redeemed, Expired
    Description NVARCHAR(200),
    BookingId INT NULL,
    ExpiredAt DATETIME2 NULL,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId),
    FOREIGN KEY (BookingId) REFERENCES Bookings(BookingId)
);

-- =====================================================
-- 7. QUẢN LÝ THÔNG BÁO & LIÊN HỆ
-- =====================================================

-- Bảng thông báo
CREATE TABLE Notifications (
    NotificationId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Content NVARCHAR(500) NOT NULL,
    Type NVARCHAR(50) NOT NULL, -- Booking, Payment, System, Promotion
    IsRead BIT DEFAULT 0,
    ReadAt DATETIME2 NULL,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

-- Bảng liên hệ
CREATE TABLE Contacts (
    ContactId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    Phone NVARCHAR(20),
    Subject NVARCHAR(200) NOT NULL,
    Message NVARCHAR(1000) NOT NULL,
    Status NVARCHAR(20) DEFAULT 'New', -- New, Read, Replied, Closed
    RepliedAt DATETIME2 NULL,
    RepliedBy INT NULL,
    ReplyMessage NVARCHAR(1000),
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (RepliedBy) REFERENCES Users(UserId)
);

-- =====================================================
-- 8. CẤU HÌNH HỆ THỐNG
-- =====================================================

-- Bảng cấu hình hệ thống
CREATE TABLE SystemConfigs (
    ConfigId INT IDENTITY(1,1) PRIMARY KEY,
    ConfigKey NVARCHAR(100) NOT NULL UNIQUE,
    ConfigValue NVARCHAR(MAX) NOT NULL,
    Description NVARCHAR(500),
    ConfigType NVARCHAR(50) NOT NULL, -- SEPay, System, General, Email, SMS
    DataType NVARCHAR(50) DEFAULT 'String', -- String, Number, Boolean, JSON
    IsActive BIT DEFAULT 1,
    IsEncrypted BIT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedBy INT NULL,
    FOREIGN KEY (UpdatedBy) REFERENCES Users(UserId)
);

-- Bảng giỏ hàng
CREATE TABLE CartItems (
    CartItemId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL DEFAULT 1,
    AddedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE,
    FOREIGN KEY (ProductId) REFERENCES Products(ProductId) ON DELETE CASCADE,
    UNIQUE(UserId, ProductId)
);

-- Bảng audit log
CREATE TABLE AuditLogs (
    LogId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NULL,
    Action NVARCHAR(100) NOT NULL,
    TableName NVARCHAR(100) NOT NULL,
    RecordId INT NOT NULL,
    OldValues NVARCHAR(MAX),
    NewValues NVARCHAR(MAX),
    IpAddress NVARCHAR(45),
    UserAgent NVARCHAR(500),
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

-- =====================================================
-- 9. STORED PROCEDURES FOR DYNAMIC BOOKING
-- =====================================================

-- SP kiểm tra overlap và tính giá
CREATE PROCEDURE sp_CheckBookingOverlap
    @FieldId INT,
    @BookingDate DATE,
    @StartTime TIME,
    @EndTime TIME,
    @ExcludeBookingId INT = NULL
AS
BEGIN

    SET NOCOUNT ON;
    
    DECLARE @OverlapCount INT = 0;
    DECLARE @OverlapMessage NVARCHAR(500) = '';
    DECLARE @IsAvailable BIT = 1;
    
    -- Kiểm tra overlap với các booking đã xác nhận
    SELECT @OverlapCount = COUNT(*)
    FROM Bookings b
    WHERE b.FieldId = @FieldId 
        AND b.BookingDate = @BookingDate
        AND b.Status IN ('Confirmed', 'Playing')
        AND (@ExcludeBookingId IS NULL OR b.BookingId != @ExcludeBookingId)
        AND b.StartTime < @EndTime 
        AND b.EndTime > @StartTime;
    
    IF @OverlapCount > 0
    BEGIN
        SET @IsAvailable = 0;
        SET @OverlapMessage = N'Khoảng thời gian ' + 
            FORMAT(@StartTime, 'HH:mm') + ' - ' + 
            FORMAT(@EndTime, 'HH:mm') + 
            N' đã được đặt bởi ' + CAST(@OverlapCount AS NVARCHAR(10)) + N' khách hàng khác.';
    END
    
    -- Kiểm tra thời gian hoạt động của sân
    DECLARE @FieldOpeningTime TIME, @FieldClosingTime TIME;
    SELECT @FieldOpeningTime = OpeningTime, @FieldClosingTime = ClosingTime
    FROM Fields WHERE FieldId = @FieldId;
    
    IF @StartTime < @FieldOpeningTime OR @EndTime > @FieldClosingTime
    BEGIN
        SET @IsAvailable = 0;
        SET @OverlapMessage = N'Thời gian đặt sân ngoài giờ hoạt động (' + 
            FORMAT(@FieldOpeningTime, 'HH:mm') + ' - ' + 
            FORMAT(@FieldClosingTime, 'HH:mm') + ').';
    END
    
    SELECT 
        @IsAvailable AS IsAvailable,
        @OverlapMessage AS Message,
        @OverlapCount AS OverlapCount;
END
GO


-- Stored procedure để tạo dynamic booking
CREATE PROCEDURE sp_CreateDynamicBooking
    @BookingCode NVARCHAR(20),
    @UserId INT = NULL,
    @GuestName NVARCHAR(100) = NULL,
    @GuestPhone NVARCHAR(20) = NULL,
    @GuestEmail NVARCHAR(100) = NULL,
    @FieldId INT,
    @BookingDate DATE,
    @StartTime TIME,
    @EndTime TIME,
    @Duration INT,
    @TotalPrice DECIMAL(10,2),
    @Notes NVARCHAR(500) = NULL,
    @BookingId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRANSACTION;
    
    BEGIN TRY
        -- Tạo booking
        INSERT INTO Bookings (
            BookingCode, UserId, GuestName, GuestPhone, GuestEmail,
            FieldId, BookingDate, StartTime, EndTime, Duration,
            TotalPrice, Notes
        )
        VALUES (
            @BookingCode, @UserId, @GuestName, @GuestPhone, @GuestEmail,
            @FieldId, @BookingDate, @StartTime, @EndTime, @Duration,
            @TotalPrice, @Notes
        );
        
        SET @BookingId = SCOPE_IDENTITY();
        
        -- Tạo field schedule entry
        INSERT INTO FieldSchedules (
            FieldId, Date, StartTime, EndTime, Status, BookingId
        )
        VALUES (
            @FieldId, @BookingDate, @StartTime, @EndTime, 'Booked', @BookingId
        );
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END
GO

-- SP MỚI: Check availability (trả gợi ý start time trống)
CREATE PROCEDURE sp_CheckDynamicAvailability
    @FieldId INT,
    @BookingDate DATE,
    @MaxDuration INT = 120
AS
BEGIN
    WITH AllIntervals AS (
        SELECT 'Opening' AS Type, OpeningTime AS StartTime, OpeningTime AS EndTime 
        FROM Fields 
        WHERE FieldId = @FieldId
        UNION ALL
        SELECT 'Booking', StartTime, EndTime 
        FROM Bookings 
        WHERE FieldId = @FieldId AND BookingDate = @BookingDate AND Status IN (N'Confirmed', N'Playing')
        UNION ALL
        SELECT 'Closing', ClosingTime, ClosingTime 
        FROM Fields 
        WHERE FieldId = @FieldId
    ),
    Sorted AS (
        SELECT StartTime, EndTime,
               LAG(EndTime) OVER (ORDER BY StartTime) AS PrevEnd
        FROM AllIntervals
    ),
    PotentialGaps AS (
        SELECT PrevEnd AS GapStart, StartTime AS GapEnd, 
               DATEDIFF(MINUTE, CAST(PrevEnd AS DATETIME), CAST(StartTime AS DATETIME)) AS GapMinutes
        FROM Sorted
        WHERE PrevEnd IS NOT NULL
    )
    SELECT GapStart AS SuggestedStart, 
           CAST(DATEADD(MINUTE, @MaxDuration, CAST(GapStart AS DATETIME)) AS TIME) AS SuggestedEnd
    FROM PotentialGaps
    WHERE GapMinutes >= @MaxDuration
    ORDER BY GapStart;
END
GO

-- =====================================================
-- 10. INDEXES FOR PERFORMANCE
-- =====================================================

-- Indexes for Users table
CREATE INDEX IX_Users_Email ON Users(Email);
CREATE INDEX IX_Users_Username ON Users(Username);
CREATE INDEX IX_Users_RoleId ON Users(RoleId);

-- Indexes for Bookings table
CREATE INDEX IX_Bookings_FieldId_Date ON Bookings(FieldId, BookingDate);
CREATE INDEX IX_Bookings_UserId ON Bookings(UserId);
CREATE INDEX IX_Bookings_Status ON Bookings(Status);
CREATE INDEX IX_Bookings_StartTime_EndTime ON Bookings(StartTime, EndTime);

-- Indexes for FieldSchedules table
CREATE INDEX IX_FieldSchedules_FieldId_Date ON FieldSchedules(FieldId, Date);
CREATE INDEX IX_FieldSchedules_StartTime_EndTime ON FieldSchedules(StartTime, EndTime);

-- Indexes for Transactions table
CREATE INDEX IX_Transactions_BookingId ON Transactions(BookingId);
CREATE INDEX IX_Transactions_Status ON Transactions(Status);

-- Indexes for Notifications table
CREATE INDEX IX_Notifications_UserId ON Notifications(UserId);
CREATE INDEX IX_Notifications_IsRead ON Notifications(IsRead);

CREATE INDEX IX_Products_ProductTypeId ON Products(ProductTypeId);

-- =====================================================
-- 11. SAMPLE DATA
-- =====================================================

-- Insert roles
INSERT INTO Roles (RoleName, Description) VALUES
(N'Admin', N'Quản trị viên hệ thống'),
(N'Staff', N'Nhân viên sân bóng'),
(N'User', N'Người dùng đã đăng ký'),
(N'Guest', N'Khách hàng không đăng ký');

-- Insert field types
INSERT INTO FieldTypes (TypeName, Description, PlayerCount, BasePrice) VALUES
(N'5v5', N'Sân 5 người mỗi đội', 10, 1000),
(N'7v7', N'Sân 7 người mỗi đội', 14, 2000)


-- Insert sample fields
INSERT INTO Fields (FieldName, FieldTypeId, Location, OpeningTime, ClosingTime) VALUES
('Sân A1', 1, N'Khu vực A - Tầng 1', '06:00:00', '23:00:00'),
('Sân A2', 1, N'Khu vực A - Tầng 1', '06:00:00', '23:00:00'),
('Sân B1', 2, N'Khu vực B - Tầng 2', '06:00:00', '23:00:00'),
('Sân B2', 2, N'Khu vực B - Tầng 2', '06:00:00', '23:00:00')

-- Insert pricing rules (theo giờ và ngày trong tuần)
INSERT INTO PricingRules (FieldTypeId, StartTime, EndTime, DayOfWeek, Price, IsPeakHour, EffectiveFrom) VALUES
-- Sân 5 người - Giờ thường (6h-17h)
(1, '06:00:00', '17:00:00', 1, 150000, 0, '2024-01-01'), -- Thứ 2
(1, '06:00:00', '17:00:00', 2, 150000, 0, '2024-01-01'), -- Thứ 3
(1, '06:00:00', '17:00:00', 3, 150000, 0, '2024-01-01'), -- Thứ 4
(1, '06:00:00', '17:00:00', 4, 150000, 0, '2024-01-01'), -- Thứ 5
(1, '06:00:00', '17:00:00', 5, 150000, 0, '2024-01-01'), -- Thứ 6
(1, '06:00:00', '17:00:00', 6, 200000, 1, '2024-01-01'), -- Thứ 7 (peak)
(1, '06:00:00', '17:00:00', 7, 200000, 1, '2024-01-01'), -- Chủ nhật (peak)
-- Sân 5 người - Giờ cao điểm (17h-21h)
(1, '17:00:00', '21:00:00', 1, 250000, 1, '2024-01-01'), -- Thứ 2
(1, '17:00:00', '21:00:00', 2, 250000, 1, '2024-01-01'), -- Thứ 3
(1, '17:00:00', '21:00:00', 3, 250000, 1, '2024-01-01'), -- Thứ 4
(1, '17:00:00', '21:00:00', 4, 250000, 1, '2024-01-01'), -- Thứ 5
(1, '17:00:00', '21:00:00', 5, 250000, 1, '2024-01-01'), -- Thứ 6
(1, '17:00:00', '21:00:00', 6, 300000, 1, '2024-01-01'), -- Thứ 7
(1, '17:00:00', '21:00:00', 7, 300000, 1, '2024-01-01'); -- Chủ nhật

-- Insert payment methods
INSERT INTO PaymentMethods (MethodName, MethodType, IsActive) VALUES
('Chuyển khoản ngân hàng', 'BankTransfer', 1),
('Tiền mặt', 'Cash', 1);

-- Insert system configs
INSERT INTO SystemConfigs (ConfigKey, ConfigValue, Description, ConfigType, DataType) VALUES
-- System Settings
(N'SiteName', N'3TL Mini Soccer', N'Tên website', N'System', N'String'),
(N'SiteDescription', N'Hệ thống quản lý sân bóng đá mini', N'Mô tả website', N'System', N'String'),
(N'OpeningHour', N'06:00', N'Giờ mở cửa', N'System', N'String'),
(N'ClosingHour', N'23:00', N'Giờ đóng cửa', N'System', N'String'),
(N'Timezone', N'Asia/Ho_Chi_Minh', N'Múi giờ', N'System', N'String'),
(N'Currency', N'VND', N'Đơn vị tiền tệ', N'System', N'String'),
(N'TaxRate', N'10', N'Tỷ lệ thuế (%)', N'System', N'Number'),
(N'PeakHourMultiplier', N'1.3', N'Hệ số tăng giá giờ cao điểm', N'System', N'Number'),
(N'BookingAdvanceDays', N'7', N'Số ngày có thể đặt trước', N'System', N'Number'),
(N'CancellationHours', N'2', N'Số giờ trước khi hủy miễn phí', N'System', N'Number'),
(N'PaymentTimeoutMinutes', N'10', N'Thời gian chờ thanh toán (phút)', N'System', N'Number'),
(N'QRCodeSize', N'200', N'Kích thước QR code (px)', N'System', N'Number'),

-- SEPay Configuration
(N'SEPAY_API_URL', N'https://my.sepay.vn/userapi', N'SEPay API URL', N'SEPay', N'String'),
(N'SEPAY_API_TOKEN', N'ZNWLTBRPMMURNTNLXEK9CMKJZF0IWD1PE2YJXRTCVWV9IXOS5YAF4IKVGQNSGQEI', N'SEPay API Token để kiểm tra giao dịch', N'SEPay', N'String'),
(N'SEPAY_BANK_CODE', N'', N'Mã ngân hàng', N'SEPay', N'String'),
(N'SEPAY_ACCOUNT_NUMBER', N'', N'Số tài khoản', N'SEPay', N'String'),
(N'SEPAY_ACCOUNT_NAME', N'', N'Tên chủ tài khoản', N'SEPay', N'String'),

-- General Settings
(N'MaintenanceMode', N'false', N'Chế độ bảo trì', N'General', N'Boolean'),
(N'MaxBookingPerUser', N'3', N'Số đặt sân tối đa mỗi người', N'General', N'Number'),
(N'DefaultLanguage', N'vi-VN', N'Ngôn ngữ mặc định', N'General', N'String'),
(N'EmailNotifications', N'true', N'Bật thông báo email', N'General', N'Boolean'),
(N'SmsNotifications', N'false', N'Bật thông báo SMS', N'General', N'Boolean');

-- =====================================================
-- 12. TRIGGERS FOR AUDIT LOGGING & DYNAMIC BOOKING
-- =====================================================

-- Trigger for Users table
CREATE TRIGGER tr_Users_Audit
ON Users
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Action NVARCHAR(10);
    DECLARE @UserId INT;
    
    IF EXISTS(SELECT * FROM inserted) AND EXISTS(SELECT * FROM deleted)
        SET @Action = 'UPDATE';
    ELSE IF EXISTS(SELECT * FROM inserted)
        SET @Action = 'INSERT';
    ELSE
        SET @Action = 'DELETE';
    
    -- Get current user ID (you may need to implement this based on your auth system)
    SET @UserId = ISNULL((SELECT UserId FROM inserted), (SELECT UserId FROM deleted));
    
    INSERT INTO AuditLogs (UserId, Action, TableName, RecordId, NewValues, OldValues)
    SELECT 
        @UserId,
        @Action,
        'Users',
        ISNULL(i.UserId, d.UserId),
        CASE WHEN @Action IN ('INSERT', 'UPDATE') THEN 
            (SELECT * FROM inserted FOR JSON AUTO)
        END,
        CASE WHEN @Action IN ('UPDATE', 'DELETE') THEN 
            (SELECT * FROM deleted FOR JSON AUTO)
        END
    FROM inserted i
    FULL OUTER JOIN deleted d ON i.UserId = d.UserId;
END
GO

-- Trigger cũ (cập nhật cho dynamic) - Update FieldSchedules khi booking thay đổi
CREATE TRIGGER tr_Bookings_UpdateSchedule
ON Bookings
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Update FieldSchedules Status dựa trên Start/End mới
    UPDATE fs
    SET Status = CASE 
        WHEN i.Status = 'Confirmed' THEN 'Booked' 
        WHEN i.Status = 'Cancelled' THEN 'Available' 
        ELSE fs.Status 
    END,
    BookingId = CASE 
        WHEN i.Status = 'Confirmed' THEN i.BookingId 
        WHEN i.Status = 'Cancelled' THEN NULL 
        ELSE fs.BookingId 
    END
    FROM FieldSchedules fs
    INNER JOIN inserted i ON fs.FieldId = i.FieldId 
        AND fs.Date = i.BookingDate 
        AND fs.StartTime <= i.StartTime 
        AND fs.EndTime >= i.EndTime;  -- Match khoảng
END
GO

-- Trigger để cập nhật UpdatedAt cho Bookings
CREATE TRIGGER tr_Bookings_UpdatedAt
ON Bookings
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE b
    SET UpdatedAt = GETDATE()
    FROM Bookings b
    INNER JOIN inserted i ON b.BookingId = i.BookingId;
END
GO

-- Trigger để cập nhật UpdatedAt cho Users
CREATE TRIGGER tr_Users_UpdatedAt
ON Users
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE u
    SET UpdatedAt = GETDATE()
    FROM Users u
    INNER JOIN inserted i ON u.UserId = i.UserId;
END
GO

-- Trigger để cập nhật UpdatedAt cho Fields
CREATE TRIGGER tr_Fields_UpdatedAt
ON Fields
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE f
    SET UpdatedAt = GETDATE()
    FROM Fields f
    INNER JOIN inserted i ON f.FieldId = i.FieldId;
END
GO

-- =====================================================
-- 13. VIEWS FOR REPORTING
-- =====================================================

-- View for booking summary
CREATE VIEW vw_BookingSummary AS
SELECT 
    b.BookingId,
    b.BookingCode,
    b.BookingDate,
    b.StartTime,
    b.EndTime,
    b.Duration,
    b.TotalPrice,
    b.Status,
    b.PaymentStatus,
    f.FieldName,
    ft.TypeName as FieldType,
    u.FirstName + ' ' + u.LastName as CustomerName,
    u.PhoneNumber as CustomerPhone,
    b.GuestName,
    b.GuestPhone
FROM Bookings b
LEFT JOIN Fields f ON b.FieldId = f.FieldId
LEFT JOIN FieldTypes ft ON f.FieldTypeId = ft.FieldTypeId
LEFT JOIN Users u ON b.UserId = u.UserId;

-- View for revenue report
CREATE VIEW vw_RevenueReport AS
SELECT 
    CAST(b.BookingDate AS DATE) as BookingDate,
    f.FieldName,
    ft.TypeName as FieldType,
    COUNT(*) as BookingCount,
    SUM(b.TotalPrice) as TotalRevenue,
    AVG(b.TotalPrice) as AveragePrice
FROM Bookings b
INNER JOIN Fields f ON b.FieldId = f.FieldId
INNER JOIN FieldTypes ft ON f.FieldTypeId = ft.FieldTypeId
WHERE b.Status = 'Confirmed' AND b.PaymentStatus = 'Paid'
GROUP BY CAST(b.BookingDate AS DATE), f.FieldName, ft.TypeName;

-- View for daily booking statistics
CREATE VIEW vw_BookingStats_Daily AS
SELECT 
    b.BookingDate,
    COUNT(*) AS TotalBookings,
    SUM(b.TotalPrice) AS TotalRevenue,
    AVG(DATEDIFF(MINUTE, b.StartTime, b.EndTime)) AS AvgDurationMin,
    COUNT(CASE WHEN b.Status = 'Confirmed' THEN 1 END) AS ConfirmedBookings,
    COUNT(CASE WHEN b.Status = 'Pending' THEN 1 END) AS PendingBookings,
    COUNT(CASE WHEN b.Status = 'Cancelled' THEN 1 END) AS CancelledBookings
FROM Bookings b
GROUP BY b.BookingDate;

-- =====================================================
-- 14. FUNCTIONS
-- =====================================================

-- Function to calculate booking price based on dynamic time
CREATE FUNCTION fn_CalculateBookingPrice(
    @FieldTypeId INT,
    @StartTime TIME,
    @EndTime TIME,
    @BookingDate DATE
)
RETURNS DECIMAL(10,2)
AS
BEGIN
    DECLARE @TotalPrice DECIMAL(10,2) = 0;
    DECLARE @DayOfWeek INT = DATEPART(WEEKDAY, @BookingDate);
    DECLARE @CurrentTime TIME = @StartTime;
    DECLARE @EndTimeCalc TIME = @EndTime;
    
    WHILE @CurrentTime < @EndTimeCalc
    BEGIN
        DECLARE @NextHour TIME = DATEADD(HOUR, 1, @CurrentTime);
        IF @NextHour > @EndTimeCalc
            SET @NextHour = @EndTimeCalc;
        
        -- Get price for this hour directly from pricing rules
        DECLARE @HourPrice DECIMAL(10,2);
        SELECT TOP 1 @HourPrice = Price
        FROM PricingRules 
        WHERE FieldTypeId = @FieldTypeId 
            AND StartTime <= @CurrentTime AND EndTime >= @NextHour
            AND DayOfWeek = @DayOfWeek
            AND EffectiveFrom <= @BookingDate
            AND (EffectiveTo IS NULL OR EffectiveTo >= @BookingDate)
        ORDER BY EffectiveFrom DESC;
        
        IF @HourPrice IS NULL
        BEGIN
            -- Fallback to base price
            SELECT @HourPrice = BasePrice FROM FieldTypes WHERE FieldTypeId = @FieldTypeId;
        END
        
        SET @TotalPrice = @TotalPrice + @HourPrice;
        SET @CurrentTime = @NextHour;
    END
    
    RETURN @TotalPrice;
END
GO


CREATE PROCEDURE sp_BookDynamicField
    @FieldId INT,
    @BookingDate DATE,
    @StartTime TIME,
    @Duration INT,  -- Phút
    @UserId INT = NULL,
    @GuestName NVARCHAR(100) = NULL,
    @GuestPhone NVARCHAR(20) = NULL,
    @GuestEmail NVARCHAR(100) = NULL,
    @TotalPrice DECIMAL(10,2) OUTPUT,
    @BookingId INT OUTPUT,
    @OverlapError NVARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @EndTime TIME = DATEADD(MINUTE, @Duration, @StartTime);
    DECLARE @DayOfWeek INT = DATEPART(WEEKDAY, @BookingDate);
    DECLARE @FieldTypeId INT;
    DECLARE @BookingCode NVARCHAR(20);
    
    BEGIN TRANSACTION;
    BEGIN TRY
        -- Lấy FieldType
        SELECT @FieldTypeId = FieldTypeId FROM Fields WITH (NOLOCK) WHERE FieldId = @FieldId;
        
        -- Tính giá (tìm rule phù hợp với thời gian booking)
        SELECT @TotalPrice = pr.Price * (@Duration / 60.0) * pr.PeakMultiplier
        FROM PricingRules pr
        WHERE pr.FieldTypeId = @FieldTypeId 
            AND pr.DayOfWeek = @DayOfWeek
            AND pr.StartTime <= @StartTime AND pr.EndTime >= @EndTime  -- Match time range
            AND pr.EffectiveFrom <= @BookingDate AND (pr.EffectiveTo IS NULL OR pr.EffectiveTo >= @BookingDate);
        
        -- Nếu không tìm thấy pricing rule, sử dụng giá cơ bản
        IF @TotalPrice IS NULL OR @TotalPrice = 0 
        BEGIN
            SELECT @TotalPrice = BasePrice * (@Duration / 60.0)
            FROM FieldTypes 
            WHERE FieldTypeId = @FieldTypeId;
        END
        
        -- Check overlap (lock)
        IF EXISTS (
            SELECT 1 FROM Bookings b WITH (UPDLOCK, HOLDLOCK)
            WHERE b.FieldId = @FieldId 
                AND b.BookingDate = @BookingDate
                AND b.Status IN (N'Confirmed', N'Playing')
                AND b.StartTime < @EndTime 
                AND b.EndTime > @StartTime
        )
        BEGIN
            SET @OverlapError = N'Trùng lịch: Khoảng ' + CAST(@StartTime AS NVARCHAR(5)) + '-' + CAST(@EndTime AS NVARCHAR(5)) + N' đã được đặt.';
            ROLLBACK;
            RETURN;
        END
        
        -- Generate code (giả sử gọi SP cũ, hoặc hardcode test)
        SET @BookingCode = N'BK' + FORMAT(@BookingDate, 'yyyyMMdd') + FORMAT(GETDATE(), 'HHmmss');
        
        -- Insert Bookings
        INSERT INTO Bookings (BookingCode, UserId, GuestName, GuestPhone, GuestEmail, FieldId, BookingDate, StartTime, EndTime, Duration, Status, TotalPrice, PaymentStatus)
        VALUES (@BookingCode, @UserId, @GuestName, @GuestPhone, @GuestEmail, @FieldId, @BookingDate, @StartTime, @EndTime, @Duration, N'Pending', @TotalPrice, N'Pending');
        
        SET @BookingId = SCOPE_IDENTITY();
        
        -- Update FieldSchedules (block slot)
        INSERT INTO FieldSchedules (FieldId, Date, StartTime, EndTime, Status, BookingId)
        VALUES (@FieldId, @BookingDate, @StartTime, @EndTime, N'Booked', @BookingId);
        
        COMMIT;
        SET @OverlapError = NULL;
    END TRY
    BEGIN CATCH
        ROLLBACK;
        SET @OverlapError = ERROR_MESSAGE();
    END CATCH
END
GO
-- =====================================================
-- COMPLETION MESSAGE
-- =====================================================
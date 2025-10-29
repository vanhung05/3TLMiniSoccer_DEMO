-- =====================================================
-- 3TL MINI SOCCER MANAGEMENT SYSTEM DATABASE - PHIÊN BẢN DYNAMIC TIME
-- Phiên bản: 3.1 (Hỗ trợ đặt sân linh hoạt, tránh overlap)
-- Cập nhật: 2025-10-14
-- 
-- Thay đổi chính:
-- - Bookings và FieldSchedules: Thêm StartTime, EndTime (TIME) cho dynamic duration.
-- - Bỏ TimeSlotId unique, dùng SP để check overlap.
-- - Giữ TimeSlots cho tính giá (PricingRules).
-- - SP mới: sp_BookDynamicField (book + check), sp_CheckDynamicAvailability (gợi ý).
-- =====================================================

CREATE DATABASE MiniSoccerManagement;
GO

USE MiniSoccerManagement;
GO

-- =====================================================
-- 1. BẢNG NGƯỜI DÙNG & PHÂN QUYỀN
-- =====================================================

CREATE TABLE Roles (
    RoleId INT IDENTITY(1,1) PRIMARY KEY,
    RoleName NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(200),
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE()
);

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
    RoleId INT NOT NULL DEFAULT 3,
    IsActive BIT DEFAULT 1,
    EmailConfirmed BIT DEFAULT 0,
    PhoneConfirmed BIT DEFAULT 0,
    PreferredChannel NVARCHAR(100) DEFAULT N'Zalo',  -- Mới: Cho multi-channel reminders
    ZaloUserId NVARCHAR(50) NULL,  -- Từ integration trước
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    LastLoginAt DATETIME2,
    AssignedAt DATETIME2 DEFAULT GETDATE(),
    AssignedBy INT,
    FOREIGN KEY (RoleId) REFERENCES Roles(RoleId),
    FOREIGN KEY (AssignedBy) REFERENCES Users(UserId)
);

CREATE TABLE SocialLogins (
    SocialLoginId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    Provider NVARCHAR(50) NOT NULL,
    ProviderKey NVARCHAR(255) NOT NULL,
    ProviderDisplayName NVARCHAR(100),
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE,
    UNIQUE(Provider, ProviderKey)
);

-- =====================================================
-- 2. DANH MỤC SÂN & LỊCH (CẬP NHẬT DYNAMIC)
-- =====================================================

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

CREATE TABLE Fields (
    FieldId INT IDENTITY(1,1) PRIMARY KEY,
    FieldName NVARCHAR(100) NOT NULL,
    FieldTypeId INT NOT NULL,
    Location NVARCHAR(200) NOT NULL,
    ClusterName NVARCHAR(100),
    Status NVARCHAR(20) DEFAULT N'Active',
    Description NVARCHAR(500),
    ImageUrl NVARCHAR(255),
    OpeningTime TIME DEFAULT '06:00',  -- Mới: Giờ mở sân
    ClosingTime TIME DEFAULT '23:00',  -- Mới: Giờ đóng sân
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (FieldTypeId) REFERENCES FieldTypes(FieldTypeId)
);

CREATE TABLE TimeSlots (
    TimeSlotId INT IDENTITY(1,1) PRIMARY KEY,
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,
    Duration INT NOT NULL,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE()
);

CREATE TABLE PricingRules (
    PricingRuleId INT IDENTITY(1,1) PRIMARY KEY,
    FieldTypeId INT NOT NULL,
    TimeSlotId INT NOT NULL,  -- Giữ cho tính giá theo slot gần nhất
    DayOfWeek INT NOT NULL,
    Price DECIMAL(10,2) NOT NULL,
    IsPeakHour BIT DEFAULT 0,
    PeakMultiplier DECIMAL(3,2) DEFAULT 1.0,
    EffectiveFrom DATE NOT NULL,
    EffectiveTo DATE,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (FieldTypeId) REFERENCES FieldTypes(FieldTypeId),
    FOREIGN KEY (TimeSlotId) REFERENCES TimeSlots(TimeSlotId)
);

-- Cập nhật: FieldSchedules hỗ trợ dynamic intervals
CREATE TABLE FieldSchedules (
    ScheduleId INT IDENTITY(1,1) PRIMARY KEY,
    FieldId INT NOT NULL,
    Date DATE NOT NULL,
    StartTime TIME NOT NULL,  -- Mới: Thời gian bắt đầu động
    EndTime TIME NOT NULL,    -- Mới: Thời gian kết thúc động
    Status NVARCHAR(20) DEFAULT N'Available',
    BookingId INT NULL,
    Notes NVARCHAR(500),
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (FieldId) REFERENCES Fields(FieldId),
    -- Unique mới: Không cho overlap trên cùng field/date
    UNIQUE(FieldId, Date, StartTime, EndTime)
);

-- =====================================================
-- 3. ĐẶT SÂN (CẬP NHẬT DYNAMIC)
-- =====================================================

CREATE TABLE Bookings (
    BookingId INT IDENTITY(1,1) PRIMARY KEY,
    BookingCode NVARCHAR(20) NOT NULL UNIQUE,
    UserId INT NULL,
    GuestName NVARCHAR(100),
    GuestPhone NVARCHAR(20),
    GuestEmail NVARCHAR(100),
    FieldId INT NOT NULL,
    BookingDate DATE NOT NULL,
    StartTime TIME NOT NULL,  -- Mới: Start time động
    EndTime TIME NOT NULL,    -- Mới: End time động
    Duration INT NOT NULL,    -- Phút (60,90,120)
    Status NVARCHAR(20) DEFAULT N'Pending',
    TotalPrice DECIMAL(10,2) NOT NULL,
    PaymentStatus NVARCHAR(20) DEFAULT N'Pending',
    PaymentMethod NVARCHAR(50),
    PaymentReference NVARCHAR(100),
    Notes NVARCHAR(500),
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    ConfirmedAt DATETIME2,
    ConfirmedBy INT,
    CancelledAt DATETIME2,
    CancelledBy INT,
    FOREIGN KEY (UserId) REFERENCES Users(UserId),
    FOREIGN KEY (FieldId) REFERENCES Fields(FieldId),
    FOREIGN KEY (ConfirmedBy) REFERENCES Users(UserId),
    FOREIGN KEY (CancelledBy) REFERENCES Users(UserId)
);

CREATE TABLE BookingSessions (
    SessionId INT IDENTITY(1,1) PRIMARY KEY,
    BookingId INT NOT NULL,
    CheckInTime DATETIME2,
    CheckOutTime DATETIME2,
    ActualDuration INT,
    OvertimeFee DECIMAL(10,2) DEFAULT 0,
    StaffId INT NOT NULL,
    Notes NVARCHAR(500),
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (BookingId) REFERENCES Bookings(BookingId),
    FOREIGN KEY (StaffId) REFERENCES Users(UserId)
);

-- =====================================================
-- 4. THANH TOÁN & ĐỐI SOÁT (GỐC)
-- =====================================================

CREATE TABLE PaymentMethods (
    PaymentMethodId INT IDENTITY(1,1) PRIMARY KEY,
    MethodName NVARCHAR(50) NOT NULL,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE()
);

CREATE TABLE Transactions (
    TransactionId INT IDENTITY(1,1) PRIMARY KEY,
    BookingId INT NOT NULL,
    PaymentMethodId INT NOT NULL,
    Amount DECIMAL(10,2) NOT NULL,
    TransactionCode NVARCHAR(100),
    Status NVARCHAR(20) DEFAULT N'Pending',
    PaymentData NVARCHAR(MAX),
    ProcessedAt DATETIME2,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (BookingId) REFERENCES Bookings(BookingId),
    FOREIGN KEY (PaymentMethodId) REFERENCES PaymentMethods(PaymentMethodId)
);

CREATE TABLE PaymentOrders (
    PaymentOrderId INT IDENTITY(1,1) PRIMARY KEY,
    OrderCode NVARCHAR(50) NOT NULL UNIQUE,
    BookingId INT NOT NULL,
    UserId INT NOT NULL,
    Amount DECIMAL(10,2) NOT NULL,
    PaymentMethod NVARCHAR(50) NOT NULL,
    Status NVARCHAR(20) DEFAULT N'Pending',
    PaymentData NVARCHAR(MAX),
    PaymentReference NVARCHAR(100),
    QRCodeUrl NVARCHAR(500) NULL,  -- Từ integration QR trước
    VietQRBankCode NVARCHAR(20) NULL,
    ExpiredAt DATETIME2,
    ProcessedAt DATETIME2,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (BookingId) REFERENCES Bookings(BookingId),
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

CREATE TABLE SystemConfigs (
    ConfigId INT IDENTITY(1,1) PRIMARY KEY,
    ConfigKey NVARCHAR(100) NOT NULL UNIQUE,
    ConfigValue NVARCHAR(MAX) NOT NULL,
    Description NVARCHAR(500),
    ConfigType NVARCHAR(50) NOT NULL,
    DataType NVARCHAR(50) DEFAULT N'String',
    IsActive BIT DEFAULT 1,
    IsEncrypted BIT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedBy INT,
    FOREIGN KEY (UpdatedBy) REFERENCES Users(UserId)
);

CREATE TABLE CartItems (
    CartItemId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE,
    FOREIGN KEY (ProductId) REFERENCES Products(ProductId) ON DELETE CASCADE,
    UNIQUE(UserId, ProductId)
);

-- =====================================================
-- 5. ƯU ĐÃI & LOYALTY (GỐC)
-- =====================================================

CREATE TABLE DiscountCodes (
    DiscountCodeId INT IDENTITY(1,1) PRIMARY KEY,
    Code NVARCHAR(50) NOT NULL UNIQUE,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    DiscountType NVARCHAR(20) NOT NULL,
    DiscountValue DECIMAL(10,2) NOT NULL,
    MinOrderAmount DECIMAL(10,2),
    MaxDiscountAmount DECIMAL(10,2),
    UsageLimit INT,
    UsedCount INT DEFAULT 0,
    ValidFrom DATETIME2 NOT NULL,
    ValidTo DATETIME2 NOT NULL,
    CodeType NVARCHAR(20) DEFAULT N'Discount',
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    CreatedBy INT,
    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
);

CREATE TABLE DiscountCodeUsages (
    UsageId INT IDENTITY(1,1) PRIMARY KEY,
    DiscountCodeId INT NOT NULL,
    BookingId INT NOT NULL,
    UserId INT NOT NULL,
    DiscountAmount DECIMAL(10,2) NOT NULL,
    UsedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (DiscountCodeId) REFERENCES DiscountCodes(DiscountCodeId),
    FOREIGN KEY (BookingId) REFERENCES Bookings(BookingId),
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

CREATE TABLE LoyaltyPoints (
    PointId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    Points INT NOT NULL,
    PointType NVARCHAR(20) NOT NULL,
    Description NVARCHAR(200),
    BookingId INT,
    ExpiryDate DATE,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId),
    FOREIGN KEY (BookingId) REFERENCES Bookings(BookingId)
);

-- =====================================================
-- 6. NỘI DUNG & GIAO DIỆN (GỐC)
-- =====================================================

CREATE TABLE NewsCategories (
    CategoryId INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(200),
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE()
);

CREATE TABLE News (
    NewsId INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(200) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    Summary NVARCHAR(500),
    CategoryId INT NOT NULL,
    ImageUrl NVARCHAR(255),
    IsPublished BIT DEFAULT 0,
    PublishedAt DATETIME2,
    ViewCount INT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    CreatedBy INT NOT NULL,
    FOREIGN KEY (CategoryId) REFERENCES NewsCategories(CategoryId),
    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
);

CREATE TABLE Products (
    ProductId INT IDENTITY(1,1) PRIMARY KEY,
    ProductName NVARCHAR(200) NOT NULL,
    Description NVARCHAR(500),
    Price DECIMAL(10,2) NOT NULL,
    Category NVARCHAR(50),
    ImageUrl NVARCHAR(255),
    IsAvailable BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    CreatedBy INT NOT NULL,
    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
);

CREATE TABLE Orders (
    OrderId INT IDENTITY(1,1) PRIMARY KEY,
    OrderCode NVARCHAR(20) NOT NULL UNIQUE,
    BookingId INT NOT NULL,
    UserId INT NOT NULL,
    TotalAmount DECIMAL(10,2) NOT NULL,
    Status NVARCHAR(20) DEFAULT N'Pending',
    PaymentStatus NVARCHAR(20) DEFAULT N'Pending',
    Notes NVARCHAR(500),
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (BookingId) REFERENCES Bookings(BookingId),
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

CREATE TABLE OrderItems (
    OrderItemId INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(10,2) NOT NULL,
    TotalPrice DECIMAL(10,2) NOT NULL,
    FOREIGN KEY (OrderId) REFERENCES Orders(OrderId),
    FOREIGN KEY (ProductId) REFERENCES Products(ProductId)
);

-- =====================================================
-- 7. THÔNG BÁO & LIÊN HỆ (GỐC + ZALO QUEUE)
-- =====================================================

CREATE TABLE Notifications (
    NotificationId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Content NVARCHAR(500) NOT NULL,
    Type NVARCHAR(50) NOT NULL,
    IsRead BIT DEFAULT 0,
    ReadAt DATETIME2,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

CREATE TABLE Contacts (
    ContactId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    Phone NVARCHAR(20),
    Subject NVARCHAR(200) NOT NULL,
    Message NVARCHAR(MAX) NOT NULL,
    Status NVARCHAR(20) DEFAULT N'New',
    Response NVARCHAR(MAX),
    RespondedAt DATETIME2,
    RespondedBy INT,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (RespondedBy) REFERENCES Users(UserId)
);

-- Zalo Queue (từ trước, mở rộng cho multi-channel)
CREATE TABLE ZaloNotificationQueue (
    QueueId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    BookingId INT NOT NULL,
    NotificationType NVARCHAR(50) NOT NULL,
    MessageTemplate NVARCHAR(500) NOT NULL,
    ScheduledAt DATETIME2 NOT NULL,
    SentAt DATETIME2 NULL,
    Status NVARCHAR(20) DEFAULT N'Pending',
    ErrorMessage NVARCHAR(500),
    Channel NVARCHAR(20) DEFAULT N'Zalo',  -- Mới: Zalo/SMS/Email
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId),
    FOREIGN KEY (BookingId) REFERENCES Bookings(BookingId)
);

-- =====================================================
-- 8. BÁO CÁO & AUDIT LOG (GỐC)
-- =====================================================

CREATE TABLE AuditLogs (
    LogId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT,
    Action NVARCHAR(100) NOT NULL,
    TableName NVARCHAR(50) NOT NULL,
    RecordId INT,
    OldValues NVARCHAR(MAX),
    NewValues NVARCHAR(MAX),
    IpAddress NVARCHAR(45),
    UserAgent NVARCHAR(500),
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

-- =====================================================
-- 9. INSERT DỮ LIỆU MẪU (CẬP NHẬT DYNAMIC)
-- =====================================================

-- Roles
INSERT INTO Roles (RoleName, Description) VALUES
(N'Admin', N'Quản trị viên hệ thống'),
(N'Staff', N'Nhân viên sân bóng'),
(N'User', N'Người dùng đã đăng ký'),
(N'Guest', N'Khách hàng không đăng ký');

-- FieldTypes
INSERT INTO FieldTypes (TypeName, Description, PlayerCount, BasePrice) VALUES
(N'5v5', N'Sân 5 người mỗi đội', 10, 100000.00),
(N'7v7', N'Sân 7 người mỗi đội', 14, 200000.00),
(N'11v11', N'Sân 11 người mỗi đội', 22, 500000.00);

-- TimeSlots (giữ cho giá, nhưng không dùng cho scheduling)
INSERT INTO TimeSlots (StartTime, EndTime, Duration) VALUES
('06:00', '07:00', 60), ('07:00', '08:00', 60), ('08:00', '09:00', 60),
('09:00', '10:00', 60), ('10:00', '11:00', 60), ('11:00', '12:00', 60),
('12:00', '13:00', 60), ('13:00', '14:00', 60), ('14:00', '15:00', 60),
('15:00', '16:00', 60), ('16:00', '17:00', 60), ('17:00', '18:00', 60),
('18:00', '19:00', 60), ('19:00', '20:00', 60), ('20:00', '21:00', 60),
('21:00', '22:00', 60), ('22:00', '23:00', 60);

-- PaymentMethods
INSERT INTO PaymentMethods (MethodName) VALUES (N'Tiền mặt'), (N'Chuyển khoản');

-- NewsCategories
INSERT INTO NewsCategories (CategoryName, Description) VALUES
(N'Tin tức', N'Tin tức chung về sân bóng'), (N'Khuyến mãi', N'Thông tin khuyến mãi và ưu đãi'),
(N'Sự kiện', N'Các sự kiện và giải đấu'), (N'Hướng dẫn', N'Hướng dẫn sử dụng dịch vụ');

-- SystemConfigs (thêm Gemini key ví dụ)
INSERT INTO SystemConfigs (ConfigKey, ConfigValue, Description, ConfigType, DataType) VALUES
(N'SiteName', N'3TL Mini Soccer', N'Tên website', N'System', N'String'),
(N'Gemini_API_Key', N'YOUR_GEMINI_KEY', N'API Key cho Gemini Chatbot', N'System', N'String'),  -- Ví dụ
-- ... (các config khác như trước)

-- Fields
INSERT INTO Fields (FieldName, FieldTypeId, Location, ClusterName, Status, Description) VALUES
(N'Sân Hoàng Kim A1', 1, N'Tầng 1', N'Khu A', N'Active', N'Sân 5v5 trong nhà'),
(N'Sân Hoàng Kim A2', 1, N'Tầng 1', N'Khu A', N'Active', N'Sân 5v5 trong nhà'),
(N'Sân Hoàng Kim B1', 2, N'Tầng 2', N'Khu B', N'Active', N'Sân 7v7 ngoài trời'),
(N'Sân Hoàng Kim B2', 2, N'Tầng 2', N'Khu B', N'Active', N'Sân 7v7 ngoài trời'),
(N'Sân Hoàng Kim VIP1', 3, N'Tầng 3', N'Khu VIP', N'Active', N'Sân 11v11 cao cấp'),
(N'Sân Hoàng Kim VIP2', 3, N'Tầng 3', N'Khu VIP', N'Active', N'Sân 11v11 cao cấp');

-- PricingRules (ví dụ cho day 1 - Monday)
INSERT INTO PricingRules (FieldTypeId, TimeSlotId, DayOfWeek, Price, IsPeakHour, PeakMultiplier, EffectiveFrom) VALUES
(1, 1, 1, 100000.00, 0, 1.00, '2025-10-01'),  -- 5v5, slot 1, thường
(1, 15, 1, 130000.00, 1, 1.30, '2025-10-01'), -- Cao điểm
(2, 1, 1, 200000.00, 0, 1.00, '2025-10-01'),
(2, 15, 1, 260000.00, 1, 1.30, '2025-10-01'),
(3, 1, 1, 500000.00, 0, 1.00, '2025-10-01'),
(3, 15, 1, 650000.00, 1, 1.30, '2025-10-01');

-- FieldSchedules mẫu (dynamic intervals, available)
INSERT INTO FieldSchedules (FieldId, Date, StartTime, EndTime, Status) VALUES
(1, '2025-10-15', '06:00', '23:00', N'Available'),  -- Toàn ngày available ban đầu
(2, '2025-10-15', '06:00', '23:00', N'Available');

-- Users
INSERT INTO Users (Username, Email, PasswordHash, FirstName, LastName, PhoneNumber, RoleId, IsActive, EmailConfirmed) VALUES
(N'admin', N'admin@3tlminisoccer.com', N'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMQrcbby6=', N'Admin', N'System', N'0123456789', 1, 1, 1),
(N'staff1', N'staff1@3tlminisoccer.com', N'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMQrcbby6=', N'Nhân viên', N'Một', N'0123456790', 2, 1, 1),
(N'user1', N'user1@example.com', N'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMQrcbby6=', N'Người', N'Dùng', N'0123456791', 3, 1, 1);

-- Bookings mẫu (dynamic)
INSERT INTO Bookings (BookingCode, UserId, FieldId, BookingDate, StartTime, EndTime, Duration, Status, TotalPrice, PaymentStatus) VALUES
(N'BK202510151001', 3, 1, '2025-10-15', '06:00', '07:30', 90, N'Confirmed', 150000.00, N'Paid'),  -- 90p
(N'BK202510151002', NULL, N'Khách lẻ', N'0123456792', N'guest@example.com', 1, '2025-10-15', '08:00', '09:30', 90, N'Pending', 150000.00, N'Pending');  -- Overlap check sẽ fail nếu trùng

-- Các bảng khác (Products, News, v.v.) như trước...

-- =====================================================
-- 10. INDEXES (CẬP NHẬT CHO DYNAMIC)
-- =====================================================

-- Users
CREATE INDEX IX_Users_Email ON Users(Email);
CREATE INDEX IX_Users_PhoneNumber ON Users(PhoneNumber);
CREATE INDEX IX_Users_RoleId ON Users(RoleId);

-- Bookings (mới: Index cho overlap query)
CREATE INDEX IX_Bookings_FieldId_Date_StartTime_EndTime ON Bookings(FieldId, BookingDate, StartTime, EndTime);
CREATE INDEX IX_Bookings_Status ON Bookings(Status);
CREATE INDEX IX_Bookings_BookingDate ON Bookings(BookingDate);

-- FieldSchedules (mới)
CREATE INDEX IX_FieldSchedules_FieldId_Date_StartTime ON FieldSchedules(FieldId, Date, StartTime);
CREATE INDEX IX_FieldSchedules_Status ON FieldSchedules(Status);

-- Các index khác như trước...

-- =====================================================
-- 11. STORED PROCEDURES (MỚI CHO DYNAMIC)
-- =====================================================

-- SP Generate BookingCode (gốc)
GO
CREATE PROCEDURE sp_GenerateBookingCode
AS
BEGIN
    -- Như trước...
END
GO

-- SP MỚI: Book dynamic với check overlap
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
        
        -- Tính giá (average từ rules, scale theo duration)
        SELECT @TotalPrice = SUM(pr.Price * (DATEDIFF(MINUTE, ts.StartTime, ts.EndTime) / 60.0)) * (@Duration / 60.0) * AVG(pr.PeakMultiplier)
        FROM PricingRules pr
        INNER JOIN TimeSlots ts ON pr.TimeSlotId = ts.TimeSlotId
        WHERE pr.FieldTypeId = @FieldTypeId 
            AND pr.DayOfWeek = @DayOfWeek
            AND ts.StartTime <= @StartTime AND ts.EndTime >= @EndTime  -- Match slot gần
            AND pr.EffectiveFrom <= @BookingDate AND (pr.EffectiveTo IS NULL OR pr.EffectiveTo >= @BookingDate);
        
        IF @TotalPrice IS NULL OR @TotalPrice = 0 SET @TotalPrice = (SELECT BasePrice FROM FieldTypes WHERE FieldTypeId = @FieldTypeId) * (@Duration / 60.0);
        
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

-- SP MỚI: Check availability (trả gợi ý start time trống)
GO
CREATE PROCEDURE sp_CheckDynamicAvailability
    @FieldId INT,
    @BookingDate DATE,
    @MaxDuration INT = 120  -- Max gợi ý duration
AS
BEGIN
    -- Tìm gaps giữa bookings
    WITH BookedIntervals AS (
        SELECT StartTime, EndTime FROM Bookings 
        WHERE FieldId = @FieldId AND BookingDate = @BookingDate AND Status IN (N'Confirmed', N'Playing')
        UNION ALL
        SELECT CAST('23:00' AS TIME), CAST('23:59' AS TIME)  -- Closing
        UNION ALL
        SELECT CAST('00:00' AS TIME), OpeningTime FROM Fields WHERE FieldId = @FieldId  -- Opening
    ),
    Gaps AS (
        SELECT LAG(EndTime) OVER (ORDER BY StartTime) AS PrevEnd, StartTime AS NextStart
        FROM BookedIntervals
        WHERE DATEDIFF(MINUTE, LAG(EndTime) OVER (ORDER BY StartTime), StartTime) > @MaxDuration
    )
    SELECT PrevEnd AS SuggestedStart, DATEADD(MINUTE, @MaxDuration, PrevEnd) AS SuggestedEnd
    FROM Gaps
    WHERE DATEDIFF(MINUTE, PrevEnd, NextStart) >= @MaxDuration;
END
GO

-- Trigger cũ (cập nhật cho dynamic)
GO
CREATE TRIGGER tr_Bookings_UpdateSchedule
ON Bookings
AFTER INSERT, UPDATE
AS
BEGIN
    -- Update FieldSchedules Status dựa trên Start/End mới
    UPDATE fs
    SET Status = CASE WHEN i.Status = 'Confirmed' THEN 'Booked' WHEN i.Status = 'Cancelled' THEN 'Available' ELSE fs.Status END,
        BookingId = CASE WHEN i.Status = 'Confirmed' THEN i.BookingId WHEN i.Status = 'Cancelled' THEN NULL ELSE fs.BookingId END
    FROM FieldSchedules fs
    INNER JOIN inserted i ON fs.FieldId = i.FieldId AND fs.Date = i.BookingDate 
        AND fs.StartTime <= i.StartTime AND fs.EndTime >= i.EndTime;  -- Match khoảng
END
GO

-- Các trigger khác như tr_Bookings_UpdatedAt, v.v. (giữ nguyên)

-- =====================================================
-- 12. VIEWS (CẬP NHẬT)
-- =====================================================

GO
CREATE VIEW vw_BookingStats_Daily
AS
SELECT 
    b.BookingDate,
    COUNT(*) AS TotalBookings,
    SUM(b.TotalPrice) AS TotalRevenue,
    AVG(DATEDIFF(MINUTE, b.StartTime, b.EndTime)) AS AvgDurationMin
FROM Bookings b
GROUP BY b.BookingDate;
GO

-- Các view khác như vw_FieldStats_ByType, vw_Revenue_Monthly (giữ nguyên, adjust nếu cần)

-- =====================================================
-- HOÀN THÀNH: Chạy script này để test
-- Ví dụ gọi SP: EXEC sp_BookDynamicField @FieldId=1, @BookingDate='2025-10-15', @StartTime='06:00', @Duration=90, @TotalPrice OUTPUT, @OverlapError OUTPUT
-- =====================================================
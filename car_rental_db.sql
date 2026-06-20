-- 1. TẠO BẢNG PHÂN QUYỀN (ROLES)
CREATE TABLE Roles (
    role_id INT IDENTITY(1,1) NOT NULL,
    role_name NVARCHAR(50) NOT NULL,
    
    CONSTRAINT PK_Roles PRIMARY KEY (role_id),
    CONSTRAINT UC_RoleName UNIQUE (role_name)
);

-- 2. TẠO BẢNG TÀI KHOẢN GỐC (USERS)
CREATE TABLE Users (
    user_id INT IDENTITY(1,1) NOT NULL,
    role_id INT NOT NULL,
    email VARCHAR(100) NOT NULL UNIQUE,
    password VARCHAR(255) NOT NULL,
    phone_number VARCHAR(15) NOT NULL UNIQUE,
    status VARCHAR(20) NOT NULL DEFAULT 'Active', -- Active, Inactive, Blocked, Pending_Approval
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    updated_at DATETIME NOT NULL DEFAULT GETDATE(),
    
    CONSTRAINT PK_Users PRIMARY KEY (user_id),
    CONSTRAINT UC_UserEmail UNIQUE (email),
    CONSTRAINT UC_UserPhone UNIQUE (phone_number),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (role_id) REFERENCES Roles(role_id)
);

-- 3. TẠO BẢNG HỒ SƠ CHI TIẾT CÁ NÂN (PROFILES)
-- 3. TẠO BẢNG HỒ SƠ CHI TIẾT CÁ NHÂN (PROFILES)
CREATE TABLE Profiles (
    profile_id INT IDENTITY(1,1) NOT NULL,
    user_id INT NOT NULL,
    full_name NVARCHAR(100) NOT NULL,
    avatar_url VARCHAR(255) NULL,
    driver_license_no VARCHAR(20) NULL,
    id_card_no VARCHAR(20) NULL,
    address NVARCHAR(255) NULL,
    
    CONSTRAINT PK_Profiles PRIMARY KEY (profile_id),
    CONSTRAINT UC_ProfileUser UNIQUE (user_id),
    -- ĐÃ XÓA CONSTRAINT UC_DriverLicense Ở ĐÂY
    -- ĐÃ XÓA CONSTRAINT UC_IdCard Ở ĐÂY
    CONSTRAINT FK_Profiles_Users FOREIGN KEY (user_id) REFERENCES Users(user_id) ON DELETE CASCADE
);

-- TẠO CHỈ MỤC DUY NHẤT CÓ BỘ LỌC (Cho phép nhiều tài khoản để trống NULL bằng lái & CCCD)
CREATE UNIQUE NONCLUSTERED INDEX UC_DriverLicense 
ON Profiles(driver_license_no) 
WHERE driver_license_no IS NOT NULL;

CREATE UNIQUE NONCLUSTERED INDEX UC_IdCard 
ON Profiles(id_card_no) 
WHERE id_card_no IS NOT NULL;


-- 4. TẠO BẢNG PHÂN LOẠI XE (CAR TYPES)
CREATE TABLE CarTypes (
    type_id INT IDENTITY(1,1) NOT NULL,
    type_name NVARCHAR(50) NOT NULL,
    description NVARCHAR(MAX) NULL,
    
    CONSTRAINT PK_CarTypes PRIMARY KEY (type_id),
    CONSTRAINT UC_TypeName UNIQUE (type_name)
);

-- 5. TẠO BẢNG DANH SÁCH XE (CARS)
CREATE TABLE Cars (
    car_id INT IDENTITY(1,1) NOT NULL,
    owner_id INT NOT NULL,
    type_id INT NOT NULL,
    car_name NVARCHAR(100) NOT NULL,
    brand NVARCHAR(50) NOT NULL,
    model VARCHAR(50) NOT NULL,
    license_plate VARCHAR(20) NOT NULL,
    specs_json NVARCHAR(MAX) NULL, -- Lưu trữ thông tin kỹ thuật dạng chuỗi JSON
    price_per_day DECIMAL(12,2) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'Pending_Approval', -- Pending_Approval, Available, Rented, Maintenance, Hidden
    document_url VARCHAR(255) NULL,
    
    CONSTRAINT PK_Cars PRIMARY KEY (car_id),
    CONSTRAINT UC_LicensePlate UNIQUE (license_plate),
    CONSTRAINT FK_Cars_Owners FOREIGN KEY (owner_id) REFERENCES Users(user_id),
    CONSTRAINT FK_Cars_CarTypes FOREIGN KEY (type_id) REFERENCES CarTypes(type_id)
);

-- 6. TẠO BẢNG LƯU TRỮ ẢNH XE (CAR IMAGES)
CREATE TABLE CarImages (
    image_id INT IDENTITY(1,1) NOT NULL,
    car_id INT NOT NULL,
    image_url VARCHAR(255) NOT NULL,
    is_primary BIT NOT NULL DEFAULT 0,
    
    CONSTRAINT PK_CarImages PRIMARY KEY (image_id),
    CONSTRAINT FK_CarImages_Cars FOREIGN KEY (car_id) REFERENCES Cars(car_id) ON DELETE CASCADE
);

-- 7. TẠO BẢNG ĐƠN ĐẶT THUÊ XE (BOOKINGS)
CREATE TABLE Bookings (
    booking_id INT IDENTITY(1,1) NOT NULL,
    customer_id INT NOT NULL,
    car_id INT NOT NULL,
    start_date DATETIME NOT NULL,
    end_date DATETIME NOT NULL,
    total_days INT NOT NULL,
    subtotal_fee DECIMAL(12,2) NOT NULL,
    platform_commission DECIMAL(12,2) NOT NULL,
    owner_payout DECIMAL(12,2) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'Pending', -- Pending, Approved, Rejected, Active, Completed, Cancelled
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    
    CONSTRAINT PK_Bookings PRIMARY KEY (booking_id),
    CONSTRAINT FK_Bookings_Customers FOREIGN KEY (customer_id) REFERENCES Users(user_id),
    CONSTRAINT FK_Bookings_Cars FOREIGN KEY (car_id) REFERENCES Cars(car_id)
);

-- 8. TẠO BẢNG NHẬT KÝ TRẠNG THÁI ĐƠN HÀNG (BOOKING HISTORY)
CREATE TABLE BookingHistory (
    history_id INT IDENTITY(1,1) NOT NULL,
    booking_id INT NOT NULL,
    changed_by INT NOT NULL,
    old_status VARCHAR(20) NULL,
    new_status VARCHAR(20) NOT NULL,
    note NVARCHAR(MAX) NULL,
    changed_at DATETIME NOT NULL DEFAULT GETDATE(),
    
    CONSTRAINT PK_BookingHistory PRIMARY KEY (history_id),
    CONSTRAINT FK_History_Bookings FOREIGN KEY (booking_id) REFERENCES Bookings(booking_id) ON DELETE CASCADE,
    CONSTRAINT FK_History_Users FOREIGN KEY (changed_by) REFERENCES Users(user_id)
);

-- 9. TẠO BẢNG ĐÁNH GIÁ (FEEDBACKS) - ĐÃ LOẠI BỎ CAR_ID ĐỂ ĐẠT CHUẨN 3NF
CREATE TABLE Feedbacks (
    feedback_id INT IDENTITY(1,1) NOT NULL,
    booking_id INT NOT NULL,
    customer_id INT NOT NULL,
    rating INT NOT NULL CHECK (rating BETWEEN 1 AND 5),
    comment NVARCHAR(MAX) NULL,
    owner_reply NVARCHAR(MAX) NULL,
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    updated_at DATETIME NOT NULL DEFAULT GETDATE(),
    
    CONSTRAINT PK_Feedbacks PRIMARY KEY (feedback_id),
    CONSTRAINT UC_FeedbackBooking UNIQUE (booking_id), -- Một booking chỉ được đánh giá 1 lần duy nhất
    CONSTRAINT FK_Feedbacks_Bookings FOREIGN KEY (booking_id) REFERENCES Bookings(booking_id),
    CONSTRAINT FK_Feedbacks_Customers FOREIGN KEY (customer_id) REFERENCES Users(user_id)
);

-- 10. TẠO BẢNG GIAO DỊCH THANH TOÁN (PAYMENTS)
CREATE TABLE Payments (
    payment_id INT IDENTITY(1,1) NOT NULL,
    booking_id INT NOT NULL,
    amount DECIMAL(12,2) NOT NULL,
    payment_method VARCHAR(50) NOT NULL, -- VNPay, Momo, Banking, Cash
    transaction_reference VARCHAR(100) NULL,
    payment_status VARCHAR(20) NOT NULL DEFAULT 'Pending', -- Pending, Paid, Failed, Refunded
    paid_at DATETIME NULL,
    
    CONSTRAINT PK_Payments PRIMARY KEY (payment_id),
    CONSTRAINT FK_Payments_Bookings FOREIGN KEY (booking_id) REFERENCES Bookings(booking_id) ON DELETE CASCADE
);

-- ====================================================================
-- CHÈN DỮ LIỆU MẪU BAN ĐẦU CHO HỆ THỐNG (SEED DATA)
-- ====================================================================
INSERT INTO Roles (role_name) VALUES 
('Admin'),
('Staff'),
('Customer'),
('Owner');
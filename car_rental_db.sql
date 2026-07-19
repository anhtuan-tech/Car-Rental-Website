SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

CREATE TABLE Role (
    role_id INT IDENTITY(1,1) NOT NULL,
    role_name NVARCHAR(50) NOT NULL,
    
    CONSTRAINT PK_Role PRIMARY KEY (role_id),
    CONSTRAINT UC_RoleName UNIQUE (role_name)
);

CREATE TABLE [User] (
    user_id INT IDENTITY(1,1) NOT NULL,
    role_id INT NOT NULL,
    email VARCHAR(100) NOT NULL UNIQUE,
    password VARCHAR(255) NOT NULL,
    phone_number VARCHAR(15) NOT NULL UNIQUE,
    status VARCHAR(20) NOT NULL DEFAULT 'Active', -- Active, Deleted, Inactive, Banned, Pending_Approval
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    updated_at DATETIME NOT NULL DEFAULT GETDATE(),
    
    CONSTRAINT PK_User PRIMARY KEY (user_id),
    CONSTRAINT UC_UserEmail UNIQUE (email),
    CONSTRAINT UC_UserPhone UNIQUE (phone_number),
    CONSTRAINT FK_User_Role FOREIGN KEY (role_id) REFERENCES Role(role_id)
);

CREATE TABLE Profile (
    profile_id INT IDENTITY(1,1) NOT NULL,
    user_id INT NOT NULL,
    full_name NVARCHAR(100) NOT NULL,
    avatar_url VARCHAR(255) NULL,
    driver_license_no VARCHAR(20) NULL,
    id_card_no VARCHAR(20) NULL,
    address NVARCHAR(255) NULL,
    
    CONSTRAINT PK_Profile PRIMARY KEY (profile_id),
    CONSTRAINT UC_ProfileUser UNIQUE (user_id),
    CONSTRAINT FK_Profile_User FOREIGN KEY (user_id) REFERENCES [User](user_id) ON DELETE CASCADE
);

CREATE UNIQUE NONCLUSTERED INDEX UC_DriverLicense 
ON Profile(driver_license_no) 
WHERE driver_license_no IS NOT NULL;

CREATE UNIQUE NONCLUSTERED INDEX UC_IdCard 
ON Profile(id_card_no) 
WHERE id_card_no IS NOT NULL;


CREATE TABLE CarType (
    type_id INT IDENTITY(1,1) NOT NULL,
    type_name NVARCHAR(50) NOT NULL,
    description NVARCHAR(MAX) NULL,
    
    CONSTRAINT PK_CarType PRIMARY KEY (type_id),
    CONSTRAINT UC_TypeName UNIQUE (type_name)
);

CREATE TABLE Car (
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
    
    CONSTRAINT PK_Car PRIMARY KEY (car_id),
    CONSTRAINT UC_LicensePlate UNIQUE (license_plate),
    CONSTRAINT FK_Cars_Owner FOREIGN KEY (owner_id) REFERENCES [User](user_id),
    CONSTRAINT FK_Cars_CarType FOREIGN KEY (type_id) REFERENCES CarType(type_id)
);

CREATE TABLE CarImage (
    image_id INT IDENTITY(1,1) NOT NULL,
    car_id INT NOT NULL,
    image_url VARCHAR(255) NOT NULL,
    is_primary BIT NOT NULL DEFAULT 0,
    
    CONSTRAINT PK_CarImage PRIMARY KEY (image_id),
    CONSTRAINT FK_CarImage_Car FOREIGN KEY (car_id) REFERENCES Car(car_id) ON DELETE CASCADE
);

CREATE TABLE Booking (
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
    
    CONSTRAINT PK_Booking PRIMARY KEY (booking_id),
    CONSTRAINT FK_Booking_Customer FOREIGN KEY (customer_id) REFERENCES [User](user_id),
    CONSTRAINT FK_Booking_Car FOREIGN KEY (car_id) REFERENCES Car(car_id)
);

CREATE TABLE BookingHistory (
    history_id INT IDENTITY(1,1) NOT NULL,
    booking_id INT NOT NULL,
    changed_by INT NOT NULL,
    old_status VARCHAR(20) NULL,
    new_status VARCHAR(20) NOT NULL,
    note NVARCHAR(MAX) NULL,
    changed_at DATETIME NOT NULL DEFAULT GETDATE(),
    
    CONSTRAINT PK_BookingHistory PRIMARY KEY (history_id),
    CONSTRAINT FK_History_Booking FOREIGN KEY (booking_id) REFERENCES Booking(booking_id) ON DELETE CASCADE,
    CONSTRAINT FK_History_User FOREIGN KEY (changed_by) REFERENCES [User](user_id)
);

CREATE TABLE Feedback (
    feedback_id INT IDENTITY(1,1) NOT NULL,
    booking_id INT NOT NULL,
    customer_id INT NOT NULL,
    rating INT NOT NULL CHECK (rating BETWEEN 1 AND 5),
    comment NVARCHAR(MAX) NULL,
    owner_reply NVARCHAR(MAX) NULL,
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    updated_at DATETIME NOT NULL DEFAULT GETDATE(),
    
    CONSTRAINT PK_Feedback PRIMARY KEY (feedback_id),
    CONSTRAINT UC_FeedbackBooking UNIQUE (booking_id), -- Một booking chỉ được đánh giá 1 lần duy nhất
    CONSTRAINT FK_Feedback_Booking FOREIGN KEY (booking_id) REFERENCES Booking(booking_id),
    CONSTRAINT FK_Feedback_Customer FOREIGN KEY (customer_id) REFERENCES [User](user_id)
);

CREATE TABLE Payment (
    payment_id INT IDENTITY(1,1) NOT NULL,
    booking_id INT NOT NULL,
    amount DECIMAL(12,2) NOT NULL,
    payment_method VARCHAR(50) NOT NULL, -- VNPay, Momo, Banking, Cash
    transaction_reference VARCHAR(100) NULL,
    payment_status VARCHAR(20) NOT NULL DEFAULT 'Pending', -- Pending, Paid, Failed, Refunded
    paid_at DATETIME NULL,
    
    CONSTRAINT PK_Payment PRIMARY KEY (payment_id),
    CONSTRAINT FK_Payment_Booking FOREIGN KEY (booking_id) REFERENCES Booking(booking_id) ON DELETE CASCADE
);

-- ====================================================================
-- CHÈN DỮ LIỆU MẪU BAN ĐẦU CHO HỆ THỐNG (SEED DATA)
-- ====================================================================
INSERT INTO Role (role_name) VALUES 
('Admin'),
('Staff'),
('Customer'),
('Owner');
-- ====================================================================
-- KỊCH BẢN CHÈN DỮ LIỆU MẪU (SEED DATA) 
-- Mật khẩu mặc định cho TẤT CẢ tài khoản: 123456
-- Chuỗi SHA-256 của '123456': 8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92
-- ====================================================================

-- --------------------------------------------------------------------
-- 1. BẢNG [User]
-- Role ID mapping: 1 = Admin, 2 = Staff, 3 = Customer, 4 = Owner
-- --------------------------------------------------------------------
INSERT INTO [User] (role_id, email, password, phone_number, status) VALUES
-- Admin & Staff (Pass: 123456)
(1, 'admin@gara.com', '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', '0901000001', 'Active'), -- user_id = 1
(2, 'staff01@gara.com', '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', '0902000002', 'Active'), -- user_id = 2

-- Customers (Pass: 123456)
(3, 'customer1@gmail.com', '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', '0903000003', 'Active'), -- user_id = 3 (KHÁCH CÓ 10 BOOKINGS)
(3, 'customer2@gmail.com', '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', '0903000004', 'Active'), -- user_id = 4

-- Car Owners (Pass: 123456)
(4, 'owner.nguyen@gmail.com', '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', '0904000005', 'Active'), -- user_id = 5
(4, 'owner.tran@gmail.com', '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', '0904000006', 'Active');  -- user_id = 6


-- --------------------------------------------------------------------
-- 2. BẢNG Profile
-- --------------------------------------------------------------------
INSERT INTO Profile (user_id, full_name, avatar_url, driver_license_no, id_card_no, address) VALUES
(1, N'Quản Trị Viên', 'https://cdn-icons-png.flaticon.com/512/3135/3135715.png', NULL, '001090000001', N'123 Lê Lợi, Q.1, TP.HCM'),
(2, N'Nhân Viên Nguyễn Văn B', 'https://cdn-icons-png.flaticon.com/512/3135/3135715.png', NULL, '001090000002', N'456 Nguyễn Huệ, Q.1, TP.HCM'),
(3, N'Trần Văn Khách (Vip Test)', 'https://i.pravatar.cc/150?img=3', 'GPLX-99887766', '001090000003', N'789 Cách Mạng Tháng 8, Q.3, TP.HCM'),
(4, N'Lê Thị Thu', 'https://i.pravatar.cc/150?img=4', 'GPLX-11223344', '001090000004', N'101 Võ Văn Tần, Q.3, TP.HCM'),
(5, N'Chủ Xe Nguyễn Văn Phát', 'https://i.pravatar.cc/150?img=5', 'GPLX-55667788', '001090000005', N'12 Điện Biên Phủ, Bình Thạnh, TP.HCM'),
(6, N'Chủ Xe Trần Thị Giàu', 'https://i.pravatar.cc/150?img=6', 'GPLX-66778899', '001090000006', N'34 Xô Viết Nghệ Tĩnh, Bình Thạnh, TP.HCM');


-- --------------------------------------------------------------------
-- 3. BẢNG CarType
-- --------------------------------------------------------------------
INSERT INTO CarType (type_name, description) VALUES
(N'Sedan', N'Xe 4-5 chỗ gầm thấp, phù hợp di chuyển gia đình, đô thị'),
(N'SUV', N'Xe 5-7 chỗ gầm cao, thể thao, phù hợp đi đường trường/dã ngoại'),
(N'Hatchback', N'Xe 5 chỗ nhỏ gọn, dễ xoay sở trong phố'),
(N'MPV / 7 Chỗ', N'Xe gia đình đông người hoặc chở hành lý rộng rãi');


-- --------------------------------------------------------------------
-- 4. BẢNG Car
-- --------------------------------------------------------------------
INSERT INTO Car (owner_id, type_id, car_name, brand, model, license_plate, specs_json, price_per_day, status, document_url) VALUES
-- Xe của Chủ xe Nguyễn Văn Phát (owner_id = 5)
(5, 1, N'Toyota Camry 2.5Q', N'Toyota', '2022', '51H-123.45', N'{"seats": 5, "transmission": "Automatic", "fuel": "Gasoline", "consumption": "7.5L/100km"}', 1500000.00, 'Available', 'https://gara.com/docs/51h12345.pdf'),
(5, 2, N'Mazda CX-5 2.0 Luxury', N'Mazda', '2023', '51K-678.90', N'{"seats": 5, "transmission": "Automatic", "fuel": "Gasoline", "consumption": "7.0L/100km"}', 1200000.00, 'Available', 'https://gara.com/docs/51k67890.pdf'),
(5, 4, N'Mitsubishi Xpander Premium', N'Mitsubishi', '2022', '51G-333.44', N'{"seats": 7, "transmission": "Automatic", "fuel": "Gasoline", "consumption": "6.8L/100km"}', 1000000.00, 'Available', 'https://gara.com/docs/51g33344.pdf'),

-- Xe của Chủ xe Trần Thị Giàu (owner_id = 6)
(6, 2, N'Hyundai SantaFe 2.2 Diesel', N'Hyundai', '2023', '51L-888.99', N'{"seats": 7, "transmission": "Automatic", "fuel": "Diesel", "consumption": "6.5L/100km"}', 1800000.00, 'Available', 'https://gara.com/docs/51l88899.pdf'),
(6, 3, N'Kia Morning GT-Line', N'Kia', '2021', '51F-555.66', N'{"seats": 5, "transmission": "Automatic", "fuel": "Gasoline", "consumption": "5.5L/100km"}', 600000.00, 'Pending_Approval', 'https://gara.com/docs/51f55566.pdf'),
(6, 1, N'Honda City RS', N me, '2022', '51H-999.11', N'{"seats": 5, "transmission": "Automatic", "fuel": "Gasoline", "consumption": "6.0L/100km"}', 800000.00, 'Maintenance', 'https://gara.com/docs/51h99911.pdf');


-- --------------------------------------------------------------------
-- 5. BẢNG CarImage
-- --------------------------------------------------------------------
INSERT INTO CarImage (car_id, image_url, is_primary) VALUES
-- Toyota Camry (car_id = 1)
(1, 'https://images.unsplash.com/photo-1621007947382-bb3c3994e3fb?w=800', 1),
(1, 'https://images.unsplash.com/photo-1549399542-7e3f8b79c341?w=800', 0),
-- Mazda CX-5 (car_id = 2)
(2, 'https://images.unsplash.com/photo-1533473359331-0135ef1b58bf?w=800', 1),
-- Xpander (car_id = 3)
(3, 'https://images.unsplash.com/photo-1552519507-da3b142c6e3d?w=800', 1),
-- SantaFe (car_id = 4)
(4, 'https://images.unsplash.com/photo-1503376780353-7e6692767b70?w=800', 1);


-- --------------------------------------------------------------------
-- 6. BẢNG Booking (Tạo 10 Bookings khác nhau cho Customer1 - customer_id = 3)
-- Tỷ lệ ăn chia: Commission (10%), Owner Payout (90%)
-- --------------------------------------------------------------------
INSERT INTO Booking (customer_id, car_id, start_date, end_date, total_days, subtotal_fee, platform_commission, owner_payout, status, created_at) VALUES
-- 1. Completed (Đã hoàn thành xuất sắc)
(3, 2, '2026-01-10 08:00:00', '2026-01-13 08:00:00', 3, 3600000.00, 360000.00, 3240000.00, 'Completed', '2026-01-08 09:00:00'),

-- 2. Completed (Đã hoàn thành chuyến 2)
(3, 1, '2026-02-01 08:00:00', '2026-02-03 08:00:00', 2, 3000000.00, 300000.00, 2700000.00, 'Completed', '2026-01-28 10:15:00'),

-- 3. Completed (Đã hoàn thành chuyến 3)
(3, 4, '2026-03-05 07:00:00', '2026-03-09 07:00:00', 4, 7200000.00, 720000.00, 6480000.00, 'Completed', '2026-03-01 14:20:00'),

-- 4. Active (Khách đang lấy xe chạy hiện tại)
(3, 2, '2026-07-19 08:00:00', '2026-07-22 08:00:00', 3, 3600000.00, 360000.00, 3240000.00, 'Active', '2026-07-17 11:00:00'),

-- 5. Approved (Đã duyệt & Đã thanh toán, chờ tới ngày lấy xe)
(3, 1, '2026-08-01 08:00:00', '2026-08-04 08:00:00', 3, 4500000.00, 450000.00, 4050000.00, 'Approved', '2026-07-18 16:00:00'),

-- 6. Approved (Đã duyệt nhưng chưa thanh toán)
(3, 3, '2026-08-10 07:00:00', '2026-08-12 07:00:00', 2, 2000000.00, 200000.00, 1800000.00, 'Approved', '2026-07-19 08:30:00'),

-- 7. Pending (Mới gửi yêu cầu, đang chờ Admin/Staff duyệt)
(3, 4, '2026-09-01 08:00:00', '2026-09-05 08:00:00', 4, 7200000.00, 720000.00, 6480000.00, 'Pending', '2026-07-20 10:00:00'),

-- 8. Cancelled (Khách tự hủy đơn)
(3, 3, '2026-04-10 08:00:00', '2026-04-11 08:00:00', 1, 1000000.00, 100000.00, 900000.00, 'Cancelled', '2026-04-05 09:00:00'),

-- 9. Rejected (Gara từ chối do trùng lịch)
(3, 1, '2026-05-01 08:00:00', '2026-05-03 08:00:00', 2, 3000000.00, 300000.00, 2700000.00, 'Rejected', '2026-04-28 15:45:00'),

-- 10. Completed (Chuyến đi hồi đầu năm)
(3, 3, '2026-01-01 08:00:00', '2026-01-02 08:00:00', 1, 1000000.00, 100000.00, 900000.00, 'Completed', '2025-12-29 11:30:00');


-- --------------------------------------------------------------------
-- 7. BẢNG BookingHistory (Nhật ký thay đổi trạng thái của đơn)
-- --------------------------------------------------------------------
INSERT INTO BookingHistory (booking_id, changed_by, old_status, new_status, note, changed_at) VALUES
-- History cho Booking 1
(1, 3, NULL, 'Pending', N'Khách hàng khởi tạo đơn hàng', '2026-01-08 09:00:00'),
(1, 2, 'Pending', 'Approved', N'Nhân viên đã xác nhận GPLX hợp lệ', '2026-01-08 10:00:00'),
(1, 2, 'Approved', 'Active', N'Khách đã đến gara bàn giao xe', '2026-01-10 08:15:00'),
(1, 2, 'Active', 'Completed', N'Khách trả xe nguyên vẹn, kết thúc hợp đồng', '2026-01-13 08:30:00'),

-- History cho Booking 4 (Đang Active)
(4, 3, NULL, 'Pending', N'Khách hàng tạo đơn', '2026-07-17 11:00:00'),
(4, 2, 'Pending', 'Approved', N'Duyệt đơn', '2026-07-17 14:00:00'),
(4, 2, 'Approved', 'Active', N'Giao xe cho khách tại quầy', '2026-07-19 08:10:00'),

-- History cho Booking 8 (Hủy)
(8, 3, NULL, 'Pending', N'Khách tạo đơn', '2026-04-05 09:00:00'),
(8, 3, 'Pending', 'Cancelled', N'Khách báo bận việc đột xuất nên tự hủy', '2026-04-06 10:00:00'),

-- History cho Booking 9 (Từ chối)
(9, 3, NULL, 'Pending', N'Khách tạo đơn', '2026-04-28 15:45:00'),
(9, 2, 'Pending', 'Rejected', N'Xe bị trùng lịch bảo dưỡng định kỳ', '2026-04-29 09:00:00');


-- --------------------------------------------------------------------
-- 8. BẢNG Payment
-- --------------------------------------------------------------------
INSERT INTO Payment (booking_id, amount, payment_method, transaction_reference, payment_status, paid_at) VALUES
(1, 3600000.00, 'VNPay', 'VNP13904812', 'Paid', '2026-01-08 10:15:00'),
(2, 3000000.00, 'Momo', 'MM99812401', 'Paid', '2026-01-28 10:20:00'),
(3, 7200000.00, 'Banking', 'FT260305102', 'Paid', '2026-03-01 15:00:00'),
(4, 3600000.00, 'Banking', 'FT260718001', 'Paid', '2026-07-17 14:30:00'),
(5, 4500000.00, 'VNPay', 'VNP26071899', 'Paid', '2026-07-18 16:05:00'),
(6, 2000000.00, 'Cash', NULL, 'Pending', NULL), -- Booking 6 chưa trả tiền
(10, 1000000.00, 'Cash', 'CASH-0012', 'Paid', '2026-01-01 08:00:00');


-- --------------------------------------------------------------------
-- 9. BẢNG Feedback (Khách đánh giá cho các đơn Completed)
-- --------------------------------------------------------------------
INSERT INTO Feedback (booking_id, customer_id, rating, comment, owner_reply, created_at, updated_at) VALUES
(1, 3, 5, N'Xe Mazda CX-5 chạy rất mượt, nội thất sạch sẽ thơm tho. Gara hỗ trợ rất nhiệt tình!', N'Cảm ơn bạn đã ủng hộ dịch vụ của gara và chủ xe!', '2026-01-13 10:00:00', '2026-01-13 11:30:00'),
(2, 3, 5, N'Camry đi êm, cách âm chuẩn hạng D. Lần sau sẽ tiếp tục thuê lại.', NULL, '2026-02-03 09:30:00', '2026-02-03 09:30:00'),
(10, 3, 4, N'Xe ổn trong tầm giá, tuy nhiên điều hòa lạnh hơi chậm một chút.', N'Gara đã tiếp nhận phản hồi và cho bảo dưỡng lại hệ thống điều hòa rồi ạ.', '2026-01-02 10:00:00', '2026-01-02 14:00:00');
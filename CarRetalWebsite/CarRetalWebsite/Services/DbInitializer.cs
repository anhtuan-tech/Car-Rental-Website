using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CarRetalWebsite.Models;

namespace CarRetalWebsite.Services
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(CarRentalDbContext context)
        {
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // 1. Seed Roles
            if (!await context.Roles.AnyAsync())
            {
                var roles = new List<Role>
                {
                    new Role { RoleName = "Admin" },
                    new Role { RoleName = "Staff" },
                    new Role { RoleName = "Customer" },
                    new Role { RoleName = "Owner" }
                };
                await context.Roles.AddRangeAsync(roles);
                await context.SaveChangesAsync();
            }

            var rolesDict = await context.Roles.ToDictionaryAsync(r => r.RoleName, r => r.RoleId);

            // Helper for SHA256 Password Hashing ("123456")
            string hashPassword(string pass)
            {
                using var sha256 = SHA256.Create();
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(pass));
                var sb = new StringBuilder();
                foreach (var b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }

            string defaultPasswordHash = hashPassword("123456");

            // 2. Seed Users & Profiles
            if (!await context.Users.AnyAsync())
            {
                var users = new List<User>
                {
                    new User
                    {
                        RoleId = rolesDict["Admin"],
                        Email = "admin@carrental.vn",
                        Password = defaultPasswordHash,
                        PhoneNumber = "0901111111",
                        Status = "Active",
                        Profile = new Profile
                        {
                            FullName = "Hệ Thống Quản Trị",
                            Address = "123 Nguyễn Huệ, Quận 1, TP. Hồ Chí Minh",
                            AvatarUrl = "https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&w=300&q=80",
                            IdCardNo = "079199000001"
                        }
                    },
                    new User
                    {
                        RoleId = rolesDict["Staff"],
                        Email = "staff@carrental.vn",
                        Password = defaultPasswordHash,
                        PhoneNumber = "0902222222",
                        Status = "Active",
                        Profile = new Profile
                        {
                            FullName = "Nhân Viên Gara",
                            Address = "456 Lê Lợi, Quận 1, TP. Hồ Chí Minh",
                            AvatarUrl = "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&w=300&q=80",
                            IdCardNo = "079199000002"
                        }
                    },
                    new User
                    {
                        RoleId = rolesDict["Owner"],
                        Email = "owner1@carrental.vn",
                        Password = defaultPasswordHash,
                        PhoneNumber = "0903333333",
                        Status = "Active",
                        Profile = new Profile
                        {
                            FullName = "Nguyễn Văn Chủ (Chủ Xe 1)",
                            Address = "789 Điện Biên Phủ, Bình Thạnh, TP. HCM",
                            AvatarUrl = "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?auto=format&fit=crop&w=300&q=80",
                            IdCardNo = "079199000003"
                        }
                    },
                    new User
                    {
                        RoleId = rolesDict["Owner"],
                        Email = "owner2@carrental.vn",
                        Password = defaultPasswordHash,
                        PhoneNumber = "0904444444",
                        Status = "Active",
                        Profile = new Profile
                        {
                            FullName = "Trần Thị Gara (Chủ Xe 2)",
                            Address = "101 Võ Văn Kệt, Quận 5, TP. HCM",
                            AvatarUrl = "https://images.unsplash.com/photo-1544005313-94ddf0286df2?auto=format&fit=crop&w=300&q=80",
                            IdCardNo = "079199000004"
                        }
                    },
                    new User
                    {
                        RoleId = rolesDict["Customer"],
                        Email = "customer1@carrental.vn",
                        Password = defaultPasswordHash,
                        PhoneNumber = "0905555555",
                        Status = "Active",
                        Profile = new Profile
                        {
                            FullName = "Lê Hoàng Khách",
                            Address = "12 Nguyễn Văn Cừ, Quận 5, TP. HCM",
                            AvatarUrl = "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?auto=format&fit=crop&w=300&q=80",
                            DriverLicenseNo = "123456789012",
                            IdCardNo = "079199000005"
                        }
                    },
                    new User
                    {
                        RoleId = rolesDict["Customer"],
                        Email = "customer2@carrental.vn",
                        Password = defaultPasswordHash,
                        PhoneNumber = "0906666666",
                        Status = "Active",
                        Profile = new Profile
                        {
                            FullName = "Phạm Minh Quân",
                            Address = "88 Hoàng Diệu, Quận 4, TP. HCM",
                            AvatarUrl = "https://images.unsplash.com/photo-1570295999919-56ceb5ecca61?auto=format&fit=crop&w=300&q=80",
                            DriverLicenseNo = "987654321098",
                            IdCardNo = "079199000006"
                        }
                    }
                };

                await context.Users.AddRangeAsync(users);
                await context.SaveChangesAsync();
            }

            // 3. Seed CarTypes
            if (!await context.CarTypes.AnyAsync())
            {
                var carTypes = new List<CarType>
                {
                    new CarType { TypeName = "Sedan", Description = "Dòng xe 4-5 chỗ gầm thấp, sang trọng, di chuyển êm ái đô thị." },
                    new CarType { TypeName = "SUV", Description = "Dòng xe thể thao đa dụng gầm cao, phù hợp di chuyển đa địa hình." },
                    new CarType { TypeName = "Crossover", Description = "Xe gầm cao đô thị linh hoạt, tiết kiệm nhiên liệu." },
                    new CarType { TypeName = "Bán tải", Description = "Xe bán tải mạnh mẽ, chở hàng hóa và địa hình đèo dốc." },
                    new CarType { TypeName = "MPV", Description = "Xe gia đình 7-8 chỗ rộng rãi, tiện nghi cho chuyến đi xa." },
                    new CarType { TypeName = "Hatchback", Description = "Xe compact nhỏ gọn, dễ quay đầu trong đường phố hẹp." }
                };
                await context.CarTypes.AddRangeAsync(carTypes);
                await context.SaveChangesAsync();
            }

            var typeDict = await context.CarTypes.ToDictionaryAsync(t => t.TypeName, t => t.TypeId);
            var ownerUsers = await context.Users.Where(u => u.RoleId == rolesDict["Owner"]).ToListAsync();
            if (!ownerUsers.Any())
            {
                var newOwner = new User
                {
                    RoleId = rolesDict["Owner"],
                    Email = "owner1@carrental.vn",
                    Password = defaultPasswordHash,
                    PhoneNumber = "0903333333",
                    Status = "Active",
                    Profile = new Profile
                    {
                        FullName = "Nguyễn Văn Chủ (Chủ Xe 1)",
                        Address = "789 Điện Biên Phủ, Bình Thạnh, TP. HCM",
                        AvatarUrl = "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?auto=format&fit=crop&w=300&q=80",
                        IdCardNo = "079199000003"
                    }
                };
                await context.Users.AddAsync(newOwner);
                await context.SaveChangesAsync();
                ownerUsers.Add(newOwner);
            }

            var owner1 = ownerUsers.FirstOrDefault(u => u.Email == "owner1@carrental.vn") ?? ownerUsers.First();
            var owner2 = ownerUsers.FirstOrDefault(u => u.Email == "owner2@carrental.vn") ?? ownerUsers.Last();

            // 4. Seed Cars & CarImages
            if (!await context.Cars.AnyAsync())
            {
                var cars = new List<Car>
                {
                    new Car
                    {
                        OwnerId = owner1.UserId,
                        TypeId = typeDict["SUV"],
                        CarName = "Mazda CX-5 Premium",
                        Brand = "Mazda",
                        Model = "2023",
                        LicensePlate = "51H-888.88",
                        PricePerDay = 1200000,
                        Status = "Available",
                        SpecsJson = "{\"seats\":5,\"transmission\":\"Số tự động\",\"fuel\":\"Xăng\",\"consumption\":\"7.2 L/100km\",\"engine\":\"2.0L SkyActiv\",\"year\":2023,\"features\":[\"Bản đồ GPS\",\"Bluetooth\",\"Camera 360\",\"Cửa sổ trời\",\"Phanh ABS\",\"Hệ thống Bose 10 loa\"]}",
                        DocumentUrl = "https://example.com/docs/cx5.pdf",
                        CarImages = new List<CarImage>
                        {
                            new CarImage { ImageUrl = "https://images.unsplash.com/photo-1549399542-7e3f8b79c341?auto=format&fit=crop&w=1200&q=80", IsPrimary = true },
                            new CarImage { ImageUrl = "https://images.unsplash.com/photo-1503376780353-7e6692767b70?auto=format&fit=crop&w=1200&q=80", IsPrimary = false },
                            new CarImage { ImageUrl = "https://images.unsplash.com/photo-1552519507-da3b142c6e3d?auto=format&fit=crop&w=1200&q=80", IsPrimary = false }
                        }
                    },
                    new Car
                    {
                        OwnerId = owner1.UserId,
                        TypeId = typeDict["Sedan"],
                        CarName = "Toyota Camry 2.5Q",
                        Brand = "Toyota",
                        Model = "2022",
                        LicensePlate = "30F-999.99",
                        PricePerDay = 1500000,
                        Status = "Available",
                        SpecsJson = "{\"seats\":5,\"transmission\":\"Số tự động\",\"fuel\":\"Xăng\",\"consumption\":\"7.5 L/100km\",\"engine\":\"2.5L Dynamic Force\",\"year\":2022,\"features\":[\"Bản đồ GPS\",\"Bluetooth\",\"Camera lùi\",\"Ghế da cao cấp\",\"Âm thanh JBL\",\"Cảnh báo điểm mù\"]}",
                        DocumentUrl = "https://example.com/docs/camry.pdf",
                        CarImages = new List<CarImage>
                        {
                            new CarImage { ImageUrl = "https://images.unsplash.com/photo-1621007947382-bb3c3994e3fb?auto=format&fit=crop&w=1200&q=80", IsPrimary = true },
                            new CarImage { ImageUrl = "https://images.unsplash.com/photo-1617814076367-b759c7d7e738?auto=format&fit=crop&w=1200&q=80", IsPrimary = false }
                        }
                    },
                    new Car
                    {
                        OwnerId = owner2.UserId,
                        TypeId = typeDict["SUV"],
                        CarName = "Hyundai SantaFe 2.2D Máy Dầu",
                        Brand = "Hyundai",
                        Model = "2023",
                        LicensePlate = "51G-777.77",
                        PricePerDay = 1600000,
                        Status = "Available",
                        SpecsJson = "{\"seats\":7,\"transmission\":\"Số tự động\",\"fuel\":\"Dầu\",\"consumption\":\"6.8 L/100km\",\"engine\":\"2.2L SmartStream Diesel\",\"year\":2023,\"features\":[\"Bản đồ GPS\",\"Bluetooth\",\"Camera 360\",\"Dẫn động 4 bánh HTRAC\",\"Cửa sổ trời Panorama\",\"Sưởi/làm mát ghế\"]}",
                        DocumentUrl = "https://example.com/docs/santafe.pdf",
                        CarImages = new List<CarImage>
                        {
                            new CarImage { ImageUrl = "https://images.unsplash.com/photo-1563720223185-11003d516935?auto=format&fit=crop&w=1200&q=80", IsPrimary = true },
                            new CarImage { ImageUrl = "https://images.unsplash.com/photo-1533473359331-0135ef1b58bf?auto=format&fit=crop&w=1200&q=80", IsPrimary = false }
                        }
                    },
                    new Car
                    {
                        OwnerId = owner2.UserId,
                        TypeId = typeDict["Bán tải"],
                        CarName = "Ford Ranger Wildtrak 2.0 Bi-Turbo",
                        Brand = "Ford",
                        Model = "2022",
                        LicensePlate = "51C-666.66",
                        PricePerDay = 1100000,
                        Status = "Available",
                        SpecsJson = "{\"seats\":5,\"transmission\":\"Số tự động\",\"fuel\":\"Dầu\",\"consumption\":\"8.0 L/100km\",\"engine\":\"2.0L Bi-Turbo Diesel\",\"year\":2022,\"features\":[\"Bản đồ GPS\",\"Bluetooth\",\"Camera 360\",\"Nắp thùng cuộn điện\",\"Hỗ trợ đỗ xe tự động\",\"Gài cầu điện tử 4x4\"]}",
                        DocumentUrl = "https://example.com/docs/ranger.pdf",
                        CarImages = new List<CarImage>
                        {
                            new CarImage { ImageUrl = "https://images.unsplash.com/photo-1533473359331-0135ef1b58bf?auto=format&fit=crop&w=1200&q=80", IsPrimary = true },
                            new CarImage { ImageUrl = "https://images.unsplash.com/photo-1503376780353-7e6692767b70?auto=format&fit=crop&w=1200&q=80", IsPrimary = false }
                        }
                    },
                    new Car
                    {
                        OwnerId = owner1.UserId,
                        TypeId = typeDict["SUV"],
                        CarName = "VinFast VF 8 Plus",
                        Brand = "VinFast",
                        Model = "2023",
                        LicensePlate = "51K-555.55",
                        PricePerDay = 1400000,
                        Status = "Available",
                        SpecsJson = "{\"seats\":5,\"transmission\":\"Số tự động\",\"fuel\":\"Điện\",\"consumption\":\"Quãng đường 420km / sạc\",\"engine\":\"Động cơ điện 402 mã lực\",\"year\":2023,\"features\":[\"Màn hình 15.6 inch\",\"Trợ lý ảo ViVi\",\"Hệ thống ADAS thông minh\",\"HUD hiển thị kính lái\",\"Trạm sạc phủ rộng toàn quốc\"]}",
                        DocumentUrl = "https://example.com/docs/vf8.pdf",
                        CarImages = new List<CarImage>
                        {
                            new CarImage { ImageUrl = "https://images.unsplash.com/photo-1555215695-3004980ad54e?auto=format&fit=crop&w=1200&q=80", IsPrimary = true }
                        }
                    },
                    new Car
                    {
                        OwnerId = owner2.UserId,
                        TypeId = typeDict["MPV"],
                        CarName = "Kia Carnival Signature 3.5V6",
                        Brand = "Kia",
                        Model = "2023",
                        LicensePlate = "51H-333.33",
                        PricePerDay = 2000000,
                        Status = "Available",
                        SpecsJson = "{\"seats\":7,\"transmission\":\"Số tự động\",\"fuel\":\"Xăng\",\"consumption\":\"9.5 L/100km\",\"engine\":\"Smartstream 3.5L V6\",\"year\":2023,\"features\":[\"Ghế thương gia chỉnh điện\",\"Cửa lùa tự động\",\"Màn hình kép 12.3 inch\",\"Cửa sổ trời kép\",\"Âm thanh 12 loa Bose\"]}",
                        DocumentUrl = "https://example.com/docs/carnival.pdf",
                        CarImages = new List<CarImage>
                        {
                            new CarImage { ImageUrl = "https://images.unsplash.com/photo-1549399542-7e3f8b79c341?auto=format&fit=crop&w=1200&q=80", IsPrimary = true }
                        }
                    }
                };

                await context.Cars.AddRangeAsync(cars);
                await context.SaveChangesAsync();
            }

            // 5. Seed Bookings & Feedbacks
            if (!await context.Bookings.AnyAsync())
            {
                var customer1 = await context.Users.FirstOrDefaultAsync(u => u.Email == "customer1@carrental.vn");
                var customer2 = await context.Users.FirstOrDefaultAsync(u => u.Email == "customer2@carrental.vn");
                var cx5Car = await context.Cars.FirstOrDefaultAsync(c => c.CarName.Contains("CX-5"));
                var santafeCar = await context.Cars.FirstOrDefaultAsync(c => c.CarName.Contains("SantaFe"));
                var vf8Car = await context.Cars.FirstOrDefaultAsync(c => c.CarName.Contains("VF 8"));

                if (customer1 != null && cx5Car != null)
                {
                    var booking1 = new Booking
                    {
                        CustomerId = customer1.UserId,
                        CarId = cx5Car.CarId,
                        StartDate = DateTime.Today.AddDays(-10),
                        EndDate = DateTime.Today.AddDays(-7),
                        TotalDays = 3,
                        SubtotalFee = 3600000,
                        PlatformCommission = 360000,
                        OwnerPayout = 3240000,
                        Status = "Completed",
                        CreatedAt = DateTime.Today.AddDays(-12)
                    };

                    await context.Bookings.AddAsync(booking1);
                    await context.SaveChangesAsync();

                    // Seed Feedback for booking 1
                    var feedback1 = new Feedback
                    {
                        BookingId = booking1.BookingId,
                        CustomerId = customer1.UserId,
                        Rating = 5,
                        Comment = "Xe mới sạch sẽ, máy vận hành rất êm ái. Gara hỗ trợ giao nhận xe cực kỳ nhanh chóng và đúng giờ!",
                        OwnerReply = "Cảm ơn bạn Lê Hoàng Khách đã tin tưởng lựa chọn xe của bên mình! Rất mong sẽ được đồng hành cùng bạn ở các chuyến đi tiếp theo.",
                        CreatedAt = DateTime.Today.AddDays(-6),
                        UpdatedAt = DateTime.Today.AddDays(-6)
                    };
                    await context.Feedbacks.AddAsync(feedback1);

                    // Seed Payment for booking 1
                    var payment1 = new Payment
                    {
                        BookingId = booking1.BookingId,
                        Amount = 3600000,
                        PaymentMethod = "Banking",
                        TransactionReference = "VNPAY-88997711",
                        PaymentStatus = "Paid",
                        PaidAt = DateTime.Today.AddDays(-12)
                    };
                    await context.Payments.AddAsync(payment1);
                }

                if (customer2 != null && santafeCar != null)
                {
                    var booking2 = new Booking
                    {
                        CustomerId = customer2.UserId,
                        CarId = santafeCar.CarId,
                        StartDate = DateTime.Today.AddDays(-5),
                        EndDate = DateTime.Today.AddDays(-3),
                        TotalDays = 2,
                        SubtotalFee = 3200000,
                        PlatformCommission = 320000,
                        OwnerPayout = 2880000,
                        Status = "Completed",
                        CreatedAt = DateTime.Today.AddDays(-6)
                    };

                    await context.Bookings.AddAsync(booking2);
                    await context.SaveChangesAsync();

                    var feedback2 = new Feedback
                    {
                        BookingId = booking2.BookingId,
                        CustomerId = customer2.UserId,
                        Rating = 5,
                        Comment = "Xe 7 chỗ rộng rãi thoải mái cho gia đình đi Đà Lạt. Máy dầu khỏe vượt đèo rất nhẹ nhàng.",
                        OwnerReply = "Cảm ơn anh Quân nhiều nhé! Chúc anh và gia đình luôn có những hành trình thượng lộ bình an.",
                        CreatedAt = DateTime.Today.AddDays(-2),
                        UpdatedAt = DateTime.Today.AddDays(-2)
                    };
                    await context.Feedbacks.AddAsync(feedback2);

                    var payment2 = new Payment
                    {
                        BookingId = booking2.BookingId,
                        Amount = 3200000,
                        PaymentMethod = "Banking",
                        TransactionReference = "VNPAY-99221144",
                        PaymentStatus = "Paid",
                        PaidAt = DateTime.Today.AddDays(-6)
                    };
                    await context.Payments.AddAsync(payment2);
                }

                if (customer1 != null && vf8Car != null)
                {
                    var booking3 = new Booking
                    {
                        CustomerId = customer1.UserId,
                        CarId = vf8Car.CarId,
                        StartDate = DateTime.Today.AddDays(-15),
                        EndDate = DateTime.Today.AddDays(-13),
                        TotalDays = 2,
                        SubtotalFee = 2800000,
                        PlatformCommission = 280000,
                        OwnerPayout = 2520000,
                        Status = "Completed",
                        CreatedAt = DateTime.Today.AddDays(-16)
                    };

                    await context.Bookings.AddAsync(booking3);
                    await context.SaveChangesAsync();

                    var feedback3 = new Feedback
                    {
                        BookingId = booking3.BookingId,
                        CustomerId = customer1.UserId,
                        Rating = 4,
                        Comment = "Trải nghiệm xe điện VinFast VF 8 rât bốc, tăng tốc nhanh và nhiều tính năng ADAS hay.",
                        OwnerReply = "Cảm ơn bạn đã phản hồi tốt. Gara sẽ nâng cấp chuẩn bị sạc sẵn 100% pin trước khi giao xe!",
                        CreatedAt = DateTime.Today.AddDays(-12),
                        UpdatedAt = DateTime.Today.AddDays(-12)
                    };
                    await context.Feedbacks.AddAsync(feedback3);
                }

                await context.SaveChangesAsync();
            }
        }
    }
}

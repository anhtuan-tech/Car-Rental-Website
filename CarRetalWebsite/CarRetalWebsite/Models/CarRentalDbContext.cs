using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace CarRetalWebsite.Models;

public partial class CarRentalDbContext : DbContext
{
    public CarRentalDbContext()
    {
    }

    public CarRentalDbContext(DbContextOptions<CarRentalDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<BookingHistory> BookingHistories { get; set; }

    public virtual DbSet<Car> Cars { get; set; }

    public virtual DbSet<CarImage> CarImages { get; set; }

    public virtual DbSet<CarType> CarTypes { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Profile> Profiles { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<User> Users { get; set; }

    private string GetConnectionString()
    {
        IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile("appsettings.Development.json", true, true)
                .Build();
        return configuration["ConnectionStrings:DefaultConnectionString"] 
               ?? configuration["ConnectionStrings:DefaultConnection"];
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer(GetConnectionString());
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.BookingId).HasName("PK_Bookings");

            entity.ToTable("Booking");

            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.CarId).HasColumnName("car_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.EndDate)
                .HasColumnType("datetime")
                .HasColumnName("end_date");
            entity.Property(e => e.OwnerPayout)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("owner_payout");
            entity.Property(e => e.PlatformCommission)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("platform_commission");
            entity.Property(e => e.StartDate)
                .HasColumnType("datetime")
                .HasColumnName("start_date");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending")
                .HasColumnName("status");
            entity.Property(e => e.SubtotalFee)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("subtotal_fee");
            entity.Property(e => e.TotalDays).HasColumnName("total_days");

            entity.HasOne(d => d.Car).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.CarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bookings_Cars");

            entity.HasOne(d => d.Customer).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bookings_Customers");
        });

        modelBuilder.Entity<BookingHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId);

            entity.ToTable("BookingHistory");

            entity.Property(e => e.HistoryId).HasColumnName("history_id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.ChangedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("changed_at");
            entity.Property(e => e.ChangedBy).HasColumnName("changed_by");
            entity.Property(e => e.NewStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("new_status");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.OldStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("old_status");

            entity.HasOne(d => d.Booking).WithMany(p => p.BookingHistories)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("FK_History_Bookings");

            entity.HasOne(d => d.ChangedByNavigation).WithMany(p => p.BookingHistories)
                .HasForeignKey(d => d.ChangedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_History_Users");
        });

        modelBuilder.Entity<Car>(entity =>
        {
            entity.HasKey(e => e.CarId).HasName("PK_Cars");

            entity.ToTable("Car");

            entity.HasIndex(e => e.LicensePlate, "UC_LicensePlate").IsUnique();

            entity.Property(e => e.CarId).HasColumnName("car_id");
            entity.Property(e => e.Brand)
                .HasMaxLength(50)
                .HasColumnName("brand");
            entity.Property(e => e.CarName)
                .HasMaxLength(100)
                .HasColumnName("car_name");
            entity.Property(e => e.DocumentUrl)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("document_url");
            entity.Property(e => e.LicensePlate)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("license_plate");
            entity.Property(e => e.Model)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("model");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.PricePerDay)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("price_per_day");
            entity.Property(e => e.SpecsJson).HasColumnName("specs_json");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending_Approval")
                .HasColumnName("status");
            entity.Property(e => e.TypeId).HasColumnName("type_id");

            entity.HasOne(d => d.Owner).WithMany(p => p.Cars)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Cars_Owners");

            entity.HasOne(d => d.Type).WithMany(p => p.Cars)
                .HasForeignKey(d => d.TypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Cars_CarTypes");
        });

        modelBuilder.Entity<CarImage>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("PK_CarImages");

            entity.ToTable("CarImage");

            entity.Property(e => e.ImageId).HasColumnName("image_id");
            entity.Property(e => e.CarId).HasColumnName("car_id");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("image_url");
            entity.Property(e => e.IsPrimary).HasColumnName("is_primary");

            entity.HasOne(d => d.Car).WithMany(p => p.CarImages)
                .HasForeignKey(d => d.CarId)
                .HasConstraintName("FK_CarImages_Cars");
        });

        modelBuilder.Entity<CarType>(entity =>
        {
            entity.HasKey(e => e.TypeId).HasName("PK_CarTypes");

            entity.ToTable("CarType");

            entity.HasIndex(e => e.TypeName, "UC_TypeName").IsUnique();

            entity.Property(e => e.TypeId).HasColumnName("type_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.TypeName)
                .HasMaxLength(50)
                .HasColumnName("type_name");
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.FeedbackId).HasName("PK_Feedbacks");

            entity.ToTable("Feedback");

            entity.HasIndex(e => e.BookingId, "UC_FeedbackBooking").IsUnique();

            entity.Property(e => e.FeedbackId).HasColumnName("feedback_id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.OwnerReply).HasColumnName("owner_reply");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Booking).WithOne(p => p.Feedback)
                .HasForeignKey<Feedback>(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Feedbacks_Bookings");

            entity.HasOne(d => d.Customer).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Feedbacks_Customers");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK_Payments");

            entity.ToTable("Payment");

            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.PaidAt)
                .HasColumnType("datetime")
                .HasColumnName("paid_at");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("payment_method");
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending")
                .HasColumnName("payment_status");
            entity.Property(e => e.TransactionReference)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("transaction_reference");

            entity.HasOne(d => d.Booking).WithMany(p => p.Payments)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("FK_Payments_Bookings");
        });

        modelBuilder.Entity<Profile>(entity =>
        {
            entity.HasKey(e => e.ProfileId).HasName("PK_Profiles");

            entity.ToTable("Profile");

            entity.HasIndex(e => e.DriverLicenseNo, "UC_DriverLicense").IsUnique();

            entity.HasIndex(e => e.IdCardNo, "UC_IdCard").IsUnique();

            entity.HasIndex(e => e.UserId, "UC_ProfileUser").IsUnique();

            entity.Property(e => e.ProfileId).HasColumnName("profile_id");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .HasColumnName("address");
            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("avatar_url");
            entity.Property(e => e.DriverLicenseNo)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("driver_license_no");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .HasColumnName("full_name");
            entity.Property(e => e.IdCardNo)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("id_card_no");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithOne(p => p.Profile)
                .HasForeignKey<Profile>(d => d.UserId)
                .HasConstraintName("FK_Profiles_Users");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK_Roles");

            entity.ToTable("Role");

            entity.HasIndex(e => e.RoleName, "UC_RoleName").IsUnique();

            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.RoleName)
                .HasMaxLength(50)
                .HasColumnName("role_name");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK_Users");

            entity.ToTable("User");

            entity.HasIndex(e => e.Email, "UC_UserEmail").IsUnique();

            entity.HasIndex(e => e.PhoneNumber, "UC_UserPhone").IsUnique();

            entity.HasIndex(e => e.PhoneNumber, "UQ__Users__A1936A6B4F0FEA3C").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Users__AB6E616435B5C6D5").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("password");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("phone_number");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Active")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Roles");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

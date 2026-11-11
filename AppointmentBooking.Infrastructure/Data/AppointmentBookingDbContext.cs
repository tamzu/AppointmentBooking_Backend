using AppointmentBooking.Core.Entities;
using AppointmentBooking.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AppointmentBooking.Infrastructure.Data
{
    public class AppointmentBookingDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppointmentBookingDbContext(
            DbContextOptions<AppointmentBookingDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers => Set<Customer>();

        public DbSet<CustomerIdentityDocument> CustomerIdentityDocuments
            => Set<CustomerIdentityDocument>();

        public DbSet<IdentityDocumentType> IdentityDocumentTypes
            => Set<IdentityDocumentType>();

        public DbSet<IdentityVerificationStatus> IdentityVerificationStatuses
            => Set<IdentityVerificationStatus>();

        public DbSet<CustomerIdentityVerification> CustomerIdentityVerifications
            => Set<CustomerIdentityVerification>();

        public DbSet<Branch> Branches => Set<Branch>();

        public DbSet<Service> Services => Set<Service>();

        public DbSet<BranchService> BranchServices => Set<BranchService>();

        public DbSet<BranchOperatingHour> BranchOperatingHours
            => Set<BranchOperatingHour>();

        public DbSet<BranchClosure> BranchClosures => Set<BranchClosure>();

        public DbSet<AppointmentSlot> AppointmentSlots
            => Set<AppointmentSlot>();

        public DbSet<Appointment> Appointments => Set<Appointment>();

        public DbSet<AppointmentStatus> AppointmentStatuses
            => Set<AppointmentStatus>();

        public DbSet<AppointmentNotification> AppointmentNotifications
            => Set<AppointmentNotification>();

        public DbSet<AppointmentHistory> AppointmentHistory
            => Set<AppointmentHistory>();
        public DbSet<AppointmentAccessCode> AppointmentAccessCodes
    => Set<AppointmentAccessCode>();
        public DbSet<AppointmentAccessSession> AppointmentAccessSessions
    => Set<AppointmentAccessSession>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureCustomers(modelBuilder);
            ConfigureIdentityDocuments(modelBuilder);
            ConfigureIdentityVerifications(modelBuilder);
            ConfigureBranches(modelBuilder);
            ConfigureServices(modelBuilder);
            ConfigureBranchServices(modelBuilder);
            ConfigureOperatingHours(modelBuilder);
            ConfigureClosures(modelBuilder);
            ConfigureAppointmentSlots(modelBuilder);
            ConfigureAppointments(modelBuilder);
            ConfigureNotifications(modelBuilder);
            ConfigureAppointmentHistory(modelBuilder);
            ConfigureAppointmentAccessCodes(modelBuilder);
            ConfigureAppointmentAccessSessions(modelBuilder);
            SeedLookupData(modelBuilder);
        }

        private static void ConfigureAppointmentAccessSessions(
    ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppointmentAccessSession>(entity =>
            {
                entity.ToTable("AppointmentAccessSession", table =>
                {
                    table.HasCheckConstraint(
                        "CK_AppointmentAccessSession_Expiry",
                        "[ExpiresAt] > [CreatedAt]");
                });

                entity.HasKey(x => x.AppointmentAccessSessionId);

                entity.Property(x => x.TokenHash)
                    .HasMaxLength(64)
                    .IsRequired();

                entity.Property(x => x.CreatedAt)
                    .HasDefaultValueSql("SYSUTCDATETIME()");

                entity.Property(x => x.ExpiresAt)
                    .IsRequired();

                entity.HasOne(x => x.Appointment)
                    .WithMany(x => x.AccessSessions)
                    .HasForeignKey(x => x.AppointmentId)
                    .OnDelete(DeleteBehavior.Restrict);

                // A token hash must be unique.
                entity.HasIndex(x => x.TokenHash)
                    .IsUnique();

                entity.HasIndex(x => new
                {
                    x.AppointmentId,
                    x.ExpiresAt
                });
            });
        }

        private static void ConfigureAppointmentAccessCodes(
    ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppointmentAccessCode>(entity =>
            {
                entity.ToTable("AppointmentAccessCode", table =>
                {
                    table.HasCheckConstraint(
                        "CK_AppointmentAccessCode_AttemptCount",
                        "[AttemptCount] >= 0");

                    table.HasCheckConstraint(
                        "CK_AppointmentAccessCode_Channel",
                        "[Channel] IN ('EMAIL', 'SMS')");
                });

                entity.HasKey(x => x.AppointmentAccessCodeId);

                entity.Property(x => x.CodeHash)
                    .HasMaxLength(64)
                    .IsRequired();

                entity.Property(x => x.Channel)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(x => x.AttemptCount)
                    .HasDefaultValue(0);

                entity.Property(x => x.CreatedAt)
                    .HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasOne(x => x.Appointment)
                    .WithMany(x => x.AccessCodes)
                    .HasForeignKey(x => x.AppointmentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => new
                {
                    x.AppointmentId,
                    x.ExpiresAt
                });
            });
        }
        private static void ConfigureCustomers(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(x => x.CustomerId);

                entity.Property(x => x.FirstName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.LastName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.Email).HasMaxLength(255);

                entity.Property(x => x.PhoneNumber)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(x => x.CreatedAt)
                    .HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasIndex(x => x.Email);
                entity.HasIndex(x => x.PhoneNumber);
            });
        }

        private static void ConfigureIdentityDocuments(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CustomerIdentityDocument>(entity =>
            {
                entity.HasKey(x => x.IdentityDocumentId);

                entity.Property(x => x.DocumentNumberEncrypted)
                    .HasColumnType("varbinary(512)")
                    .IsRequired();

                entity.Property(x => x.DocumentNumberHash)
                    .HasColumnType("binary(32)");

                entity.Property(x => x.CountryOfIssue)
                    .HasColumnType("char(2)")
                    .IsRequired();

                entity.Property(x => x.CreatedAt)
                    .HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasOne(x => x.Customer)
                    .WithMany(x => x.IdentityDocuments)
                    .HasForeignKey(x => x.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.IdentityDocumentType)
                    .WithMany(x => x.Documents)
                    .HasForeignKey(x => x.IdentityDocumentTypeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => x.CustomerId);

                entity.HasIndex(x => new
                {
                    x.IdentityDocumentTypeId,
                    x.CountryOfIssue,
                    x.DocumentNumberHash
                })
                .IsUnique()
                .HasFilter("[DocumentNumberHash] IS NOT NULL AND [IsActive] = 1");
            });
        }

        private static void ConfigureIdentityVerifications(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CustomerIdentityVerification>(entity =>
            {
                entity.HasKey(x => x.CustomerIdentityVerificationId);

                entity.Property(x => x.VerificationMethod)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(x => x.ProviderReference)
                    .HasMaxLength(100);

                entity.Property(x => x.FailureReason)
                    .HasMaxLength(500);

                entity.Property(x => x.CreatedAt)
                    .HasDefaultValueSql("SYSUTCDATETIME()");
                entity.HasIndex(x => new
                {
                    x.CustomerIdentityDocumentId,
                    x.CreatedAt
                });

                entity.HasOne(x => x.CustomerIdentityDocument)
                    .WithMany()
                    .HasForeignKey(x => x.CustomerIdentityDocumentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.IdentityVerificationStatus)
                    .WithMany()
                    .HasForeignKey(x => x.IdentityVerificationStatusId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureBranches(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Branch>(entity =>
            {
                entity.HasKey(x => x.BranchId);

                entity.HasIndex(x => x.BranchCode).IsUnique();

                entity.Property(x => x.BranchCode).HasMaxLength(20);
                entity.Property(x => x.BranchName).HasMaxLength(150);

                entity.Property(x => x.AddressLine1).HasMaxLength(200);
                entity.Property(x => x.AddressLine2).HasMaxLength(200);

                entity.Property(x => x.City).HasMaxLength(100);
                entity.Property(x => x.Province).HasMaxLength(100);
                entity.Property(x => x.PostalCode).HasMaxLength(10);

                entity.Property(x => x.CountryCode)
                    .HasColumnType("char(2)");

                entity.Property(x => x.TimeZoneId)
                    .HasMaxLength(100)
                    .HasDefaultValue("South Africa Standard Time");

                entity.Property(x => x.CreatedAt)
                    .HasDefaultValueSql("SYSUTCDATETIME()");
            });
        }

        private static void ConfigureServices(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Service>(entity =>
            {
                entity.HasKey(x => x.ServiceId);

                entity.HasIndex(x => x.ServiceCode).IsUnique();

                entity.Property(x => x.ServiceCode).HasMaxLength(30);
                entity.Property(x => x.ServiceName).HasMaxLength(150);
                entity.Property(x => x.Description).HasMaxLength(500);

                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_Services_Duration",
                    "[DurationMinutes] > 0"));
            });
        }

        private static void ConfigureBranchServices(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BranchService>(entity =>
            {
                entity.HasKey(x => x.BranchServiceId);

                entity.HasAlternateKey(x => new
                {
                    x.BranchId,
                    x.ServiceId
                });

                entity.HasOne(x => x.Branch)
                .WithMany(b => b.BranchServices)
                    .HasForeignKey(x => x.BranchId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Service)
                    .WithMany(s => s.BranchServices)
                    .HasForeignKey(x => x.ServiceId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureOperatingHours(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BranchOperatingHour>(entity =>
            {
                entity.HasKey(x => x.OperatingHourId);

                entity.Property(x => x.OpeningTime).HasColumnType("time");
                entity.Property(x => x.ClosingTime).HasColumnType("time");

                entity.HasIndex(x => new { x.BranchId, x.DayOfWeek })
                    .IsUnique();

                entity.HasOne(x => x.Branch)
                    .WithMany(x => x.OperatingHours)
                    .HasForeignKey(x => x.BranchId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_OperatingHours_Day",
                    "[DayOfWeek] BETWEEN 0 AND 6"));

                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_OperatingHours_Times",
                    "([IsClosed] = 1 AND [OpeningTime] IS NULL AND [ClosingTime] IS NULL) OR " +
                    "([IsClosed] = 0 AND [OpeningTime] IS NOT NULL AND " +
                    "[ClosingTime] IS NOT NULL AND [OpeningTime] < [ClosingTime])"));
            });
        }

        private static void ConfigureClosures(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BranchClosure>(entity =>
            {
                entity.HasKey(x => x.BranchClosureId);

                entity.Property(x => x.StartDate)
                    .HasColumnType("date");

                entity.Property(x => x.EndDate)
                    .HasColumnType("date");

                entity.Property(x => x.Reason)
                    .HasMaxLength(250);

                entity.Property(x => x.CreatedAt)
                    .HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasIndex(x => new
                {
                    x.BranchId,
                    x.StartDate,
                    x.EndDate
                });

                entity.HasOne(x => x.Branch)
                    .WithMany(x => x.Closures)
                    .HasForeignKey(x => x.BranchId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.ToTable(t =>
                    t.HasCheckConstraint(
                        "CK_BranchClosures_DateRange",
                        "[EndDate] >= [StartDate]"));
            });
        }

        private static void ConfigureAppointmentSlots(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppointmentSlot>(entity =>
            {
                entity.HasKey(x => x.SlotId);

                entity.Property(x => x.RowVersion).IsRowVersion();

                entity.HasIndex(x => new
                {
                    x.BranchId,
                    x.ServiceId,
                    x.StartDateTime
                }).IsUnique();

                entity.HasIndex(x => new
                {
                    x.BranchId,
                    x.ServiceId,
                    x.StartDateTime,
                    x.IsActive
                }).IncludeProperties(x => new
                {
                    x.EndDateTime,
                    x.Capacity,
                    x.BookedCount
                });

                entity.HasOne(x => x.Branch)
                    .WithMany(x => x.AppointmentSlots)
                    .HasForeignKey(x => x.BranchId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Service)
                    .WithMany(x => x.AppointmentSlots)
                    .HasForeignKey(x => x.ServiceId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<BranchService>()
                    .WithMany()
                    .HasForeignKey(x => new { x.BranchId, x.ServiceId })
                    .HasPrincipalKey(x => new { x.BranchId, x.ServiceId })
                    .OnDelete(DeleteBehavior.Restrict);

                entity.ToTable(t =>
                {
                    t.HasCheckConstraint(
                        "CK_AppointmentSlots_Capacity",
                        "[Capacity] > 0 AND [BookedCount] >= 0 AND [BookedCount] <= [Capacity]");

                    t.HasCheckConstraint(
                        "CK_AppointmentSlots_Dates",
                        "[EndDateTime] > [StartDateTime]");
                });
            });
        }

        private static void ConfigureAppointments(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Appointment>(entity =>
            {
                entity.HasKey(x => x.AppointmentId);

                entity.Property(x => x.AppointmentReference)
                    .HasDefaultValueSql("NEWSEQUENTIALID()");

                entity.HasIndex(x => x.AppointmentReference)
                    .IsUnique();

                entity.Property(x => x.RowVersion).IsRowVersion();

                entity.Property(x => x.CustomerNotes).HasMaxLength(500);
                entity.Property(x => x.CancellationReason).HasMaxLength(500);

                entity.HasOne(x => x.Customer)
                    .WithMany(x => x.Appointments)
                    .HasForeignKey(x => x.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Slot)
                    .WithMany(x => x.Appointments)
                    .HasForeignKey(x => x.SlotId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Status)
                    .WithMany(x => x.Appointments)
                    .HasForeignKey(x => x.AppointmentStatusId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => x.CustomerId);
                entity.HasIndex(x => new { x.SlotId, x.AppointmentStatusId });
            });


            modelBuilder.Entity<AppointmentNotification>(entity =>
            {
                entity.HasKey(x => x.NotificationId);

                entity.Property(x => x.NotificationType).HasMaxLength(30);
                entity.Property(x => x.Channel).HasMaxLength(20);
                entity.Property(x => x.Recipient).HasMaxLength(255);
                entity.Property(x => x.Subject).HasMaxLength(200);
                entity.Property(x => x.Status).HasMaxLength(20);

                entity.HasOne(x => x.Appointment)
            .WithMany(x => x.Notifications)
            .HasForeignKey(x => x.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => x.AppointmentId);

                entity.ToTable(t =>
                {
                    t.HasCheckConstraint(
                        "CK_Notifications_Channel",
                        "[Channel] IN ('EMAIL', 'SMS')");

                    t.HasCheckConstraint(
                        "CK_Notifications_Status",
                        "[Status] IN ('PENDING', 'PROCESSING', 'SENT', 'FAILED')");
                });
            });

            modelBuilder.Entity<AppointmentNotificationAttempt>(entity =>
            {
                entity.HasKey(x => x.NotificationAttemptId);

                entity.Property(x => x.Status)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(x => x.ErrorMessage)
                .HasMaxLength(2000);

                entity.Property(x => x.ProviderReference)
                .HasMaxLength(150);

                entity.Property(x => x.StartedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasOne(x => x.Notification)
                .WithMany(x => x.Attempts)
                .HasForeignKey(x => x.NotificationId)
                .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => new
                {
                    x.NotificationId,
                    x.AttemptNumber
                })
            .IsUnique();

                entity.ToTable(t =>
                {
                    t.HasCheckConstraint(
                        "CK_NotificationAttempts_Status",
                        "[Status] IN ('PROCESSING', 'SENT', 'FAILED')");

                    t.HasCheckConstraint(
                        "CK_NotificationAttempts_AttemptNumber",
                        "[AttemptNumber] > 0");
                });
            });
        }


        private static void ConfigureNotifications(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppointmentNotification>(entity =>
            {
                entity.HasKey(x => x.NotificationId);

                entity.Property(x => x.NotificationType).HasMaxLength(30);
                entity.Property(x => x.Channel).HasMaxLength(20);
                entity.Property(x => x.Recipient).HasMaxLength(255);
                entity.Property(x => x.Subject).HasMaxLength(200);
                entity.Property(x => x.Status).HasMaxLength(20);

                entity.HasOne(x => x.Appointment)
                    .WithMany(x => x.Notifications)
                    .HasForeignKey(x => x.AppointmentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => x.AppointmentId);
                entity.HasIndex(x => new
                {
                    x.Status,
                    x.NextAttemptAt,
                    x.CreatedAt
                });
                entity.ToTable(t =>
                {
                    t.HasCheckConstraint(
                        "CK_Notifications_Channel",
                        "[Channel] IN ('EMAIL', 'SMS')");

                    t.HasCheckConstraint(
                        "CK_Notifications_Status",
                        "[Status] IN ('PENDING', 'SENT', 'FAILED')");
                });
            });
        }

        private static void ConfigureAppointmentHistory(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppointmentHistory>(entity =>
            {
                entity.HasKey(x => x.AppointmentHistoryId);

                entity.Property(x => x.ChangedBy)
                    .HasMaxLength(100)
                    .HasDefaultValue("SYSTEM");

                entity.Property(x => x.Comments).HasMaxLength(500);

                entity.HasOne(x => x.Appointment)
                    .WithMany(x => x.History)
                    .HasForeignKey(x => x.AppointmentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.OldStatus)
                    .WithMany()
                    .HasForeignKey(x => x.OldStatusId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.NewStatus)
                    .WithMany()
                    .HasForeignKey(x => x.NewStatusId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => new { x.AppointmentId, x.ChangedAt });
            });
        }

        private static void SeedLookupData(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<IdentityDocumentType>().HasData(
                new IdentityDocumentType
                {
                    IdentityDocumentTypeId = 1,
                    Code = "RSA_ID",
                    Name = "South African ID"
                },
                new IdentityDocumentType
                {
                    IdentityDocumentTypeId = 2,
                    Code = "PASSPORT",
                    Name = "Passport"
                });

            modelBuilder.Entity<IdentityVerificationStatus>(entity =>
            {
                entity.HasKey(x => x.IdentityVerificationStatusId);

                entity.Property(x => x.Code)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(x => x.IsActive)
                    .HasDefaultValue(true);

                entity.HasIndex(x => x.Code)
                    .IsUnique();
                entity.HasData(
            new IdentityVerificationStatus
            {
                IdentityVerificationStatusId = 1,
                Code = "PENDING",
                Name = "Pending",
                IsActive = true
            },
            new IdentityVerificationStatus
            {
                IdentityVerificationStatusId = 2,
                Code = "VERIFIED",
                Name = "Verified",
                IsActive = true
            },
            new IdentityVerificationStatus
            {
                IdentityVerificationStatusId = 3,
                Code = "REJECTED",
                Name = "Rejected",
                IsActive = true
            },
            new IdentityVerificationStatus
            {
                IdentityVerificationStatusId = 4,
                Code = "EXPIRED",
                Name = "Expired",
                IsActive = true
            },
            new IdentityVerificationStatus
            {
                IdentityVerificationStatusId = 5,
                Code = "NEEDS_REVIEW",
                Name = "Needs Review",
                IsActive = true
            });
            });
            modelBuilder.Entity<AppointmentStatus>().HasData(
                new AppointmentStatus
                {
                    AppointmentStatusId = 1,
                    Code = "PENDING",
                    Name = "Pending"
                },
                new AppointmentStatus
                {
                    AppointmentStatusId = 2,
                    Code = "CONFIRMED",
                    Name = "Confirmed"
                },
                new AppointmentStatus
                {
                    AppointmentStatusId = 3,
                    Code = "CANCELLED",
                    Name = "Cancelled"
                },
                new AppointmentStatus
                {
                    AppointmentStatusId = 4,
                    Code = "COMPLETED",
                    Name = "Completed"
                },
                new AppointmentStatus
                {
                    AppointmentStatusId = 5,
                    Code = "NO_SHOW",
                    Name = "No Show"
                });
        }
    }
}
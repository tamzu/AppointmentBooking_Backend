using AppointmentBooking.Core.DTOs.AppointmentSlots;
using AppointmentBooking.Core.Entities;
using AppointmentBooking.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace AppointmentBooking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentSlotsController : ControllerBase
    {
        private readonly AppointmentBookingDbContext _context;

        public AppointmentSlotsController(AppointmentBookingDbContext context)
        {
            _context = context;
        }

        // POST: api/appointmentslots/generate
        [Authorize(Roles = "Admin,Staff")]
        [HttpPost("generate")]
        public async Task<IActionResult> Generate(
            [FromBody] GenerateAppointmentSlotsRequest request,
            CancellationToken cancellationToken)
        {
            // 1. Validate request
            if (request.BranchId <= 0)
                return BadRequest(new { message = "A valid branch is required." });

            if (request.ServiceId <= 0)
                return BadRequest(new { message = "A valid service is required." });

            if (request.StartDate == default || request.EndDate == default)
                return BadRequest(new { message = "Start date and end date are required." });

            if (request.EndDate < request.StartDate)
                return BadRequest(new { message = "End date cannot be earlier than start date." });

            if (request.EndDate.DayNumber - request.StartDate.DayNumber > 90)
                return BadRequest(new { message = "Maximum generation period is 90 days." });

            if (request.Capacity < 1 || request.Capacity > 100)
                return BadRequest(new { message = "Capacity must be between 1 and 100." });

            // 2. Validate branch
            var branch = await _context.Branches
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.BranchId == request.BranchId,
                    cancellationToken);

            if (branch == null)
                return NotFound(new { message = "Branch not found." });

            if (!branch.IsActive)
                return BadRequest(new { message = "Cannot generate slots for an inactive branch." });

            // 3. Validate service
            var service = await _context.Services
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.ServiceId == request.ServiceId,
                    cancellationToken);

            if (service == null)
                return NotFound(new { message = "Service not found." });

            if (!service.IsActive)
                return BadRequest(new { message = "Service is inactive." });

            if (service.DurationMinutes <= 0)
                return BadRequest(new { message = "Service duration must be greater than zero." });

            // 4. Validate service availability at branch
            var branchService = await _context.BranchServices
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.BranchId == request.BranchId &&
                         x.ServiceId == request.ServiceId &&
                         x.IsAvailable,
                    cancellationToken);

            if (branchService == null)
            {
                return BadRequest(new
                {
                    message = "This service is not available at the selected branch."
                });
            }

            // 5. Validate branch timezone
            TimeZoneInfo timeZone;

            try
            {
                timeZone = TimeZoneInfo.FindSystemTimeZoneById(branch.TimeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
                return BadRequest(new
                {
                    message = "Branch timezone is invalid or unavailable."
                });
            }
            catch (InvalidTimeZoneException)
            {
                return BadRequest(new
                {
                    message = "Branch timezone configuration is invalid."
                });
            }

            // 6. Execute transaction using EF Core execution strategy
            var executionStrategy = _context.Database.CreateExecutionStrategy();

            var result = await executionStrategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database
                    .BeginTransactionAsync(
                        IsolationLevel.Serializable,
                        cancellationToken);

                try
                {
                    // Load branch operating hours
                    var operatingHours = await _context.BranchOperatingHours
                        .AsNoTracking()
                        .Where(x => x.BranchId == request.BranchId)
                        .ToListAsync(cancellationToken);

                    // Load branch closures within the requested date range
                    var closures = await _context.BranchClosures
                        .AsNoTracking()
                        .Where(x =>
                            x.BranchId == request.BranchId &&
                            x.StartDate <= request.EndDate &&
                            x.EndDate >= request.StartDate)
                        .ToListAsync(cancellationToken);

                    // Load existing slots to prevent duplicates
                    var existingSlots = await _context.AppointmentSlots
                        .AsNoTracking()
                        .Where(x =>
                            x.BranchId == request.BranchId &&
                            x.ServiceId == request.ServiceId)
                        .ToListAsync(cancellationToken);

                    var existingStartTimes = existingSlots
                        .Select(x => x.StartDateTime)
                        .ToHashSet();

                    var generatedSlots = new List<AppointmentSlot>();

                    var skippedClosedDays = 0;
                    var skippedPastSlots = 0;
                    var skippedExistingSlots = 0;

                    var todayUtc = DateTime.UtcNow;

                    // 7. Generate slots for each requested date
                    for (var date = request.StartDate;
                         date <= request.EndDate;
                         date = date.AddDays(1))
                    {
                        // Skip branch closure dates
                        var isClosureDate = closures.Any(x =>
                            date >= x.StartDate &&
                            date <= x.EndDate);

                        if (isClosureDate)
                        {
                            skippedClosedDays++;
                            continue;
                        }

                        // Find operating hours for the current weekday
                        var dayOfWeek = (byte)date.DayOfWeek;

                        var dailyHours = operatingHours
                            .FirstOrDefault(x => x.DayOfWeek == dayOfWeek);

                        // Skip days without valid operating hours
                        if (dailyHours == null ||
                            dailyHours.IsClosed ||
                            !dailyHours.OpeningTime.HasValue ||
                            !dailyHours.ClosingTime.HasValue)
                        {
                            skippedClosedDays++;
                            continue;
                        }

                        // Convert opening and closing times into minutes
                        var currentMinute =
                            dailyHours.OpeningTime.Value.Hour * 60 +
                            dailyHours.OpeningTime.Value.Minute;

                        var closingMinute =
                            dailyHours.ClosingTime.Value.Hour * 60 +
                            dailyHours.ClosingTime.Value.Minute;

                        // Generate slots based on service duration
                        while (currentMinute + service.DurationMinutes <= closingMinute)
                        {
                            var slotEndMinute = currentMinute + service.DurationMinutes;

                            var currentTime = TimeOnly.FromTimeSpan(
                                TimeSpan.FromMinutes(currentMinute));

                            var slotEndTime = TimeOnly.FromTimeSpan(
                                TimeSpan.FromMinutes(slotEndMinute));

                            // Create branch-local date/time values
                            var localStart = date.ToDateTime(currentTime);
                            var localEnd = date.ToDateTime(slotEndTime);

                            // Validate daylight-saving time transitions
                            if (timeZone.IsInvalidTime(localStart) ||
                                timeZone.IsInvalidTime(localEnd))
                            {
                                throw new InvalidOperationException(
                                    $"Invalid local time encountered on {date}.");
                            }

                            if (timeZone.IsAmbiguousTime(localStart) ||
                                timeZone.IsAmbiguousTime(localEnd))
                            {
                                throw new InvalidOperationException(
                                    $"Ambiguous local time encountered on {date}.");
                            }

                            // Convert branch-local times to UTC
                            var startUtc = TimeZoneInfo.ConvertTimeToUtc(
                                DateTime.SpecifyKind(localStart, DateTimeKind.Unspecified),
                                timeZone);

                            var endUtc = TimeZoneInfo.ConvertTimeToUtc(
                                DateTime.SpecifyKind(localEnd, DateTimeKind.Unspecified),
                                timeZone);

                            // Skip past appointment slots
                            if (startUtc <= todayUtc)
                            {
                                skippedPastSlots++;
                                currentMinute = slotEndMinute;
                                continue;
                            }

                            // Skip slots that already exist
                            if (existingStartTimes.Contains(startUtc))
                            {
                                skippedExistingSlots++;
                                currentMinute = slotEndMinute;
                                continue;
                            }

                            // Create appointment slot
                            generatedSlots.Add(new AppointmentSlot
                            {
                                BranchId = request.BranchId,
                                ServiceId = request.ServiceId,
                                StartDateTime = startUtc,
                                EndDateTime = endUtc,
                                Capacity = request.Capacity,
                                BookedCount = 0,
                                IsActive = true
                            });

                            // Track generated slot to prevent duplicates
                            // within this generation request
                            existingStartTimes.Add(startUtc);

                            currentMinute = slotEndMinute;
                        }
                    }

                    // 8. Save generated slots
                    if (generatedSlots.Count > 0)
                    {
                        _context.AppointmentSlots.AddRange(generatedSlots);

                        await _context.SaveChangesAsync(cancellationToken);
                    }

                    // 9. Commit transaction
                    await transaction.CommitAsync(cancellationToken);

                    // Return generation summary
                    return new
                    {
                        message = "Appointment slot generation completed successfully.",
                        branchId = request.BranchId,
                        serviceId = request.ServiceId,
                        startDate = request.StartDate,
                        endDate = request.EndDate,
                        generatedCount = generatedSlots.Count,
                        skippedClosedDays,
                        skippedPastSlots,
                        skippedExistingSlots
                    };
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);

                    // Clear tracked entities before a possible retry
                    _context.ChangeTracker.Clear();

                    throw;
                }
            });

            return Ok(result);
        }
        //[HttpPost("generate")]
        //public async Task<IActionResult> Generate(
        //    [FromBody] GenerateAppointmentSlotsRequest request,
        //    CancellationToken cancellationToken)
        //{
        //    if (request.StartDate == default || request.EndDate == default)
        //        return BadRequest(new { message = "Start date and end date are required." });

        //    if (request.EndDate < request.StartDate)
        //        return BadRequest(new { message = "End date cannot be earlier than start date." });

        //    if (request.EndDate.DayNumber - request.StartDate.DayNumber > 90)
        //        return BadRequest(new { message = "Maximum generation period is 90 days." });

        //    var branch = await _context.Branches
        //        .AsNoTracking()
        //        .FirstOrDefaultAsync(
        //            x => x.BranchId == request.BranchId,
        //            cancellationToken);

        //    if (branch == null)
        //        return NotFound(new { message = "Branch not found." });

        //    if (!branch.IsActive)
        //        return BadRequest(new { message = "Cannot generate slots for an inactive branch." });

        //    var service = await _context.Services
        //        .AsNoTracking()
        //        .FirstOrDefaultAsync(
        //            x => x.ServiceId == request.ServiceId,
        //            cancellationToken);

        //    if (service == null)
        //        return NotFound(new { message = "Service not found." });

        //    if (!service.IsActive)
        //        return BadRequest(new { message = "Service is inactive." });

        //    var branchService = await _context.BranchServices
        //        .AsNoTracking()
        //        .FirstOrDefaultAsync(
        //            x => x.BranchId == request.BranchId &&
        //                 x.ServiceId == request.ServiceId &&
        //                 x.IsAvailable,
        //            cancellationToken);

        //    if (branchService == null)
        //    {
        //        return BadRequest(new
        //        {
        //            message = "This service is not available at the selected branch."
        //        });
        //    }

        //    if (service.DurationMinutes <= 0)
        //        return BadRequest(new { message = "Service duration must be greater than zero." });

        //    TimeZoneInfo timeZone;

        //    try
        //    {
        //        timeZone = TimeZoneInfo.FindSystemTimeZoneById(branch.TimeZoneId);
        //    }
        //    catch (TimeZoneNotFoundException)
        //    {
        //        return BadRequest(new { message = "Branch timezone is invalid or unavailable." });
        //    }
        //    catch (InvalidTimeZoneException)
        //    {
        //        return BadRequest(new { message = "Branch timezone configuration is invalid." });
        //    }

        //    await using var transaction = await _context.Database
        //        .BeginTransactionAsync(
        //            IsolationLevel.Serializable,
        //            cancellationToken);
        //    var executionStrategy = _context.Database.CreateExecutionStrategy();
        //    var generatedCount = await executionStrategy.ExecuteAsync(async () =>
        //    {
        //        await using var transaction = await _context.Database
        //            .BeginTransactionAsync(
        //                IsolationLevel.Serializable,
        //                cancellationToken);

        //        //}
        //        try
        //        {
        //            var operatingHours = await _context.BranchOperatingHours
        //                .AsNoTracking()
        //                .Where(x => x.BranchId == request.BranchId)
        //                .ToListAsync(cancellationToken);

        //            var closures = await _context.BranchClosures
        //                .AsNoTracking()
        //                .Where(x =>
        //                    x.BranchId == request.BranchId &&
        //                    x.StartDate <= request.EndDate &&
        //                    x.EndDate >= request.StartDate)
        //                .ToListAsync(cancellationToken);

        //            var existingSlots = await _context.AppointmentSlots
        //                .Where(x =>
        //                    x.BranchId == request.BranchId &&
        //                    x.ServiceId == request.ServiceId)
        //                .ToListAsync(cancellationToken);

        //            var existingStartTimes = existingSlots
        //                .Select(x => x.StartDateTime)
        //                .ToHashSet();

        //            var generatedSlots = new List<AppointmentSlot>();

        //            var skippedClosedDays = 0;
        //            var skippedPastSlots = 0;
        //            var skippedExistingSlots = 0;

        //            var todayUtc = DateTime.UtcNow;

        //            for (var date = request.StartDate;
        //                 date <= request.EndDate;
        //                 date = date.AddDays(1))
        //            {
        //                // Exclude branch closure dates.
        //                var isClosureDate = closures.Any(x =>
        //                    date >= x.StartDate &&
        //                    date <= x.EndDate);

        //                if (isClosureDate)
        //                {
        //                    skippedClosedDays++;
        //                    continue;
        //                }

        //                // Match the configured weekly operating hours.
        //                var dayOfWeek = (byte)date.DayOfWeek;

        //                var dailyHours = operatingHours
        //                    .FirstOrDefault(x => x.DayOfWeek == dayOfWeek);

        //                if (dailyHours == null ||
        //                    dailyHours.IsClosed ||
        //                    !dailyHours.OpeningTime.HasValue ||
        //                    !dailyHours.ClosingTime.HasValue)
        //                {
        //                    skippedClosedDays++;
        //                    continue;
        //                }

        //                var currentMinute = dailyHours.OpeningTime.Value.Hour * 60 +
        //dailyHours.OpeningTime.Value.Minute;
        //                var closingMinute = dailyHours.ClosingTime.Value.Hour * 60 +
        //dailyHours.ClosingTime.Value.Minute;

        //                while (currentMinute + service.DurationMinutes
        //                       <= closingMinute)
        //                {
        //                    var slotEndMinute = currentMinute + service.DurationMinutes;

        //                    var currentTime = TimeOnly.FromTimeSpan(
        //TimeSpan.FromMinutes(currentMinute));

        //                    var slotEndTime = TimeOnly.FromTimeSpan(
        //                        TimeSpan.FromMinutes(slotEndMinute));

        //                    var localStart = date.ToDateTime(currentTime);
        //                    var localEnd = date.ToDateTime(slotEndTime);

        //                    // Convert branch-local time into UTC.
        //                    if (timeZone.IsInvalidTime(localStart) ||
        //                        timeZone.IsInvalidTime(localEnd))
        //                    {
        //                        throw new InvalidOperationException(
        //                            $"Invalid local time encountered on {date}.");
        //                    }

        //                    if (timeZone.IsAmbiguousTime(localStart) ||
        //                        timeZone.IsAmbiguousTime(localEnd))
        //                    {
        //                        throw new InvalidOperationException(
        //                            $"Ambiguous local time encountered on {date}.");
        //                    }

        //                    var startUtc = TimeZoneInfo.ConvertTimeToUtc(
        //                        DateTime.SpecifyKind(localStart, DateTimeKind.Unspecified),
        //                        timeZone);

        //                    var endUtc = TimeZoneInfo.ConvertTimeToUtc(
        //                        DateTime.SpecifyKind(localEnd, DateTimeKind.Unspecified),
        //                        timeZone);

        //                    if (startUtc <= todayUtc)
        //                    {
        //                        skippedPastSlots++;
        //                        currentTime = slotEndTime;
        //                        continue;
        //                    }

        //                    if (existingStartTimes.Contains(startUtc))
        //                    {
        //                        skippedExistingSlots++;
        //                        currentTime = slotEndTime;
        //                        continue;
        //                    }

        //                    generatedSlots.Add(new AppointmentSlot
        //                    {
        //                        BranchId = request.BranchId,
        //                        ServiceId = request.ServiceId,
        //                        StartDateTime = startUtc,
        //                        EndDateTime = endUtc,
        //                        Capacity = request.Capacity,
        //                        BookedCount = 0,
        //                        IsActive = true
        //                    });

        //                    existingStartTimes.Add(startUtc);

        //                    currentTime = slotEndTime;
        //                }
        //            }

        //            if (generatedSlots.Count > 0)
        //            {
        //                _context.AppointmentSlots.AddRange(generatedSlots);

        //                await _context.SaveChangesAsync(cancellationToken);
        //            }

        //            await transaction.CommitAsync(cancellationToken);

        //            return new
        //            {
        //                message = "Appointment slot generation completed successfully.",
        //                branchId = request.BranchId,
        //                serviceId = request.ServiceId,
        //                startDate = request.StartDate,
        //                endDate = request.EndDate,
        //                generatedCount = generatedSlots.Count,
        //                skippedClosedDays,
        //                skippedPastSlots,
        //                skippedExistingSlots
        //            };
        //        }
        //        catch
        //        {
        //            await transaction.RollbackAsync(cancellationToken);

        //            // Clear entities from a failed attempt before a possible retry.
        //            _context.ChangeTracker.Clear();

        //            throw;
        //        }

        //    });
        //    return Ok(result);
        //}

        // GET: api/appointmentslots/available?branchId=1&serviceId=1&date=2026-11-09
        // GET: api/appointmentslots/available?branchId=1&serviceId=1&date=2026-11-09
        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableSlots(
            [FromQuery] int branchId,
            [FromQuery] int serviceId,
            [FromQuery] DateOnly date,
            CancellationToken cancellationToken)
        {
            // 1. Validate branch
            var branch = await _context.Branches
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.BranchId == branchId && x.IsActive,
                    cancellationToken);

            if (branch == null)
                return NotFound(new { message = "Active branch not found." });

            // 2. Validate service availability at branch
            var serviceAvailable = await _context.BranchServices
                .AnyAsync(x =>
                    x.BranchId == branchId &&
                    x.ServiceId == serviceId &&
                    x.IsAvailable,
                    cancellationToken);

            if (!serviceAvailable)
                return BadRequest(new { message = "Service is not available at this branch." });

            var serviceActive = await _context.Services
                .AnyAsync(
                    x => x.ServiceId == serviceId && x.IsActive,
                    cancellationToken);

            if (!serviceActive)
                return BadRequest(new { message = "Service is inactive." });

            // 3. Validate branch timezone
            TimeZoneInfo timeZone;

            try
            {
                timeZone = TimeZoneInfo.FindSystemTimeZoneById(branch.TimeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
                return BadRequest(new { message = "Branch timezone is invalid or unavailable." });
            }
            catch (InvalidTimeZoneException)
            {
                return BadRequest(new { message = "Branch timezone configuration is invalid." });
            }

            // 4. Check whether branch is closed on the requested date
            var isClosed = await _context.BranchClosures
                .AnyAsync(x =>
                    x.BranchId == branchId &&
                    x.StartDate <= date &&
                    x.EndDate >= date,
                    cancellationToken);

            if (isClosed)
                return Ok(Array.Empty<AvailableAppointmentSlotResponse>());

            // 5. Check operating hours for requested weekday
            var dayOfWeek = (byte)date.DayOfWeek;

            var operatingHours = await _context.BranchOperatingHours
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.BranchId == branchId &&
                    x.DayOfWeek == dayOfWeek,
                    cancellationToken);

            if (operatingHours == null ||
                operatingHours.IsClosed ||
                !operatingHours.OpeningTime.HasValue ||
                !operatingHours.ClosingTime.HasValue)
            {
                return Ok(Array.Empty<AvailableAppointmentSlotResponse>());
            }

            // 6. Convert requested branch-local date to UTC
            var localStart = date.ToDateTime(TimeOnly.MinValue);
            var localEnd = date.AddDays(1).ToDateTime(TimeOnly.MinValue);

            if (timeZone.IsInvalidTime(localStart) ||
                timeZone.IsInvalidTime(localEnd))
            {
                return BadRequest(new { message = "Invalid local date boundary for branch timezone." });
            }

            if (timeZone.IsAmbiguousTime(localStart) ||
                timeZone.IsAmbiguousTime(localEnd))
            {
                return BadRequest(new { message = "Ambiguous local date boundary for branch timezone." });
            }

            var startUtc = TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(localStart, DateTimeKind.Unspecified),
                timeZone);

            var endUtc = TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(localEnd, DateTimeKind.Unspecified),
                timeZone);

            var nowUtc = DateTime.UtcNow;

            // 7. Retrieve available appointment slots
            var slots = await _context.AppointmentSlots
                .AsNoTracking()
                .Where(x =>
                    x.BranchId == branchId &&
                    x.ServiceId == serviceId &&
                    x.IsActive &&
                    x.StartDateTime >= startUtc &&
                    x.StartDateTime < endUtc &&
                    x.StartDateTime > nowUtc &&
                    x.BookedCount < x.Capacity)
                .OrderBy(x => x.StartDateTime)
                .Select(x => new AvailableAppointmentSlotResponse
                {
                    SlotId = x.SlotId,
                    BranchId = x.BranchId,
                    BranchName = x.Branch.BranchName,
                    ServiceId = x.ServiceId,
                    ServiceName = x.Service.ServiceName,
                    StartDateTime = x.StartDateTime,
                    EndDateTime = x.EndDateTime,
                    Capacity = x.Capacity,
                    BookedCount = x.BookedCount
                })
                .ToListAsync(cancellationToken);

            // 8. Filter slots against current operating hours
            var availableSlots = slots
                .Where(x =>
                {
                    var localSlotStart = TimeZoneInfo.ConvertTimeFromUtc(
                        DateTime.SpecifyKind(x.StartDateTime, DateTimeKind.Utc),
                        timeZone);

                    var localSlotEnd = TimeZoneInfo.ConvertTimeFromUtc(
                        DateTime.SpecifyKind(x.EndDateTime, DateTimeKind.Utc),
                        timeZone);

                    var slotStartTime = TimeOnly.FromDateTime(localSlotStart);
                    var slotEndTime = TimeOnly.FromDateTime(localSlotEnd);

                    return slotStartTime >= operatingHours.OpeningTime.Value &&
                           slotEndTime <= operatingHours.ClosingTime.Value;
                })
                .ToList();

            foreach (var slot in availableSlots)
            {
                slot.StartDateTime = DateTime.SpecifyKind(
                    slot.StartDateTime,
                    DateTimeKind.Utc);

                slot.EndDateTime = DateTime.SpecifyKind(
                    slot.EndDateTime,
                    DateTimeKind.Utc);
            }
            return Ok(availableSlots);
        }

       
    }
}

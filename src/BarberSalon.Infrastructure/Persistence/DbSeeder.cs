using BarberSalon.Domain.Auth.Entities;
using BarberSalon.Domain.Auth.Enums;
using BarberSalon.Domain.Booking.Entities;
using BarberSalon.Domain.Booking.ValueObjects;
using BarberSalon.Domain.Customers.Entities;
using BarberSalon.Domain.Loyalty.Entities;
using BarberSalon.Domain.Portfolio.Entities;
using BarberSalon.Domain.Reminders.Entities;
using BarberSalon.Domain.Reviews.Entities;
using BarberSalon.Domain.SalonServices.Entities;
using BarberSalon.Domain.Staff.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BarberSalon.Infrastructure.Persistence;

/// <summary>
/// Seeds essential salon data into the database if the database is newly initialized or empty.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await context.Database.EnsureCreatedAsync();

        if (await context.SalonServices.AnyAsync())
        {
            return;
        }

        // 1. Services
        var s1 = SalonService.Create("کوتاهی مو", "کوتاهی و اصلاح حرفه‌ای موی سر با جدیدترین متدها", 45, 150_000, "haircut");
        var s2 = SalonService.Create("رنگ مو", "رنگ، مش، دکلره و بالیاژ با بهترین متریال", 90, 350_000, "color");
        var s3 = SalonService.Create("اصلاح صورت", "اصلاح صورت و خط‌کشی ریش و سبیل", 30, 80_000, "haircut");
        var s4 = SalonService.Create("شینیون", "استایل و شینیون مدرن مجلسی", 60, 250_000, "brushing");
        var s5 = SalonService.Create("کراتین مو", "احیا و صافی مو با کراتین برزیلی اصل", 120, 800_000, "keratin");
        var s6 = SalonService.Create("مش فویلی", "تکنیک مش فویلی و لایت حرفه‌ای", 150, 600_000, "color");
        var s7 = SalonService.Create("براشینگ", "فرم‌دهی و براشینگ ماندگار", 45, 120_000, "brushing");

        context.SalonServices.AddRange(s1, s2, s3, s4, s5, s6, s7);
        await context.SaveChangesAsync();

        // 2. Staff Members
        var staff1 = StaffMember.Create(
            "آقای کریمی",
            "karimi",
            "09121234567",
            "آرایشگر با ۱۰ سال تجربه در زمینه کوتاهی و اصلاح مردانه. تخصص در fade، خط‌کشی و استایل‌های مدرن.",
            "آرایشگر ارشد",
            10,
            specialties: new List<string> { "fade", "اصلاح صورت", "خط‌کشی" },
            serviceIds: new List<Guid> { s1.Id, s3.Id, s7.Id }
        );

        var staff2 = StaffMember.Create(
            "خانم احمدی",
            "ahmadi",
            "09129876543",
            "متخصص رنگ و هایلایت با تجربه کار در سالن‌های معتبر. تکنیک‌های balayage و ombré.",
            "متخصص رنگ",
            8,
            specialties: new List<string> { "balayage", "هایلایت", "رنگ دودی" },
            serviceIds: new List<Guid> { s2.Id, s4.Id, s5.Id, s6.Id }
        );

        var staff3 = StaffMember.Create(
            "آقای رضایی",
            "rezaei",
            "09123456789",
            "آرایشگر جوان و بااستعداد، متخصص fade مدرن، طراحی خط و استایلینگ ژورنالی.",
            "آرایشگر",
            5,
            specialties: new List<string> { "استایل", "فید مدرن" },
            serviceIds: new List<Guid> { s1.Id, s3.Id }
        );

        context.StaffMembers.AddRange(staff1, staff2, staff3);
        await context.SaveChangesAsync();

        // 3. Users (admin and customer)
        var now = DateTime.UtcNow;
        var adminUser = User.Create("09120000000", now, UserRole.Admin, "مدیر سیستم");
        var customerUser = User.Create("09121112233", now, UserRole.Customer, "سارا محمدی");

        context.Users.AddRange(adminUser, customerUser);
        await context.SaveChangesAsync();

        // 4. Customers
        var c1 = Customer.Create("سارا محمدی", "09121112233", "female", "مشتری VIP", customerUser.Id);
        var c2 = Customer.Create("علی حسینی", "09122223344", "male", "ترجیح می‌دهد آخر هفته‌ها بیاید");
        var c3 = Customer.Create("مریم احمدی", "09123334455", "female", "");

        context.Customers.AddRange(c1, c2, c3);
        await context.SaveChangesAsync();

        // 5. Appointments
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var slot1 = TimeSlot.Create(today, new TimeOnly(10, 0), new TimeOnly(10, 45));
        var slot2 = TimeSlot.Create(today, new TimeOnly(11, 0), new TimeOnly(11, 30));
        var slot3 = TimeSlot.Create(today, new TimeOnly(14, 0), new TimeOnly(15, 30));

        var appt1 = Appointment.Create(c1.Id, staff1.Id, s1.Id, slot1, s1.Price, "رزرو اول وقت");
        var appt2 = Appointment.Create(c2.Id, staff1.Id, s3.Id, slot2, s3.Price, "اصلاح صورت");
        var appt3 = Appointment.Create(c3.Id, staff2.Id, s2.Id, slot3, s2.Price, "رنگ مو");

        appt1.Confirm();
        appt2.Confirm();

        context.Appointments.AddRange(appt1, appt2, appt3);
        await context.SaveChangesAsync();

        // 6. Reviews
        var r1 = Review.Create(c1.Id, c1.FullName, 5, "بسیار عالی و حرفه‌ای بود. ممنون از آقای کریمی.", staff1.Id, staff1.FullName, s1.Id, s1.Name, appt1.Id);
        var r2 = Review.Create(c3.Id, c3.FullName, 5, "رنگ مو دقیقا همونی شد که می‌خواستم.", staff2.Id, staff2.FullName, s2.Id, s2.Name, appt3.Id);
        context.Reviews.AddRange(r1, r2);

        // 7. Portfolio
        var p1 = PortfolioItem.Create("فید کلاسیک و اصلاح ریش", "haircut", "/images/portfolio/p1-before.jpg", "/images/portfolio/p1-after.jpg", "اجرا توسط آقای کریمی", staff1.Id);
        var p2 = PortfolioItem.Create("بالیاژ کاراملی روی موی تیره", "color", "/images/portfolio/p2-before.jpg", "/images/portfolio/p2-after.jpg", "اجرا توسط خانم احمدی", staff2.Id);
        context.PortfolioItems.AddRange(p1, p2);

        // 8. Reminders
        var rem1 = ReminderRule.Create("یادآوری ۲۴ ساعت قبل", "24h_before", "sms", "سلام {CustomerName} عزیز، نوبت شما فردا ساعت {Time} در سالن زیبایی رزرو است.");
        var rem2 = ReminderRule.Create("یادآوری ۲ ساعت قبل", "2h_before", "sms", "سلام {CustomerName}، نوبت شما تا ۲ ساعت دیگر ساعت {Time} آغاز می‌شود.");
        context.ReminderRules.AddRange(rem1, rem2);

        // 9. Loyalty
        var loyalty1 = LoyaltyAccount.Create(c1.Id, "SARA100");
        loyalty1.AddPoints(450);
        context.LoyaltyAccounts.Add(loyalty1);

        await context.SaveChangesAsync();
    }
}

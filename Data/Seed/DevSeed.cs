using LibApp.Models;
using System.Security.Cryptography;
using System.Text;

namespace LibApp.Data.Seed;

public static class DevSeed
{
    public static void Seed(AppDbContext context)
    {
        // Порядок важен: сначала справочники без FK, потом зависимые таблицы

        SeedRoles(context);
        SeedUsers(context);
        SeedCategories(context);
        SeedAuthors(context);
        SeedPublishers(context);
        SeedBooks(context);
        SeedVersionBooks(context);
        SeedExampleBooks(context);
        SeedLoans(context);
        SeedFines(context);
    }

    // ─── Roles ────────────────────────────────────────────────────
    private static void SeedRoles(AppDbContext context)
    {
        if (context.Roles.Any()) return;

        context.Roles.AddRange(
            new Role { Name = "Admin" },
            new Role { Name = "Librarian" },
            new Role { Name = "Reader" }
        );
        context.SaveChanges();
    }

    // ─── Users ────────────────────────────────────────────────────
    private static void SeedUsers(AppDbContext context)
    {
        if (context.Users.Any()) return;
        
        // Берём реальные ID из БД
        var adminRoleId    = context.Roles.First(r => r.Name == "Admin").RoleId;
        var librarianRoleId = context.Roles.First(r => r.Name == "Librarian").RoleId;
        var readerRoleId   = context.Roles.First(r => r.Name == "Reader").RoleId;

        context.Users.AddRange(
            new User
            {
                RoleId = adminRoleId,
                Login = "admin", Email = "admin@lib.ru",
                FullName = "Иванов Иван Иванович",
                HashPass = Hash("admin123"),
                PhoneNumber = "+79001112233"
            },
            new User
            {
                RoleId = librarianRoleId,
                Login = "librarian1", Email = "lib1@lib.ru",
                FullName = "Петрова Мария Сергеевна",
                HashPass = Hash("lib123"),
                PhoneNumber = "+79002223344"
            },
            new User
            {
                RoleId = readerRoleId,
                Login = "reader1", Email = "reader1@mail.ru",
                FullName = "Сидоров Алексей Петрович",
                HashPass = Hash("reader123"),
                PhoneNumber = "+79003334455"
            },
            new User
            {
                RoleId = readerRoleId,
                Login = "reader2", Email = "reader2@mail.ru",
                FullName = "Козлова Анна Дмитриевна",
                HashPass = Hash("reader123"),
                PhoneNumber = null
            },
            new User
            {
                RoleId = readerRoleId,
                Login = "reader3", Email = "reader3@mail.ru",
                FullName = "Новиков Дмитрий Олегович",
                HashPass = Hash("reader123"),
                PhoneNumber = "+79005556677"
            }
        );
        context.SaveChanges();
    }

    // ─── Categories ───────────────────────────────────────────────
    private static void SeedCategories(AppDbContext context)
    {
        if (context.Categories.Any()) return;

        context.Categories.AddRange(
            new Category { Name = "Роман",       Description = "Художественная литература, романы" },
            new Category { Name = "Фантастика",  Description = "Научная и социальная фантастика" },
            new Category { Name = "Детектив",    Description = "Детективы и триллеры" },
            new Category { Name = "Классика",    Description = "Классическая русская и зарубежная литература" },
            new Category { Name = "Техническая", Description = "Техническая и учебная литература" }
        );
        context.SaveChanges();
    }

    // ─── Authors ──────────────────────────────────────────────────
    private static void SeedAuthors(AppDbContext context)
    {
        if (context.Authors.Any()) return;

        context.Authors.AddRange(
            new Author { FullName = "Лев Николаевич Толстой",      Bio = "Русский писатель, классик мировой литературы" },
            new Author { FullName = "Фёдор Михайлович Достоевский", Bio = "Русский писатель, мыслитель, философ" },
            new Author { FullName = "Айзек Азимов",                Bio = "Американский писатель-фантаст и биохимик" },
            new Author { FullName = "Агата Кристи",                Bio = "Английская писательница, «королева детектива»" },
            new Author { FullName = "Михаил Афанасьевич Булгаков", Bio = "Русский писатель, драматург и театральный режиссёр" }
        );
        context.SaveChanges();
    }

    // ─── Publishers ───────────────────────────────────────────────
    private static void SeedPublishers(AppDbContext context)
    {
        if (context.Publishers.Any()) return;

        context.Publishers.AddRange(
            new Publisher { Name = "Эксмо",       Description = "Крупнейшее российское издательство", Address = "Москва, ул. Тверская, 1" },
            new Publisher { Name = "АСТ",         Description = "Издательская группа АСТ",            Address = "Москва, пр. Мира, 5" },
            new Publisher { Name = "Азбука",      Description = "Санкт-Петербургское издательство",   Address = "СПб, ул. Невская, 10" },
            new Publisher { Name = "Питер",       Description = "Издательский дом Питер",             Address = "СПб, Петроградская наб., 3" }
        );
        context.SaveChanges();
    }

    // ─── Books ────────────────────────────────────────────────────
    private static void SeedBooks(AppDbContext context)
    {
        if (context.Books.Any()) return;
        
        // Получаем ID по имени
        var tolstoy     = context.Authors.First(a => a.FullName.Contains("Толстой")).AuthorId;
        var dostoevsky  = context.Authors.First(a => a.FullName.Contains("Достоевский")).AuthorId;
        var asimov      = context.Authors.First(a => a.FullName.Contains("Азимов")).AuthorId;
        var christie    = context.Authors.First(a => a.FullName.Contains("Кристи")).AuthorId;
        var bulgakov    = context.Authors.First(a => a.FullName.Contains("Булгаков")).AuthorId;

        var roman       = context.Categories.First(c => c.Name == "Роман").CategoryId;
        var fantasy     = context.Categories.First(c => c.Name == "Фантастика").CategoryId;
        var detective   = context.Categories.First(c => c.Name == "Детектив").CategoryId;
        var classic     = context.Categories.First(c => c.Name == "Классика").CategoryId;

        context.Books.AddRange(
            new Book { AuthorId = tolstoy,    CategoryId = classic,   Name = "Война и мир" },
            new Book { AuthorId = tolstoy,    CategoryId = classic,   Name = "Анна Каренина" },
            new Book { AuthorId = dostoevsky, CategoryId = classic,   Name = "Преступление и наказание" },
            new Book { AuthorId = asimov,     CategoryId = fantasy,   Name = "Основание" },
            new Book { AuthorId = christie,   CategoryId = detective, Name = "Убийство в Восточном экспрессе" },
            new Book { AuthorId = bulgakov,   CategoryId = roman,     Name = "Мастер и Маргарита" }
        );
        context.SaveChanges();
    }

    // ─── VersionBooks ─────────────────────────────────────────────
    private static void SeedVersionBooks(AppDbContext context)
    {
        if (context.VersionBooks.Any()) return;
        
        // Книги
        var voyna        = context.Books.First(b => b.Name == "Война и мир").BookId;
        var anna         = context.Books.First(b => b.Name == "Анна Каренина").BookId;
        var prestuplenie = context.Books.First(b => b.Name == "Преступление и наказание").BookId;
        var osnovanie    = context.Books.First(b => b.Name == "Основание").BookId;
        var ubiystvo     = context.Books.First(b => b.Name == "Убийство в Восточном экспрессе").BookId;
        var master       = context.Books.First(b => b.Name == "Мастер и Маргарита").BookId;

        // Издатели
        var eksmo  = context.Publishers.First(p => p.Name == "Эксмо").PublisherId;
        var ast    = context.Publishers.First(p => p.Name == "АСТ").PublisherId;
        var azbuka = context.Publishers.First(p => p.Name == "Азбука").PublisherId;
        var piter  = context.Publishers.First(p => p.Name == "Питер").PublisherId;

        context.VersionBooks.AddRange(
            new VersionBook { BookId = voyna,        PublisherId = eksmo,  Name = "Война и мир. Полное издание",      CreateAt = Utc(2018, 5, 1),  CountSheets = 1274 },
            new VersionBook { BookId = anna,         PublisherId = ast,    Name = "Анна Каренина. Юбилейное издание", CreateAt = Utc(2020, 3, 15), CountSheets = 864  },
            new VersionBook { BookId = prestuplenie, PublisherId = eksmo,  Name = "Преступление и наказание",         CreateAt = Utc(2019, 9, 1),  CountSheets = 592  },
            new VersionBook { BookId = osnovanie,    PublisherId = azbuka, Name = "Основание. Научное издание",       CreateAt = Utc(2021, 1, 10), CountSheets = 448  },
            new VersionBook { BookId = ubiystvo,     PublisherId = piter,  Name = "Убийство в Восточном экспрессе",   CreateAt = Utc(2022, 6, 20), CountSheets = 320  },
            new VersionBook { BookId = master,       PublisherId = ast,    Name = "Мастер и Маргарита. Классика",     CreateAt = Utc(2017, 11, 5), CountSheets = 480  }
        );
        context.SaveChanges();
    }

    // ─── ExampleBooks ─────────────────────────────────────────────
    private static void SeedExampleBooks(AppDbContext context)
    {
        if (context.ExampleBooks.Any()) return;
        
        var voyna        = context.VersionBooks.First(v => v.Name == "Война и мир. Полное издание").VersionBookId;
        var anna         = context.VersionBooks.First(v => v.Name == "Анна Каренина. Юбилейное издание").VersionBookId;
        var prestuplenie = context.VersionBooks.First(v => v.Name == "Преступление и наказание").VersionBookId;
        var osnovanie    = context.VersionBooks.First(v => v.Name == "Основание. Научное издание").VersionBookId;
        var ubiystvo     = context.VersionBooks.First(v => v.Name == "Убийство в Восточном экспрессе").VersionBookId;
        var master       = context.VersionBooks.First(v => v.Name == "Мастер и Маргарита. Классика").VersionBookId;
        
        context.ExampleBooks.AddRange(
            new ExampleBook { VersionBookId = voyna },
            new ExampleBook { VersionBookId = voyna },
            new ExampleBook { VersionBookId = anna  },
            new ExampleBook { VersionBookId = prestuplenie },
            new ExampleBook { VersionBookId = prestuplenie },
            new ExampleBook { VersionBookId = osnovanie },
            new ExampleBook { VersionBookId = ubiystvo },
            new ExampleBook { VersionBookId = master },
            new ExampleBook { VersionBookId = master },
            new ExampleBook { VersionBookId = anna }
        );
        context.SaveChanges();
    }

    // ─── Loans ────────────────────────────────────────────────────
private static void SeedLoans(AppDbContext context)
{
    if (context.Loans.Any()) return;

    // Пользователи по логину
    var reader1 = context.Users.First(u => u.Login == "reader1").UserId;
    var reader2 = context.Users.First(u => u.Login == "reader2").UserId;
    var reader3 = context.Users.First(u => u.Login == "reader3").UserId;

    // ExampleBooks не имеют имени — берём по порядку вставки (OrderBy PK)
    var exampleBooks = context.ExampleBooks
        .OrderBy(e => e.ExampleBookId)
        .Select(e => e.ExampleBookId)
        .ToList();

    // exampleBooks[0] = 1й экземпляр, [2] = 3й, [4] = 5й, [6] = 7й, [8] = 9й
    context.Loans.AddRange(
        // Выдана и возвращена
        new Loan
        {
            UserId = reader1, ExampleBookId = exampleBooks[0],
            IssuedAt = Utc(2025, 1, 10), DueDate = Utc(2025, 2, 10),
            ExtensionsCount = 0, ReturnedAt = Utc(2025, 2, 5)
        },
        // Просрочена (штраф будет)
        new Loan
        {
            UserId = reader2, ExampleBookId = exampleBooks[2],
            IssuedAt = Utc(2025, 3, 1), DueDate = Utc(2025, 4, 1),
            ExtensionsCount = 1, ReturnedAt = Utc(2025, 4, 20)
        },
        // Активная (не возвращена)
        new Loan
        {
            UserId = reader3, ExampleBookId = exampleBooks[4],
            IssuedAt = Utc(2026, 2, 1), DueDate = Utc(2026, 3, 1),
            ExtensionsCount = 0, ReturnedAt = null
        },
        new Loan
        {
            UserId = reader1, ExampleBookId = exampleBooks[6],
            IssuedAt = Utc(2026, 1, 15), DueDate = Utc(2026, 3, 15),
            ExtensionsCount = 0, ReturnedAt = null
        },
        new Loan
        {
            UserId = reader2, ExampleBookId = exampleBooks[8],
            IssuedAt = Utc(2025, 11, 1), DueDate = Utc(2025, 12, 1),
            ExtensionsCount = 0, ReturnedAt = Utc(2025, 11, 28)
        }
    );
    context.SaveChanges();
}

// ─── Fines ────────────────────────────────────────────────────
private static void SeedFines(AppDbContext context)
{
    if (context.Fines.Any()) return;

    // Читатели и библиотекарь по логину
    var reader1    = context.Users.First(u => u.Login == "reader1").UserId;
    var reader2    = context.Users.First(u => u.Login == "reader2").UserId;
    var reader3    = context.Users.First(u => u.Login == "reader3").UserId;
    var librarian1 = context.Users.First(u => u.Login == "librarian1").UserId;

    context.Fines.AddRange(
        // Оплачен
        new Fine
        {
            ReaderId = reader2, LibrarianId = librarian1,
            Amount   = 150.00m,
            IssuedAt = Utc(2025, 4, 21),
            PaidAt   = Utc(2025, 4, 25),
            Reason   = "Просрочка возврата книги на 19 дней"
        },
        // Не оплачен
        new Fine
        {
            ReaderId = reader1, LibrarianId = librarian1,
            Amount   = 200.00m,
            IssuedAt = Utc(2026, 2, 16),
            PaidAt   = null,
            Reason   = "Просрочка возврата, повреждение обложки"
        },
        new Fine
        {
            ReaderId = reader3, LibrarianId = librarian1,
            Amount   = 50.00m,
            IssuedAt = Utc(2025, 12, 5),
            PaidAt   = Utc(2025, 12, 10),
            Reason   = "Незначительные загрязнения страниц"
        }
    );
    context.SaveChanges();
}


    // ─── Helpers ──────────────────────────────────────────────────
    private static string Hash(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLower();
    }

    private static DateTime Utc(int year, int month, int day, int hour = 0, int minute = 0, int second = 0)
        => DateTime.SpecifyKind(new DateTime(year, month, day, hour, minute, second), DateTimeKind.Utc);
}

using System.Net.Http.Json;
using System.Text.Json;
using Bogus;

// Configuration
var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:8080";
var totalPatients = 100;

Console.WriteLine($"Patient Generator - Posting {totalPatients} patients to {apiBaseUrl}");
Console.WriteLine(new string('-', 60));

var httpClient = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false
};

// Russian-style name generator using Bogus
var faker = new Faker("ru");

var russianFirstNames = new[] {
    "Александр", "Михаил", "Иван", "Дмитрий", "Андрей", "Сергей", "Алексей", "Николай",
    "Мария", "Анна", "Екатерина", "Ольга", "Наталья", "Елена", "Татьяна", "Ирина",
    "Артём", "Кирилл", "Максим", "Роман", "Даниил", "Денис", "Владимир", "Павел",
    "Дарья", "Валентина", "Светлана", "Людмила", "Юлия", "Виктория", "Ксения", "Полина"
};

var russianMiddleNamesM = new[] {
    "Александрович", "Михайлович", "Иванович", "Дмитриевич", "Андреевич",
    "Сергеевич", "Алексеевич", "Николаевич", "Артёмович", "Кириллович"
};

var russianMiddleNamesF = new[] {
    "Александровна", "Михайловна", "Ивановна", "Дмитриевна", "Андреевна",
    "Сергеевна", "Алексеевна", "Николаевна", "Артёмовна", "Кирилловна"
};

var russianLastNamesM = new[] {
    "Иванов", "Смирнов", "Кузнецов", "Попов", "Васильев", "Петров", "Соколов",
    "Михайлов", "Новиков", "Федоров", "Морозов", "Волков", "Алексеев", "Лебедев"
};

var russianLastNamesF = new[] {
    "Иванова", "Смирнова", "Кузнецова", "Попова", "Васильева", "Петрова", "Соколова",
    "Михайлова", "Новикова", "Федорова", "Морозова", "Волкова", "Алексеева", "Лебедева"
};

var genders = new[] { "male", "female", "other", "unknown" };
var rnd = new Random();

int success = 0, failed = 0;

var startOf2025 = new DateTime(2025, 1, 1);
var daysElapsed = (int)(DateTime.Today - startOf2025).TotalDays;

for (int i = 1; i <= totalPatients; i++)
{
    var isMale = rnd.Next(2) == 0;
    var gender = i <= 90
        ? (isMale ? "male" : "female")  // 90% male/female
        : genders[rnd.Next(genders.Length)]; // 10% other/unknown

    bool patientIsMale = gender == "male" || (gender == "unknown" && rnd.Next(2) == 0);

    var lastName = patientIsMale
        ? russianLastNamesM[rnd.Next(russianLastNamesM.Length)]
        : russianLastNamesF[rnd.Next(russianLastNamesF.Length)];

    var firstName = russianFirstNames
        .Where(n => patientIsMale
            ? !n.EndsWith("а") && !n.EndsWith("я") && !n.EndsWith("ь")
            : n.EndsWith("а") || n.EndsWith("я"))
        .OrderBy(_ => rnd.Next())
        .FirstOrDefault() ?? russianFirstNames[rnd.Next(russianFirstNames.Length)];

    var middleName = patientIsMale
        ? russianMiddleNamesM[rnd.Next(russianMiddleNamesM.Length)]
        : russianMiddleNamesF[rnd.Next(russianMiddleNamesF.Length)];

    // BirthDates spread across 2025 up to today(newborns)
    var birthDate = startOf2025
        .AddDays(rnd.Next(0, daysElapsed))
        .AddHours(rnd.Next(0, 24))
        .AddMinutes(rnd.Next(0, 60))
        .AddSeconds(rnd.Next(0, 60));

    var patient = new
    {
        name = new
        {
            use = "official",
            family = lastName,
            given = new[] { firstName, middleName }
        },
        gender = gender,
        birthDate = birthDate.ToString("yyyy-MM-ddTHH:mm:ss"),
        active = true
    };

    try
    {
        var response = await httpClient.PostAsJsonAsync("/api/patient", patient, jsonOptions);
        if (response.IsSuccessStatusCode)
        {
            success++;
            var created = await response.Content.ReadFromJsonAsync<dynamic>(jsonOptions);
            Console.WriteLine($"[{i:D3}] ✓ {lastName} {firstName} ({gender}) - {birthDate:yyyy-MM-dd}");
        }
        else
        {
            failed++;
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[{i:D3}] ✗ {lastName} {firstName} - HTTP {(int)response.StatusCode}: {error}");
        }
    }
    catch (Exception ex)
    {
        failed++;
        Console.WriteLine($"[{i:D3}] ✗ Error: {ex.Message}");
    }

    // Small delay to avoid overwhelming the API
    await Task.Delay(50);
}

Console.WriteLine(new string('-', 60));
Console.WriteLine($"Done! Success: {success}, Failed: {failed}");

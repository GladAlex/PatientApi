# Patient API

REST API для управления пациентами (новорождённые дети) на базе **.NET 6**, **MS SQL Server** и **Docker**.

---

## Структура проекта

```
PatientApi/
├── PatientApi/                        # Основной API-проект
│   ├── Controllers/
│   │   └── PatientController.cs       # CRUD + поиск по birthDate
│   ├── Data/
│   │   └── PatientApiDbContext.cs     # EF Core контекст
│   ├── DTOs/
│   │   ├── PatientCreateRequestDto.cs # DTO для запросов/ответов
│   │   ├── PatientNameDto.cs    
│   │   ├── PatientResponseDto.cs
│   │   └── PatientUpdateRequestDto.cs   
│   ├── Migrations/                    # EF Core миграции
│   ├── Models/
│   │   ├── Patient.cs                 # Сущности Patient, PatientName
│   │   ├── PatientName.cs
│   │   └── Gender.cs
│   ├── Services/
│   │   ├── FhirDateSearchParser.cs    # Парсер FHIR-поиска по дате
│   │   ├── PatientService.cs          # Бизнес-логика
│   │   └── IPatientService.cs         # Интерфейс
│   ├── Program.cs
│   ├── appsettings.json
│   └── appsettings.Development.json
├── PatientApi.Console/                # Консольное приложение (генератор)
│   └── Program.cs                     # Генерирует и POST'ит 100 пациентов
├── Dockerfile                         # Образ API
├── Dockerfile.Console                 # Образ консольного приложения
├── docker-compose.yml                 # Оркестрация: DB + API + Console
├── PatientApi.postman_collection.json # Postman-коллекция
├── PatientApi.postman_environment.json
└── PatientApi.sln
```

---

## Модель данных

```json
{
  "name": {
    "id": "d8ff176f-bd0a-4b8e-b329-871952e32e1f",
    "use": "official",
    "family": "Иванов",
    "given": ["Иван", "Иванович"]
  },
  "gender": "male",
  "birthDate": "2024-01-13T18:25:43",
  "active": true
}
```

**Обязательные поля:** `name.family`, `birthDate`

**Справочники:**
- `gender`: `male | female | other | unknown`
- `active`: `true | false`

**Таблицы БД:**
- `Patients` — `Id (PK/GUID)`, `Gender`, `BirthDate`, `Active`
- `PatientNames` — `Id (PK + FK → Patients.Id)`, `Use`, `Family`, `Given`

## Быстрый старт с Docker

```bash
# Клонируйте / распакуйте проект
cd PatientApi

# Запуск всего стека (БД + API + консольный сидер)
docker compose up --build

# API будет доступен на: https://localhost:8080
# Swagger UI:           https://localhost:8080/index.html
# MS SQL Server:        localhost:1433
```

После запуска консольное приложение автоматически добавит **100 сгенерированных пациентов**.

---

## API Endpoints

| Метод  | URL                         | Описание                          |
|--------|-----------------------------|-----------------------------------|
| GET    | `/api/patient/{id}`         | Получить пациента по ID           |
| GET    | `/api/patient?birthDate=…`  | Поиск по дате рождения (FHIR)     |
| POST   | `/api/patient`              | Создать пациента                  |
| PUT    | `/api/patient/{id}`         | Обновить пациента                 |
| DELETE | `/api/patient/{id}`         | Удалить пациента                  |

---

## Поиск по birthDate (FHIR)

Реализован по спецификации: https://www.hl7.org/fhir/search.html#date

### Поддерживаемые форматы дат

| Формат               | Описание                    | Диапазон поиска              |
|----------------------|-----------------------------|------------------------------|
| `YYYY`               | Год                         | Весь год                     |
| `YYYY-MM`            | Год и месяц                 | Весь месяц                   |
| `YYYY-MM-DD`         | Полная дата                 | Весь день                    |
| `YYYY-MM-DDThh:mm:ss`| Дата и время                | Та же секунда                |

### Поддерживаемые префиксы

| Префикс | Значение         | Пример                   |
|---------|------------------|--------------------------|
| `eq`    | Равно (по умолчанию) | `birthDate=2026-01-13`   |
| `ne`    | Не равно         | `birthDate=ne2026-01-13` |
| `lt`    | Меньше           | `birthDate=lt2025-06-01` |
| `gt`    | Больше           | `birthDate=gt2026-01-01` |
| `le`    | Меньше или равно | `birthDate=le2025-12-31` |
| `ge`    | Больше или равно | `birthDate=ge2026-01-01` |
| `sa`    | Начинается после | `birthDate=sa2025-06-01` |
| `eb`    | Заканчивается до | `birthDate=eb2025-06-01` |
| `ap`    | Приблизительно   | `birthDate=ap2025-06-15` |

### Примеры запросов

```bash
# Все пациенты, рождённые 13 января 2026
GET /api/patient?birthDate=2026-01-13

# Все пациенты, рождённые в 2026 году
GET /api/patient?birthDate=2026

# Все пациенты, рождённые в январе 2024
GET /api/patient?birthDate=2026-01

# Пациенты, рождённые после 1 января 2026 (не включая)
GET /api/patient?birthDate=gt2026-01-01

# Пациенты, рождённые до 1 июня 2025 (не включая)
GET /api/patient?birthDate=lt2025-06-01

# Пациенты, рождённые в 2026 году и позже
GET /api/patient?birthDate=ge2026-01-01

# Пациенты, НЕ рождённые 13 января 2026
GET /api/patient?birthDate=ne2026-01-13

# Приблизительно около 15 июня 2025 (±10% от точности)
GET /api/patient?birthDate=ap2025-06-15

# Точный поиск с временем
GET /api/patient?birthDate=eq2026-01-13T18:25:43
```

## Postman

Импортируйте в Postman:
- `PatientApi.postman_collection.json` — коллекция со всеми запросами
- `PatientApi.postman_environment.json` — окружение (baseUrl, patientId)

**Переменные:**
- `baseUrl` — базовый URL API (по умолчанию `http://localhost:8080`)
- `patientId` — заполняется автоматически после запроса **Create Patient**

**Рекомендуемый порядок выполнения:**
1. Create Patient → сохраняет ID в `{{patientId}}`
2. Get Patient by ID
3. Update Patient
4. Search (различные варианты)
5. Delete Patient

---

## Технологии

- **.NET 6** / **ASP.NET Core 6**
- **Entity Framework Core 6** + SQL Server provider
- **MS SQL Server 2022** (Express)
- **Swashbuckle** (Swagger/OpenAPI)
- **Bogus** (генерация фейковых данных в консольном приложении)
- **Docker** + **Docker Compose**

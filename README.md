# Event Manager API

Сервис для управления мероприятиями и бронированиями. Базовый каркас приложения, реализованный на ASP.NET Core Web API с применением принципов чистой архитектуры (Clean Architecture), паттерна Repository, фоновых задач, а также интегрированный с реляционной базой данных PostgreSQL через Entity Framework Core с управлением схемы через миграции.

## Технологический стек

- **C# / .NET 10**
- **ASP.NET Core Web API**
- **База данных:** PostgreSQL (через Docker)
- **ORM:** Entity Framework Core (с провайдером Npgsql и управлением через Migrations)
- **Архитектура:** Clean Architecture (4 слоя: Domain, Application, Infrastructure, Presentation)
- **Фоновая обработка:** BackgroundService (отложенная обработка бронирований)
- **Потокобезопасность:** `SemaphoreSlim` (защита от овербукинга на уровне сервиса)
- **Документация:** Swagger (OpenAPI)
- **Тестирование:**
    - xUnit + EF Core InMemory Provider (юнит-тесты сервисов)
    - xUnit + Testcontainers (интеграционные тесты репозиториев на реальной PostgreSQL)

## Архитектура проекта

Проект реализован по принципам **Clean Architecture** — зависимости направлены строго внутрь, от внешних слоёв к внутренним:

```
Presentation → Application → Domain ← Infrastructure
```

### Слои и их ответственность

| Слой | Проект | Зависимости | Ответственность |
|------|--------|-------------|-----------------|
| **Domain** | `EventManager.Domain` | Нет зависимостей | Доменные сущности (`Event`, `Booking`), исключения (`DomainException`, `NotFoundException`, `NoAvailableSeatsException`, `DomainValidationException`), перечисления (`BookingStatus`) |
| **Application** | `EventManager.Application` | Domain | Бизнес-логика (`EventService`, `BookingService`), DTO, порт-интерфейсы репозиториев (`IEventRepository`, `IBookingRepository`), интерфейсы сервисов (`IEventService`, `IBookingService`), фоновый сервис (`BookingBackgroundService`), `DependencyInjection.cs` |
| **Infrastructure** | `EventManager.Infrastructure` | Application, Domain | `AppDbContext`, EF Core конфигурации (`IEntityTypeConfiguration`), реализации репозиториев (`EventRepository`, `BookingRepository`), миграции, `DependencyInjection.cs` |
| **Presentation** | `EventManager.Api` | Application, Infrastructure | Контроллеры, `Program.cs` (Composition Root), Swagger, `GlobalExceptionHandler` |

### Ключевые принципы

- **Разделение ответственности (SRP):** Каждый слой имеет чёткую зону ответственности. Контроллеры — только маршрутизация, сервисы — бизнес-логика, репозитории — доступ к данным.
- **Инверсия зависимостей (DIP):** Порт-интерфейсы (`IEventRepository`, `IBookingRepository`) определены в **Application**, а их реализации — в **Infrastructure**. Сервисы зависят только от абстракций.
- **Composition Root:** Все регистрации DI собраны в `Program.cs` через два extension-метода: `AddApplicationServices()` и `AddInfrastructureServices()`.
- **Domain без зависимостей:** Слой Domain не ссылается ни на какие внешние пакеты — даже `StatusCodes` заменён на литеральные значения `int`.
- **Жизненные циклы DI:**
    - `AppDbContext` и репозитории — **Scoped** (один экземпляр на HTTP-запрос).
    - Доменные сервисы — **Scoped**.
    - Фоновый сервис — **Singleton** (использует `IServiceScopeFactory` для создания scoped-зависимостей внутри себя).
- **Обработка ошибок:** Единообразный формат RFC 7807 (Problem Details) для 400, 404, 409 и 500 ошибок через `GlobalExceptionHandler`. Доменные исключения наследуются от базового `DomainException` (без зависимости от ASP.NET).
- **База данных:** Маппинг сущностей реализован через Fluent API (`IEntityTypeConfiguration`). Схема БД управляется миграциями и применяется автоматически при старте приложения.
- **Observability (Логирование):** Внедрен `ILogger` в сервисы для трассировки жизненного цикла запросов и отладки.

## Структура проекта

```text
EventManagerAPI/
├── EventManager.Domain/                  <-- Доменный слой (без внешних зависимостей)
│   ├── Entities/                         <-- Сущности: Event, Booking
│   │   ├── Event.cs
│   │   ├── Booking.cs
│   │   └── BookingStatus.cs
│   └── Exceptions/                       <-- Доменные исключения
│       ├── DomainException.cs            <--   Базовый класс (без ASP.NET-зависимостей)
│       ├── NotFoundException.cs
│       ├── NoAvailableSeatsException.cs
│       └── DomainValidationException.cs
│
├── EventManager.Application/             <-- Слой бизнес-логики
│   ├── DTOs/                             <-- Объекты передачи данных
│   │   ├── Events/
│   │   │   ├── CreateEventDto.cs
│   │   │   ├── UpdateEventDto.cs
│   │   │   ├── EventResponseDto.cs
│   │   │   └── GetEventsQueryParams.cs
│   │   └── Bookings/
│   │       ├── CreateBookingDto.cs
│   │       └── BookingResponseDto.cs
│   ├── Interfaces/                       <-- Порт-интерфейсы
│   │   ├── IEventRepository.cs
│   │   ├── IBookingRepository.cs
│   │   ├── IEventService.cs
│   │   └── IBookingService.cs
│   ├── Services/                         <-- Бизнес-логика
│   │   ├── EventService.cs
│   │   └── BookingService.cs
│   ├── BookingBackgroundService.cs       <-- Фоновая обработка бронирований
│   └── DependencyInjection.cs            <-- Extension-метод AddApplicationServices()
│
├── EventManager.Infrastructure/          <-- Инфраструктурный слой
│   ├── Data/
│   │   ├── AppDbContext.cs               <-- Контекст EF Core
│   │   └── Configurations/               <-- Fluent API (IEntityTypeConfiguration)
│   │       ├── EventConfiguration.cs
│   │       └── BookingConfiguration.cs
│   ├── Repositories/                     <-- Реализации порт-интерфейсов
│   │   ├── EventRepository.cs
│   │   └── BookingRepository.cs
│   ├── Migrations/                       <-- EF Core миграции
│   └── DependencyInjection.cs            <-- Extension-метод AddInfrastructureServices()
│
├── EventManager.Api/                     <-- Presentation-слой
│   ├── Controllers/                      <-- Тонкие контроллеры (маршрутизация)
│   │   ├── EventsController.cs
│   │   └── BookingsController.cs
│   ├── GlobalExceptionHandler.cs         <-- Глобальный обработчик ошибок (RFC 7807)
│   ├── Program.cs                        <-- Composition Root
│   ├── appsettings.json
│   └── EventManagerAPI.csproj
│
├── EventManagerAPI.Tests/                <-- Юнит-тесты (InMemory)
│   ├── EventServiceTests.cs
│   ├── BookingServiceTests.cs
│   └── DtoValidationTests.cs
│
└── EventManagerAPI.IntegrationTests/     <-- Интеграционные тесты (Testcontainers + PostgreSQL)
    ├── Fixtures/                         <-- Настройка жизненного цикла Docker-контейнера
    ├── IntegrationTestBase.cs            <-- Базовый класс с очисткой БД и применением миграций
    ├── MigrationTests.cs                 <-- Тесты структуры БД (таблицы, FK, ограничения)
    ├── EventRepositoryTests.cs           <-- Полное покрытие CRUD и фильтров EventRepository
    └── BookingRepositoryTests.cs         <-- Полное покрытие методов BookingRepository
```

## Подготовка и запуск проекта

### 1. Требования

Для работы приложения и интеграционных тестов требуется установленный и запущенный **Docker**.

### 2. Запуск базы данных

В корне проекта находится файл `docker-compose.yml` для локальной базы данных. Откройте терминал в корне проекта и выполните команду:

```bash
docker compose up -d
```

### 3. Настройка строки подключения

Строка подключения к PostgreSQL находится в `EventManager.Api/appsettings.json`.

```json
{
        "ConnectionStrings": {
                "DefaultConnection": "Host=localhost;Port=5432;Database=eventapi;Username=postgres;Password=postgres"
        }
}
```

### 4. Управление схемой БД (Миграции)

Схема базы данных управляется **миграциями EF Core**. Миграции находятся в проекте `EventManager.Infrastructure`.

В файле `Program.cs` перед стартом API вызывается метод `Migrate()`:

```csharp
using (var scope = app.Services.CreateScope())
{
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate(); // Применяет все неактивные миграции при запуске
}
```

**Что это значит:** Приложение при запуске проверяет таблицу истории миграций в PostgreSQL и накатывает все недостающие изменения (создание таблиц, внешних ключей и т.д.). Это безопасный способ обновления схемы БД в продакшене.

#### Как создать новую миграцию (если изменили модели):

Если вы изменили доменную модель или `IEntityTypeConfiguration`, сгенерируйте новую миграцию с помощью CLI (находясь в корне решения):

```bash
# Установка инструмента (если еще не установлено)
dotnet new tool-manifest
dotnet tool install dotnet-ef

# Генерация миграции (миграции создаются в Infrastructure-проекте)
dotnet ef migrations add <Название_миграции> --project EventManager.Infrastructure --startup-project EventManager.Api
```

### 5. Сборка и запуск

1. Клонируйте репозиторий и перейдите в папку решения.
2. Выполните сборку:

```bash
dotnet build
```

3. Запустите API:

```bash
dotnet run --project EventManager.Api
```

4. Откройте Swagger UI по адресу из логов консоли (например `http://localhost:5000/swagger`).

---
## Запуск тестов

Для запуска всех тестов (юнит и интеграционных) выполните команду из корня решения:

```bash
dotnet test
```

### Юнит-тесты (InMemory)

Проект `EventManagerAPI.Tests` использует **Microsoft.EntityFrameworkCore.InMemory**. Это обеспечивает максимальную скорость выполнения и полную изоляцию от внешних зависимостей. В тестовых классах DI-контейнер настраивается с подменой `AppDbContext` in-memory базой, а репозитории мокаются/подменяются для проверки бизнес-логики сервисов.

### Интеграционные тесты (Testcontainers)

Проект `EventManagerAPI.IntegrationTests` предназначен для проверки реального взаимодействия с PostgreSQL на уровне репозиториев.

- Использует библиотеку **Testcontainers**, которая автоматически скачивает образ, поднимает изолированный контейнер PostgreSQL, выполняет тесты и удаляет контейнер.
- **Важно:** Для их успешного прохождения обязательно должен быть запущен Docker.
- **Изоляция:** Перед каждым тестом база данных приводится к чистому состоянию (`EnsureDeleted()` + `Migrate()`), что гарантирует независимость тестов друг от друга.
- **Покрытие репозиториев:** Все тесты оформлены с явными блоками Arrange / Act / Assert. Покрыты абсолютно все методы обоих репозиториев:
    - **EventRepository:** `GetFilteredAsync` (отдельные тесты для `title`, `from`, `to` и пагинации), `GetByIdAsync`, `Update` (проверка реального сохранения в БД), `Remove`.
    - **BookingRepository:** `Add` (позитивный сценарий), `GetByIdAsync`, `GetTrackedByIdAsync` (проверка отслеживания сущностей EF Core), `GetEventByIdAsync`, `GetPendingBookingIdsAsync`, негативный тест на нарушение внешнего ключа (FK).

---
## Документация API

### События (Events)

|Метод|Эндпоинт|Описание|Успешный статус|
|---|---|---|---|
|`GET`|`/events`|Получить список мероприятий (с фильтрацией и пагинацией)|200 OK|
|`GET`|`/events/{id}`|Получить мероприятие по ID|200 OK / 404 Not Found|
|`POST`|`/events`|Создать новое мероприятие (с обязательным указанием `totalSeats`)|201 Created / 400 Bad Request|
|`PUT`|`/events/{id}`|Обновить мероприятие целиком|204 No Content / 404 Not Found|
|`DELETE`|`/events/{id}`|Удалить мероприятие|204 No Content / 404 Not Found|
### Бронирования (Bookings)

|Метод|Эндпоинт|Описание|Успешный статус|
|---|---|---|---|
|`POST`|`/events/{id}/book`|Создать бронь (быстрый ответ + отложенная обработка)|202 Accepted|
|`GET`|`/bookings/{id}`|Получить статус брони|200 OK / 404 Not Found|

**Особенности:**

- `POST /events/{id}/book` мгновенно резервирует место через репозиторий, сохраняет бронь со статусом `Pending` и возвращает `202 Accepted`.
- Если мест нет, `POST /events/{id}/book` возвращает **409 Conflict** (защита от овербукинга).

### Пример ответа события

```json
{
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "title": "Тренировка по баскету",
        "description": "Опоздаешь-отожмёшься!",
        "startAt": "2024-01-15T20:00:00Z",
        "endAt": "2024-01-15T23:00:00Z",
        "totalSeats": 50,
        "availableSeats": 42
}
```


### Фильтрация и пагинация (GET /events)

Эндпоинт `GET /events` принимает опциональные query-параметры для поиска:

- `title` (string) — поиск по названию (частичное совпадение, регистронезависимый через `ILike` в PostgreSQL).
- `from` (DateTime) — события, которые начинаются **не раньше** указанной даты.
- `to` (DateTime) — события, которые заканчиваются **не позже** указанной даты.
- `page` (int, по умолчанию 1) — номер страницы (не может быть < 1).
- `pageSize` (int, по умолчанию 10) — количество элементов на странице (не может быть < 1).

---
## Правила валидации

Сервис осуществляет строгую проверку входных данных:

- **Title**: Не может быть пустой строкой.
- **Даты**: `EndAt` строго позже `StartAt`.
- **Дата в прошлом**: `StartAt` не может быть меньше текущего времени сервера (UTC).
- **Пагинация**: Параметры `page` и `pageSize` не могут быть меньше 1 (возвращает 400 Bad Request).

---
## Обработка ошибок

Все ошибки (как от встроенного валидатора, так и от бизнес-логики) возвращаются в **единообразном формате RFC 7807 (Problem Details)**.

### Пример 1: Ошибка валидации (400 Bad Request)

Возникает при невалидных данных в теле запроса или некорректных параметрах пагинации.

```json
{
          "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
          "title": "One or more validation errors occurred.",
          "status": 400,
          "instance": "/events",
          "traceId": "00-873ba4af2ca7f9c6f861fb3cdb5c6669-0c1015a54df57858-00",
          "errors": {
            "Title": [
              "Название мероприятия не может быть пустым!"
            ]
        }
}
```
### Пример 2: Ресурс не найден (404 Not Found)

Возникает при запросе, обновлении или удалении несуществующего ID.

```json

{
        "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
        "title": "Not Found",
        "status": 404,
        "detail": "Мероприятие с ID 00000000-0000-0000-0000-000000000000 не найдено",
        "instance": "/events/00000000-0000-0000-0000-000000000000",
        "traceId": "00-2f74d953e90ca3678214d2e930e48b51-fd9e4692eb25c93b-00"
}
```

## Конкурентность и защита от овербукинга

### Поля модели Event

- `TotalSeats` (int) — общее количество мест на мероприятии.
- `AvailableSeats` (int) — текущее количество свободных мест.

### Архитектура многопоточности

- **`SemaphoreSlim` в `BookingService`:** Защищает критическую секцию "получение события через репозиторий -> проверка мест -> резервирование -> сохранение", гарантируя, что параллельные запросы не забронируют одно и то же место.
- **`IServiceScopeFactory` в `BookingBackgroundService`:** Позволяет фоновым задачам (Singleton) получать правильные `Scoped` репозитории для каждой обрабатываемой брони, избегая проблем с жизненным циклом контекста базы данных.

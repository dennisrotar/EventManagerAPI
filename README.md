# Event Manager API

Сервис для управления мероприятиями и бронированиями. Каркас приложения, реализованный на ASP.NET Core Web API с применением принципов чистой архитектуры (Clean Architecture), паттерна Repository, фоновых задач, JWT-аутентификации, ролевой авторизации, а также интегрированный с реляционной базой данных PostgreSQL через Entity Framework Core.

## Технологический стек

- **C# / .NET 10**
- **ASP.NET Core Web API**
- **База данных:** PostgreSQL (через Docker)
- **ORM:** Entity Framework Core (с провайдером Npgsql и управлением через Migrations)
- **Аутентификация:** JWT Bearer Token
- **Архитектура:** Clean Architecture (4 слоя: Domain, Application, Infrastructure, Presentation)
- **Фоновая обработка:** BackgroundService (отложенная обработка бронирований)
- **Потокобезопасность:** `SemaphoreSlim` (защита от овербукинга на уровне сервиса)
- **Документация:** Swagger (OpenAPI) с поддержкой JWT-авторизации
- **Тестирование:**
    - xUnit + EF Core InMemory Provider (юнит-тесты сервисов)
    - xUnit + Testcontainers (интеграционные тесты репозиториев на реальной PostgreSQL)

## Архитектура проекта

Проект реализован по принципам **Clean Architecture** — зависимости направлены строго внутрь, от внешних слоёв к внутренним:

Presentation → Application → Domain ← Infrastructure
### Слои и их ответственность

| Слой               | Проект                        | Зависимости                 | Ответственность                                                                                                                                                                                                                                                    |
| ------------------ | ----------------------------- | --------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **Domain**         | `EventManager.Domain`         | Нет зависимостей            | Доменные сущности (`Event`, `Booking`, `User`), исключения (`DomainException`, `NotFoundException`, `NoAvailableSeatsException`, `PastEventBookingException`, `ActiveBookingLimitExceededException`, `ForbiddenException`), перечисления (`BookingStatus`, `Role`) |
| **Application**    | `EventManager.Application`    | Domain                      | Бизнес-логика (`EventService`, `BookingService`, `UserService`), DTO, порт-интерфейсы (`IEventRepository`, `IBookingRepository`, `IUserRepository`, `IPasswordHasher`, `ITokenService`), фоновый сервис                                                            |
| **Infrastructure** | `EventManager.Infrastructure` | Application, Domain         | `AppDbContext`, EF Core конфигурации, реализации репозиториев, сервисы безопасности (`PasswordHasher`, `JwtTokenService`), миграции                                                                                                                                |
| **Presentation**   | `EventManager.Api`            | Application, Infrastructure | Контроллеры (`EventsController`, `BookingsController`, `AuthController`), `Program.cs` (Composition Root), Swagger, `GlobalExceptionHandler`                                                                                                                       |
### Ключевые принципы
- **Разделение ответственности (SRP):** Каждый слой имеет чёткую зону ответственности. Контроллеры — только маршрутизация, сервисы — бизнес-логика, репозитории — доступ к данным.
- **Инверсия зависимостей (DIP):** Порт-интерфейсы определены в **Application**, а их реализации — в **Infrastructure**. Сервисы зависят только от абстракций.
- **Composition Root:** Все регистрации DI собраны в `Program.cs` через два extension-метода: `AddApplicationServices()` и `AddInfrastructureServices()`.
- **Жизненные циклы DI:**
	- `AppDbContext` и репозитории — **Scoped** (один экземпляр на HTTP-запрос).
	- Доменные сервисы — **Scoped**.
	- Фоновый сервис — **Singleton** (использует `IServiceScopeFactory` для создания scoped-зависимостей внутри себя).
- **Обработка ошибок:** Единообразный формат RFC 7807 (Problem Details) для 400, 401, 403, 404, 409 и 500 ошибок через `GlobalExceptionHandler`.
## Структура проекта

```
EventManagerAPI/
├── EventManager.Domain/ <-- Доменный слой (без внешних зависимостей)
│ ├── Entities/ <-- Сущности: Event, Booking, User
│ │ ├── Event.cs
│ │ ├── Booking.cs
│ │ ├── User.cs
│ │ ├── BookingStatus.cs
│ │ └── Role.cs <-- Перечисление ролей (Admin, User)
│ └── Exceptions/ <-- Доменные исключения
│ ├── DomainException.cs <-- Базовый класс
│ ├── NotFoundException.cs
│ ├── NoAvailableSeatsException.cs
│ ├── DomainValidationException.cs
│ ├── PastEventBookingException.cs <-- Бронирование прошедшего события
│ ├── ActiveBookingLimitExceededException.cs <-- Лимит броней превышен
│ └── ForbiddenException.cs <-- Нет прав на действие
│
├── EventManager.Application/ <-- Слой бизнес-логики
│ ├── DTOs/ <-- Объекты передачи данных
│ │ ├── Events/
│ │ │ ├── CreateEventDto.cs
│ │ │ ├── UpdateEventDto.cs
│ │ │ ├── EventResponseDto.cs
│ │ │ └── GetEventsQueryParams.cs
│ │ ├── Bookings/
│ │ │ ├── CreateBookingDto.cs
│ │ │ └── BookingResponseDto.cs
│ │ └── Auth/ <--  DTO для аутентификации
│ │ ├── RegisterUserDto.cs
│ │ ├── LoginUserDto.cs
│ │ └── TokenResponseDto.cs
│ ├── Interfaces/ <-- Порт-интерфейсы
│ │ ├── IEventRepository.cs
│ │ ├── IBookingRepository.cs
│ │ ├── IUserRepository.cs
│ │ ├── IEventService.cs
│ │ ├── IBookingService.cs
│ │ ├── IUserService.cs
│ │ ├── IPasswordHasher.cs
│ │ └── ITokenService.cs
│ ├── Services/ <-- Бизнес-логика
│ │ ├── EventService.cs
│ │ ├── BookingService.cs
│ │ └── UserService.cs <-- Регистрация и Логин
│ ├── BookingBackgroundService.cs <-- Фоновая обработка бронирований
│ └── DependencyInjection.cs <-- Extension-метод AddApplicationServices()
│
├── EventManager.Infrastructure/ <-- Инфраструктурный слой
│ ├── DataAccess/
│ │ ├── AppDbContext.cs <-- Контекст EF Core
│ │ └── Configurations/ <-- Fluent API (IEntityTypeConfiguration)
│ │ ├── EventConfiguration.cs
│ │ ├── BookingConfiguration.cs
│ │ └── UserConfiguration.cs <-- Настройка пользователя (уникальный логин)
│ ├── Repositories/ <-- Реализации порт-интерфейсов
│ │ ├── EventRepository.cs
│ │ ├── BookingRepository.cs
│ │ └── UserRepository.cs
│ ├── Security/ <-- Сервисы безопасности
│ │ ├── PasswordHasher.cs <-- Хеширование SHA-256
│ │ └── JwtTokenService.cs <-- Генерация JWT
│ ├── Migrations/ <-- EF Core миграции
│ └── DependencyInjection.cs <-- Extension-метод AddInfrastructureServices()
│
├── EventManager.Api/ <-- Presentation-слой
│ ├── Controllers/ <-- Тонкие контроллеры
│ │ ├── EventsController.cs
│ │ ├── BookingsController.cs
│ │ └── AuthController.cs <-- Эндпоинты регистрации и логина
│ ├── GlobalExceptionHandler.cs <-- Глобальный обработчик ошибок (RFC 7807)
└── UserRepositoryTests.cs <-- Тесты уникальности логина
```
## Ролевая модель и авторизация

В системе реализованы две роли: `Admin` и `User`.

- **Администратор (`Admin`)**:
    - Полный доступ к управлению событиями (создание, редактирование, удаление).
    - Может отменять любые бронирования пользователей.
- **Обычный пользователь (`User`)**:
    - Может просматривать события и бронировать их.
    - Может отменять **только свои** бронирования.

## Защита эндпоинтов

- `POST /auth/register`, `POST /auth/login`, `GET /events` — доступны без токена.
- `POST /events/`, `PUT /events/{id}`, `DELETE /events/{id}` — защищены атрибутом `[Authorize(Roles = "Admin")]`.
- `POST /events/{id}/book`, `GET /bookings/{id}`, `DELETE /bookings/{id}` — защищены атрибутом `[Authorize]`. Идентификатор пользователя читается из JWT-токена (Claims).

## Бизнес-правила бронирования

В `BookingService` инкапсулированы следующие правила:

1. **Запрет бронирования прошедшего события:** Если `StartAt` события меньше текущего времени UTC, выбрасывается `PastEventBookingException` (HTTP 400).
2. **Лимит активных броней:** У одного пользователя не может быть более 10 активных броней (статусы `Pending` или `Confirmed`). При превышении выбрасывается `ActiveBookingLimitExceededException` (HTTP 409). Лимит вынесен в именованную константу.
3. **Проверка прав при отмене:** При отмене брони (`DELETE /bookings/{id}`) проверяется владелец. Если отменяет не владелец (и не админ), выбрасывается `ForbiddenException` (HTTP 403).
4. **Возврат мест:** При успешной отмене брони вызывается метод `ReleaseSeats()` у сущности `Event`, увеличивающий количество доступных мест (`AvailableSeats`).

## Подготовка и запуск проекта

### 1. Требования
Для работы приложения и интеграционных тестов требуется установленный и запущенный **Docker**.

### 2. Запуск базы данных
В корне проекта находится `docker-compose.yml`. Выполните:

```bash
docker compose up -d
```
### 3. Настройка конфигурации
Строка подключения к БД и параметры JWT находятся в `EventManager.Api/appsettings.json`.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=eventapi;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Secret": "SuperSecretKeyForTestingPurposesOnly123!@#",
    "Issuer": "EventManagerAPI",
    "Audience": "EventManagerUsers",
    "ExpiresMinutes": 60
  }
}
```


**⚠️ Внимание:** Секретный ключ `Jwt:Secret` сделан для удобства локальной разработки. В production-окружении его **обязательно** следует переопределить через переменные окружения (Environment Variables) или Secret Manager, не допуская попадания ключа в исходный код.
### 4. Управление схемой БД (Миграции)

Схема БД управляется миграциями EF Core. При запуске приложения (в `Program.cs`) вызывается `db.Database.Migrate()`, что автоматически применяет все новые миграции (включая добавление таблицы `Users` и внешнего ключа `UserId` в `Bookings`).

### 5. Сборка и запуск
```bash

dotnet build

dotnet run --project EventManager.Api
```

Swagger UI будет доступен по адресу `http://localhost:<порт>/swagger`.

---

## Документация API и использование Swagger

### Как авторизоваться в Swagger:

1. Зарегистрируйте пользователя через `POST /auth/register` (можно указать `"role": "Admin"`).
2. Выполните `POST /auth/login` с логином и паролем.
3. Скопируйте полученный `token` из ответа.
4. Нажмите кнопку **Authorize** 🔒 в правом верхнем углу Swagger UI.
5. Вставьте токен в поле ввода. В Swagger настроен `Type = SecuritySchemeType.Http`, добавление префикса `Bearer` перед токеном не требуется, сразу вставляйте токен как есть, без "Bearer " (например, `eyJhbG...`).
6. Нажмите **Authorize** -> **Close**. Теперь все защищенные эндпоинты будут работать.
### Эндпоинты
#### Аутентификация (Auth)

|Метод|Эндпоинт|Описание|Статусы|
|---|---|---|---|
|`POST`|`/auth/register`|Регистрация нового пользователя|204 No Content / 400 Bad Request|
|`POST`|`/auth/login`|Вход и получение JWT-токена|200 OK / 404 Not Found|
#### События (Events)

|Метод|Эндпоинт|Описание|Авторизация|
|---|---|---|---|
|`GET`|`/events`|Список событий (фильтрация, пагинация)|Без токена|
|`GET`|`/events/{id}`|Получить событие по ID|Без токена|
|`POST`|`/events`|Создать событие|Admin|
|`PUT`|`/events/{id}`|Обновить событие|Admin|
|`DELETE`|`/events/{id}`|Удалить событие|Admin|
|`POST`|`/events/{id}/book`|Забронировать место на событии|User / Admin|
#### Бронирования (Bookings)

|Метод|Эндпоинт|Описание|Авторизация|
|---|---|---|---|
|`GET`|`/bookings/{id}`|Получить статус своей брони|User / Admin|
|`DELETE`|`/bookings/{id}`|Отменить бронь (с возвратом места)|User (свою) / Admin (любую)|

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

## Запуск тестов

Для запуска всех тестов выполните:

```bash
dotnet test
```

- **Юнит-тесты (`EventManagerAPI.Tests`)**: Используют InMemory провайдер EF Core. Покрывают все бизнес-правила (лимит броней, прошлая дата, права отмены, овербукинг, возврат мест при отмене).
- **Интеграционные тесты (`EventManagerAPI.IntegrationTests`)**: Используют **Testcontainers** для поднятия реального Docker-контейнера PostgreSQL. Проверяют корректность маппинга EF Core, уникальные индексы (логин пользователя), внешние ключи и репозитории (включая `UserRepositoryTests`).

---

## Обработка ошибок

Все ошибки возвращаются в стандарте **RFC 7807 (Problem Details)**. `GlobalExceptionHandler` перехватывает доменные исключения и маппит их в правильные HTTP-статусы:

- `NotFoundException` → 404
- `DomainValidationException` → 400
- `PastEventBookingException` → 400
- `NoAvailableSeatsException` → 409
- `ActiveBookingLimitExceededException` → 409 (в сообщение включено значение лимита)
- `ForbiddenException` → 403

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

- **`SemaphoreSlim` в `BookingService`:** Защищает критическую секцию "получение события -> проверка лимита броней -> проверка мест -> резервирование -> сохранение", гарантируя, что параллельные запросы не забронируют одно и то же место.
- **`IServiceScopeFactory` в `BookingBackgroundService`:** Позволяет фоновым задачам получать правильные `Scoped` репозитории для каждой обрабатываемой брони.
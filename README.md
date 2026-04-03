# Event Manager API

Сервис для управления мероприятиями. Базовый каркас, реализованный на ASP.NET Core Web API.

## Технологический стек

- C# / .NET 10
- ASP.NET Core Web API
- In-memory хранилище
- Swagger (OpenAPI)

## Запуск проекта

1. Клонируйте репозиторий:
    ```bash
    git clone https://github.com/dennisrotar/EventManagerAPI.git EventManagerAPI
    ```

2. Перейдите в репозиторий
    ```bash
    cd EventManagerAPI
    ```

3. Соберите проект:
    ```bash
    dotnet build
    ```

4. Запустите проект:
    ```bash
    dotnet run
    ```

5. Откройте браузер и перейдите по адресу: `http://localhost:<порт>/swagger` (порт будет указан в консоли после запуска).

## Документация API

Базовый путь: `/events`

|Метод|Эндпоинт|Описание|Успешный статус|
|---|---|---|---|
|`GET`|`/events`|Получить список всех мероприятий|200 OK|
|`GET`|`/events/{id}`|Получить мероприятие по ID|200 OK|
|`POST`|`/events`|Создать новое мероприятие|201 Created|
|`PUT`|`/events/{id}`|Обновить мероприятие целиком|204 No Content|
|`DELETE`|`/events/{id}`|Удалить мероприятие|204 No Content|

### Правила валидации

- Поля `Title`, `StartAt`, `EndAt` обязательны.
- Дата окончания (`EndAt`) должна быть строго позже даты начала (`StartAt`).
- При ошибке валидации возвращается статус 400 Bad Request.
- При обращении к несуществующему мероприятию возвращается статус 404 Not Found.
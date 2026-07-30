# Этап 1: Сборка
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Копируем проекты
COPY ["EventManager.Shared/EventManager.Shared.csproj", "EventManager.Shared/"]
COPY ["Services/Events/Events.Domain/Events.Domain.csproj", "Services/Events/Events.Domain/"]
COPY ["Services/Events/Events.Application/Events.Application.csproj", "Services/Events/Events.Application/"]
COPY ["Services/Events/Events.Infrastructure/Events.Infrastructure.csproj", "Services/Events/Events.Infrastructure/"]
COPY ["Services/Events/Events.Api/Events.Api.csproj", "Services/Events/Events.Api/"]

# Восстанавливаем зависимости
RUN dotnet restore "Services/Events/Events.Api/Events.Api.csproj"

# Копируем остальной исходный код
COPY . .

# Собираем и публикуем
WORKDIR /src/Services/Events/Events.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Этап 2: Выполнение (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Открываем порт и запускаем
EXPOSE 8080
ENTRYPOINT ["dotnet", "Events.Api.dll"]
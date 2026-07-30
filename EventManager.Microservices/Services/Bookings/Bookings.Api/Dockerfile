# Этап 1: Сборка
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Копируем проекты
COPY ["EventManager.Shared/EventManager.Shared.csproj", "EventManager.Shared/"]
COPY ["Services/Bookings/Bookings.Domain/Bookings.Domain.csproj", "Services/Bookings/Bookings.Domain/"]
COPY ["Services/Bookings/Bookings.Application/Bookings.Application.csproj", "Services/Bookings/Bookings.Application/"]
COPY ["Services/Bookings/Bookings.Infrastructure/Bookings.Infrastructure.csproj", "Services/Bookings/Bookings.Infrastructure/"]
COPY ["Services/Bookings/Bookings.Api/Bookings.Api.csproj", "Services/Bookings/Bookings.Api/"]

# Восстанавливаем зависимости
RUN dotnet restore "Services/Bookings/Bookings.Api/Bookings.Api.csproj"

# Копируем остальной исходный код
COPY . .

# Собираем и публикуем
WORKDIR /src/Services/Bookings/Bookings.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Этап 2: Выполнение (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Открываем порт и запускаем
EXPOSE 8080
ENTRYPOINT ["dotnet", "Bookings.Api.dll"]
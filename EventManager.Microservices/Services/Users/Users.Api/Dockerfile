# Этап 1: Сборка
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Копируем проекты (сначала csproj для кэширования restore)
COPY ["EventManager.Shared/EventManager.Shared.csproj", "EventManager.Shared/"]
COPY ["Services/Users/Users.Domain/Users.Domain.csproj", "Services/Users/Users.Domain/"]
COPY ["Services/Users/Users.Application/Users.Application.csproj", "Services/Users/Users.Application/"]
COPY ["Services/Users/Users.Infrastructure/Users.Infrastructure.csproj", "Services/Users/Users.Infrastructure/"]
COPY ["Services/Users/Users.Api/Users.Api.csproj", "Services/Users/Users.Api/"]

# Восстанавливаем зависимости
RUN dotnet restore "Services/Users/Users.Api/Users.Api.csproj"

# Копируем весь остальной исходный код
COPY . .

# Собираем и публикуем
WORKDIR /src/Services/Users/Users.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Этап 2: Выполнение (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Открываем порт и запускаем
EXPOSE 8080
ENTRYPOINT ["dotnet", "Users.Api.dll"]
# ── Build stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files and restore (separate layer for better caching)
COPY ["src/CustomerApi.Domain/CustomerApi.Domain.csproj",         "src/CustomerApi.Domain/"]
COPY ["src/CustomerApi.Application/CustomerApi.Application.csproj", "src/CustomerApi.Application/"]
COPY ["src/CustomerApi.Infrastructure/CustomerApi.Infrastructure.csproj", "src/CustomerApi.Infrastructure/"]
COPY ["src/CustomerApi.API/CustomerApi.API.csproj",               "src/CustomerApi.API/"]
RUN dotnet restore "src/CustomerApi.API/CustomerApi.API.csproj"

# Copy all source and publish
COPY . .
WORKDIR /src/src/CustomerApi.API
RUN dotnet publish -c Release -o /app/publish --no-restore

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create a non-root user for security
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "CustomerApi.API.dll"]

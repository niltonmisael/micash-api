# =========================
# 🔧 BUILD STAGE
# =========================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# 🔹 Copia solution
COPY MiCash.sln .

# 🔹 Copia csproj (melhor cache)
COPY src/MiCash.API/*.csproj src/MiCash.API/
COPY src/MiCash.Application/*.csproj src/MiCash.Application/
COPY src/MiCash.Communication/*.csproj src/MiCash.Communication/
COPY src/MiCash.Domain/*.csproj src/MiCash.Domain/
COPY src/MiCash.Exception/*.csproj src/MiCash.Exception/
COPY src/MiCash.Infrastructure/*.csproj src/MiCash.Infrastructure/

# 🔹 Restore
RUN dotnet restore

# 🔹 Copia resto do código
COPY . .

# 🔹 Publica só a API
RUN dotnet publish src/MiCash.API/MiCash.API.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# =========================
# 🚀 RUNTIME STAGE
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

# Copia build final
COPY --from=build /app/publish .

# 🔐 Segurança
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

# 🌐 Porta
EXPOSE 80

ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

# 🚀 Start
ENTRYPOINT ["dotnet", "MiCash.API.dll"]
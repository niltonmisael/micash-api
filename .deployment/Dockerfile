# =========================
# 🔧 BUILD STAGE
# =========================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# 🔹 Copia só os csproj (restore fica em cache enquanto as dependências não mudam)
COPY src/MiCash.API/*.csproj src/MiCash.API/
COPY src/MiCash.Application/*.csproj src/MiCash.Application/
COPY src/MiCash.Communication/*.csproj src/MiCash.Communication/
COPY src/MiCash.Domain/*.csproj src/MiCash.Domain/
COPY src/MiCash.Exception/*.csproj src/MiCash.Exception/
COPY src/MiCash.Infrastructure/*.csproj src/MiCash.Infrastructure/

# 🔹 Restore só da API (a solution também lista os projetos de teste, que não entram na imagem)
RUN dotnet restore src/MiCash.API/MiCash.API.csproj

# 🔹 Copia o código-fonte (só src/: testes, .deployment e .env ficam de fora)
COPY src/ src/

# 🔹 Publica só a API
RUN dotnet publish src/MiCash.API/MiCash.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# =========================
# 🚀 RUNTIME STAGE
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# 🩺 curl para o HEALTHCHECK (a imagem aspnet não traz). Antes da cópia da app para ficar em cache.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copia build final
COPY --from=build /app/publish .

# 🔐 Segurança: usuário sem privilégios que já vem na imagem do .NET
USER $APP_UID

# 🌐 Porta: 8080 é o padrão do .NET para usuário sem privilégios (portas < 1024 exigem root)
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENV ASPNETCORE_ENVIRONMENT=Production

# 🩺 Saúde: /health só responde de dentro do container (loopback); start-period cobre as migrations no start
HEALTHCHECK --interval=30s --timeout=5s --start-period=60s --retries=3 \
    CMD curl -fsS http://localhost:8080/health || exit 1

# 🚀 Start
ENTRYPOINT ["dotnet", "MiCash.API.dll"]

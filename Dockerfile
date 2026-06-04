# ==========================================
# 1. AŞAMA: .NET SDK ile Uygulamayı Derleme (Build)
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /src

COPY backend/TicketSystem.Api.csproj ./backend/
RUN dotnet restore ./backend/TicketSystem.Api.csproj

COPY . .

RUN dotnet publish ./backend/TicketSystem.Api.csproj -c Release -o /app

# ==========================================
# 2. AŞAMA: Çalışma Zamanı (Runtime) İmajı
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build-env /app .

ENV ASPNETCORE_URLS=http://+:3000# ==========================================
# 1. AŞAMA: .NET SDK ile Uygulamayı Derleme (Build)
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /src

COPY backend/TicketSystem.Api.csproj ./backend/
RUN dotnet restore ./backend/TicketSystem.Api.csproj

COPY . .

RUN dotnet publish ./backend/TicketSystem.Api.csproj -c Release -o /app

# ==========================================
# 2. AŞAMA: Çalışma Zamanı (Runtime) İmajı
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build-env /app .

WORKDIR /
RUN mkdir -p data

COPY ticket-system/data/db.json /data/db.json


WORKDIR /app
# ----------------------------------------------------------------------------

ENV ASPNETCORE_URLS=http://+:3000
EXPOSE 3000


ENTRYPOINT ["dotnet", "TicketSystem.Api.dll"]
EXPOSE 3000

ENTRYPOINT ["dotnet", "TicketSystem.Api.dll"]

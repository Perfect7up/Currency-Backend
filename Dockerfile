# =========================
# Build stage
# =========================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY Backend.csproj ./
RUN dotnet restore

# Copy everything else
COPY . .

# Publish app
RUN dotnet publish -c Release -o /app/publish

# =========================
# Runtime stage
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# Copy published output
COPY --from=build /app/publish .

# ASP.NET Core config
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port
EXPOSE 8080

# Start the app
ENTRYPOINT ["dotnet", "Backend.dll"]

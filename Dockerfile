# Dockerfile
# Multi-stage build for .NET 8

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# If your .csproj is not at repo root, update the path below accordingly.
COPY ["SupportTicketAPI.csproj", "./"]
RUN dotnet restore "SupportTicketAPI.csproj"

COPY . .
RUN dotnet publish "SupportTicketAPI.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Bind to port 8080 (matching appsettings.json Urls)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SupportTicketAPI.dll"]
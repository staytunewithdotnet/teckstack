# ==============================================================================
# Build Stage
# ==============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies (leverages Docker layer caching)
COPY ["SmartTicket.Api/SmartTicket.Api.csproj", "SmartTicket.Api/"]
RUN dotnet restore "SmartTicket.Api/SmartTicket.Api.csproj"

# Copy the rest of the code and build
COPY ["SmartTicket.Api/", "SmartTicket.Api/"]
WORKDIR "/src/SmartTicket.Api"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# ==============================================================================
# Runtime Stage
# ==============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Security Best Practice: Create a non-root user and switch to it
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser

# Copy the published output from the build stage
COPY --from=build /app/publish .

# Expose the default ASP.NET port
EXPOSE 8080

# Set environment variables for ASP.NET Core
ENV ASPNETCORE_HTTP_PORTS=8080
ENV DOTNET_RUNNING_IN_CONTAINER=true

ENTRYPOINT ["dotnet", "SmartTicket.Api.dll"]

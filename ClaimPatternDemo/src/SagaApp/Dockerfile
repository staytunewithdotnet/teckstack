# ============================================================================
# DOCKERFILE: Build Your .NET App into a Docker Image
# ============================================================================
# This file tells Docker how to package your .NET app into a container.
# We use MULTI-STAGE BUILD to keep the final image small.
# ============================================================================

# STAGE 1: Base runtime (small image for running the app)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

# STAGE 2: Build the app (large image with SDK)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["SagaApp.csproj", "./"]
RUN dotnet restore "./SagaApp.csproj"

# Copy all source code
COPY . .

# Build the app
RUN dotnet build "SagaApp.csproj" -c Release -o /app/build

# STAGE 3: Publish the app (optimized for production)
FROM build AS publish
RUN dotnet publish "SagaApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

# STAGE 4: Final image (copy published app to small runtime)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set the entry point (what runs when the container starts)
ENTRYPOINT ["dotnet", "SagaApp.dll"]

# Use the official .NET SDK image for the build environment
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /api

# Copy the .csproj file and restore dependencies
COPY ./api/*.csproj ./
RUN dotnet restore

# Copy the entire project and build it
COPY ./api ./
RUN dotnet publish -c Release -o out

# Use the official ASP.NET Core runtime image for the final image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Copy the output from the build stage to the runtime stage
COPY --from=build-env /api/out ./

# Expose port 80 for HTTP traffic
EXPOSE 80

# Set the environment to production
ENV ASPNETCORE_ENVIRONMENT=Production

# Start the application
ENTRYPOINT ["dotnet", "Snake-1v1-api.dll"]

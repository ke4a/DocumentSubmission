# Build context is the repository root (see docker-compose.yml).

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, so the package layer is cached until a project file changes.
COPY DocumentSubmission.Domain/DocumentSubmission.Domain.csproj DocumentSubmission.Domain/
COPY DocumentSubmission.Infrastructure/DocumentSubmission.Infrastructure.csproj DocumentSubmission.Infrastructure/
COPY DocumentSubmission.Api/DocumentSubmission.Api.csproj DocumentSubmission.Api/
RUN dotnet restore DocumentSubmission.Api/DocumentSubmission.Api.csproj

COPY DocumentSubmission.Domain/ DocumentSubmission.Domain/
COPY DocumentSubmission.Infrastructure/ DocumentSubmission.Infrastructure/
COPY DocumentSubmission.Api/ DocumentSubmission.Api/
RUN dotnet publish DocumentSubmission.Api/DocumentSubmission.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "DocumentSubmission.Api.dll"]

# Build context is the repository root (see docker-compose.yml).

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# No separate restore layer: restore must see the .razor files, otherwise the SDK does not add
# the package that contains Blazor's framework script (_framework/blazor.web.js).
COPY DocumentSubmission.Web/ DocumentSubmission.Web/
RUN dotnet publish DocumentSubmission.Web/DocumentSubmission.Web.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "DocumentSubmission.Web.dll"]

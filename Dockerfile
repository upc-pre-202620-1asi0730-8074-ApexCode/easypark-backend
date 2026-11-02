# EasyPark Platform API - multi-stage image (build with the .NET SDK, run on the ASP.NET runtime).
# Build context: the "backend" folder.  Example (from backend/):  docker build -t easypark-api .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY EasyPark.Platform/EasyPark.Platform.csproj EasyPark.Platform/
RUN dotnet restore EasyPark.Platform/EasyPark.Platform.csproj
COPY EasyPark.Platform/ EasyPark.Platform/
RUN dotnet publish EasyPark.Platform/EasyPark.Platform.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish ./
# Non-root user provided by the official .NET images.
USER $APP_UID
ENTRYPOINT ["dotnet", "EasyPark.Platform.dll"]

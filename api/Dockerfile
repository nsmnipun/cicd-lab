FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Notes.Api/Notes.Api.csproj src/Notes.Api/
RUN dotnet restore src/Notes.Api/Notes.Api.csproj
COPY src/ src/
RUN dotnet publish src/Notes.Api/Notes.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Notes.Api.dll"]
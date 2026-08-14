# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/AsmsApi/AsmsApi.csproj src/AsmsApi/
RUN dotnet restore src/AsmsApi/AsmsApi.csproj

COPY src/AsmsApi/ src/AsmsApi/
RUN dotnet publish src/AsmsApi/AsmsApi.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "AsmsApi.dll"]

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src


COPY src/Domain/Domain.csproj src/Domain/
COPY src/Infrastructure/Infrastructure.csproj src/Infrastructure/
COPY src/Ingest/Ingest.csproj src/Ingest/
COPY src/Api/Api.csproj src/Api/
RUN dotnet restore src/Api/Api.csproj



COPY src/ src/
RUN dotnet publish src/Api/Api.csproj -c Release -o /app --no-restore


FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet","Api.dll" ]

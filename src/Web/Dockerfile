FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src


COPY src/Domain/Domain.csproj src/Domain/
COPY src/Web/Web.csproj src/Web/
RUN dotnet restore src/Web/Web.csproj



COPY src/ src/
RUN dotnet publish src/Web/Web.csproj -c Release -o /app --no-restore




FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet","Web.dll" ]

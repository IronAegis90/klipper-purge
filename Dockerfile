FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY ./src/Klipper.Purge.Console/Klipper.Purge.Console.csproj ./
RUN dotnet restore

COPY ./src/Klipper.Purge.Console ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/out .

ENTRYPOINT ["dotnet", "Klipper.Purge.Console.dll"]
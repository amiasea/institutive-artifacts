FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY . .

RUN dotnet tool install --global dotnet-ef --version 10.0.11

ENV PATH="$PATH:/root/.dotnet/tools"

RUN dotnet restore ./Amiasea.Data.Migrations/Amiasea.Data.Migrations.csproj

RUN dotnet ef migrations bundle \
    --project ./Amiasea.Data.Migrations/Amiasea.Data.Migrations.csproj \
    --startup-project ./Amiasea.Data.Migrations/Amiasea.Data.Migrations.csproj \
    --configuration Release \
    --self-contained \
    --target-runtime linux-x64 \
    --output /app/efbundle

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0

WORKDIR /app

COPY --from=build /app/efbundle .

ENTRYPOINT ["./efbundle"]
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore ./Amiasea.Data.Migration/Amiasea.Data.Migration.csproj
RUN dotnet ef migrations bundle \
    --project ./Amiasea.Data.Migration/Amiasea.Data.Migration.csproj \
    --startup-project ./Amiasea.Data.Migration/Amiasea.Data.Migration.csproj \
    --configuration Release \
    --self-contained \
    --target-runtime linux-x64 \
    --output /app/efbundle

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0
WORKDIR /app
COPY --from=build /app/efbundle .
ENTRYPOINT ["./efbundle"]

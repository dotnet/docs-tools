FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app
COPY . ./
RUN dotnet publish ./src/ActionRunner/ActionRunner.csproj -c Release -o out --no-self-contained

# Build the runtime image
FROM mcr.microsoft.com/dotnet/runtime:9.0
COPY --from=build /app/out .

ENTRYPOINT [ "dotnet", "/ActionRunner.dll" ]

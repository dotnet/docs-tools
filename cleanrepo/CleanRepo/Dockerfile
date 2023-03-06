FROM mcr.microsoft.com/dotnet/sdk:7.0 as build-env
# Copy everything and publish the release (publish implicitly restores and builds)
WORKDIR /app
COPY . ./
RUN dotnet publish "CleanRepo.csproj" -c Release -o out --no-self-contained

# Relayer the .NET SDK, anew with the build output
FROM mcr.microsoft.com/dotnet/sdk:7.0
COPY --from=build-env /app/out .
ENTRYPOINT [ "dotnet", "/CleanRepo.dll" ]
# RUN chmod +x /multiplexer.sh
# ENTRYPOINT [ "/bin/bash", "/multiplexer.sh" ]

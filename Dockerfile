# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /source

# copy and publish app and libraries
COPY . .
RUN dotnet restore DvrMqtt.sln -r linux-x64
RUN dotnet publish DvrMqtt/DvrMqtt.csproj -c Release -o /app -r linux-x64 --self-contained false --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/runtime:7.0-bullseye-slim-amd64
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["./DvrMqtt"]
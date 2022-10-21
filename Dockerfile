FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-dotnet
    WORKDIR /build

    COPY sources/ .
    RUN dotnet restore

    RUN dotnet publish . -c Release -o ./.publish

# Create the runtime image.
FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine AS runtime
    WORKDIR /app
    COPY --from=build-dotnet /build/.publish ./

    ENTRYPOINT ["dotnet", "Mentalist.Monitoring.Pods.dll"]
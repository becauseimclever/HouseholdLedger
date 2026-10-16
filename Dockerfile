# syntax=docker/dockerfile:1
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0.402 AS build
WORKDIR /source
COPY . .
RUN dotnet restore src/HouseholdLedger.Api/HouseholdLedger.Api.csproj --locked-mode
RUN dotnet publish src/HouseholdLedger.Api/HouseholdLedger.Api.csproj \
    --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0.13 AS runtime
WORKDIR /app
RUN apt-get update && apt-get install --no-install-recommends -y libgssapi-krb5-2 \
    && apt-get clean \
    && mkdir -p /var/lib/householdledger/keys \
    && chown -R app:app /var/lib/householdledger
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DataProtection__KeyPath=/var/lib/householdledger/keys
USER $APP_UID
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD ["dotnet", "HouseholdLedger.Api.dll", "--check-readiness"]
ENTRYPOINT ["dotnet", "HouseholdLedger.Api.dll"]

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for layer caching
COPY ContentLocalizationSaaS.slnx ./
COPY ContentLocalizationSaaS.Api/ContentLocalizationSaaS.Api.csproj ContentLocalizationSaaS.Api/
COPY ContentLocalizationSaaS.Application/ContentLocalizationSaaS.Application.csproj ContentLocalizationSaaS.Application/
COPY ContentLocalizationSaaS.Domain/ContentLocalizationSaaS.Domain.csproj ContentLocalizationSaaS.Domain/
COPY ContentLocalizationSaaS.Infrastructure/ContentLocalizationSaaS.Infrastructure.csproj ContentLocalizationSaaS.Infrastructure/
COPY ContentLocalizationSaaS.ServiceDefaults/ContentLocalizationSaaS.ServiceDefaults.csproj ContentLocalizationSaaS.ServiceDefaults/

RUN dotnet restore ContentLocalizationSaaS.Api/ContentLocalizationSaaS.Api.csproj

# Copy everything and publish
COPY . .
RUN dotnet publish ContentLocalizationSaaS.Api/ContentLocalizationSaaS.Api.csproj \
    -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:5135
EXPOSE 5135

ENTRYPOINT ["dotnet", "ContentLocalizationSaaS.Api.dll"]

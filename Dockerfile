FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# Npgsql opportunistically probes for GSSAPI (Kerberos) support at startup even though this project
# only uses password authentication; without this library it prints a harmless-but-alarming-looking
# "Cannot load library libgssapi_krb5.so.2" error to stdout on every start.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/RentalPipeline.Api/RentalPipeline.Api.csproj", "src/RentalPipeline.Api/"]
COPY ["src/RentalPipeline.Application/RentalPipeline.Application.csproj", "src/RentalPipeline.Application/"]
COPY ["src/RentalPipeline.Domain/RentalPipeline.Domain.csproj", "src/RentalPipeline.Domain/"]
COPY ["src/RentalPipeline.Infrastructure/RentalPipeline.Infrastructure.csproj", "src/RentalPipeline.Infrastructure/"]
RUN dotnet restore "src/RentalPipeline.Api/RentalPipeline.Api.csproj"

COPY src/ src/
WORKDIR "/src/src/RentalPipeline.Api"
RUN dotnet build "RentalPipeline.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "RentalPipeline.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RentalPipeline.Api.dll"]

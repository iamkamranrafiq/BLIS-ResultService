#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER root
RUN apt-get update && apt-get install -y --no-install-recommends gss-ntlmssp libfontconfig1 libfreetype6 fonts-dejavu-core
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG NUGET_USER
ARG NUGET_PASSWORD
ARG PACKAGE_REPO
WORKDIR /src
COPY ["src/Presentation/Bioreference.ScanningService.WebAPI/Bioreference.ScanningService.WebAPI.csproj", "Presentation/Bioreference.ScanningService.WebAPI/"]
COPY ["src/Application/Bioreference.ScanningService.Application/Bioreference.ScanningService.Application.csproj", "Application/Bioreference.ScanningService.Application/"]
COPY ["src/Infrastructure/Bioreference.ScanningService.Infrastructure/Bioreference.ScanningService.Infrastructure.csproj", "Infrastructure/Bioreference.ScanningService.Infrastructure/"]
COPY ["src/Domain/Bioreference.ScanningService.Domain/Bioreference.ScanningService.Domain.csproj", "Domain/Bioreference.ScanningService.Domain/"]
RUN dotnet nuget add source --username $NUGET_USER --password $NUGET_PASSWORD --store-password-in-clear-text --name github $PACKAGE_REPO
RUN dotnet restore "./Presentation/Bioreference.ScanningService.WebAPI/Bioreference.ScanningService.WebAPI.csproj"
COPY src/Presentation/ Presentation/
COPY src/Application/ Application/
COPY src/Infrastructure/ Infrastructure/
COPY src/Domain/ Domain/
WORKDIR "/src/Presentation/Bioreference.ScanningService.WebAPI"
RUN dotnet build "./Bioreference.ScanningService.WebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Bioreference.ScanningService.WebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
USER app
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Bioreference.ScanningService.WebAPI.dll"]

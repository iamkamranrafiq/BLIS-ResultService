#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER root
RUN apt-get update && apt-get install -y --no-install-recommends gss-ntlmssp
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG NUGET_USER
ARG NUGET_PASSWORD
ARG PACKAGE_REPO
WORKDIR /src
COPY ["ResultService/Presentation/Bioreference.Result.WebAPI/Bioreference.ResultService.WebAPI.csproj", "ResultService/Presentation/Bioreference.Result.WebAPI/"]
COPY ["ResultService/Application/Bioreference.ResultService.Application/Bioreference.ResultService.Application.csproj", "ResultService/Application/Bioreference.ResultService.Application/"]
COPY ["ResultService/Domain/Bioreference.LIS/Bioreference.LIS.csproj", "ResultService/Domain/Bioreference.LIS/"]
COPY ["ResultService/Application/Bioreference.ResultService.Abstractions/Bioreference.ResultService.Abstractions.csproj", "ResultService/Application/Bioreference.ResultService.Abstractions/"]
COPY ["ResultService/Application/Bioreference.ResultService.Application.Model/Bioreference.ResultService.Application.Model.csproj", "ResultService/Application/Bioreference.ResultService.Application.Model/"]
COPY ["ResultService/Application/Bioreference.ResultService.DI.Interface/Bioreference.ResultService.DI.Interface.csproj", "ResultService/Application/Bioreference.ResultService.DI.Interface/"]
RUN dotnet nuget add source --username $NUGET_USER --password $NUGET_PASSWORD --store-password-in-clear-text --name github $PACKAGE_REPO 
RUN dotnet restore "./ResultService/Presentation/Bioreference.Result.WebAPI/Bioreference.ResultService.WebAPI.csproj"
COPY ResultService/ ResultService/
WORKDIR "/src/ResultService/Presentation/Bioreference.Result.WebAPI"
RUN dotnet build "./Bioreference.ResultService.WebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Bioreference.ResultService.WebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
USER app
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Bioreference.ResultService.WebAPI.dll"]

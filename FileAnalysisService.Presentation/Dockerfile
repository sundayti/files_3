FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 5001
EXPOSE 5002

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["FileAnalysisService.Presentation/FileAnalysisService.Presentation.csproj", "FileAnalysisService.Presentation/"]
COPY ["FileAnalysisService.Infrastructure/FileAnalysisService.Infrastructure.csproj", "FileAnalysisService.Infrastructure/"]
COPY ["FileAnalysisService.Domain/FileAnalysisService.Domain.csproj", "FileAnalysisService.Domain/"]
COPY ["FileAnalysisService.Application/FileAnalysisService.Application.csproj", "FileAnalysisService.Application/"]
COPY ["FileAnalysisService.Grpc/FileAnalysisService.Grpc.csproj", "FileAnalysisService.Grpc/"]
RUN dotnet restore "FileAnalysisService.Presentation/FileAnalysisService.Presentation.csproj"
COPY . .
WORKDIR "/src/FileAnalysisService.Presentation"
RUN dotnet build "./FileAnalysisService.Presentation.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./FileAnalysisService.Presentation.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FileAnalysisService.Presentation.dll"]

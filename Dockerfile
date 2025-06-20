FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Test_MindeeApi/Test_MindeeApi.csproj", "Test_MindeeApi/"]
RUN dotnet restore "Test_MindeeApi/Test_MindeeApi.csproj"
COPY . .
WORKDIR "/src/Test_MindeeApi"
RUN dotnet build "Test_MindeeApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Test_MindeeApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Test_MindeeApi.dll"]

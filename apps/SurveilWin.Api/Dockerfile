FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["apps/SurveilWin.Api/SurveilWin.Api.csproj", "apps/SurveilWin.Api/"]
COPY ["libs/Contracts/Contracts.csproj", "libs/Contracts/"]
COPY ["libs/Processing/Processing.csproj", "libs/Processing/"]
COPY ["libs/Utils/Utils.csproj", "libs/Utils/"]
RUN dotnet restore "apps/SurveilWin.Api/SurveilWin.Api.csproj"
COPY . .
WORKDIR "/src/apps/SurveilWin.Api"
RUN dotnet build "SurveilWin.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SurveilWin.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SurveilWin.Api.dll"]

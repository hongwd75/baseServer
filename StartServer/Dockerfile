FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["StartUp/StartUp.csproj", "StartUp/"]
RUN dotnet restore "StartUp/StartUp.csproj"
COPY . .
WORKDIR "/src/StartUp"
RUN dotnet build "StartUp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "StartUp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "StartUp.dll"]

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Backend/StoreExplorer.Backend.csproj", "Backend/"]
RUN dotnet restore "Backend/StoreExplorer.Backend.csproj"

COPY . .
WORKDIR "/src/Backend"
RUN dotnet publish "StoreExplorer.Backend.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "StoreExplorer.Backend.dll"]

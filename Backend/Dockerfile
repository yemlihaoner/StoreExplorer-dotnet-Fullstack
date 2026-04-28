FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Backend/MyMAUIApp1.Backend.csproj", "Backend/"]
RUN dotnet restore "Backend/MyMAUIApp1.Backend.csproj"

COPY . .
WORKDIR "/src/Backend"
RUN dotnet publish "MyMAUIApp1.Backend.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MyMAUIApp1.Backend.dll"]

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

COPY ["PatientApi/PatientApi.csproj", "PatientApi/"]
RUN dotnet restore "PatientApi/PatientApi.csproj"

COPY PatientApi/ PatientApi/
WORKDIR /src/PatientApi
RUN dotnet build "PatientApi.csproj" -c Release -o /app/build
RUN dotnet publish "PatientApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "PatientApi.dll"]

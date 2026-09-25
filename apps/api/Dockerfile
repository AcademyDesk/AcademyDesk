FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY apps/api/AcademyDesk.Api.csproj apps/api/
RUN dotnet restore apps/api/AcademyDesk.Api.csproj
COPY apps/api/ apps/api/
RUN dotnet publish apps/api/AcademyDesk.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AcademyDesk.Api.dll"]

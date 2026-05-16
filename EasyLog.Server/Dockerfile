FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY EasyLog/EasyLog.csproj EasyLog/
COPY EasyLog.Server/EasyLog.Server.csproj EasyLog.Server/
RUN dotnet restore EasyLog.Server/EasyLog.Server.csproj

COPY EasyLog/ EasyLog/
COPY EasyLog.Server/ EasyLog.Server/
RUN dotnet publish EasyLog.Server/EasyLog.Server.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV EASYLOG_LOGS_DIRECTORY=/logs
RUN mkdir -p /logs
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "EasyLog.Server.dll"]

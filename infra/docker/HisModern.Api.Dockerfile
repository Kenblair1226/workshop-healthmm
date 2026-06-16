# 重構後的單體 (HisModern.Api) — multi-stage build
# build context = src/solution
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . ./
RUN dotnet restore HisModern.Api/HisModern.Api.csproj
RUN dotnet publish HisModern.Api/HisModern.Api.csproj -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "HisModern.Api.dll"]

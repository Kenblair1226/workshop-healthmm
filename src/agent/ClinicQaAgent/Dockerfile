# Clinic Q&A Agent — multi-stage build
# build context = src/agent/ClinicQaAgent
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY *.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet publish -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_URLS=http://+:8080
# Azure OpenAI 設定請於部署時以環境變數 / Key Vault 注入,切勿寫進映像檔。
# 未設定時 Agent 會自動以 mock 模式運作。
EXPOSE 8080
ENTRYPOINT ["dotnet", "ClinicQaAgent.dll"]

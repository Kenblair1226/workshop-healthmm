# Lab 05 · 容器化 → Azure App Service

> ⏱ 時間：約 10 分鐘
> 🎤 **本段由講師示範**（客戶環境因素）：學員觀看流程即可，不需自行操作。
> 🎯 目標：用 Copilot 產生 multi-stage Dockerfile，把重構後的系統打包成容器並部署到 Azure App Service。

---

## 路徑選擇

App Service for Containers 是 **最快上雲** 的路徑：託管平台、免管 K8s、適合單體或內部系統。

參考檔案：
- `infra/docker/HisModern.Api.Dockerfile`
- `infra/appservice/deploy.sh`

---

## 步驟

### 1. 用 Copilot 產生 Dockerfile

在 `src/solution/HisModern.Api` 開 Copilot Chat：

```
為這個 ASP.NET Core 8 Web API 產生一個 multi-stage Dockerfile:
- build 階段用 sdk:8.0 還原並發佈
- runtime 階段用 aspnet:8.0
- 設定 ASPNETCORE_URLS=http://+:8080，EXPOSE 8080
```

對照 `infra/docker/HisModern.Api.Dockerfile`。

### 2. 本機建置與測試

```bash
cd src/solution
docker build -f ../../infra/docker/HisModern.Api.Dockerfile -t hismodern:v1 .
docker run -p 8080:8080 hismodern:v1
# 開 http://localhost:8080 驗證
```

### 3. 推送到 Azure Container Registry (ACR)

```bash
az acr build --registry <your-acr> --image hismodern:v1 \
  --file infra/docker/HisModern.Api.Dockerfile src/solution
```

### 4. 部署到 App Service

```bash
az webapp create \
  --resource-group <rg> \
  --plan <app-service-plan> \
  --name his-web-<unique> \
  --deployment-container-image-name <your-acr>.azurecr.io/hismodern:v1

az webapp config appsettings set --resource-group <rg> --name his-web-<unique> \
  --settings WEBSITES_PORT=8080
```

> 完整指令見 `infra/appservice/deploy.sh`（含參數說明）。

---

## ✅ 驗收

- [ ] 本機 `docker run` 能開啟系統
- [ ] 映像檔成功推送至 ACR
- [ ] App Service URL 可開啟門診掛號系統

## 講師筆記

- 強調 **multi-stage build** 讓最終映像檔小、無 SDK。
- `WEBSITES_PORT=8080` 是 App Service 容器常見坑，務必提醒。
- 沒有 Azure 訂用帳戶的學員，可只完成本機 `docker run` 段落。

➡️ 下一站（講師 demo）：[Lab 06 · 臨床問答 AI Agent](06-ai-agent.md)

# infra · 容器化與雲端部署

進階 Lab（05–06，講師示範）用到的容器與 Azure 部署設定。

```
infra/
├── docker/
│   └── HisModern.Api.Dockerfile   # Lab 05:重構後單體 (context = src/solution)
└── appservice/
    └── deploy.sh                  # Lab 05:ACR build + App Service 部署
```

> AI Agent 自帶 Dockerfile，見 `src/agent/ClinicQaAgent/Dockerfile`（Lab 06）。

## Lab 05 · App Service（單體最快上雲）

```bash
# 本機驗證映像檔
docker build -f infra/docker/HisModern.Api.Dockerfile -t hismodern:v1 src/solution
docker run -p 8080:8080 hismodern:v1     # → http://localhost:8080

# 一鍵部署到 Azure（先編輯 deploy.sh 內的變數）
bash infra/appservice/deploy.sh
```

> ⚠️ App Service 容器務必設定 `WEBSITES_PORT=8080`（deploy.sh 已包含）。

## 安全提醒

- Azure OpenAI / DB 等祕密一律放 **Key Vault** 或 App Settings，切勿寫進程式或映像檔。
- App Service 拉取 ACR 建議用 **Managed Identity / AcrPull** 而非密碼。
- 對外服務記得加上 WAF / 限流 / 稽核（醫療場景的法規遵循要求）。

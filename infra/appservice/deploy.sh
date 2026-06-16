#!/usr/bin/env bash
# Lab 06 · 部署重構後的單體到 Azure App Service for Containers
# 用法: 編輯下方變數後執行  bash infra/appservice/deploy.sh
set -euo pipefail

# ---- 請替換為你的環境 ----
RG="rg-his-workshop"            # Resource Group
LOCATION="eastasia"            # 區域
ACR="acrhisworkshop"          # Container Registry 名稱 (全域唯一)
PLAN="plan-his"               # App Service Plan
APP="his-web-$RANDOM"         # Web App 名稱 (全域唯一)
IMAGE="hismodern:v1"
# 建置 context = src/solution
CONTEXT="$(git rev-parse --show-toplevel)/src/solution"
DOCKERFILE="$(git rev-parse --show-toplevel)/infra/docker/HisModern.Api.Dockerfile"

echo ">> 1. 建立 Resource Group"
az group create -n "$RG" -l "$LOCATION" -o none

echo ">> 2. 建立 ACR 並雲端建置映像檔"
az acr create -g "$RG" -n "$ACR" --sku Basic -o none
az acr build -r "$ACR" -t "$IMAGE" -f "$DOCKERFILE" "$CONTEXT"

echo ">> 3. 建立 App Service Plan (Linux)"
az appservice plan create -g "$RG" -n "$PLAN" --is-linux --sku B1 -o none

echo ">> 4. 建立 Web App (容器)"
az webapp create -g "$RG" -p "$PLAN" -n "$APP" \
  --deployment-container-image-name "$ACR.azurecr.io/$IMAGE" -o none

echo ">> 5. 容器 port 設定 (App Service 容器常見坑)"
az webapp config appsettings set -g "$RG" -n "$APP" \
  --settings WEBSITES_PORT=8080 -o none

echo ">> 6. 讓 Web App 以 Managed Identity 拉取 ACR"
az webapp identity assign -g "$RG" -n "$APP" -o none
ACR_ID=$(az acr show -n "$ACR" --query id -o tsv)
PRINCIPAL=$(az webapp identity show -g "$RG" -n "$APP" --query principalId -o tsv)
az role assignment create --assignee "$PRINCIPAL" --scope "$ACR_ID" --role AcrPull -o none

echo ">> 完成! 開啟: https://$APP.azurewebsites.net"

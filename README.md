# PoeNinja Pricer Plugin

一個用於查詢 Path of Exile 通貨價格的 ExileCore 插件，資料來源為 poe.ninja API。

## 功能特性

- **即時價格查詢**: 從 poe.ninja 獲取當前聯盟的通貨和碎片價格
- **自動更新**: 可設定自動更新間隔（預設 5 分鐘）
- **智慧快取**: 本地快取系統，減少 API 請求並支援離線使用
- **搜尋過濾**: 快速搜尋特定通貨
- **動態 Divine 匯率**: 自動計算和更新 Divine Orb 匯率
- **可自訂 UI**: 可調整顯示欄位和視窗大小

## 使用方法

1. 按 **F8** 開啟/關閉價格視窗
2. 在搜尋框輸入通貨名稱進行過濾
3. 點擊「重新整理」手動更新價格
4. 在設定中調整更新間隔和顯示選項

## 設定選項

- **Toggle Price Window**: 開啟價格視窗的快捷鍵（預設 F8）
- **Update Interval**: 自動更新間隔（分鐘）
- **League Name**: 聯盟名稱（留空自動偵測，預設 Mercenaries）
- **Show Chaos/Divine Values**: 顯示 Chaos/Divine 價值
- **Show Price Changes**: 顯示 24 小時價格變化
- **Auto Update Prices**: 啟用自動更新

## 支援的資料類型

- **Currency**: 基本通貨（Chaos Orb, Exalted Orb, Divine Orb 等）
- **Fragment**: 地圖碎片和其他碎片類物品

## 建置指南

確保已設定環境變數：
```bash
setx exapiPackage "C:\Users\user\Downloads\ExileApi-Compiled-3.26.last"
```

在插件目錄執行：
```bash
dotnet build
```

## 故障排除

1. **無法獲取價格資料**: 檢查網路連線和防火牆設定
2. **聯盟名稱錯誤**: 在設定中手動指定正確的聯盟名稱
3. **插件載入失敗**: 檢查 ExileCore 日誌檔案中的錯誤訊息

## 技術資訊

- **框架**: ExileCore (ExileAPI)
- **UI**: ImGui.NET
- **HTTP**: .NET HttpClient
- **序列化**: Newtonsoft.Json
- **資料來源**: poe.ninja API

## 版本資訊

- **v1.0.0**: 基礎功能實作
  - 通貨和碎片價格查詢
  - 自動更新和快取系統
  - 基本 UI 和搜尋功能
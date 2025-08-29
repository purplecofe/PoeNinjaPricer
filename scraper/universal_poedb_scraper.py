#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
通用 PoeDB 爬蟲程式
支援多種物品類別的統一爬取，透過配置檔案控制不同類別的爬取參數
"""

import asyncio
import json
import logging
import argparse
from pathlib import Path
from typing import Dict, List, Optional, Any
from urllib.parse import urljoin
from playwright.async_api import async_playwright, Page, Browser, BrowserContext

# 設定日誌
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')
logger = logging.getLogger(__name__)

class UniversalPoeDBScraper:
    def __init__(self, config: Dict[str, Any]):
        self.base_url = config['base_url']
        self.container_selector = config['container_selector']
        self.link_selector = config['link_selector']
        self.output_file = config['output_file']
        self.category_name = config['category_name']
        self.scraped_data: List[Dict] = []
        self.progress_count = 0
        
    async def init_browser(self) -> tuple[Browser, BrowserContext, Page]:
        """初始化瀏覽器"""
        playwright = await async_playwright().start()
        browser = await playwright.chromium.launch(headless=False)
        context = await browser.new_context(
            user_agent="Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
        )
        page = await context.new_page()
        page.set_default_timeout(30000)  # 30秒
        return browser, context, page
    
    async def get_item_links(self, page: Page) -> List[str]:
        """從主頁面獲取所有物品連結"""
        logger.info(f"正在載入 {self.category_name} 頁面...")
        await page.goto(self.base_url)
        
        # 根據容器類型選擇不同的等待策略
        if 'table' in self.container_selector:
            await page.wait_for_selector(self.container_selector.split(' > ')[0])
        else:
            await page.wait_for_selector(self.container_selector.split(' > ')[0])
        
        logger.info(f"正在提取 {self.category_name} 物品連結...")
        
        # 動態生成 JavaScript 程式碼
        js_code = f"""
            () => {{
                const container = document.querySelector("{self.container_selector}");
                if (!container) {{
                    console.log("找不到容器: {self.container_selector}");
                    return [];
                }}
                
                const links = [];
                const seenUrls = new Set();
                
                // 根據容器類型使用不同的邏輯
                if (container.tagName === 'TBODY') {{
                    // 表格結構：處理每個 row
                    const rows = container.querySelectorAll("tr");
                    for (const row of rows) {{
                        const rowLinks = row.querySelectorAll("{self.link_selector}");
                        for (const link of rowLinks) {{
                            if (link && link.href && !seenUrls.has(link.href)) {{
                                links.push(link.href);
                                seenUrls.add(link.href);
                                console.log("找到連結:", link.href);
                            }}
                        }}
                    }}
                }} else {{
                    // DIV 結構：處理每個子項目
                    const items = container.children;
                    for (const item of items) {{
                        const itemLinks = item.querySelectorAll("{self.link_selector}");
                        if (itemLinks.length === 0) {{
                            // 嘗試直接在 item 中查找連結
                            const directLink = item.querySelector("a");
                            if (directLink && directLink.href && !seenUrls.has(directLink.href)) {{
                                links.push(directLink.href);
                                seenUrls.add(directLink.href);
                                console.log("找到連結:", directLink.href);
                            }}
                        }} else {{
                            for (const link of itemLinks) {{
                                if (link && link.href && !seenUrls.has(link.href)) {{
                                    links.push(link.href);
                                    seenUrls.add(link.href);
                                    console.log("找到連結:", link.href);
                                }}
                            }}
                        }}
                    }}
                }}
                
                return links;
            }}
        """
        
        links = await page.evaluate(js_code)
        logger.info(f"找到 {len(links)} 個 {self.category_name} 連結")
        return links
    
    async def scrape_item_data(self, page: Page, url: str) -> Optional[Dict]:
        """爬取單個物品的資料"""
        try:
            await page.goto(url, wait_until="networkidle")
            
            # 等待表格載入，增加容錯性
            try:
                await page.wait_for_selector("table", timeout=15000)
            except Exception as e:
                logger.warning(f"等待表格載入超時，嘗試直接解析: {str(e)}")
            
            # 統一的資料提取邏輯
            item_data = await page.evaluate("""
                () => {
                    // 尋找包含資料的表格
                    const tables = document.querySelectorAll("table");
                    let targetTable = null;
                    
                    // 尋找包含 BaseType 的表格
                    for (const table of tables) {
                        const rows = table.querySelectorAll("tr");
                        for (const row of rows) {
                            const firstCell = row.querySelector("td");
                            if (firstCell && firstCell.textContent.includes("BaseType")) {
                                targetTable = table;
                                break;
                            }
                        }
                        if (targetTable) break;
                    }
                    
                    if (!targetTable) {
                        console.log("找不到包含 BaseType 的表格");
                        return null;
                    }
                    
                    const rows = targetTable.querySelectorAll("tr");
                    let baseTypeEn = null;
                    let baseTypeZh = null;
                    let type = null;
                    let baseTypeCount = 0;
                    
                    for (const row of rows) {
                        const cells = row.querySelectorAll("td");
                        if (cells.length >= 2) {
                            const key = cells[0].textContent.trim();
                            const value = cells[1].textContent.trim();
                            
                            if (key.includes("BaseType")) {
                                baseTypeCount++;
                                if (baseTypeCount === 1) {
                                    baseTypeEn = value;
                                } else if (baseTypeCount === 2) {
                                    baseTypeZh = value;
                                }
                            } else if (key.includes("Type") && !key.includes("BaseType")) {
                                type = value;
                            }
                        }
                    }
                    
                    return {
                        base_type_en: baseTypeEn,
                        base_type_zh: baseTypeZh,
                        type: type,
                        url: window.location.href
                    };
                }
            """)
            
            if item_data and item_data['base_type_en']:
                return item_data
            else:
                logger.warning(f"無法從 {url} 提取完整資料")
                return None
                
        except Exception as e:
            logger.error(f"爬取 {url} 時發生錯誤: {str(e)}")
            return None
    
    async def save_progress(self, data: List[Dict], filename: Optional[str] = None):
        """儲存進度"""
        if filename is None:
            filename = f"{self.category_name.lower()}_progress.json"
        
        try:
            with open(filename, 'w', encoding='utf-8') as f:
                json.dump(data, f, ensure_ascii=False, indent=2)
            logger.info(f"進度已儲存到 {filename}")
        except Exception as e:
            logger.error(f"儲存進度失敗: {str(e)}")
    
    async def scrape_all_items(self) -> List[Dict]:
        """爬取所有物品資料"""
        browser, context, page = await self.init_browser()
        
        try:
            # 獲取所有物品連結
            item_links = await self.get_item_links(page)
            total_items = len(item_links)
            
            if total_items == 0:
                logger.error(f"沒有找到任何 {self.category_name} 連結")
                return []
            
            logger.info(f"開始爬取 {total_items} 個 {self.category_name} 物品...")
            
            # 爬取每個物品的資料
            for i, url in enumerate(item_links, 1):
                logger.info(f"正在處理第 {i}/{total_items} 個物品: {url}")
                
                item_data = await self.scrape_item_data(page, url)
                
                if item_data:
                    self.scraped_data.append(item_data)
                    logger.info(f"成功爬取: {item_data['base_type_en']} ({item_data['base_type_zh']})")
                
                self.progress_count = i
                
                # 每爬取 10 個物品就儲存一次進度
                if i % 10 == 0:
                    await self.save_progress(self.scraped_data)
                
                # 添加延遲避免過度請求
                await asyncio.sleep(2)
            
            logger.info(f"爬取完成! 總共成功獲取 {len(self.scraped_data)} 個 {self.category_name} 物品資料")
            return self.scraped_data
            
        finally:
            await browser.close()
    
    async def save_final_result(self, data: List[Dict], filename: Optional[str] = None):
        """儲存最終結果"""
        if filename is None:
            filename = self.output_file
        
        try:
            # 格式化資料結構
            formatted_data = []
            for item in data:
                formatted_item = {
                    "name_zh": item['base_type_zh'],
                    "name_en": item['base_type_en'],
                    "base_type_zh": item['base_type_zh'],
                    "base_type_en": item['base_type_en'],
                    "type": item['type'],
                    "url": item['url']
                }
                formatted_data.append(formatted_item)
            
            with open(filename, 'w', encoding='utf-8') as f:
                json.dump(formatted_data, f, ensure_ascii=False, indent=2)
            
            logger.info(f"最終結果已儲存到 {filename}")
            logger.info(f"共包含 {len(formatted_data)} 個 {self.category_name} 物品")
            
        except Exception as e:
            logger.error(f"儲存最終結果失敗: {str(e)}")


def load_scraper_config(config_file: str = "scraper_configs.json") -> Dict[str, Dict]:
    """載入爬蟲配置"""
    try:
        with open(config_file, 'r', encoding='utf-8') as f:
            return json.load(f)
    except FileNotFoundError:
        logger.error(f"配置檔案 {config_file} 不存在")
        return {}
    except json.JSONDecodeError as e:
        logger.error(f"配置檔案格式錯誤: {str(e)}")
        return {}


async def scrape_category(category: str, config: Dict[str, Any]) -> bool:
    """爬取指定類別的物品"""
    logger.info(f"開始爬取 {config['category_name']} 類別...")
    
    scraper = UniversalPoeDBScraper(config)
    
    try:
        scraped_data = await scraper.scrape_all_items()
        
        if scraped_data:
            await scraper.save_final_result(scraped_data)
            print(f"\n{config['category_name']} 爬取完成!")
            print(f"成功獲取 {len(scraped_data)} 個物品資料")
            print(f"結果已儲存到 {config['output_file']}")
            return True
        else:
            print(f"{config['category_name']} 爬取失敗，沒有獲取到任何資料")
            return False
            
    except Exception as e:
        logger.error(f"爬取 {config['category_name']} 時發生錯誤: {str(e)}")
        print(f"爬取 {config['category_name']} 過程中發生錯誤: {str(e)}")
        return False


async def main():
    """主函式"""
    parser = argparse.ArgumentParser(description='通用 PoeDB 爬蟲程式')
    parser.add_argument('--category', nargs='+', help='要爬取的類別名稱')
    parser.add_argument('--all', action='store_true', help='爬取所有已配置的類別')
    parser.add_argument('--config', default='scraper_configs.json', help='配置檔案路徑')
    parser.add_argument('--list', action='store_true', help='列出所有可用的類別')
    
    args = parser.parse_args()
    
    # 載入配置
    configs = load_scraper_config(args.config)
    
    if not configs:
        print("沒有找到有效的配置檔案")
        return
    
    # 列出可用類別
    if args.list:
        print("可用的類別:")
        for category, config in configs.items():
            print(f"  - {category}: {config['category_name']}")
        return
    
    # 確定要爬取的類別
    categories_to_scrape = []
    
    if args.all:
        categories_to_scrape = list(configs.keys())
    elif args.category:
        for category in args.category:
            if category in configs:
                categories_to_scrape.append(category)
            else:
                print(f"未知類別: {category}")
                print(f"可用類別: {', '.join(configs.keys())}")
    else:
        print("請指定要爬取的類別或使用 --all 參數")
        print("使用 --list 查看所有可用類別")
        return
    
    if not categories_to_scrape:
        print("沒有有效的類別可爬取")
        return
    
    print(f"準備爬取 {len(categories_to_scrape)} 個類別: {', '.join(categories_to_scrape)}")
    
    # 開始爬取
    successful = 0
    failed = 0
    
    for category in categories_to_scrape:
        config = configs[category]
        success = await scrape_category(category, config)
        
        if success:
            successful += 1
        else:
            failed += 1
        
        # 在類別之間添加延遲
        if len(categories_to_scrape) > 1:
            await asyncio.sleep(5)
    
    print(f"\n爬取總結:")
    print(f"成功: {successful} 個類別")
    print(f"失敗: {failed} 個類別")


if __name__ == "__main__":
    asyncio.run(main())
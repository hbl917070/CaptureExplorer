# 多螢幕獨立覆蓋調整計劃

## 目標

目前選取截圖模式會：

1. 先把所有螢幕視為一個大虛擬桌面截成一張大圖
2. 再開一個跨所有螢幕的單一 WPF 覆蓋視窗
3. 使用同一組縮放比例處理滑鼠、顯示與裁切

這在「不同螢幕使用不同縮放比例」時容易失真，因為：

- `System.Windows.Forms.Screen.Bounds` / `CopyFromScreen` 取得的是實體像素座標
- WPF 視窗大小、滑鼠定位、控制項位置使用的是 DIP
- 目前 `W_截圖` 只記錄一組 `d_解析度比例_x/y`
- 這組比例是從主視窗 `PresentationSource.FromVisual(M)` 取得，不一定等於目前滑鼠所在螢幕的比例

初步方向「每個螢幕建立自己的覆蓋視窗」是合理的，而且會比在單一超大視窗裡硬兜各螢幕 DPI 更穩定。

---

## 目前程式結構觀察

### 主要入口

- `CaptureExplorer/CaptureExplorer/MainWindow.xaml.cs`
  - `func_截圖()` 會建立 `new W_截圖(this, e_截圖類型.選取截圖);`
  - `func_截圖_全螢幕()` 也重用 `W_截圖`

### 目前核心實作

- `CaptureExplorer/CaptureExplorer/window/W_screenshot.xaml.cs`
  - 建構子先列舉 `Screen.AllScreens`
  - 算出所有螢幕的最小左上角與總包圍寬高
  - `CaptureScreen()` 直接把整個虛擬桌面截成一張 `Bitmap`
  - `func_初始化()` 把這張大圖設成單一視窗背景
  - `func_取得滑鼠()` 先扣掉虛擬桌面起點，再除以單一 DPI 比例
  - `fun_確認儲存()` 直接用畫面上的選取矩形去裁切 `bimg`

### 相關輔助視窗

- `CaptureExplorer/CaptureExplorer/window/W_sc_effects.xaml.cs`
  - 全螢幕效果也採用同樣的「整體包圍範圍 + 單一比例」邏輯

### 專案設定

- `CaptureExplorer/CaptureExplorer/App.config`
  - 目前沒有 DPI awareness 宣告

---

## 問題拆解

如果保留「單一大視窗 + 單一比例」，即使改進局部計算，仍然會遇到幾個結構性問題：

1. 同一個 WPF 視窗跨越多個不同 DPI 螢幕時，DIP 與實體像素的換算不是單一常數。
2. 覆蓋背景圖如果是一整張大圖，跨螢幕接縫處很容易因為比例換算不同而錯位。
3. 滑鼠位置、選取框位置、放大鏡取樣位置、最終裁切範圍必須全部對同一套座標系，否則只修其中一段會留下偏移 bug。

所以建議不要只修補 `func_取得滑鼠()`，而是把截圖狀態改成「一個協調器 + 多個螢幕視窗」。

---

## 建議方案

## 方案核心

把目前單一 `W_截圖` 的責任拆成兩層：

1. **截圖協調器**
   - 負責列舉所有螢幕
   - 為每個螢幕先截一張對應圖片
   - 建立多個覆蓋視窗
   - 管理共用的選取狀態與最終輸出

2. **單螢幕覆蓋視窗**
   - 每個視窗只覆蓋一個實體螢幕
   - 只顯示該螢幕自己的截圖背景
   - 只在自己的螢幕座標系內處理滑鼠與顯示
   - 把互動事件回報給協調器

這次先以你的限制為準，**不支援跨螢幕選取**，並且 **任一時間只允許一個螢幕存在作用中的選取框**。

這樣每個覆蓋視窗只需要處理「單一螢幕自己的 DPI / Bounds / Bitmap」，複雜度會下降很多。

---

## 建議資料模型

新增一個螢幕描述結構，例如：

```csharp
class ScreenCaptureContext {
    public Screen Screen;
    public Rect DipBounds;
    public System.Drawing.Rectangle PixelBounds;
    public double ScaleX;
    public double ScaleY;
    public Bitmap Bitmap;
}
```

再新增一個共用選取模型，例如：

```csharp
class SelectionState {
    public string ActiveScreenDeviceName;
    public bool HasSelection;
    public System.Drawing.Rectangle PixelRectInScreen;
}
```

這裡的 `PixelRectInScreen` 是「相對於作用中螢幕左上角的像素座標」。

重點是把「最終真實資料」統一收斂到 **單一螢幕內的像素座標**。

顯示時才轉成各視窗自己的 DIP。

---

## 實作步驟

### 第一階段：先把 DPI 基礎補齊

1. 在 `App.config` 補上 DPI awareness 設定
   - 至少讓程式進入 system aware 或 per-monitor aware 模式
   - 如果不先做這步，後續讀到的螢幕資訊可能仍被系統虛擬化

2. 確認目標框架下可用的 DPI 取得方式
   - 可評估使用 `VisualTreeHelper.GetDpi(window)`
   - 或在視窗建立後從 `PresentationSource` 取該視窗自己的 `TransformToDevice`
   - 不要再共用主視窗 `M` 的 DPI 當作所有螢幕的比例

### 第二階段：抽出螢幕列舉與截圖資料

1. 新增一個共用方法列舉所有螢幕
2. 每個螢幕各自保存：
   - `Screen.Bounds`
   - 對應縮放比
   - 該螢幕自己的背景截圖 `Bitmap`
3. 單螢幕截圖使用：
   - `CopyFromScreen(screen.Bounds.X, screen.Bounds.Y, 0, 0, screen.Bounds.Size, ...)`

這一階段完成後，就不再依賴一張涵蓋全桌面的 `bimg`。

### 第三階段：把 `W_截圖` 重構成單螢幕視窗

1. `W_截圖` 改成只接收一個 `ScreenCaptureContext`
2. `Width / Height / Left / Top` 全部依該螢幕的 bounds 與 scale 計算
3. 背景只顯示該螢幕 bitmap
4. `func_取得滑鼠()` 改為：
   - 直接取目前全域滑鼠像素座標
   - 扣掉當前螢幕 `PixelBounds.Left/Top`
   - 再換算成該視窗內的 DIP
5. 放大鏡取樣改從該螢幕 bitmap 上取樣

這樣單一 `W_截圖` 內部就不需要知道其他螢幕存在。

### 第四階段：新增截圖協調器

建議新增一個類別，例如：

```csharp
class ScreenshotSessionController
```

責任：

1. 建立所有 `ScreenCaptureContext`
2. 建立所有 `W_截圖`
3. 記錄目前哪一個螢幕是 active screen
4. 廣播目前選取狀態，讓所有視窗同步顯示/清除遮罩
5. 當某個螢幕開始選取時，要求其他視窗立即取消自己的選取 UI
6. 最後輸出成單一圖片

這層會取代目前 `MainWindow.w_截圖` 只指向單一視窗的模型。

---

## 視窗間互動規則

這次建議明確採用「單 active screen」模型：

1. 每個覆蓋視窗都可接收滑鼠事件，但只有一個視窗能成為 `ActiveScreen`
2. 當使用者在某個螢幕按下左鍵開始拖曳時：
   - controller 將該螢幕設為 `ActiveScreen`
   - 廣播給其他螢幕視窗執行 `ClearSelection()`
   - 其他螢幕若已有工具列或選取框，立即隱藏
3. 拖曳過程中，只有 `ActiveScreen` 自己更新選取框
4. 其他螢幕仍可顯示自己的背景遮罩，但不可同時維持另一個選取結果
5. 若使用者在 B 螢幕重新按下開始選取：
   - controller 先清掉 A 螢幕的選取狀態
   - 再把 B 切成新的 `ActiveScreen`

優點：

- 邏輯很單純
- 不需要處理跨螢幕矩形拆分與拼圖
- 可以明確避免多個螢幕同時出現工具列或選取框

---

## 最終輸出建議

最後儲存時只從 `ActiveScreen` 對應的 bitmap 裁切：

1. controller 取得目前 `ActiveScreenDeviceName`
2. 找到對應的 `ScreenCaptureContext.Bitmap`
3. 以 `PixelRectInScreen` 直接裁切

這樣可以把輸出邏輯維持得很單純，不需要做跨螢幕拼接。

---

## `W_sc_effects` 建議

這個視窗目前也沿用「整體包圍範圍 + 單一縮放」概念。

如果主截圖流程改成多視窗，這裡建議一起調整成：

1. 全螢幕效果時，為每個螢幕各開一個效果視窗
2. 或先暫時維持現狀，但標記為後續修正項

若不一起改，主流程可能修好了，但全螢幕閃光/動畫仍會在混合縮放環境下出現偏移。

---

## 風險與注意事項

### 1. DPI awareness 是前置條件

如果程式未宣告正確 DPI awareness，Windows 可能回傳經過虛擬化的座標，導致：

- 視窗大小正確但截圖錯
- 截圖正確但滑鼠偏移
- 主螢幕正常、副螢幕錯位

### 2. 現有 `W_截圖` 類別耦合較高

目前 `W_截圖` 同時負責：

- 建立截圖背景
- 管理互動
- 儲存輸出
- 控制主視窗隱藏/還原

這意味著改成多視窗時，最好同步把「session 管理」抽出去，不然後面維護會很痛。

### 3. 全域鍵盤事件要重新掛接

目前 `MainWindow.w_截圖` 是單一參考，`C_global_keyboard.cs` 也直接呼叫：

- `M.w_截圖.func_關閉程式()`
- `M.w_截圖.func_key_down(e)`

如果改成多視窗，需要讓快捷鍵交給 session controller，再由 controller 轉送到 `ActiveScreen` 視窗。

### 4. 單點取色也要統一到像素座標

目前取色是從 `bimg.GetPixel(...)` 直接讀。之後應改成：

- 先找出目前 `ActiveScreen`
- 換算成該螢幕 bitmap 的區域座標
- 再取色

---

## 建議改動清單

優先改動：

- `CaptureExplorer/CaptureExplorer/window/W_screenshot.xaml.cs`
- `CaptureExplorer/CaptureExplorer/MainWindow.xaml.cs`
- `CaptureExplorer/CaptureExplorer/cs/C_global_keyboard.cs`
- `CaptureExplorer/CaptureExplorer/App.config`

可能新增：

- `CaptureExplorer/CaptureExplorer/cs/ScreenshotSessionController.cs`
- `CaptureExplorer/CaptureExplorer/cs/ScreenCaptureContext.cs`

建議次要跟進：

- `CaptureExplorer/CaptureExplorer/window/W_sc_effects.xaml.cs`

---

## 建議分批落地

### Phase 1

先補 DPI awareness，並把螢幕列舉/截圖資料抽成獨立結構。

### Phase 2

把 `W_截圖` 限縮成「單螢幕覆蓋視窗」。

### Phase 3

加入 `ScreenshotSessionController`，改成多視窗協作。

### Phase 4

補齊單 active screen 的互斥控制、取色、放大鏡、快捷鍵與特效視窗。

這樣每一步都可測，也比較容易回退。

---

## 驗證案例

至少測以下情境：

1. 單螢幕 100%
2. 雙螢幕都 100%
3. 主螢幕 100%，副螢幕 150%
4. 主螢幕 150%，副螢幕 100%
5. 副螢幕在主螢幕左側，座標為負值
6. 副螢幕在主螢幕上方
7. 在 A 螢幕已選取後，到 B 螢幕重新開始拖曳，A 的選取框與工具列應立即消失
8. 不允許 A、B 兩個螢幕同時保留兩組選取框
9. 單點取色在不同縮放螢幕都準確
10. 放大鏡中心點與實際取樣位置一致

---

## 我對這次修改的建議結論

建議採用「每螢幕一個覆蓋視窗 + 單 active screen 模型」。

這比在現有單一超大視窗上繼續補 patch 更值得做，原因是：

- 能直接對齊 Windows 多螢幕 / 多 DPI 的實際模型
- 各視窗只處理單一螢幕，邏輯比較穩
- 你要的互斥行為也可以集中由 controller 管理

如果你要我接著往下做，我會建議先從 **補 DPI awareness + 抽出 session/context 類別** 開始，而不是直接硬改 `W_截圖` 裡的座標計算。

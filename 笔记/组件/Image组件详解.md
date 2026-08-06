# Unity Image 组件详解（uGUI）

> 适用基准：Unity 2022.3 LTS、uGUI 1.0.0。本文介绍的是 `UnityEngine.UI.Image`，不是 UI Toolkit 的 `Image`，也不是场景中的 `SpriteRenderer`。

## 1. Image 是什么

`Image` 用于在 **Canvas** 中显示一个 `Sprite`，是 uGUI 最常用的可视组件之一。它不仅能显示普通图片，还支持：

- 保持图片宽高比。
- 九宫格拉伸。
- 平铺纹理。
- 按百分比填充，用于血条、技能冷却和进度环。
- 参与按钮点击等 UI 射线检测。
- 被 `Mask` 或 `RectMask2D` 裁剪。
- 通过颜色对 Sprite 进行整体染色和透明度控制。

常见层级：

```text
Canvas
└── Panel                  Image：窗口背景
    ├── Icon               Image：物品图标
    ├── HealthBackground   Image：血条底图
    └── HealthFill         Image：Filled 类型的血量
```

创建方式：在 Hierarchy 中选择 **UI > Image**。如果场景中还没有 Canvas 和 EventSystem，Unity 通常会自动创建它们。

## 2. Image 依赖的组件

一个 uGUI Image 对象通常同时拥有：

| 组件 | 职责 |
| --- | --- |
| `RectTransform` | 决定 UI 的锚点、位置、宽高、旋转和缩放 |
| `CanvasRenderer` | 把 UI 图形提交给 Canvas 渲染 |
| `Image` | 根据 Sprite、颜色和绘制类型生成 UI 图形 |

`Image` 必须处在 Canvas 层级中才能作为 UI 正常显示。它显示在屏幕上的最终结果还会受到父级 `RectTransform`、`CanvasGroup`、`Mask`、Canvas 排序和 Canvas Scaler 的影响。

## 3. 使用前：正确导入图片

选中 Project 窗口中的图片，在 Texture Import Settings 中重点检查：

| 导入项 | 说明 |
| --- | --- |
| **Texture Type** | 一般设为 `Sprite (2D and UI)` |
| **Sprite Mode** | 单张图用 `Single`；图集中含多张图用 `Multiple` 并在 Sprite Editor 中切片 |
| **Pixels Per Unit** | Sprite 的像素与 Unity 单位换算；会影响原生尺寸和九宫格边框换算 |
| **Mesh Type** | `Tight` 可减少透明区域的几何覆盖；需要规则矩形或某些裁剪行为时可用 `Full Rect` |
| **Filter Mode** | 像素风常用 `Point`；普通 UI 常用 `Bilinear` |
| **Compression** | 压缩过强会使文字、图标边缘出现色块或发糊 |
| **Max Size** | 小于原图尺寸会发生缩放，导致细节损失 |

导入完成后，把 Sprite 拖到 Image 的 **Source Image** 字段即可。

如果图片拖不进 Source Image，通常是 Texture Type 没有设为 `Sprite (2D and UI)`，或者当前拖入的是 Texture 子资源之外的不兼容对象。

## 4. Inspector 基础属性

### 4.1 Source Image

要显示的 `Sprite`。没有指定 Sprite 时，Image 仍可以显示由默认 UI 材质生成的纯色矩形，但依赖版本和当前属性配置；需要确定的图片外观时应显式设置 Sprite。

代码属性：

```csharp
image.sprite = iconSprite;
```

修改 Sprite 不会自动把 RectTransform 改成图片原始尺寸。需要原生尺寸时显式调用：

```csharp
image.SetNativeSize();
```

### 4.2 Color

`Color` 会与 Sprite 每个像素的颜色相乘，而不是盖在图片上：

```text
最终颜色 = Sprite 像素颜色 × Image Color × 父级 CanvasGroup 透明度等影响
```

- 白色 `(1, 1, 1, 1)`：保持原色。
- 改 RGB：整体染色。
- 改 Alpha：控制透明度。
- 黑色会把图片染黑，而不是让它恢复原色。

修改透明度时要先取出 `Color` 结构体，再整体赋回：

```csharp
Color color = image.color;
color.a = 0.5f;
image.color = color;
```

`Button` 使用 **Color Tint** 过渡时，还会改变目标 Graphic 的颜色乘数。若按钮目标是该 Image，最终显示色是 Image 基础颜色与 Button 状态颜色共同作用的结果。

### 4.3 Material

默认留空即可使用 Unity 的默认 UI 材质。只有需要特殊 Shader 效果，例如灰度、溶解、描边或特殊遮罩时才设置自定义材质。

自定义 UI Shader 需要正确支持透明混合、Canvas 裁剪和需要的 Stencil 行为，否则可能出现：

- 图片变成粉色。
- Mask 失效。
- 透明区域显示错误。
- UI 合批被打断，增加 Draw Call。

不要为了只改变颜色而创建新材质；直接使用 `Image.color` 更合适。

### 4.4 Raycast Target

决定该 Image 是否作为 UI 射线检测目标。

- 勾选：可能接收或阻挡鼠标、触摸等 UI 指针射线。
- 取消：`GraphicRaycaster` 会忽略该 Image，但图片仍正常显示。

Image 本身不会因为勾选后就自动执行点击逻辑。要响应点击，还需要 `Button`、`EventTrigger`，或实现 `IPointerClickHandler` 等事件接口，并确保场景有 EventSystem、Canvas 上有 GraphicRaycaster。

纯装饰图片、背景花纹和不需要交互的遮罩图通常应该取消 **Raycast Target**。否则它们可能挡住下层按钮，也会增加不必要的射线检测候选。

### 4.5 Raycast Padding

某些 Unity 版本会在 Image/Graphic 的高级属性中显示射线检测边距。它只调整可交互区域，不改变图片的视觉尺寸。

- 正值通常向内缩小命中区域。
- 负值通常向外扩大命中区域。

适合让视觉上很小的按钮更容易点击，或排除图片边缘的空白区域。实际命中仍会受父级 CanvasGroup、Mask 和其他 UI 图形影响。

### 4.6 Maskable

决定该 Image 是否响应父级的 UI 遮罩。

- 勾选：会被父级 `Mask` 或 `RectMask2D` 裁剪。
- 取消：通常不参与父级遮罩裁剪。

大多数情况下保持勾选。只有明确希望子图越过遮罩显示时才关闭。

## 5. Image Type：四种绘制模式

Image 最重要的设置是 **Image Type**。它决定 Sprite 如何填满 RectTransform。

| 类型 | 主要用途 | 是否需要特殊 Sprite 设置 |
| --- | --- | --- |
| `Simple` | 图标、普通图片、头像 | 不需要 |
| `Sliced` | 可缩放窗口、按钮、边框 | 需要在 Sprite Editor 设置 Border |
| `Tiled` | 重复纹理背景、网格、花纹 | Border 可选，纹理需适合平铺 |
| `Filled` | 血条、进度条、技能冷却 | 不需要 Border |

### 5.1 Simple：普通拉伸

`Simple` 会把整个 Sprite 绘制到 RectTransform 中。RectTransform 的宽高比和图片不同时，默认会发生拉伸。

附加选项：

#### Preserve Aspect

保持 Sprite 原始宽高比，避免变形。Image 会在 RectTransform 范围内尽可能放大图片，但可能留下空白区域。

注意：它只改变图片在矩形中的绘制方式，不会自动修改 RectTransform 的宽高。

#### Use Sprite Mesh

使用 Sprite 导入时生成的网格，而不是始终使用完整矩形。对周围有大量透明区域且使用 Tight Mesh 的 Sprite，可能减少透明区域覆盖；但在要求严格矩形显示、特定遮罩或点击区域时，要验证实际效果。

#### Set Native Size

把 RectTransform 调整为 Sprite 的原生显示尺寸。结果会受到 Sprite 的像素尺寸、Pixels Per Unit 和 Canvas 参考像素密度影响。

它是一次性操作：之后换 Sprite 或改导入设置，RectTransform 不会自动持续跟随，除非再次调用。

### 5.2 Sliced：九宫格拉伸

`Sliced` 把图片分成 9 个区域：

```text
┌────────┬──────────────┬────────┐
│ 左上角 │    上边      │ 右上角 │
├────────┼──────────────┼────────┤
│ 左边   │    中心      │ 右边   │
├────────┼──────────────┼────────┤
│ 左下角 │    下边      │ 右下角 │
└────────┴──────────────┴────────┘
```

- 四个角保持形状。
- 四条边只沿一个方向拉伸。
- 中心区域在剩余空间中拉伸。

适合圆角按钮、对话框、面板和边框。如果用 Simple 拉伸圆角背景，圆角和边框会一起被拉宽；Sliced 可以保护它们。

#### 设置九宫格 Border

1. 选中 Sprite 资源。
2. 打开 **Sprite Editor**。
3. 设置左、右、上、下 Border。
4. Apply 保存。
5. 在 Image 上把 Image Type 改为 `Sliced`。

如果 Sprite 没有 Border，Sliced 就没有可保护的边缘，Inspector 也可能给出警告。

#### Fill Center

- 勾选：绘制中心区域，适合实心面板。
- 取消：不绘制中心，只保留边框，适合空心框。

#### Pixels Per Unit Multiplier

调整九宫格边框相对于当前 UI 的像素密度。通常保持 `1`；需要整体改变边框视觉厚度时再调整。

RectTransform 小到容不下两侧 Border 时，Unity 会压缩边框，无法继续保持理想形状。因此九宫格也有合理的最小尺寸。

### 5.3 Tiled：重复平铺

`Tiled` 不把中心纹理无限拉长，而是在可用区域内重复 Sprite 图案。

适合：

- 棋盘格或网格背景。
- 可重复的纸张、布料、噪声花纹。
- 很长但不能被拉变形的装饰条。

它也支持 Border 和 Fill Center：有 Border 时边缘按各自区域平铺，中心区域根据 Fill Center 决定是否绘制。

注意：

- Sprite 的左右边缘、上下边缘必须能无缝连接，否则接缝明显。
- 很大的 UI 区域和很小的平铺单元可能生成较多几何数据，使用前应通过 Profiler 和 Frame Debugger 检查。
- Sprite 的打包、Wrap Mode 和 Image 的 Tiled 生成方式会共同影响结果，不应把它简单等同于材质纹理的 Repeat。

### 5.4 Filled：按比例填充

`Filled` 根据 `Fill Amount` 只绘制 Sprite 的一部分，非常适合血量、经验值、读条和冷却效果。

#### Fill Amount

范围为 `0～1`：

- `0`：完全不填充。
- `0.5`：显示一半。
- `1`：完全填满。

```csharp
healthImage.fillAmount = Mathf.Clamp01(currentHealth / maxHealth);
```

如果 `currentHealth` 和 `maxHealth` 都是整数，要避免整数除法：

```csharp
healthImage.fillAmount = (float)currentHealth / maxHealth;
```

#### Fill Method

| 方法 | 效果 | 常见用途 |
| --- | --- | --- |
| `Horizontal` | 水平方向填充 | 横向血条、经验条 |
| `Vertical` | 垂直方向填充 | 能量柱、水位 |
| `Radial 90` | 在 90° 范围旋转填充 | 四分之一圆形界面 |
| `Radial 180` | 在 180° 范围旋转填充 | 半圆仪表 |
| `Radial 360` | 绕整圆旋转填充 | 技能冷却、圆形进度 |

#### Fill Origin

决定从哪一边或哪个角开始填。例如 Horizontal 可以从 Left 或 Right 开始，Vertical 可以从 Bottom 或 Top 开始。径向模式的 Origin 选项随 90/180/360 类型变化。

#### Clockwise

径向填充时决定顺时针还是逆时针增长。水平和垂直模式不使用这个选项。

Filled 改变的是 Image 生成的可见网格，不会改变 RectTransform 的尺寸。若还要让文字显示 `75%`，需要单独更新 Text/TMP 文本。

## 6. RectTransform、锚点与图片尺寸

Image 的显示区域由 `RectTransform.rect` 决定。常见概念：

- **Anchors**：决定对象相对父级的定位和拉伸方式。
- **Pivot**：旋转、缩放和部分尺寸变化的中心。
- **Anchored Position**：相对锚点的位置。
- **Size Delta**：在当前锚点规则下对宽高的调整；锚点拉伸时不能简单理解为最终宽高。

常见需求：

### 固定尺寸图标

使用不拉伸的锚点，设置明确宽高；需要保持比例时勾选 Preserve Aspect。

### 跟随父容器拉伸的背景

把锚点设为四向 Stretch，通常将 Left、Right、Top、Bottom 设为 0。圆角背景使用 Sliced 防止边角变形。

### 按原图尺寸显示

在 Inspector 点击 **Set Native Size**，或调用：

```csharp
image.SetNativeSize();
```

### 由布局系统控制尺寸

Image 可与 `HorizontalLayoutGroup`、`VerticalLayoutGroup`、`GridLayoutGroup`、`LayoutElement` 和 `ContentSizeFitter` 配合。布局组件可能在本帧后续阶段重写 RectTransform，所以手动改尺寸后又“跳回去”时，应先检查父级 Layout Group 和自身布局组件。

不要让多个系统同时争夺同一个尺寸，例如同时用 Layout Group、Content Size Fitter 和脚本每帧设置宽高，否则容易产生布局重建循环或结果抖动。

## 7. 点击、射线检测与事件系统

一次常见 uGUI 指针命中过程可以理解为：

```text
鼠标或触摸输入
      ↓
EventSystem
      ↓
Canvas 上的 GraphicRaycaster
      ↓
寻找 Raycast Target 开启且未被阻挡的 Graphic
      ↓
Button / EventTrigger / IPointer...Handler 接收事件
```

Image 是否可见和是否拦截点击是两件事。Alpha 为 0 的 Image 只要仍启用 Raycast Target，通常依旧可能挡住点击。

### 7.1 常见的“按钮点不到”原因

1. 场景中没有 EventSystem。
2. Canvas 没有 GraphicRaycaster。
3. 目标 Image 的 Raycast Target 被关闭。
4. 父级 `CanvasGroup.blocksRaycasts` 为 `false`。
5. 上层透明 Image 开着 Raycast Target，先截获了射线。
6. Mask 或 RectMask2D 把交互区域裁掉。
7. Button 不可交互，或父级 CanvasGroup 的 `interactable` 为 `false`。
8. 使用 Input System 时，EventSystem 的 Input Module 配置与当前输入方案不匹配。

### 7.2 透明像素命中：alphaHitTestMinimumThreshold

默认情况下，Image 的点击区域主要按 RectTransform 判断，即使点在 Sprite 的透明角落，也可能算命中。

可以提高 Alpha 命中阈值：

```csharp
image.alphaHitTestMinimumThreshold = 0.5f;
```

含义是只有 Sprite 对应像素 Alpha 达到阈值时才算命中。使用前注意：

- `0` 表示不做像素级 Alpha 筛选。
- 大于 `0` 通常要求纹理能够被 CPU 读取，需要检查 Texture Import Settings 的 **Read/Write Enabled**。
- Sprite Atlas、纹理压缩和某些打包方式可能限制或改变这种检测，应在目标平台验证。
- 开启 Read/Write 会增加纹理内存占用，不应仅为普通矩形按钮随意开启。

## 8. Mask 与 RectMask2D

### Mask

- 使用父对象自身的 Graphic 形状作为遮罩。
- 可实现圆形头像、特殊形状窗口。
- 通常依赖 Stencil。
- 父对象常同时带 Image 和 Mask。
- **Show Mask Graphic** 决定遮罩图形本身是否显示，不影响它继续作为遮罩。

### RectMask2D

- 只做矩形裁剪。
- 常用于 Scroll View 的 Viewport。
- 对矩形区域通常比任意形状 Mask 更直接。

如果 Image 没有被遮罩，依次检查：

1. Image 是否真的是 Mask/RectMask2D 的子级。
2. Image 的 Maskable 是否开启。
3. 自定义材质是否支持所需的 UI 裁剪或 Stencil。
4. 是否存在额外 Canvas 改变了遮罩层级关系。

## 9. 常用代码操作

需要引用命名空间：

```csharp
using UnityEngine.UI;
```

### 9.1 通过 Inspector 获取引用

```csharp
using UnityEngine;
using UnityEngine.UI;

public class PlayerHud : MonoBehaviour
{
    [SerializeField] private Image portraitImage;
    [SerializeField] private Image healthFillImage;
}
```

推荐优先在 Inspector 中拖入引用。这样依赖清晰，也避免在运行时按名称查找对象。

### 9.2 同一对象上获取 Image

```csharp
private Image image;

private void Awake()
{
    image = GetComponent<Image>();
}
```

确定该组件必须存在时，可加：

```csharp
[RequireComponent(typeof(Image))]
public class IconView : MonoBehaviour
{
}
```

### 9.3 更换 Sprite

```csharp
public void SetIcon(Sprite sprite)
{
    portraitImage.sprite = sprite;
    portraitImage.enabled = sprite != null;
}
```

是否在 Sprite 为空时禁用 Image 取决于设计需求；如果还需要纯色背景，就不应该自动禁用。

### 9.4 设置血条

```csharp
public void SetHealth(float current, float maximum)
{
    float normalized = maximum > 0f ? current / maximum : 0f;
    healthFillImage.fillAmount = Mathf.Clamp01(normalized);
}
```

前提是 Inspector 中已经把该 Image 的 Type 设为 `Filled`。

### 9.5 设置颜色与透明度

```csharp
public void SetAlpha(float alpha)
{
    Color color = portraitImage.color;
    color.a = Mathf.Clamp01(alpha);
    portraitImage.color = color;
}
```

要淡化整个 UI 分组，使用父级 `CanvasGroup.alpha` 往往比逐个修改所有子 Image 更方便。

### 9.6 启用或隐藏图片

```csharp
image.enabled = false;            // 不绘制这个 Image，GameObject 及其他组件仍然活跃
image.gameObject.SetActive(false); // 整个 GameObject 进入非激活状态
```

两者影响不同：

- `image.enabled = false` 只禁用 Image 组件。
- `SetActive(false)` 会停用对象上的其他组件及其子层级，并触发生命周期中的 `OnDisable`。

如果只是隐藏整组 UI，通常停用组的根对象；如果只临时隐藏一个图形但保留对象逻辑，可禁用 Image。

### 9.7 sprite 与 overrideSprite

`sprite` 是 Image 配置的基础 Sprite；`overrideSprite` 可以临时覆盖实际显示 Sprite，并被 Selectable 等系统用于替换视觉。

普通图标切换通常直接赋值 `sprite` 即可。只有明确需要保留基础 Sprite 并暂时覆盖时才使用 `overrideSprite`，否则两者并存容易让 Inspector 中看到的 Sprite 与实际显示不一致。

## 10. Image、RawImage 与 SpriteRenderer 的区别

| 特性 | `Image` | `RawImage` | `SpriteRenderer` |
| --- | --- | --- | --- |
| 所属系统 | uGUI / Canvas | uGUI / Canvas | 2D/3D 场景渲染 |
| 主要资源 | `Sprite` | `Texture` | `Sprite` |
| 九宫格 | 支持 | 不直接支持 | 支持 Sliced/Tiled Draw Mode |
| Filled 进度 | 支持 | 不直接支持 | 不直接支持 |
| UV Rect | 不作为主要接口 | 支持 | 不适用 |
| UI 射线检测 | 可参与 GraphicRaycaster | 可参与 GraphicRaycaster | 通常使用 Physics/Physics2D Collider |
| 常见用途 | 图标、面板、按钮、血条 | RenderTexture、视频、直接纹理显示 | 世界中的角色、道具和背景 |

选择建议：

- Canvas 中显示 Sprite、按钮背景或进度图：`Image`。
- Canvas 中显示 RenderTexture、摄像机画面、视频纹理或需要 UV Rect：`RawImage`。
- 把图片作为游戏世界中的对象：`SpriteRenderer`。

## 11. 性能注意事项

### 11.1 关闭无用的 Raycast Target

装饰 Image 不参与交互时关闭 Raycast Target，减少射线检测候选，并避免挡住按钮。

### 11.2 注意透明图片的 Overdraw

UI 即使像素透明，大片矩形仍可能产生片元处理开销。多个全屏半透明面板叠加时，移动设备尤其容易受到 Overdraw 影响。

可以通过 Unity Profiler、Frame Debugger 和 Scene 视图的 Overdraw 模式检查。

### 11.3 使用 Sprite Atlas

把会一起显示的小图标合理打入 Sprite Atlas，可以减少纹理切换并改善合批。但需要同时考虑图集尺寸、内存、加载粒度和 Alpha 命中限制，不是图集越大越好。

### 11.4 少创建材质实例

大量 Image 各用不同材质会破坏合批。只改颜色应使用 `Image.color`；确实需要独立 Shader 参数时，再评估材质实例和 Canvas 分组策略。

### 11.5 避免不必要的布局重建

频繁调用 `SetNativeSize`、修改 RectTransform、切换布局参数，可能让 Canvas Layout 重建。动态变化很频繁的 UI 与几乎不变化的 UI 可以视情况放在不同 Canvas 中，但拆分过多 Canvas 也有成本，应使用 Profiler 决定。

### 11.6 Filled 动画要合理更新

修改 `fillAmount` 会让 Image 更新几何。血条和冷却动画通常完全可以这样做，但不要在值没有变化时反复赋同一个值，也不要无意义地让大量屏外 UI 每帧更新。

## 12. 常见问题排查

### 图片完全看不见

按顺序检查：

1. GameObject 及父级是否 Active。
2. Image 组件是否 Enabled。
3. Color Alpha 和父级 CanvasGroup Alpha 是否大于 0。
4. RectTransform 宽高是否为 0，缩放是否异常。
5. 是否被 Mask、RectMask2D 或 Scroll View 裁掉。
6. Canvas 是否启用，Render Mode、相机和排序是否正确。
7. 自定义材质或 Shader 是否兼容当前渲染管线。
8. Filled 类型的 Fill Amount 是否为 0。

### 图片被拉变形

- Simple 类型勾选 Preserve Aspect。
- 需要自适应尺寸的边框图改用 Sliced，并正确设置 Border。
- 检查父级 Layout Group 是否强制改写宽高。
- 检查父节点是否使用了非等比缩放。

### 图片模糊

- 检查源图片分辨率、Max Size 和 Compression。
- 检查 Filter Mode 是否符合美术风格。
- 检查 Canvas Scaler 的 Reference Resolution 与 Match 设置。
- 避免把很小的 Sprite 过度放大。
- 像素风 UI 需要统一 Pixels Per Unit、整数缩放和像素对齐策略。

### 九宫格边角仍然变形

- 确认 Sprite Editor 的 Border 已 Apply。
- 确认 Image Type 真的是 Sliced。
- 确认 RectTransform 没有小于边框能容纳的最小尺寸。
- 检查 Pixels Per Unit Multiplier 和父级 Transform 缩放。

### 修改 fillAmount 没有效果

- Image Type 必须是 Filled。
- 确认改的是当前显示对象的 Image 引用。
- 确认计算结果不是整数除法导致一直为 0。
- 使用日志或 Inspector 检查值是否真的在 `0～1` 之间变化。

### 透明图片挡住下面按钮

关闭该 Image 的 Raycast Target，或让不参与交互的父级 CanvasGroup 不阻挡射线。仅把 Color Alpha 设为 0 不等于关闭射线检测。

### Image 变成粉色

通常代表材质 Shader 丢失、编译失败或不兼容当前渲染管线。先把 Material 清空恢复默认 UI 材质，再检查自定义 Shader。

### 运行时换图后尺寸不对

赋值 `image.sprite` 只换图片，不会自动改 RectTransform。若设计要求随图片采用原生尺寸，换图后调用 `SetNativeSize()`；如果 UI 由布局系统控制，则应让布局组件决定尺寸，不要强行调用。

## 13. 一页速查表

| 需求 | 推荐设置或 API |
| --- | --- |
| 普通图标 | `Simple` |
| 图片不变形 | `Simple/Filled + Preserve Aspect`，或正确设置 RectTransform 比例 |
| 可伸缩圆角面板 | Sprite Border + `Sliced` |
| 重复花纹背景 | `Tiled` |
| 横向血条 | `Filled + Horizontal`，修改 `fillAmount` |
| 圆形冷却 | `Filled + Radial 360` |
| 只显示边框 | `Sliced` 并关闭 `Fill Center` |
| 恢复图片原生尺寸 | `image.SetNativeSize()` |
| 更换图片 | `image.sprite = newSprite` |
| 修改透明度 | 复制 `image.color`，改 Alpha，再赋回 |
| 装饰图不挡点击 | 关闭 `Raycast Target` |
| 按透明像素命中 | 设置 `alphaHitTestMinimumThreshold` 并验证纹理可读性 |
| 裁剪圆形头像 | 父级 Image + `Mask` |
| 矩形滚动裁剪 | `RectMask2D` |
| 隐藏单个图形 | `image.enabled = false` |
| 隐藏整个 UI 组 | 对组根对象 `SetActive(false)` 或按需求使用 CanvasGroup |

## 14. 最重要的记忆原则

1. Image 属于 Canvas UI，显示资源是 Sprite。
2. `RectTransform` 决定显示区域，Image Type 决定 Sprite 如何填入该区域。
3. 普通图片用 Simple，可缩放边框用 Sliced，重复纹理用 Tiled，进度效果用 Filled。
4. Color 是乘色；Alpha 为 0 的 Image 仍可能拦截点击。
5. 纯装饰 Image 应关闭 Raycast Target。
6. 换 Sprite 不会自动换尺寸，需要时调用 `SetNativeSize()`。
7. 九宫格必须先在 Sprite Editor 中设置 Border。
8. Image 本身不等于 Button；交互依赖 EventSystem、GraphicRaycaster 和事件接收组件。
9. UI 异常先检查层级激活、RectTransform、Color/CanvasGroup Alpha、Mask、Canvas 和材质。
10. 大量透明叠层、材质实例和频繁布局重建都可能成为 UI 性能问题。


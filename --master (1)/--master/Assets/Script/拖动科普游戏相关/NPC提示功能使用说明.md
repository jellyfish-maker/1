# NPC提示功能使用说明

## 功能概述

新增的NPC提示功能允许玩家在拖拽部件到NPC（avatar）区域时，获得相应的提示信息。当部件被拖拽到NPC贴图范围内时，部件会自动回弹到原位置，同时NPC会显示对话框并说出对应的提示语句。

## 新增脚本说明

### 1. NPCHintSystem.cs
- **功能**: 管理NPC提示系统的核心脚本
- **职责**: 
  - 检测拖拽对象是否在avatar区域内
  - 管理对话框的显示和隐藏动画
  - 处理提示文本的显示逻辑

### 2. EyePartHintConfig.cs
- **功能**: 配置每个部件的提示信息
- **职责**:
  - 管理所有部件的提示语句
  - 自动设置EyeDragUI组件的NPC提示系统引用
  - 提供运行时配置接口

### 3. EyeDragUI.cs (已修改)
- **新增功能**: 
  - 在拖拽结束时检测是否在NPC区域内
  - 如果在NPC区域内，触发提示并回弹
  - 提供NPC提示系统的管理接口

## 设置步骤

### 第一步：设置NPC提示系统

1. 在场景中找到包含avatar、chatBox、text组件的GameObject（根据你的Hierarchy，应该是`game1/popup`）
2. 为该GameObject添加`NPCHintSystem`脚本
3. 在Inspector中设置以下引用：
   - **Avatar Image**: 拖入avatar的Image组件
   - **Chat Box Image**: 拖入chatBox的Image组件  
   - **Hint Text**: 拖入text的TextMeshProUGUI组件

### 第二步：配置EyeDragUI组件（主要配置）

1. 选择每个可拖拽的部件GameObject
2. 在`EyeDragUI`脚本中找到"提示设置"部分
3. 设置以下字段：
   - **Part Name**: 部件名称（例如："玻璃体"、"晶状体"等）
   - **Description**: 提示语句（例如："玻璃体是眼球内部的透明胶状物质，支撑眼球形状"）
4. 在"NPC提示设置"部分：
   - 确保`Enable NPC Hints`已勾选
   - `NPC Hint System`字段会自动由`EyePartHintConfig`脚本设置

### 第三步：可选 - 创建HintConfig（用于自定义覆盖）

如果您想为某些部件使用不同于`EyeDragUI`中`Description`的提示语句：

1. 在场景中创建一个空的GameObject，命名为"HintConfig"
2. 为该GameObject添加`EyePartHintConfig`脚本
3. 在Inspector中只配置需要自定义的部件：

```
自定义提示配置示例（只配置需要覆盖的部件）：
- 玻璃体: "玻璃体是眼球内部的透明胶状物质，就像果冻一样！"
- 瞳孔: "瞳孔会根据光线自动调节大小，就像相机的光圈一样！"
```

**注意**：如果`EyePartHintConfig`中没有配置某个部件，系统会自动使用该部件`EyeDragUI`中的`Description`作为提示语句。

## 参数说明

### NPCHintSystem参数

#### 对话框设置
- **Dialog Show Duration**: 对话框显示时长（默认3秒）
- **Fade In Speed**: 淡入速度（默认2）
- **Fade Out Speed**: 淡出速度（默认2）
- **Scale In Speed**: 缩放进入速度（默认5，已优化为更快）
- **Scale Out Speed**: 缩放退出速度（默认3）
- **Text Delay**: 文字延迟显示时间（默认0.3秒）

#### 部件提示信息
- **Part Name**: 部件名称（必须与EyeDragUI中的partName完全一致）
- **Hint Message**: 提示语句

### EyePartHintConfig参数

#### 部件提示配置
- **Part Name**: 部件名称
- **Hint Message**: 提示语句（支持多行文本）
- **Display Duration**: 显示时长
- **Enable Hint**: 是否启用此部件的提示

#### 默认设置
- **Default Display Duration**: 默认显示时长
- **Enable All Hints**: 是否启用所有提示

## 动画效果

### 优化后的动画序列

#### 对话框出现动画
1. **第一阶段 - 对话框弹出**:
   - 缩放效果: 从0缩放到原始大小（速度更快，默认5）
   - 淡入效果: 透明度从0渐变到1
   - 文字保持隐藏状态

2. **第二阶段 - 文字显示**:
   - 等待对话框完全稳定（默认0.3秒延迟）
   - 文字淡入效果: 透明度从0渐变到1
   - 平滑过渡: 使用插值动画，确保自然流畅

#### 对话框消失动画
1. **第一阶段 - 文字隐藏**:
   - 文字淡出效果: 透明度从1渐变到0

2. **第二阶段 - 对话框消失**:
   - 缩放效果: 从原始大小缩放到0
   - 淡出效果: 透明度从1渐变到0
   - 自动清理: 动画完成后自动重置状态

### 视觉效果优化
- **分层动画**: 对话框和文字分别处理，避免视觉冲突
- **时序控制**: 确保对话框完全弹出后再显示文字
- **更快响应**: 对话框弹出速度提升，用户体验更好

## 使用技巧

### 1. 提示语句编写建议
- 使用简洁明了的语言
- 突出部件的关键功能
- 避免过于技术性的术语
- 可以包含一些趣味性的描述

### 2. 动画参数调整
- **速度设置**: 数值越大动画越快，建议范围2-5
- **显示时长**: 根据提示语句长度调整，短句2-3秒，长句4-5秒
- **测试调整**: 在游戏中测试，找到最佳的视觉体验

### 3. 区域检测优化
- 确保avatar的Image组件有合适的尺寸
- 如果检测区域太小，可以适当放大avatar的RectTransform
- 测试时可以在avatar区域拖拽部件，确认检测是否正常

## 故障排除

### 问题1：拖拽到NPC区域没有反应
**解决方案**:
1. 检查NPCHintSystem是否正确设置avatar引用
2. 确认EyeDragUI的Enable NPC Hints已勾选
3. 检查部件名称是否与配置中的完全一致
4. 查看Console是否有错误信息

### 问题2：对话框不显示
**解决方案**:
1. 检查chatBox和hintText的引用是否正确
2. 确认这些组件在Hierarchy中的层级关系
3. 检查Canvas的渲染设置
4. 确认提示信息不为空

### 问题3：动画效果不自然
**解决方案**:
1. 调整动画速度参数
2. 检查原始缩放值是否正确
3. 确认颜色和透明度设置
4. 测试不同的参数组合

### 问题4：提示信息不匹配
**解决方案**:
1. 检查EyePartHintConfig中的部件名称配置
2. 确认EyeDragUI中的partName设置
3. 使用RefreshHints()方法重新初始化
4. 检查是否有重复的部件名称

## 扩展功能

### 1. 音效支持
可以在NPCHintSystem中添加音效播放功能：
```csharp
[Header("音效设置")]
[SerializeField] private AudioSource audioSource;
[SerializeField] private AudioClip hintSound;

// 在ShowPartHint方法中添加
if (audioSource != null && hintSound != null)
{
    audioSource.PlayOneShot(hintSound);
}
```

### 2. 多种提示类型
可以为不同类型的错误提供不同的提示：
- 拖拽到错误位置
- 拖拽到NPC区域
- 点击部件时的提示

### 3. 提示历史记录
记录玩家查看过的提示，避免重复显示相同信息。

## 性能优化建议

1. **对象池**: 如果提示频繁出现，可以考虑使用对象池管理对话框
2. **协程管理**: 确保协程正确停止，避免内存泄漏
3. **检测优化**: 只在拖拽结束时进行区域检测，避免频繁计算

这个NPC提示功能将大大提升游戏的用户体验，帮助玩家更好地理解眼球的结构和功能！

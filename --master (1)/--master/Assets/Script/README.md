# 视觉小说框架 - 纯净版本

这是一个基于Excel数据库的视觉小说框架，已移除所有游戏特定功能，保留核心功能。

## 核心功能

1. **Excel表格读取** - 第一行为表头，第二行开始为数据内容
2. **点击切换下一行数据** - 支持鼠标和触屏
3. **UI设置** - 对话框、角色名字、头像显示
4. **背景淡入淡出** - 平滑的背景切换效果
5. **打字机效果** - 逐字显示文本

## 文件说明

### ConstantsNew.cs
常量配置文件，包含：
- 故事文件路径
- 资源路径（头像、背景、音乐）
- 默认设置

### ExcelReaderNew.cs
Excel读取器，支持：
- 同步读取（ReadExcel）
- 异步读取（ReadExcelAsync）
- 自动跳过第一行表头
- 从第二行开始读取数据

**Excel表格列结构（从第0列开始）：**
- 列0: speaker（说话者）
- 列1: content（对话内容）
- 列2: AvatarImageFileName（头像文件名）
- 列3: backgroundImageFileName（背景图片文件名）
- 列4: backgroundMusicFileName（背景音乐文件名）
- 列5: IsProtagonist（是否主角，填"Y"表示是）

### TypeWriterEffectNew.cs
打字机效果组件，功能：
- 逐字显示文本
- 可立即完成显示
- 可检查是否正在打字

### VNManagerNew.cs
核心管理器，功能：
- 管理整个视觉小说流程
- 处理点击事件
- 更新UI显示
- 管理背景和音乐切换
- 处理角色头像和名字显示

## 使用方法

1. **准备Excel文件**
   - 将Excel文件放在 `StreamingAssets/Story/` 目录下
   - 第一行为表头（会被自动跳过）
   - 第二行开始为数据内容

2. **设置UI组件**
   - 在Unity场景中创建Canvas
   - 添加以下UI元素：
     - TextMeshProUGUI: protagonistName（主角名字）
     - TextMeshProUGUI: otherCharacterName（其他角色名字）
     - TextMeshProUGUI: speakingContent（对话内容）
     - Image: protagonistAvatar（主角头像）
     - Image: otherAvatar（其他角色头像）
     - Image: dialogueBox（对话框背景）
     - Image: backgroundImage（背景图片）
     - Image: nextBackgroundImage（用于背景切换，可选）
     - AudioSource: backgroundMusic（背景音乐）

3. **添加组件**
   - 在场景中创建一个GameObject
   - 添加 `VNManagerNew` 组件
   - 添加 `TypeWriterEffectNew` 组件（可以放在同一个GameObject或子对象上）
   - 在Inspector中绑定所有UI组件

4. **准备资源**
   - 头像：放在 `Resources/Protagonist/` 和 `Resources/Characters/` 目录
   - 背景：放在 `Resources/Background/` 目录
   - 音乐：放在 `Resources/Music/` 目录

5. **运行游戏**
   - 点击屏幕或鼠标左键切换下一行对话
   - 如果正在打字，点击会立即完成当前行

## 已移除的功能

以下游戏特定功能已被移除：
- ❌ 开场动画
- ❌ 拖动小游戏
- ❌ 人物移动控制
- ❌ GIF/特殊图片显示
- ❌ 角色行走动画
- ❌ 交互点管理器
- ❌ 位置标记
- ❌ 特殊角色立绘（newCharacter1/2/3）
- ❌ 拼接场景相关
- ❌ 探索模式

## 注意事项

1. Excel文件必须使用 `.xlsx` 格式
2. 第一行会被自动跳过（作为表头）
3. 资源文件需要放在 `Resources` 文件夹下
4. 背景切换需要两个Image组件（backgroundImage 和 nextBackgroundImage）才能实现平滑淡入淡出
5. 如果 `nextBackgroundImage` 未设置，背景切换会使用简单的淡出淡入效果

## 扩展建议

如果需要添加新功能，建议：
1. 在 `ExcelReaderNew.ExcelData` 结构体中添加新字段
2. 在 `ExcelReaderNew.ReadExcel` 方法中添加对应的读取逻辑
3. 在 `VNManagerNew.DisplayThisLine` 方法中添加对应的显示逻辑

hello,能看到修改不
hello,能看到修改不
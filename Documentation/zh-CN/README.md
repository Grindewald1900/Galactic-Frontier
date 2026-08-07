# Galactic Frontier 中文开发文档

本目录帮助开发者快速理解项目结构、当前实现、数据流和开发约束，也作为 Codex 后续修改项目时的上下文入口。

## 推荐阅读顺序

1. [快速开始](01-quick-start.md)：运行环境、启动场景和首次阅读路径。
2. [项目结构](02-project-structure.md)：第一方代码、资源和第三方目录边界。
3. [架构总览](03-architecture.md)：系统分层、生命周期和主要依赖。
4. [核心系统实现](04-core-systems.md)：卡牌、编队、抽卡、战斗、物品和 UI 的真实实现。
5. [数据与存档](05-data-and-save.md)：JSON、Resources、存档文件和路径规则。
6. [开发与验证](06-development-guide.md)：新增功能、Unity 序列化、测试和提交检查。
7. [Codex 工作指南](07-codex-guide.md)：自动化修改项目时应优先读取的上下文和安全边界。
8. [已知问题与技术债](08-known-issues.md)：原型数据、耦合点和后续重构方向。
9. [Figma UI 重构](09-figma-ui.md)：NEXUS 视觉系统、页面映射、运行时装配和扩展方式。
10. [核心类职责与关系](10-core-classes.md)：核心类的数据所有权、依赖方向、生命周期和主要调用链。

## 项目一句话说明

`Galactic Frontier` 是一个 Unity 2D 卡牌养成原型：玩家创建或加载存档，在主场景管理角色卡、编队、物品、星球和抽卡，再进入回合制战斗场景。

## 当前基线

| 项目 | 当前值 |
| --- | --- |
| Unity | `6000.0.20f1` |
| 渲染管线 | URP `17.0.3` |
| 输入 | 新 Input System 与旧 Input Manager 同时启用 |
| 公司/产品名 | `YeeStudio / Galactic Frontier` |
| 默认分辨率 | `1920 × 1080` |
| 主要代码目录 | `Assets/Resources/Scripts` |
| 主要场景目录 | `Assets/Resources/Scenes` |
| 运行时配置目录 | `Assets/Resources/data` |

> 注意：项目仍处于原型阶段。战斗敌人、抽卡材料、物品和部分星球/事件数据仍由 `FakeData()` 生成。

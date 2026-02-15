<p align="center">
  以普慈特慈的安拉之名。<br/>
  一切赞颂全归真主。
</p>

<p align="center">
  <a href="https://iqra.bot/cn">
    <img src="./.github/images/banner.png" alt="Iqra AI Banner" width="100%">
  </a>
</p>

<p align="center">
  <em>"主说：读！"</em>
</p>

<p align="center">
  <a href="./README.md"><b>English</b></a> &nbsp; • &nbsp;
  <a href="./README.ar.md"><b>العربية</b></a> &nbsp; • &nbsp;
  <a href="./README.cn.md"><b>中文</b></a> &nbsp; • &nbsp;
  <a href="./README.ru.md"><b>Русский</b></a>
</p>

<p align="center">
  <a href="https://app.iqra.bot"><b>Iqra 云</b></a> &nbsp; • &nbsp;
  <a href="https://docs.iqra.bot"><b>文档</b></a> &nbsp; • &nbsp;
  <a href="https://docs.iqra.bot/developers/self-hosting"><b>私有部署</b></a> &nbsp; • &nbsp;
  <a href="https://www.badal.om"><b>Badal Technologies</b></a>
</p>

<p align="center">
  <a href="https://github.com/abdofallah/IqraAI/blob/master/LICENSE.md">
    <img src="https://img.shields.io/badge/license-Source_Available-000000" alt="License">
  </a>
  <a href="https://discord.gg/UkKHtmqmMH">
    <img src="https://img.shields.io/badge/discord-Join_Community-5865F2" alt="Discord">
  </a>
  <a href="https://www.linkedin.com/company/badaloffical">
    <img src="https://img.shields.io/badge/linkedin-Badal_Technologies-0077B5" alt="LinkedIn">
  </a>
  <a href="https://www.linkedin.com/company/iqraai/">
    <img src="https://img.shields.io/badge/linkedin-Iqra_AI-0077B5" alt="LinkedIn">
  </a>
  <img src="https://img.shields.io/github/commit-activity/m/abdofallah/IqraAI?color=black" alt="Activity">
</p>

> [!WARNING]  
> **预发布通知 (v0.1 待定)**  
> 此代码库当前处于活跃状态，但需要手动配置服务。数据库的自动播种脚本仍在等待正式的 v0.1 发布。欢迎开发者探索架构，但目前的生产部署需要手动设置数据库。

# 动态 AI-First 引擎

**Iqra AI** 是一个编排基础设施，旨在弥合 LLM 的混乱与业务代码的可靠性之间的鸿沟。它允许您构建思维动态但行动系统的超人语音与对话代理。

与标准的“包装器”不同，Iqra AI 在 AI 的概率性质之上提供了**确定性逻辑层**。我们优先考虑架构而非“魔法”——通过多区域路由为您提供对延迟的深度控制，支持原生多语言以实现文化准确性，并提供用于企业部署的严格合规工具。

## 部署选项

### 1. Iqra 云 (SaaS)
完全托管、生产就绪的平台。包括多租户计费系统、白标管理和托管的基础设施扩展。
[开始构建](https://app.iqra.bot)

### 2. 私有部署 (开源)
在您自己的基础设施上运行核心引擎。此版本包括完整的代理引擎、脚本构建器和 FlowApp 系统，但不包括商业计费和白标模块。
**要求：** .NET 10 Runtime, MongoDB, Redis, Milvus, RustFS (S3).
[阅读部署指南](https://docs.iqra.bot/developers/self-hosting)

### 3. 企业版 (Enterprise)
适用于需要专用基础设施、定制 SLA、本地安装支持或特定合规性要求（例如特定海湾合作委员会国家内的数据驻留）的大型组织。
[联系销售](https://www.iqra.bot/cn/contact)

---

## 引擎

Iqra AI 建立在专为技术可扩展性设计的“自带一切”架构之上。

<img src="./.github/images/features/visual-ide.png" alt="Visual IDE" width="100%" style="border-radius: 8px; margin-top: 10px; margin-bottom: 20px;">

### 1. [可视化 IDE](https://docs.iqra.bot/build/script)
一个不牺牲深度的**无代码**图形编辑器。虽然非工程师也可以使用，但它暴露了对系统提示、变量状态和工具定义的精细控制。它允许您在一个统一的工作室中编排逻辑、配置智能并调试对话，无需切换上下文。

### 2. [确定性逻辑](https://docs.iqra.bot/build/script/action-flows)
将严格的、循序渐进的**工作流**直接嵌入到对话中。类似于在您的代理内部运行的可视化自动化引擎，该层以确定性方式处理条件路由 (If/Else)、循环和数学运算。AI 处理对话；系统处理执行逻辑。

### 3. [原生多语言 (并行上下文)](https://docs.iqra.bot/build/multi-language)
翻译层会造成双重延迟并丢失文化细微差别。Iqra AI 运行并行逻辑堆栈。代理可以瞬间在句子中间从英语“专业”人设（使用 Deepgram）切换到阿拉伯语“热情好客”人设（使用 Azure Speech），加载完全不同的神经配置。

### 4. 全球边缘网络 (多区域)
专为水平扩展设计。您可以在地理上分散的集群（例如 US-East 与 EU-Central 的 Kubernetes 节点）中部署 **Iqra Proxy** 和 **Backend** 的不同实例。系统将会议路由到最近的计算节点，以最大限度地减少 RTP 延迟并对抗物理限制。

### 5. [深度集成](https://docs.iqra.bot/build/tools)
模块化的**自带模型 (BYOM)** 架构。我们为行业领导者（OpenAI, Azure, Gemini, Anthropic, Groq, ElevenLabs, Deepgram）提供原生、优化的适配器，但抽象接口允许您轻松插入自定义微调模型或本地推理端点。

### 6. [安全会话 (PCI-DSS)](https://docs.iqra.bot/build/script/secure-sessions)
保持对私有上下文的控制。我们的**安全会话**功能为敏感数据收集创建了一个“洁净室”。音频/DTMF 通过严格的 **Get/Set 变量规则**由确定性引擎处理。AI 从未看到原始数字，只看到验证结果，确保符合数据隐私标准。

### 7. 全渠道部署
一个大脑，多具躯体。通过标准 SIP 中继（Twilio, Telnyx, Vonage）部署代理，或使用我们高性能的 **WebRTC/WebSocket** 网关完全绕过 PSTN，以实现亚秒级延迟的浏览器和移动应用集成。

### 8. [FlowApps 系统](https://docs.iqra.bot/developers/flowapp)
一个抽象外部 API 的开放**插件系统**。开发人员可以编写一次 C#/.NET 连接器，定义一个架构，并让最终用户直观地配置凭据和参数（例如 Cal.com, HubSpot），消除了重复编写自定义 HTTP 工具脚本的需要。

### 9. [智能轮次](https://docs.iqra.bot/build/agent/interruption)
赋予用户对打断管道的完全控制权。引擎允许您在标准 **VAD**（语音活动检测）、基于 **ML** 的投影模型或基于 **LLM** 的决策制定之间进行选择，以准确区分停顿、反馈（‘嗯哼’）和真正的强插。

### 10. 白标服务 (仅限云端)
一个供代理商转售平台的综合系统。使用您的 Logo、自定义域名重新命名整个仪表板，并为您的客户定义您自己的定价结构。

---

## 贡献

我们欢迎对核心引擎、集成和 FlowApp 生态系统的贡献。
在提交 Pull Request 之前，请阅读我们的 [贡献指南](./CONTRIBUTING.md)。

## 安全

我们要非常重视平台和用户的安全。
如果您发现漏洞，请不要通过 GitHub Issues 报告。
请阅读我们的 [安全政策](./SECURITY.md) 了解完整的披露指南。
直接发送电子邮件至：**security@iqra.bot**

## 许可与条款

Iqra AI 根据自定义的 **源可用许可证 (Source-Available License)** 获得许可。

*   **允许：** 个人使用、内部业务使用和代理使用（手动管理客户）。
*   **禁止：** 您**不得**使用此代码库创建竞争性的公共 SaaS 平台。
*   **道德条款：** 关于政治和道德立场，适用严格的使用限制。

在使用或分发此软件之前，请查看完整的 [LICENSE](./LICENSE.md)。

## 致谢

特别感谢激发了我们的架构和设计选择的开源项目：
*   [Typebot.io](https://typebot.io)
*   [Dify.ai](https://dify.ai)
*   [Scriban](https://github.com/scriban/scriban)
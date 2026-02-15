<p align="center">
  In the name of Allah, the Most Gracious, the Most Merciful.<br/>
  To Whom belongs all thanks.
</p>

<p align="center">
  <a href="https://iqra.bot">
    <img src="./.github/images/banner.png" alt="Iqra AI Banner" width="100%">
  </a>
</p>

<p align="center">
  <em>"And God said: Read!"</em>
</p>

<p align="center">
  <a href="./README.md"><b>English</b></a> &nbsp; • &nbsp;
  <a href="./README.ar.md"><b>العربية</b></a> &nbsp; • &nbsp;
  <a href="./README.cn.md"><b>中文</b></a> &nbsp; • &nbsp;
  <a href="./README.ru.md"><b>Русский</b></a>
</p>

<p align="center">
  <a href="https://app.iqra.bot"><b>Iqra Cloud</b></a> &nbsp; • &nbsp;
  <a href="https://docs.iqra.bot"><b>Documentation</b></a> &nbsp; • &nbsp;
  <a href="https://docs.iqra.bot/developers/self-hosting"><b>Self Hosting</b></a> &nbsp; • &nbsp;
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
> **Pre-Release Notice (v0.1 Pending)**  
> This codebase is currently active but requires manual service configuration. The automated seeding scripts for the database are pending the official v0.1 release. Developers are welcome to explore the architecture, but production deployment requires manual DB setup for now.

# The Dynamic AI-First Engine

**Iqra AI** is an orchestration infrastructure designed to bridge the gap between the chaos of LLMs and the reliability of business code. It allows you to build superhuman Voice & Conversational Agents that think dynamically but act systematically.

Unlike standard "wrappers," Iqra AI provides a **Deterministic Logic Layer** alongside the probabilistic nature of AI. We prioritize architecture over magic—giving you deep control over latency via multi-region routing, native multi-language support for cultural accuracy, and strict compliance tools for enterprise deployment.

## Deployment Options

### 1. Iqra Cloud (SaaS)
The fully managed, production-ready platform. Includes the multi-tenant Billing System, Whitelabeling Management, and managed infrastructure scaling.
[Start Building](https://app.iqra.bot)

### 2. Self-Hosted (Open Source)
Run the core engine on your own infrastructure. This version includes the full Agent Engine, Script Builder, and FlowApp system, but excludes the commercial Billing and Whitelabeling modules.
**Requirements:** .NET 10 Runtime, MongoDB, Redis, Milvus, RustFS (S3).
[Read the Deployment Guide](https://docs.iqra.bot/developers/self-hosting)

### 3. Enterprise
For large-scale organizations requiring dedicated infrastructure, custom SLAs, on-premise installation support, or specific compliance requirements (e.g., data residency within specific GCC nations).
[Contact Sales](https://www.iqra.bot/contact)

---

## The Engine

Iqra AI is built on a "Bring Your Own Everything" architecture designed for technical scalability.

<img src="./.github/images/features/visual-ide.png" alt="Visual IDE" width="100%" style="border-radius: 8px; margin-top: 10px; margin-bottom: 20px;">

### 1. [Visual IDE](https://docs.iqra.bot/build/script)
A **No-Code** graph-based editor that doesn't sacrifice depth. While accessible to non-engineers, it exposes granular control over system prompts, variable states, and tool definitions. It allows you to orchestrate logic, configure intelligence, and debug conversations in a unified studio without context switching.


### 2. [Deterministic Logic](https://docs.iqra.bot/build/script/action-flows)
Embed strict, step-by-step **Workflows** directly into the conversation. Similar to a visual automation engine running inside your agent, this layer handles conditional routing (If/Else), loops, and math operations deterministically. The AI handles the conversation; the System handles the execution logic.

### 3. [Native Multilingual (Parallel Contexts)](https://docs.iqra.bot/build/multi-language)
Translation layers create double latency and lose cultural nuance. Iqra AI runs parallel logic stacks. An agent can switch from an English "Professional" persona (using Deepgram) to an Arabic "Hospitable" persona (using Azure Speech) instantly, mid-sentence, loading a completely different neural configuration.

### 4. Global Edge Network (Multi-Region)
Designed for horizontal scaling. You can deploy distinct instances of the **Iqra Proxy** and **Backend** in geographically disparate clusters (e.g., Kubernetes nodes in US-East vs. EU-Central). The system routes sessions to the nearest compute node to minimize RTP latency and combat physics.

### 5. [Deep Integrations](https://docs.iqra.bot/build/tools)
A modular **Bring Your Own Model (BYOM)** architecture. We provide native, optimized adapters for industry leaders (OpenAI, Azure, Gemini, Anthropic, Groq, ElevenLabs, Deepgram), but the abstract interface allows you to plug in custom fine-tuned models or local inference endpoints easily.

### 6. [Secure Sessions (PCI-DSS)](https://docs.iqra.bot/build/script/secure-sessions)
Maintains control of the private context. Our **Secure Sessions** feature creates a "Clean Room" for sensitive data collection. The audio/DTMF is processed by the deterministic engine via strict **Get/Set variable rules**. The AI never sees the raw digits, only the validation result, ensuring compliance with data privacy standards.

### 7. Omnichannel Deployment
One brain, many bodies. Deploy agents via standard SIP Trunking (Twilio, Telnyx, Vonage) or bypass the PSTN entirely using our high-performance **WebRTC/WebSocket** gateway for browser and mobile app integration with sub-second latency.

### 8. [FlowApps System](https://docs.iqra.bot/developers/flowapp)
An open **Plugin System** that abstracts external APIs. Developers can write C#/.NET connectors once, define a schema, and let end-users configure credentials and parameters visually (e.g. Cal.com, HubSpot), eliminating the need for repetitive custom HTTP tool scripting.

### 9. [Smart Turn Taking](https://docs.iqra.bot/build/agent/interruption)
Give the user full control over the interruption pipeline. The engine allows you to select between standard **VAD** (Voice Activity Detection), **ML-based** projection models, or **LLM-based** decision making to accurately distinguish between a pause, a backchannel ('uh-huh'), and a true barge-in.

### 10. Whitelabeling (Cloud Only)
A comprehensive system for Agencies to resell the platform. Rebrand the entire dashboard with your logo, custom domain, and define your own pricing structure for your clients.

---

## Contributing

We welcome contributions to the core engine, the Integrations & the FlowApp ecosystem.
Please read our [Contribution Guidelines](./CONTRIBUTING.md) before submitting a Pull Request.

## Security

We take the security of our platform and our users seriously.
If you discover a vulnerability, please do not report it via GitHub Issues.
Please read our [Security Policy](./SECURITY.md) for full disclosure guidelines.
Email us directly at: **security@iqra.bot**

## License & Terms

Iqra AI is licensed under a custom **Source-Available License**.

*   **Permitted:** Personal use, internal business use, and agency use (managing clients manually).
*   **Prohibited:** You may **not** use this codebase to create a competing public SaaS platform.
*   **Ethical Clause:** Strict usage restrictions apply regarding political and ethical alignment.

Please review the full [LICENSE](./LICENSE.md) before using or distributing this software.

## Acknowledgments

Special thanks to the open-source projects that inspired our architecture and design choices:
*   [Typebot.io](https://typebot.io)
*   [Dify.ai](https://dify.ai)
*   [Scriban](https://github.com/scriban/scriban)
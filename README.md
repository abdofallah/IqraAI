<p align="center">
  <a href="https://iqra.bot">
    <img src="./.github/images/banner.png" alt="Iqra AI Banner" width="100%">
  </a>
</p>

<p align="center">
  <em>"And God said: Read!"</em>
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

# Iqra AI

[Iqra AI](https://iqra.bot) is a low-code orchestration platform for building high-fidelity Voice AI Agents, created by [Badal Technologies](https://www.badal.om). Designed for agencies and enterprises that require precision, it bridges the gap between Large Language Models and real-time channels(telephony, web). We prioritize architecture over magic—giving you deep control over latency via multi-region routing, native multi-language support for cultural accuracy, and a deterministic but dynamic AI first graph-based script builder.

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
[Contact Sales](mailto:change@badal.om)

## Key Features

*   **Visual Script Builder:** A graph-based editor to design deterministic conversation flows, combining LLM intelligence with strict business logic.
*   **Omnichannel Support:** Deploy agents via standard Telephony (Twilio, Telnyx, Vonage, SIP Trunking) or directly to browsers and mobile apps via high-fidelity WebRTC/WebSockets.
*   **Deep Integrations:** Native support for industry-leading providers (OpenAI, Azure, Google, ElevenLabs, Deepgram) with a modular architecture that allows plugging in custom LLM, TTS, STT, and Embedding models.
*   [**FlowApps System:**](https://docs.iqra.bot/developers/flowapp) A plugin architecture to integrate third-party tools (Cal.com, HubSpot) with native UI configuration and schema validation.
*   **Advanced Interruption:** A dedicated engine for handling barge-ins, allowing the agent to pause, listen, and determine if the user is interrupting or just backchanneling.
*   **Multi-Region Routing:** Assign specific phone numbers/session to specific processing servers (e.g., US East vs. EU Central) to optimize audio latency.
*   **Whitelabeling (Cloud Only):** A comprehensive system for agencies to resell the platform under their own brand, domain, and pricing structure.

## Contributing

We welcome contributions to the core engine and the FlowApp ecosystem.
Please read our [Contribution Guidelines](https://docs.iqra.bot/developers/contributing) before submitting a Pull Request.

## Security

We take the security of our platform and our users seriously.
If you discover a vulnerability, please do not report it via GitHub Issues.
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
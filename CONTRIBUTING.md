# Contributing to Iqra AI

**In the name of Allah, the Most Gracious, the Most Merciful.**

Thank you for your interest in contributing to Iqra AI. We are building the future of AI-First orchestration, and we welcome contributions that align with our mission of technical excellence and ethical leadership.

This document outlines the standards and workflows for contributing to the platform.

## Before You Start

Iqra AI is a complex, distributed system built on **.NET 10**. It involves real-time audio processing, multi-threading, and distributed state management.

Before writing code, you **must** read the Developer Documentation to understand the architecture.

*   **[Developer Hub](https://docs.iqra.bot/developers)**: The entry point.
*   **[System Architecture](https://docs.iqra.bot/developers/architecture)**: Understand the separation between `IqraCore` (Entities) and `IqraInfrastructure` (Logic).
*   **[FlowApp Architecture](https://docs.iqra.bot/developers/flowapp)**: If you want to add a new integration (like Slack or HubSpot), read this first.

## Ways to Contribute

### 1. Building FlowApps (Plugins)
This is the most impactful way to contribute. We have a "Code-First, Schema-Backed" plugin system that allows you to add new integrations without touching the core engine.

If you want to add support for a new API:
1.  Read the **[Create an App](https://docs.iqra.bot/developers/flowapp/create-app)** guide.
2.  Implement the `IFlowApp` and `IFlowAction` interfaces in C#.
3.  Define the Input Schemas using our JSON Schema standard.

### 2. Core Engine Improvements
We welcome optimizations to the `IqraBackendApp` regarding:
*   Latency reduction (Audio buffer handling).
*   VAD (Voice Activity Detection) tuning.
*   New LLM/STT/TTS Provider implementations.

### 3. Client SDKs
We currently support a Web Widget SDK. We are looking for contributors to help build:
*   **Python/Node.js Middleware:** Server-side wrappers for the API.
*   **Mobile SDKs:** React Native, Flutter, Swift, or Kotlin bindings.

## Development Workflow

We follow a standard Fork & Pull Request workflow.

1.  **Fork the Repository** to your own GitHub account.
2.  **Clone** the repository locally.
3.  **Create a Branch** for your feature or fix.
    *   Format: `feat/feature-name` or `fix/issue-description`.
4.  **Implement your changes.**
    *   Ensure you follow the **.NET Coding Conventions**.
    *   Ensure all new classes are registered in Dependency Injection if required.
5.  **Test your changes.**
    *   If adding a FlowApp, verify it using the Ad-Hoc Testing endpoint.
6.  **Submit a Pull Request (PR)** against the `main` branch.

## Coding Standards

*   **Architecture:** Do not put business logic in Controllers. Use Managers (`IqraInfrastructure`) for logic and keep Controllers (`IqraFrontend`) lean.
*   **Async/Await:** All I/O operations must be asynchronous.
*   **Logging:** Use `ILogger` for all significant events. Do not use `Console.WriteLine`.
*   **Security:** Never commit API keys or secrets. Use `appsettings.json` or Environment Variables.

## Code of Conduct & Ethics

Your contribution will **not** be accepted if it violates our ethical or political stance.

*   We do not support features designed for surveillance, deepfake fraud, or harassment.
*   We strictly adhere to the [Code of Conduct](./CODE_OF_CONDUCT.md). Please read it carefully.

## Reporting Issues

*   **Security Vulnerabilities:** Do not open GitHub Issues for security flaws. Email **security@iqra.bot** directly.
*   **Bugs:** Open a GitHub Issue with reproduction steps and logs.
*   **Feature Requests:** Open a GitHub Discussion to propose the idea before coding.

---

**Thank you for helping us build the Leaders of Tomorrow.**
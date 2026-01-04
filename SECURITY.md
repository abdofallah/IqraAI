# Security Policy

**In the name of Allah, the Most Gracious, the Most Merciful.**

Security is a core value at Badal Technologies. We view the protection of user data and infrastructure as an **Amanah** (Trust). We are committed to ensuring the Iqra AI platform remains secure, reliable, and private for all users.

## Supported Versions

We actively maintain and provide security updates for the following versions:

| Version | Supported          | Notes |
| :---    | :---               | :--- |
| Latest  | :white_check_mark: | We recommend always running the latest stable release or the `main` branch. |
| < 1.0   | :x:                | Older versions are not actively monitored for security patches. |

## Reporting a Vulnerability

If you discover a security vulnerability within the Iqra AI codebase, the Cloud Platform (`app.iqra.bot`), or our SDKs, please **DO NOT** open a public issue on GitHub.

### How to Report
Please email our security team directly at:
**security@iqra.bot**

In your email, please include:
1.  **Type of issue** (e.g., XSS, Injection, RCE, Authentication Bypass).
2.  **Full proof of concept** or steps to reproduce the issue.
3.  **Impact assessment** (what data or systems are at risk?).

### Our Response Process
1.  **Acknowledgment:** We will acknowledge receipt of your report within **48 hours**.
2.  **Assessment:** Our engineering team will validate the issue and determine the severity within **1 week**.
3.  **Resolution:** We will work on a fix immediately. We ask that you maintain confidentiality during this period.
4.  **Disclosure:** Once the fix is released, we will credit you (if desired) in our release notes and Security Hall of Fame.

## Scope

### In Scope
*   **Iqra Core Engine:** Vulnerabilities in the .NET 10 Backend, Proxy, or Background services.
*   **Frontend Dashboard:** Authentication bypasses, XSS, or data leakage in the dashboard.
*   **SDKs:** Vulnerabilities in the Web Widget or Middleware that could expose API keys.
*   **FlowApps:** Security flaws in the logic of official FlowApp integrations (e.g., Cal.com).

### Out of Scope
*   **DDoS Attacks:** Volumetric attacks against our infrastructure.
*   **Social Engineering:** Attacks targeting our employees or community members.
*   **Self-Hosted Misconfiguration:** Vulnerabilities caused by the user's failure to secure their own server (e.g., leaving MongoDB ports open to the public, weak passwords, failing to configure firewalls).
*   **Third-Party Providers:** Vulnerabilities within Twilio, OpenAI, or other providers, unless caused by our integration implementation.

## Guidelines for Self-Hosters

If you are self-hosting Iqra AI, you assume the role of the System Administrator. The code is secure, but your deployment environment must also be secure.

**Mandatory Security Practices:**
1.  **Change Defaults:** Immediately change the default Admin Email and Password in `appsettings.json` upon first deployment.
2.  **Secure the Data Layer:** Ensure MongoDB (`27017`) and Redis (`6379`) are **NOT** exposed to the public internet. They should only be accessible within your internal Docker network.
3.  **API Secrets:** Generate a strong, random string for the `ApiSecretToken` used for the internal handshake between Proxy and Backend.
4.  **HTTPS:** You **must** use a Reverse Proxy (Nginx/Caddy) with valid SSL certificates. WebRTC and Microphone access will fail over insecure HTTP.
5.  **Firewall:** Only expose the necessary ports:
    *   `80/443` (HTTP/HTTPS)
    *   `10000-20000` (UDP - Audio RTP)
    *   Block all other inbound ports.

## Incident Response

In the unlikely event of a data breach on the **Iqra Cloud** platform:
1.  We will notify affected users via email within 72 hours of confirmation.
2.  We will provide a transparent post-mortem detailing what happened and what steps were taken to prevent recurrence.

---

**Thank you for helping us keep You, Our Customers, and Our Community safe.**
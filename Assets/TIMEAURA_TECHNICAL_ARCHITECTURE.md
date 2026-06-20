# TIMEAURA TECHNICAL ARCHITECTURE

## Executive Summary
TimeAura is a hybrid platform consisting of a Unity mobile app (iOS/Android) for the mystical Nexus experience, and a React/Next.js web portal for professional B2B interactions, freelancer profiles, and contract management. Built to target freelancers, creators, designers, and developers who trade expertise measured in **Horas** (time credits). Monetization combines a premium **Enlightened** subscription (Unity IAP) and a small commission on transactions (Stripe Connect). The unique value proposition lies in an AI‑driven Oracle that acts as a community participant, powered by a three‑layer economy (Fiat ↔ Horas ↔ Premium).

---

## System Architecture
```text
┌─────────────────────┐         ┌─────────────────────┐
│   Unity Mobile      │         │   React Web         │
│   (iOS/Android)     │         │   (Next.js)         │
│   - Mystical UX     │         │   - B2B Portal      │
│   - Oracle Chat     │         │   - Freelancer      │
│   - Chamber         │         │     Profiles        │
│   - Feed            │         │   - Contracts       │
└──────────┬──────────┘         └──────────┬──────────┘
           │                               │
           └───────────────┬───────────────┘
                           │
                           ▼
         ┌─────────────────────────────────┐
         │      Shared Backend API         │
         │   (Firebase Functions / Node.js)│
         └──────────────┬──────────────────┘
                        │
    ┌───────────────────┼───────────────────┐
    │                   │                   │
    ▼                   ▼                   ▼
┌─────────┐      ┌──────────┐       ┌──────────┐
│Alibaba  │      │  Stripe  │       │ Firebase │
│Cloud    │      │  Connect │       │ Firestore│
│(AI/OSS/ │      │ (Escrow) │       │ (Auth,   │
│ CDN)    │      │          │       │  DB)     │
└─────────┘      └──────────┘       └──────────┘
```

## Platform Architecture

TimeAura operates as a hybrid platform with two client interfaces 
sharing a unified backend:

### Mobile App (Unity)
- **Purpose:** Mystical "Nexus" experience with AI Oracle
- **Target users:** Active freelancers, daily interactions
- **Key features:**
  - Oracle Eye (floating AI widget)
  - Sanctuary Panel (full AI chat)
  - Convergence Feed (service requests)
  - Chamber (active pacts workspace)
  - Voice intent parsing
- **Technology:** Unity 6.3 LTS (6000.3.10f1), UI Toolkit, VContainer

### Web Portal (React/Next.js)
- **Purpose:** Professional B2B interactions and contract management
- **Target users:** Business clients, freelancers managing profiles
- **Key features:**
  - Freelancer profiles with portfolios
  - Contract creation and management
  - Escrow dashboard
  - Invoice generation
  - Admin panel for TimeAura team
  - SEO-optimized public pages (for Google indexing)
- **Technology:** Next.js 14, React 18, TypeScript, Tailwind CSS

### Shared Backend
- **API Layer:** Firebase Cloud Functions (or Node.js Express)
- **Database:** Firebase Firestore (NoSQL) + ApsaraDB (relational for contracts)
- **Authentication:** Firebase Auth (unified across mobile + web)
- **Storage:** Alibaba Cloud OSS (user-generated content)
- **AI:** Alibaba Cloud DashScope (Qwen) — accessible from both platforms

---

## AI Integration Layer
**Current integration**
- **IOracleService** – unified interface for all LLM interactions.
- **Providers**:
  - `QwenOracleProvider` – primary provider using Alibaba DashScope (optimized for Slavic languages).
  - `GeminiOracleProvider` – fallback provider (Google Gemini).
- **OracleServiceProvider** – router that selects provider based on language availability and health.
- **Use‑cases**:
  - Voice intent parsing (Voice → Text → Intent).
  - Interactive chat in the Sanctuary panel.
  - Content generation (service descriptions, bios).
  - Conflict arbitration in the Chronos Court.

**Architectural benefits**
- **Modular** – new providers (e.g., Grok, OpenAI) can be added without UI changes.
- **Automatic fallback** – seamless switch when the primary provider is unavailable.
- **Localization aware** – tone adapts per‑user language settings.

---

## Planned Generative Models
1. **Image Generation** (Tongyi Wanxiang / Stable Diffusion)
   - Unique user avatars generated from Aura tags.
   - Service icons for categories (e.g., plumbing, code, design).
   - Illustrated feed posts with a mystical/RPG visual style.
2. **Video Generation**
   - Auto‑created tutorials for onboarding newcomers.
   - Short promotional clips for marketing campaigns.
   - UI animation assets (spell effects, transitions).
3. **Audio Generation**
   - Mystical sound effects for UI interactions.
   - Ambient background music for different panels.
   - Text‑to‑Speech for Oracle responses with a “seer” voice.
4. **Multimodal AI**
   - **Voice + Vision**: user shows a problem image while speaking → AI analyses both streams.
   - **Text + Image**: generate service description from an uploaded picture.

---

## Infrastructure & Scaling
**Current stack**
- **Client**: Unity Mobile (iOS/Android) with UI Toolkit.
- **Config**: Google Sheets Remote Config (temporary).
- **Database**: Firebase Firestore (profiles, posts, deals).
- **Payments**: Stripe Connect (fiat) + Unity IAP (subscriptions).

**Planned Alibaba Cloud expansion**
- **AI Models**: DashScope APIs (Qwen‑Max/Plus for text, Tongyi Wanxiang for images).
- **Storage**: OSS (Object Storage Service) for generated assets (avatars, icons, videos).
- **CDN**: Alibaba Cloud CDN for global, low‑latency delivery.
- **Compute**: Function Compute for server‑side logic (escrow, arbitration, validation).
- **Database**: ApsaraDB (RDS) as a fallback when Firestore scaling limits are reached.

**Scaling roadmap**
| Phase | Mobile Users | Web Users | Tokens/Day | Storage |
|------|--------------|-----------|------------|---------|
| Soft Launch | 1k | 200 | 50k | 10 GB |
| Full Launch | 10k | 2k | 500k | 100 GB |
| Creator Platform | 100k | 20k | 5M | 1 TB |

---

## Technology Stack

### Client — Mobile
- Unity 6.3 LTS (6000.3.10f1)
- UI Toolkit (UXML/USS)
- C# (VContainer for DI)
- Unity IAP (subscriptions)

### Client — Web (NEW)
- Next.js 14 (React framework)
- TypeScript
- Tailwind CSS (styling)
- React Query (data fetching)
- Stripe.js (payments)

### Shared Backend
- Firebase (Firestore, Auth, Cloud Functions)
- Stripe Connect (payments)
- Google Sheets API (configuration)
- Node.js (API layer for web)

### AI/ML
- Alibaba Cloud DashScope (Qwen-Max, Qwen-Plus — text)
- Google Gemini (fallback LLM)
- Planned: Tongyi Wanxiang (image), Video/Audio generation models

### Infrastructure (Alibaba Cloud)
- OSS (object storage)
- CDN (content delivery)
- Function Compute (serverless logic)
- ApsaraDB RDS (relational database)
- API Gateway (unified API for mobile + web)

---

*Prepared for Alibaba Cloud partnership discussion.*

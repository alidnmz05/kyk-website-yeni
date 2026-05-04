# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
npm run dev      # Start dev server at http://localhost:3000
npm run build    # Production build
npm start        # Start production server
```

No test framework is configured yet.

## Architecture

**yurtmenu-yeni** is a mobile-first Next.js 16 (App Router) PWA that displays KYK (Turkish student dining hall) menus by city and meal type. The app is API-driven with no database of its own.

### Tech Stack

- **Next.js 16** with App Router, React 19, TypeScript 5 (strict mode)
- **TailwindCSS v4** (via `@tailwindcss/postcss`)
- **Path alias:** `@/` → `src/`

### Data Flow

```
Browser → /api/proxy/* (Next.js Route Handlers) → Backend at API_BASE (server-side only)
```

The backend is a .NET/Node service. Its URL is exposed only via the `API_BASE` env var (never `NEXT_PUBLIC_`). All frontend data fetching goes through the proxy routes.

**Backend endpoints:**
- `GET /api/city` → `[{ id, name }]`
- `GET /api/menu/liste?cityId=X&mealType=Y&date=YYYY-MM-DD` → menu items

**Caching strategy:**
- Cities: `revalidate: 86400` (24h)
- Menus: `revalidate: 3600` (1h)

### URL Structure

```
/                        → Homepage (city picker + today's menu)
/[citySlug]/kahvalti     → Breakfast menu (mealType: 0)
/[citySlug]/aksam        → Dinner menu (mealType: 1)
/sehirler                → All 81 cities (SEO hub)
/rehber                  → Guide/articles
/sss                     → FAQ
/hakkinda                → About
```

City slugs use Turkish-to-ASCII conversion (`İstanbul` → `istanbul`, `Şanlıurfa` → `sanliurfa`). Alternate slugs (`/sabah`, `/ogle`) redirect 301 to canonical routes.

Default meal type is determined by Turkish local time: before noon → kahvalti, after → aksam.

### Key Types

```typescript
type MenuItem = {
  id: number; date: string; mealType: number; cityId: number;
  first?: string; firstCalories?: string;    // "Item1 / Item2", "210,80"
  second?: string; secondCalories?: string;
  third?: string; thirdCalories?: string;
  fourth?: string; fourthCalories?: string;
  totalCalories?: number;
};
type City = { id: number; name: string; };
```

### Environment Variables

```env
API_BASE=http://localhost:5181           # Backend URL — server-side only
INTERNAL_API_SECRET=<token>             # Proxy route protection
NEXT_PUBLIC_GOOGLE_SITE_VERIFICATION=
NEXT_PUBLIC_ADSENSE_ID=ca-pub-2074568539798437
INDEXNOW_KEY=
```

### Rendering Approach

- City/meal pages: SSG + ISR
- Homepage: SSR
- No client-side state management library; React Context or local state only

### Planned Features (not yet implemented)

See `YENI_UYGULAMA_PROMPT.md` for the full specification. Pending work includes: API proxy routes, dynamic `[citySlug]` pages, PWA manifest + service worker, JSON-LD schema, AdSense integration, IndexNow endpoint, and mobile tab bar UI.

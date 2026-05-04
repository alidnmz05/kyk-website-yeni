# Yeni Uygulama — Referans Prompt

Bu dosya, **KYK Yemek Listesi** API mimarisini temel alarak yeni bir mobil-öncelikli uygulama geliştirmek için eksiksiz bir prompt'tur. Aynı backend API kullanılacak, ancak UI tamamen mobil uygulama gibi hissettirmeli ve 2026 Google + AI agent SEO standartlarını karşılamalıdır.

---

## Hedef

- **Görünüm:** Native mobil uygulama hissi (PWA — yüklenebilir, offline capable)
- **Birincil Hedef Anahtar Kelime:** `[şehir adı] KYK menüsü` → Google ilk sıra
- **İkincil Hedef:** Google AI Overviews, Perplexity, ChatGPT, Gemini gibi AI arama ajanlarında öne çıkmak
- **Stack:** Next.js 15 (App Router) + TypeScript + TailwindCSS
- **Deploy:** Vercel
- **Backend:** Mevcut .NET/Node API (değişmiyor)

---

## Backend API

### Ortam Değişkenleri

```env
# .env.local (geliştirme)
API_BASE=http://localhost:5181          # Sunucu tarafı — tarayıcıya açılmaz
NEXT_PUBLIC_GOOGLE_SITE_VERIFICATION=<kod>
NEXT_PUBLIC_ADSENSE_ID=ca-pub-XXXXXXXXX
INDEXNOW_KEY=<key>
INTERNAL_API_SECRET=<rastgele_uzun_string>  # Proxy route koruma tokeni

# .env.production (Vercel dashboard'a ekle)
API_BASE=http://localhost:5181
```

> ⚠️ **Güvenlik:** `NEXT_PUBLIC_` prefix'ini API URL için KULLANMA. Tarayıcı → Next.js proxy → Backend şeklinde çalış. Gerçek backend URL'si hiç tarayıcıya gitmez.

### Proxy Mimarisi

```
Tarayıcı
  ↓ fetch("/api/proxy/menu?cityId=1&mealType=0")
Next.js Route Handler  (sunucu tarafı)
  ↓ fetch(process.env.API_BASE + "/api/menu/liste?...")
Backend API  ← URL gizli, doğrudan erişilemiyor
```

Proxy route (`src/app/api/proxy/menu/route.ts`):
```ts
const ALLOWED_PARAMS = ["cityId", "mealType", "date"];

export async function GET(request: Request) {
  const incoming = new URL(request.url);
  const safe = new URLSearchParams();
  for (const key of ALLOWED_PARAMS) {
    const val = incoming.searchParams.get(key);
    if (val) safe.set(key, val);
  }
  const upstream = `${process.env.API_BASE}/api/menu/liste?${safe}`;
  const res = await fetch(upstream, { next: { revalidate: 3600 } });
  const data = await res.json();
  return Response.json(data);
}
```

Proxy route (`src/app/api/proxy/city/route.ts`):
```ts
export async function GET() {
  const res = await fetch(`${process.env.API_BASE}/api/city`, {
    next: { revalidate: 86400 },
  });
  const data = await res.json();
  return Response.json(data);
}
```

---

## API Endpoint'leri

### Şehir Listesi
```
GET /api/city
```
**Yanıt:**
```json
[
  { "id": 1, "name": "İstanbul" },
  { "id": 2, "name": "Ankara" }
]
```
- Cache: `revalidate: 86400` (24 saat)
- Proxy üzerinden çağır: `GET /api/proxy/city`

---

### Menü Listesi
```
GET /api/menu/liste?cityId=1&mealType=0&date=2026-05-01
```

**Parametreler:**
| Parametre | Tip    | Zorunlu | Açıklama                        |
|-----------|--------|---------|---------------------------------|
| cityId    | number | ✅      | Şehir ID'si (`/api/city`'den)   |
| mealType  | number | ✅      | 0 = Kahvaltı, 1 = Akşam         |
| date      | string | ❌      | `yyyy-mm-dd` formatı. Yoksa tüm ay gelir |

**Yanıt (array):**
```json
[
  {
    "id": 101,
    "date": "2026-05-01",
    "mealType": 0,
    "cityId": 1,
    "first": "Menemen / Haşlanmış Yumurta",
    "firstCalories": "210,80",
    "second": "Zeytin / Peynir",
    "secondCalories": "45,90",
    "third": "Simit",
    "thirdCalories": "280",
    "fourth": "Çay",
    "fourthCalories": "5",
    "totalCalories": 710
  }
]
```

**Yemek Alanları:** `first`, `second`, `third`, `fourth`  
Her alan `/` ile ayrılmış birden fazla yemek içerebilir: `"Menemen / Haşlanmış Yumurta"`

**Kalori Alanları:** `firstCalories`, `secondCalories`, `thirdCalories`, `fourthCalories`  
Her alan `,` ile ayrılmış değerler içerir (yemeklerle bire bir eşleşir): `"210,80"`

- Cache: `revalidate: 3600` (1 saat)
- Proxy üzerinden çağır: `GET /api/proxy/menu?cityId=1&mealType=0&date=2026-05-01`

---

## Veri Tipleri (TypeScript)

```ts
// Menü satırı
type MenuItem = {
  id: number;
  date: string;          // "yyyy-mm-dd"
  mealType: number;      // 0=kahvaltı, 1=akşam
  cityId: number;
  first?: string;        // "Yemek1 / Yemek2"
  firstCalories?: string; // "210,80"
  second?: string;
  secondCalories?: string;
  third?: string;
  thirdCalories?: string;
  fourth?: string;
  fourthCalories?: string;
  totalCalories?: number;
};

// Şehir
type City = { id: number; name: string };
```

---

## Öğün Tipi Mantığı

```ts
// mealType değerleri
0 → Kahvaltı (sabah)
1 → Akşam yemeği

// URL slug karşılıkları — SADECE iki canonical slug kullan
"kahvalti" → mealType: 0   ✅ canonical
"aksam"    → mealType: 1   ✅ canonical
// "sabah" ve "ogle" slug'larını KULLANMA — duplicate content riski

// Türkiye saatine göre varsayılan öğün
saat < 12  → kahvaltı (0)
saat >= 12 → akşam (1)
```

---

## Şehir Slug Dönüşümü

81 il için Türkçe karakter normalize fonksiyonu:

```ts
function slugifyCity(name: string): string {
  return name
    .toLowerCase()
    .replaceAll("ç","c").replaceAll("ğ","g").replaceAll("ı","i")
    .replaceAll("ö","o").replaceAll("ş","s").replaceAll("ü","u")
    .replace(/\s+/g,"-")
    .replace(/[^a-z0-9-]/g,"")
    .replace(/-+/g,"-");
}

// Örnekler:
// "İstanbul" → "istanbul"
// "Afyonkarahisar" → "afyonkarahisar"
// "Şanlıurfa" → "sanliurfa"
```

---

## URL Yapısı

```
/                          → Ana sayfa (şehir seçimi + bugünkü menü — SSR)
/[citySlug]/kahvalti       → Şehir kahvaltı menüsü (SSG+ISR)
/[citySlug]/aksam          → Şehir akşam menüsü (SSG+ISR)
/sehirler                  → Tüm 81 şehir listesi (SEO hub sayfası)
/rehber                    → KYK rehber içerikleri (bilgi makaleleri)
/sss                       → Sık sorulan sorular (FAQPage schema)
/hakkinda                  → Hakkında
/iletisim                  → İletişim
/gizlilik-politikasi       → Gizlilik politikası
```

**Redirect kuralları (`next.config.ts`):**
```ts
redirects: async () => [
  { source: "/:city/sabah", destination: "/:city/kahvalti", permanent: true },
  { source: "/:city/ogle",  destination: "/:city/aksam",   permanent: true },
],
```

---

## Diğer Entegrasyonlar

### Google AdSense
```
Publisher ID: ca-pub-2074568539798437
```
`next/script` ile `afterInteractive` stratejisiyle yükle (Core Web Vitals etkilenmesin).

### IndexNow (Bing bildirimi)
```
POST /api/indexnow
Body: { "url": "https://siteadi.com/..." }
Env: INDEXNOW_KEY=<key>
```
Route'u koruma altına al: sadece bilinen URL pattern'larını kabul et, rate limiting ekle.

### Google Search Console
```
Env: NEXT_PUBLIC_GOOGLE_SITE_VERIFICATION=<verification_code>
```

---

## Kontrol Listesi (Yeni Uygulama Başlamadan Önce)

- [ ] `API_BASE` env değişkeni `NEXT_PUBLIC_` olmadan tanımlandı
- [ ] `/api/proxy/menu` ve `/api/proxy/city` route'ları oluşturuldu
- [ ] Tüm menü verisi ilk HTML'de (SSG/ISR), client fetch yok
- [ ] PWA manifest ve service worker aktif
- [ ] JSON-LD schema her sayfada (BreadcrumbList + FAQPage)
- [ ] `llms.txt` `public/` klasöründe
- [ ] `robots.txt` AI botlara (GPTBot, ClaudeBot, PerplexityBot) izin veriyor
- [ ] Sitemap sadece canonical URL'leri içeriyor (sabah/ogle yok)
- [ ] Bottom tab bar mobilde görünüyor
- [ ] LCP < 1.8s hedefi test edildi (Lighthouse)
- [ ] `sabah` ve `ogle` slug'ları 301 redirect ediyor

---

## Yeni Uygulama İçin Önerilen Mimari

```
Tarayıcı → Next.js /api/proxy/* → Backend API (gizli URL)
```

Backend URL'yi `NEXT_PUBLIC_` olmayan bir env değişkenine al:
```env
API_BASE=http://localhost:5181   # Tarayıcıya açılmaz
```

---

## Mobil Uygulama Hissi — UI/UX Gereksinimleri

### PWA (Progressive Web App)
Next.js'e `next-pwa` paketi ekle. Uygulama Android/iOS'a yüklenebilir olmalı.

`next.config.ts`:
```ts
import withPWA from "next-pwa";
export default withPWA({
  dest: "public",
  register: true,
  skipWaiting: true,
  disable: process.env.NODE_ENV === "development",
})({ /* diğer Next.js config */ });
```

`public/manifest.json`:
```json
{
  "name": "KYK Yemek Listesi",
  "short_name": "KYK Menü",
  "description": "81 il KYK yurt yemek listesi",
  "start_url": "/",
  "display": "standalone",
  "background_color": "#ffffff",
  "theme_color": "#69C2D3",
  "icons": [
    { "src": "/icon-192.png", "sizes": "192x192", "type": "image/png" },
    { "src": "/icon-512.png", "sizes": "512x512", "type": "image/png" }
  ]
}
```

### Navigasyon Yapısı
- **Bottom tab bar** (mobil native his): Ana Sayfa / Şehirler / Takvim / Ayarlar
- **Sticky header** — şehir adı + öğün switcher
- **Kaydırmalı tarih şeridi** — yatay scroll, bugün vurgulanmış
- **Kart listesi** — menü kartları dikey scroll
- **Pull-to-refresh** desteği
- Tüm dokunma hedefleri minimum 44×44 px
- Sayfa geçişlerinde animasyon (`framer-motion` veya CSS transition)

### Performans (Core Web Vitals 2026)
- **LCP < 1.8s** — menü kartları SSR veya SSG ile ilk HTML'de olmalı, client fetch olmamalı
- **INP < 100ms** — event handler'lar debounce edilmeli
- **CLS = 0** — skeleton placeholder boyutları içerikle aynı olmalı
- **TTFB < 600ms** — ISR (Incremental Static Regeneration) kullan
- Görseller `next/image` ile, format WebP/AVIF
- Font: `next/font` ile self-hosted, `display: swap`
- Kritik CSS inline, JS lazy load

---

## 2026 SEO Gereksinimleri

### 1. Google AI Overviews & AI Agent Optimizasyonu

2026'da aramalar Google AI Overviews, Perplexity, ChatGPT Search, Gemini üzerinden geliyor. Bu ajanlar sayfayı HTML olarak okur; **içerik ilk HTML'de olmalı** (client fetch kabul etmez).

**llms.txt** dosyası oluştur (`public/llms.txt`):
```
# KYK Yemek Listesi

KYK (Kredi ve Yurtlar Kurumu) yurtlarının günlük kahvaltı ve akşam yemek menülerini listeleyen Türkiye'nin kapsamlı yurt menü sitesi.

## İçerik
- 81 il KYK yurt menüsü
- Günlük ve aylık menü takvimi  
- Kahvaltı ve akşam yemeği listeleri
- Şehir bazında filtreleme

## API Kaynağı
Veriler KYK resmi sistemiyle senkronize güncellenir.

## Temel URL'ler
- /[şehir]/kahvalti — şehir kahvaltı menüsü
- /[şehir]/aksam — şehir akşam menüsü
- /sehirler — tüm şehirler
```

**robots.txt** — AI botlara izin ver:
```
User-agent: *
Allow: /

User-agent: GPTBot
Allow: /

User-agent: ClaudeBot
Allow: /

User-agent: PerplexityBot
Allow: /

User-agent: Googlebot
Allow: /

Sitemap: https://siteadi.com/sitemap.xml
```

### 2. Yapısal Veri (JSON-LD Schema)

Her şehir/öğün sayfasına eklenecek schema'lar:

**BreadcrumbList:**
```json
{
  "@context": "https://schema.org",
  "@type": "BreadcrumbList",
  "itemListElement": [
    { "@type": "ListItem", "position": 1, "name": "Ana Sayfa", "item": "https://siteadi.com" },
    { "@type": "ListItem", "position": 2, "name": "İstanbul", "item": "https://siteadi.com/istanbul" },
    { "@type": "ListItem", "position": 3, "name": "Kahvaltı Menüsü", "item": "https://siteadi.com/istanbul/kahvalti" }
  ]
}
```

**FAQPage** (her şehir sayfasına):
```json
{
  "@context": "https://schema.org",
  "@type": "FAQPage",
  "mainEntity": [
    {
      "@type": "Question",
      "name": "İstanbul KYK yurdu bugün kahvaltıda ne var?",
      "acceptedAnswer": {
        "@type": "Answer",
        "text": "İstanbul KYK yurtları bugünkü kahvaltı menüsünü bu sayfadan tarih seçerek görüntüleyebilirsiniz."
      }
    },
    {
      "@type": "Question", 
      "name": "İstanbul KYK menüsü ne zaman güncellenir?",
      "acceptedAnswer": {
        "@type": "Answer",
        "text": "KYK menüleri aylık olarak güncellenir. Sitemizdeki veriler her gün otomatik yenilenir."
      }
    }
  ]
}
```

**WebSite + SearchAction** (ana sayfaya):
```json
{
  "@context": "https://schema.org",
  "@type": "WebSite",
  "name": "KYK Yemek Listesi",
  "url": "https://siteadi.com",
  "potentialAction": {
    "@type": "SearchAction",
    "target": "https://siteadi.com/{city}/kahvalti",
    "query-input": "required name=city"
  }
}
```

### 3. Metadata Stratejisi

**Ana sayfa:**
```ts
title: "KYK Yemek Listesi 2026 | Günlük Yurt Menüsü 81 İl"
description: "KYK yemek listesi bugün ne var? 81 il KYK yurtları kahvaltı ve akşam menüsü. Günlük ve aylık yurt yemek listesi."
```

**Şehir sayfası** (dinamik, SSG):
```ts
// İstanbul için:
title: "İstanbul KYK Menüsü Mayıs 2026 | Yurt Kahvaltı ve Akşam Yemeği"
description: "İstanbul KYK yurtları Mayıs 2026 menüsü. Bugün kahvaltıda ve akşam yemeğinde ne var? Günlük İstanbul yurt yemek listesi."
```

**Canonical URL'ler:** Her sayfa için mutlaka canonical belirt. `sabah`/`ogle` slug'larını `kahvalti`/`aksam`'a 301 redirect yap.

### 4. İçerik Gereksinimleri (E-E-A-T + AI)

Her şehir sayfasının ilk HTML'inde (SSR/SSG) bulunması gerekenler:
- `<h1>` — `[Şehir] KYK Yemek Listesi — [Ay] [Yıl]`
- Bugünkü menü kartları (client fetch değil, SSG veri)
- Şehre özel kısa bilgi paragrafı (unique content, her şehir farklı)
- SSS bölümü (en az 3 soru)
- Diğer şehirlere iç link

**SSG + ISR Yaklaşımı:**
```ts
// page.tsx (server component)
export const revalidate = 3600; // 1 saatte bir yenile

export async function generateStaticParams() {
  // 81 il × 2 öğün = 162 statik sayfa
  return ALL_CITIES_TR.flatMap(city => [
    { city: slugifyCity(city), meal: "kahvalti" },
    { city: slugifyCity(city), meal: "aksam" },
  ]);
}

// Veriyi server'da çek, props olarak component'e ver
export default async function Page({ params }) {
  const { city, meal } = await params;
  const cityData = await fetchCityFromProxy(city);
  const menuData = await fetchMenuFromProxy(cityData.id, mealType, today);
  
  // JSON-LD schema server'da üret
  return (
    <>
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: JSON.stringify(schema) }} />
      <MenuPageClient initialMenus={menuData} city={cityData} />
    </>
  );
}
```

### 5. Sitemap Yapısı

```ts
// sitemap.ts — sadece canonical URL'ler
const urls = [
  { url: "/", priority: 1.0, changeFrequency: "daily" },
  { url: "/sehirler", priority: 0.9, changeFrequency: "weekly" },
  // 81 il × 2 öğün
  ...ALL_CITIES_TR.flatMap(city => [
    { url: `/${slugifyCity(city)}/kahvalti`, priority: 0.85, changeFrequency: "daily" },
    { url: `/${slugifyCity(city)}/aksam`, priority: 0.85, changeFrequency: "daily" },
  ]),
  // Bilgi sayfaları
  { url: "/rehber", priority: 0.7, changeFrequency: "monthly" },
  { url: "/sss", priority: 0.7, changeFrequency: "monthly" },
];
```

### 6. İç Linkleme

- Her şehir sayfasında "Diğer Şehirlerin Menüleri" bölümü (en az 10 şehir linki)
- Ana sayfada tüm 81 il grid olarak linklenmeli
- Footer'da popüler şehirler: İstanbul, Ankara, İzmir, Bursa, Antalya, Konya
- Breadcrumb navigasyon her sayfada

---

## Veri Tipleri (TypeScript)

```ts
type MenuItem = {
  id: number;
  date: string;           // "yyyy-mm-dd"
  mealType: number;       // 0=kahvaltı, 1=akşam
  cityId: number;
  first?: string;         // "Yemek1 / Yemek2"
  firstCalories?: string; // "210,80"
  second?: string;
  secondCalories?: string;
  third?: string;
  thirdCalories?: string;
  fourth?: string;
  fourthCalories?: string;
  totalCalories?: number;
};

type City = { id: number; name: string };
```

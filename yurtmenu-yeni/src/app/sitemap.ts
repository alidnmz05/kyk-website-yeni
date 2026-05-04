import type { MetadataRoute } from "next";
import { ALL_CITIES } from "@/lib/cities";
import { slugifyCity } from "@/lib/utils";

const BASE = process.env.NEXT_PUBLIC_SITE_URL ?? "https://kykyemekliste.com";

export default function sitemap(): MetadataRoute.Sitemap {
  const now = new Date().toISOString();

  const cityUrls = ALL_CITIES.flatMap((name) => [
    {
      url: `${BASE}/${slugifyCity(name)}/kahvalti`,
      changeFrequency: "daily" as const,
      priority: 0.85,
      lastModified: now,
    },
    {
      url: `${BASE}/${slugifyCity(name)}/aksam`,
      changeFrequency: "daily" as const,
      priority: 0.85,
      lastModified: now,
    },
  ]);

  return [
    { url: BASE, changeFrequency: "daily", priority: 1.0, lastModified: now },
    { url: `${BASE}/sehirler`, changeFrequency: "weekly", priority: 0.9, lastModified: now },
    { url: `${BASE}/sss`, changeFrequency: "monthly", priority: 0.7, lastModified: now },
    { url: `${BASE}/hakkinda`, changeFrequency: "monthly", priority: 0.5, lastModified: now },
    { url: `${BASE}/gizlilik-politikasi`, changeFrequency: "monthly", priority: 0.3, lastModified: now },
    ...cityUrls,
  ];
}

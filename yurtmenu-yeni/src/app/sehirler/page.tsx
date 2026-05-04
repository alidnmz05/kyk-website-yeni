import type { Metadata } from "next";
import Link from "next/link";
import { fetchCities } from "@/lib/api";
import { slugifyCity, defaultMealSlug, getCurrentMonthName, getCurrentYear } from "@/lib/utils";

const SITE_URL = process.env.NEXT_PUBLIC_SITE_URL ?? "https://kykyemekliste.com";

export const metadata: Metadata = {
  title: "Tüm Şehirlerin KYK Yemek Listesi ve Yurt Menüleri",
  description:
    "Türkiye'deki 81 ilin KYK yurt yemek listesine ulaşın. Şehir seçerek günlük kahvaltı ve akşam KYK yemek menüsü detaylarını görüntüleyin.",
  openGraph: {
    title: "Tüm Şehirlerin KYK Yemek Listesi",
    description: "Türkiye'deki 81 ilin KYK yurt yemek listesi. Şehir seçerek günlük kahvaltı ve akşam menüsüne ulaşın.",
    images: [{ url: `${SITE_URL}/og-default.png`, width: 1200, height: 630, alt: "KYK 81 İl Menüsü" }],
  },
};

export const revalidate = 86400;

export default async function SehirlerPage() {
  const cities = await fetchCities();
  const meal = defaultMealSlug();
  const monthName = getCurrentMonthName();
  const year = getCurrentYear();

  const sortedCities = [...cities].sort((a, b) => a.name.localeCompare(b.name, "tr"));

  const itemListSchema = {
    "@context": "https://schema.org",
    "@type": "ItemList",
    name: "Türkiye KYK Yurt Menüleri — Tüm Şehirler",
    description: "Türkiye'deki tüm illerin KYK yurt yemek listesi",
    numberOfItems: sortedCities.length,
    itemListElement: sortedCities.map((city, idx) => ({
      "@type": "ListItem",
      position: idx + 1,
      name: `${city.name} KYK Menüsü`,
      url: `${SITE_URL}/${slugifyCity(city.name)}/kahvalti`,
    })),
  };

  return (
    <>
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(itemListSchema) }}
      />

      <header className="sticky top-0 z-50 brand-gradient px-4 pt-6 pb-3 shadow-md shadow-brand/10 rounded-b-3xl mb-4">
        <div className="flex items-center gap-3">
          <Link href="/" className="w-8 h-8 flex items-center justify-center bg-white/20 rounded-full text-white hover:bg-white/30 backdrop-blur-md transition-colors">
            <span className="text-lg leading-none transform -translate-x-0.5">←</span>
          </Link>
          <div>
            <h1 className="text-white font-bold text-xl drop-shadow-sm">Tüm Şehirler</h1>
            <p className="text-white/80 text-xs font-medium">81 il KYK menüsü — {monthName} {year}</p>
          </div>
        </div>
      </header>

      <div className="px-4 py-4">
        {sortedCities.length === 0 ? (
          <div className="glass-panel rounded-3xl p-10 text-center flex flex-col items-center justify-center min-h-[200px]">
            <div className="w-16 h-16 bg-slate-100 rounded-full flex items-center justify-center mb-4">
              <span className="text-3xl">🔌</span>
            </div>
            <p className="font-bold text-slate-800 text-lg mb-1">Şehirler Yüklenemedi</p>
            <p className="text-sm text-slate-500">Backend API şu an kapalı. Lütfen daha sonra tekrar deneyin.</p>
          </div>
        ) : (
          <div className="grid grid-cols-2 gap-3">
            {sortedCities.map((city) => (
              <Link
                key={city.id}
                href={`/${slugifyCity(city.name)}/${meal}`}
                className="group flex items-center justify-between glass-panel rounded-2xl px-4 py-3.5 shadow-sm hover:shadow-md hover:border-brand/40 transition-all active:scale-[0.98]"
              >
                <span className="text-[13px] font-bold text-slate-700 group-hover:text-brand transition-colors">{city.name}</span>
                <span className="text-brand/50 group-hover:text-brand transition-colors group-hover:translate-x-0.5 transform">→</span>
              </Link>
            ))}
          </div>
        )}
      </div>
    </>
  );
}

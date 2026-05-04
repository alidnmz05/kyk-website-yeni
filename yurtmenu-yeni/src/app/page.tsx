import type { Metadata } from "next";
import Link from "next/link";
import { fetchCities, fetchMenu } from "@/lib/api";
import { slugifyCity, defaultMealSlug, getCurrentMonthName, getCurrentYear, todayString, mealTypeFromSlug } from "@/lib/utils";
import { POPULAR_CITIES } from "@/lib/cities";
import CitySearch from "@/components/CitySearch";
import HomeMenuWidget from "@/components/HomeMenuWidget";

export const metadata: Metadata = {
  title: "KYK Yemek Listesi 2026: Bugün Ne Var? | Güncel Menü",
  description: "81 ilin en güncel KYK yemek listesi. Ankara, İstanbul, İzmir ve tüm illerin günlük KYK yurt menüsünü buradan takip edin.",
};

export const revalidate = 3600;

export default async function HomePage() {
  const cities = await fetchCities();
  const meal = defaultMealSlug();
  const mealType = mealTypeFromSlug(meal);
  const mealLabel = meal === "kahvalti" ? "Kahvaltı" : "Akşam Yemeği";
  const monthName = getCurrentMonthName();
  const year = getCurrentYear();
  const date = todayString();

  const popularWithData = POPULAR_CITIES.map((name) => ({
    name,
    slug: slugifyCity(name),
  }));

  // Fetch default Ankara menu for the widget
  let fallbackMenu = null;
  const ankara = cities.find(c => slugifyCity(c.name) === "ankara");
  if (ankara) {
    const menus = await fetchMenu(ankara.id, mealType, date);
    fallbackMenu = menus.find((m: any) => m.date === date) ?? menus[0] ?? null;
  }


  const schema = {
    "@context": "https://schema.org",
    "@type": "WebSite",
    name: "KYK Yemek Listesi",
    url: process.env.NEXT_PUBLIC_SITE_URL ?? "https://kykyemekliste.com",
    potentialAction: {
      "@type": "SearchAction",
      target: {
        "@type": "EntryPoint",
        urlTemplate: `${process.env.NEXT_PUBLIC_SITE_URL ?? "https://kykyemekliste.com"}/{city}/kahvalti`,
      },
      "query-input": "required name=city",
    },
  };

  return (
    <>
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(schema) }}
      />

      {/* Header */}
      <header className="sticky top-0 z-50 brand-gradient px-5 pt-8 pb-4 shadow-md shadow-brand/10 rounded-b-3xl">
        <h1 className="text-white text-3xl font-extrabold mb-0.5 drop-shadow-sm tracking-tight">KYK Menü</h1>
        <p className="text-white/80 text-[13px] font-medium mb-4">Türkiye'nin En Kapsamlı Yurt Yemek Listesi</p>
        <div className="relative">
          {cities.length > 0 ? (
            <CitySearch cities={cities} />
          ) : (
            <div className="w-full px-5 py-3.5 bg-white/20 backdrop-blur-md rounded-2xl text-white/70 text-sm font-medium border border-white/20 shadow-inner">
              Şehir ara...
            </div>
          )}
        </div>
      </header>

      {ankara && (
        <HomeMenuWidget 
          fallbackCity={{ name: ankara.name, slug: "ankara" }} 
          fallbackMenu={fallbackMenu} 
          fallbackMealLabel={mealLabel}
        />
      )}


      <div className="px-4 py-5 space-y-6">
        {/* Popüler şehirler */}
        <section>
          <h2 className="text-[11px] font-bold text-slate-400 uppercase tracking-wider mb-4 flex items-center gap-2">
            <span className="w-4 h-[1px] bg-slate-300"></span> Popüler Şehirler
          </h2>
          <div className="grid grid-cols-2 gap-3">
            {popularWithData.map(({ name, slug }) => (
              <Link
                key={slug}
                href={`/${slug}/${meal}`}
                className="group flex items-center justify-between glass-panel rounded-2xl px-4 py-3.5 shadow-sm hover:shadow-md hover:border-brand/40 transition-all active:scale-[0.98]"
              >
                <span className="text-[13px] font-bold text-slate-700 group-hover:text-brand transition-colors">{name}</span>
                <span className="text-brand/50 group-hover:text-brand transition-colors group-hover:translate-x-0.5 transform">→</span>
              </Link>
            ))}
          </div>
        </section>

        {/* Tüm şehirler */}
        <Link
          href="/sehirler"
          className="group flex items-center justify-between brand-gradient rounded-2xl px-5 py-4 shadow-md shadow-brand/20 active:scale-[0.98] transition-all"
        >
          <div>
            <p className="text-sm font-bold text-white tracking-wide">Tüm 81 İli Gör</p>
            <p className="text-xs text-white/80 font-medium mt-1">Her şehrin menüsüne ulaş</p>
          </div>
          <div className="w-10 h-10 bg-white/20 rounded-full flex items-center justify-center backdrop-blur-sm group-hover:scale-110 transition-transform">
            <span className="text-xl">🗺️</span>
          </div>
        </Link>

        {/* Kapsamlı SEO İçeriği */}
        <div className="space-y-4">
          <section className="glass-panel rounded-3xl p-6 border border-white/40">
            <h2 className="font-bold text-slate-800 text-lg mb-3 flex items-center gap-2">
              <span className="text-brand">ℹ️</span> KYK Yemek ve Yurt Menüleri Hakkında
            </h2>
            <div className="space-y-3 text-[13px] text-slate-600 font-medium leading-relaxed">
              <p>
                Türkiye genelindeki devlet yurtlarında kalan öğrenciler için günlük <strong>KYK yemek listesi</strong>, sabah kahvaltısı ve akşam yemeği olarak iki ana öğün şeklinde sunulmaktadır. Öğrenciler tarafından her gün en çok aranan <strong>KYK yemek</strong>, <strong>yurt menü</strong> ve <strong>KYK menü</strong> detaylarına platformumuz üzerinden tek tıkla ulaşabilirsiniz.
              </p>
              <p>
                KYK yurtlarında sunulan yemekler, öğrencilerin günlük kalori ihtiyaçları hesaplanarak diyetisyenler eşliğinde hazırlanır. Sistemimiz üzerinden sadece bugünün değil, geçmiş ve gelecek günlerdeki <strong>KYK yemek menüsü</strong> bilgilerine de erişebilir, yediğiniz öğünlerin toplam kalori değerlerini inceleyebilirsiniz.
              </p>
            </div>
          </section>

          <section className="glass-panel rounded-3xl p-6 border border-white/40">
            <h2 className="font-bold text-slate-800 text-lg mb-3">KYK Yemek Saatleri ve İşleyiş</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div className="bg-white/50 rounded-2xl p-4">
                <h3 className="font-bold text-brand mb-1">Sabah Kahvaltısı</h3>
                <p className="text-xs text-slate-600 font-medium leading-relaxed">
                  Kahvaltı servisi genellikle hafta içi 06:30 - 12:00, hafta sonu ise 06:30 - 12:30 saatleri arasında yapılmaktadır. Standart kahvaltı tabağı ve seçmeli ürünler sunulur.
                </p>
              </div>
              <div className="bg-white/50 rounded-2xl p-4">
                <h3 className="font-bold text-brand mb-1">Akşam Yemeği</h3>
                <p className="text-xs text-slate-600 font-medium leading-relaxed">
                  Akşam yemeği servisi 16:00 - 22:30 saatleri arasındadır. Menüde genellikle çorba, ana yemek, yardımcı yemek (pilav/makarna) ve tatlı/meyve/salata bulunur.
                </p>
              </div>
            </div>
          </section>
        </div>
      </div>
    </>
  );
}

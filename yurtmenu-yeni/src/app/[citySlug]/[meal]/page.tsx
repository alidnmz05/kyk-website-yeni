import { notFound } from "next/navigation";
import Link from "next/link";
import { fetchCities, fetchMenu } from "@/lib/api";
import {
  slugifyCity,
  mealTypeFromSlug,
  findCityBySlug,
  findCityNameBySlug,
  todayString,
  formatDateTR,
  getCurrentMonthName,
  getCurrentYear,
} from "@/lib/utils";
import { ALL_CITIES } from "@/lib/cities";
import MenuCard from "@/components/MenuCard";
import MealTabs from "@/components/MealTabs";
import DateStrip from "@/components/DateStrip";
import CityTracker from "@/components/CityTracker";

export const dynamic = "force-dynamic";

type Props = {
  params: Promise<{ citySlug: string; meal: string }>;
  searchParams: Promise<{ date?: string }>;
};

export async function generateMetadata({ params, searchParams }: Props) {
  const { citySlug, meal } = await params;
  const { date: dateParam } = await searchParams;
  const cityName = findCityNameBySlug(citySlug);
  const mealLabel = meal === "kahvalti" ? "Kahvaltı" : "Akşam Yemeği";
  const date = dateParam || todayString();
  const dateFormatted = formatDateTR(date);
  const SITE_URL = process.env.NEXT_PUBLIC_SITE_URL ?? "https://kykyemekliste.com";
  const ogImage = `${SITE_URL}/og-default.png`;
  const description = `${cityName} KYK yemek listesi bugün ne var? ${cityName} KYK yurtları ${dateFormatted} günü ${mealLabel.toLowerCase()} menüsü ve güncel yemek listesi.`;

  return {
    title: `${cityName} KYK Yemek: Bugün Ne Var? | ${dateFormatted}`,
    description,
    alternates: {
      canonical: `/${citySlug}/${meal}${dateParam ? `?date=${dateParam}` : ""}`,
    },
    openGraph: {
      title: `${cityName} KYK Yemek Listesi — ${dateFormatted}`,
      description,
      images: [{ url: ogImage, width: 1200, height: 630, alt: `${cityName} KYK Yemek Listesi` }],
    },
    twitter: {
      card: "summary_large_image",
      title: `${cityName} KYK Yemek Menüsü`,
      description,
      images: [ogImage],
    },
  };
}





export default async function CityMealPage({ params, searchParams }: Props) {
  const { citySlug, meal } = await params;
  const { date: dateParam } = await searchParams;

  if (meal !== "kahvalti" && meal !== "aksam") notFound();

  const today = todayString();
  const date = dateParam ?? today;
  const mealType = mealTypeFromSlug(meal);
  const mealLabel = meal === "kahvalti" ? "Kahvaltı" : "Akşam Yemeği";
  const cityName = findCityNameBySlug(citySlug);

  const cities = await fetchCities();
  const city = findCityBySlug(cities, citySlug);

  if (!city) {
    return (
      <>
        <header className="sticky top-0 z-50 brand-gradient px-4 pt-10 pb-4 flex items-center gap-3 shadow-md rounded-b-3xl">
          <Link href="/" className="w-8 h-8 flex items-center justify-center bg-white/20 rounded-full text-white hover:bg-white/30 backdrop-blur-md transition-colors">
            <span className="text-lg leading-none transform -translate-x-0.5">←</span>
          </Link>
          <h1 className="text-white font-bold text-lg">{cityName}</h1>
        </header>
        <div className="px-4 py-8 text-center text-gray-500">
          <p className="text-4xl mb-3">🔌</p>
          <p className="font-medium text-gray-700">Sunucu bağlantısı kurulamadı</p>
          <p className="text-sm mt-1">Backend API şu an kapalı. Lütfen daha sonra tekrar deneyin.</p>
        </div>
      </>
    );
  }

  const menus = await fetchMenu(city.id, mealType, date);
  const menuItem = menus.find((m: any) => m.date === date) ?? menus[0] ?? null;

  const SITE_URL = process.env.NEXT_PUBLIC_SITE_URL ?? "https://kykyemekliste.com";

  const breadcrumbSchema = {
    "@context": "https://schema.org",
    "@type": "BreadcrumbList",
    itemListElement: [
      { "@type": "ListItem", position: 1, name: "Ana Sayfa", item: SITE_URL },
      { "@type": "ListItem", position: 2, name: cityName, item: `${SITE_URL}/${citySlug}/kahvalti` },
      { "@type": "ListItem", position: 3, name: `${mealLabel} Menüsü`, item: `${SITE_URL}/${citySlug}/${meal}` },
    ],
  };

  const faqSchema = {
    "@context": "https://schema.org",
    "@type": "FAQPage",
    mainEntity: [
      {
        "@type": "Question",
        name: `${cityName} KYK yurdu bugün ${meal === "kahvalti" ? "kahvaltıda" : "akşam yemeğinde"} ne var?`,
        acceptedAnswer: {
          "@type": "Answer",
          text: `${cityName} KYK yurtları bugünkü ${mealLabel.toLowerCase()} menüsünü bu sayfadan tarih seçerek görüntüleyebilirsiniz.`,
        },
      },
      {
        "@type": "Question",
        name: `${cityName} KYK menüsü ne zaman güncellenir?`,
        acceptedAnswer: {
          "@type": "Answer",
          text: "KYK menüleri aylık olarak güncellenir. Sitemizdeki veriler her gün otomatik yenilenir.",
        },
      },
      {
        "@type": "Question",
        name: "KYK yurt yemeği kaç kalori?",
        acceptedAnswer: {
          "@type": "Answer",
          text: "KYK yurt yemeklerinin toplam kalori değeri öğüne göre değişmektedir. Her menü kartında toplam kalori bilgisi gösterilmektedir.",
        },
      },
    ],
  };

  // ItemList Schema — menü öğelerini yapısal veriyle işaretle (rich snippet)
  const courses = [
    { key: "first", calKey: "firstCalories" },
    { key: "second", calKey: "secondCalories" },
    { key: "third", calKey: "thirdCalories" },
    { key: "fourth", calKey: "fourthCalories" },
  ] as const;

  const itemListElements = menuItem
    ? courses
        .map(({ key, calKey }, idx) => {
          const val = menuItem[key as keyof typeof menuItem] as string | undefined;
          const cal = menuItem[calKey as keyof typeof menuItem] as string | undefined;
          if (!val) return null;
          return {
            "@type": "ListItem",
            position: idx + 1,
            name: val.split("/")[0].trim(),
            description: cal ? `${cal.split(",")[0]} kcal` : undefined,
          };
        })
        .filter(Boolean)
    : [];

  const itemListSchema = menuItem
    ? {
        "@context": "https://schema.org",
        "@type": "ItemList",
        name: `${cityName} KYK ${mealLabel} Menüsü — ${formatDateTR(date)}`,
        description: `${cityName} KYK yurtları ${formatDateTR(date)} ${mealLabel.toLowerCase()} yemek listesi`,
        numberOfItems: itemListElements.length,
        itemListElement: itemListElements,
      }
    : null;

  return (
    <>
      <CityTracker slug={citySlug} />
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: JSON.stringify(breadcrumbSchema) }} />
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: JSON.stringify(faqSchema) }} />
      {itemListSchema && (
        <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: JSON.stringify(itemListSchema) }} />
      )}

      {/* Sticky header */}
      <header className="sticky top-0 z-50 brand-gradient px-4 pt-6 pb-3 shadow-md shadow-brand/10 rounded-b-3xl mb-4">
        <div className="flex items-center justify-between mb-2">
          <div className="flex items-center gap-3">
            <Link href="/" className="w-8 h-8 flex items-center justify-center bg-white/20 rounded-full text-white hover:bg-white/30 backdrop-blur-md transition-colors">
              <span className="text-lg leading-none transform -translate-x-0.5">←</span>
            </Link>
            <div>
              <h1 className="text-white font-bold text-xl leading-tight drop-shadow-sm">{cityName} KYK Menüsü</h1>
              <p className="text-white/80 text-xs font-medium">{formatDateTR(date)} - {mealLabel} Yemek Listesi</p>
            </div>
          </div>
        </div>
        <div className="-mx-4">
          <MealTabs citySlug={citySlug} activeMeal={meal} date={date} />
        </div>
      </header>

      {/* Date strip */}
      <DateStrip citySlug={citySlug} mealSlug={meal} selectedDate={date} />

      {/* Menu content */}
      <div className="px-4 py-4 space-y-4">
        {menuItem ? (
          <MenuCard item={menuItem} mealLabel={mealLabel} />
        ) : (
          <div className="glass-panel rounded-3xl p-10 text-center flex flex-col items-center justify-center min-h-[200px]">
            <div className="w-16 h-16 bg-slate-100 rounded-full flex items-center justify-center mb-4">
              <span className="text-3xl">🍽️</span>
            </div>
            <p className="font-bold text-slate-800 text-lg mb-1">Menü Bulunamadı</p>
            <p className="text-sm text-slate-500">Seçtiğiniz tarih için henüz menü girilmemiş veya güncelleniyor.</p>
          </div>
        )}

        {/* Breadcrumb nav */}
        <nav className="text-xs text-gray-400 flex items-center gap-1 flex-wrap pt-2">
          <Link href="/" className="hover:text-brand transition-colors">Ana Sayfa</Link>
          <span>/</span>
          <Link href="/sehirler" className="hover:text-brand transition-colors">Şehirler</Link>
          <span>/</span>
          <span className="text-gray-600">{cityName}</span>
          <span>/</span>
          <span className="text-gray-600">{mealLabel}</span>
        </nav>

        {/* Kapsamlı SEO İçeriği */}
        <div className="space-y-4">
          <section className="glass-panel rounded-3xl p-6 border border-white/40">
            <h2 className="font-bold text-slate-800 text-lg mb-3 flex items-center gap-2">
              <span className="text-brand">ℹ️</span> {cityName} KYK Yemek ve Yurt Menüsü Hakkında
            </h2>
            <div className="space-y-3 text-[13px] text-slate-600 font-medium leading-relaxed">
              <p>
                {cityName} ilindeki Kredi ve Yurtlar Kurumu (KYK) yurtlarında kalan öğrenciler için her gün <strong>{cityName} KYK yemek listesi</strong> düzenli olarak güncellenmektedir. Öğrenciler sabah kahvaltısı ve akşam yemeği olmak üzere iki ana öğün üzerinden beslenme ihtiyaçlarını karşılar. Eğer <strong>{cityName} KYK menü</strong> detaylarını merak ediyorsanız, bulunduğunuz sayfa üzerinden bugünün ve diğer günlerin detaylı yemek listesine ve menünün toplam kalori değerlerine kolayca ulaşabilirsiniz.
              </p>
              <p>
                Diyetisyenler tarafından planlanan <strong>{cityName} yurt yemekleri</strong>, öğrencilerin sağlıklı beslenmesine katkıda bulunmak amacıyla özel olarak hazırlanmaktadır. Yukarıdaki tarih çubuğunu kullanarak geçmiş tarihlere veya gelecek günlerin <strong>{cityName} yurt menü</strong> bilgilerine erişebilir, yemeğin içeriği hakkında önceden bilgi sahibi olabilirsiniz.
              </p>
            </div>
          </section>

          <section className="glass-panel rounded-3xl p-6 border border-white/40">
            <h2 className="font-bold text-slate-800 text-lg mb-3">{cityName} KYK Yemek Saatleri</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div className="bg-white/50 rounded-2xl p-4 border border-slate-100">
                <h3 className="font-bold text-brand mb-1 flex items-center gap-2">
                  <span>🍳</span> Sabah Kahvaltısı
                </h3>
                <p className="text-xs text-slate-600 font-medium leading-relaxed">
                  {cityName} KYK yurtlarında sabah kahvaltısı genellikle hafta içi 06:30 - 12:00, hafta sonu ise 06:30 - 12:30 saatleri arasında sunulmaktadır.
                </p>
              </div>
              <div className="bg-white/50 rounded-2xl p-4 border border-slate-100">
                <h3 className="font-bold text-brand mb-1 flex items-center gap-2">
                  <span>🍲</span> Akşam Yemeği
                </h3>
                <p className="text-xs text-slate-600 font-medium leading-relaxed">
                  {cityName} yurtlarında akşam yemeği servisi 16:00 - 22:30 saatleri arasında yapılmaktadır. Günlük menüde çorba, ana yemek ve tatlı/meyve gibi seçenekler bulunur.
                </p>
              </div>
            </div>
          </section>
        </div>

        {/* Diğer şehirler */}
        <section className="pb-6">
          <h2 className="text-[11px] font-bold text-slate-400 uppercase tracking-wider mb-4 flex items-center gap-2">
            <span className="w-4 h-[1px] bg-slate-300"></span> Diğer Şehirlerin Menüleri
          </h2>
          <div className="grid grid-cols-3 gap-2.5">
            {cities
              .filter((c) => slugifyCity(c.name) !== citySlug)
              .slice(0, 9)
              .map((c) => (
                <Link
                  key={c.id}
                  href={`/${slugifyCity(c.name)}/${meal}`}
                  className="bg-white rounded-2xl px-2 py-3 text-[11px] text-center font-semibold text-slate-700 shadow-sm border border-slate-100 hover:border-brand/40 hover:text-brand hover:shadow-md transition-all truncate"
                >
                  {c.name}
                </Link>
              ))}
          </div>
        </section>
      </div>
    </>
  );
}

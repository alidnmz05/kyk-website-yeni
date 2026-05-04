import type { Metadata } from "next";
import Link from "next/link";

const SITE_URL = process.env.NEXT_PUBLIC_SITE_URL ?? "https://kykyemekliste.com";

export const metadata: Metadata = {
  title: "Sık Sorulan Sorular | KYK Yemek Listesi",
  description: "KYK yurt yemek listesi hakkında sık sorulan sorular ve cevaplar. KYK menüsü, kalori bilgileri, yemek saatleri ve daha fazlası.",
  openGraph: {
    title: "KYK Yemek Listesi — Sık Sorulan Sorular",
    description: "KYK yurt menüsü hakkında merak ettiğiniz her şey: yemek saatleri, kalori değerleri, menü güncelleme sıklığı.",
    images: [{ url: `${SITE_URL}/og-default.png`, width: 1200, height: 630, alt: "KYK SSS" }],
  },
};

const faqs = [
  {
    q: "KYK menüsü ne zaman güncellenir?",
    a: "KYK yurt menüleri aylık olarak Kredi ve Yurtlar Kurumu tarafından belirlenir. Sitemizdeki veriler her saat otomatik olarak yenilenmektedir. Yeni ay menüsü genellikle önceki ayın son haftasında sisteme yüklenir.",
  },
  {
    q: "KYK yemek listesi bugün ne var?",
    a: "Bugünün KYK menüsünü görmek için anasayfadan şehrinizi seçin veya doğrudan şehir adını arama çubuğuna yazın. Kahvaltı ve akşam yemeği seçenekleri tarih çubuğundan seçilebilir.",
  },
  {
    q: "Kahvaltı ve akşam yemeği saatleri nedir?",
    a: "KYK yurtlarında sabah kahvaltısı genellikle hafta içi 06:30–12:00, hafta sonu 06:30–12:30 saatleri arasında servis edilmektedir. Akşam yemeği ise 16:00–22:30 arasında sunulmaktadır. Saatler yurda ve şehre göre değişebilir.",
  },
  {
    q: "Tüm illerde aynı KYK menüsü mü uygulanıyor?",
    a: "Hayır. Her şehrin KYK yurtları kendi bölgesel menüsünü uygulayabilir. Bu nedenle İstanbul KYK menüsü ile Ankara, İzmir veya Bursa KYK menüsü farklı olabilmektedir. Şehrinizi seçerek size özel menüyü görüntüleyin.",
  },
  {
    q: "KYK yurt yemeği kaç kalori?",
    a: "KYK kahvaltısı yaklaşık 400–650 kcal, akşam yemeği ise 600–900 kcal arasında değişmektedir. Her menü kartında toplam kalori bilgisi ve her yemeğin ayrı ayrı kalori değeri gösterilmektedir. Bu değerler KYK'nın resmi verilerine dayanmaktadır.",
  },
  {
    q: "Menüde kalori bilgisi doğru mu?",
    a: "Kalori değerleri KYK'nın resmi verilerine dayanmaktadır. Pişirme yöntemi, porsiyon büyüklüğü ve malzeme farklılıklarına göre gerçek değerler hafifçe değişebilir.",
  },
  {
    q: "Geçmiş tarihlerin KYK menüsüne bakabilir miyim?",
    a: "Evet. Şehir sayfasındaki tarih çubuğunu kullanarak geçmiş tarihlerin menülerine ve mevcut ay içindeki gelecek günlerin menülerine kolayca ulaşabilirsiniz.",
  },
  {
    q: "KYK yemeği ücretsiz mi?",
    a: "KYK yurtlarında kalan burslu öğrenciler için kahvaltı ve akşam yemeği ücretsiz olarak sunulmaktadır. Misafir veya farklı statüdeki kullanıcılar için ücret politikası yurda göre değişebilir.",
  },
  {
    q: "Veriler nereden geliyor?",
    a: "Veriler KYK'nın resmi sistemiyle entegre bir API aracılığıyla alınmakta ve her saat yenilenmektedir. Herhangi bir tutarsızlık fark ederseniz lütfen bize bildirin.",
  },
  {
    q: "Ankara KYK menüsü nasıl öğrenilir?",
    a: "Anasayfada 'Ankara' şehrine tıklayarak ya da doğrudan kykyemekliste.com/ankara/kahvalti veya kykyemekliste.com/ankara/aksam adreslerine giderek Ankara KYK kahvaltı ve akşam menüsüne ulaşabilirsiniz.",
  },
  {
    q: "İstanbul KYK menüsü nasıl görüntülenir?",
    a: "Anasayfadan 'İstanbul' seçeneğine tıklayarak ya da kykyemekliste.com/istanbul/kahvalti bağlantısından İstanbul KYK yurt menüsüne doğrudan erişebilirsiniz.",
  },
  {
    q: "KYK menüsünde hangi yemekler var?",
    a: "KYK kahvaltısı genellikle omlet, peynir, zeytin, ekmek çeşitleri, reçel ve tereyağından oluşur. Akşam yemeğinde çorba, et/tavuk/sebze yemeği, pilav veya makarna ve tatlı/meyve/salata sunulmaktadır. Detaylar şehre ve güne göre farklılık gösterir.",
  },
  {
    q: "KYK menüsüne aylık olarak bakabilir miyim?",
    a: "Evet. Şehir menü sayfasındaki tarih çubuğu üzerinden ayın tüm günlerine ait menüleri inceleyebilirsiniz. Bir sonraki aya ait menü yüklendiğinde otomatik olarak görüntülenecektir.",
  },
  {
    q: "KYK yurt yemeklerini beğenmediğimde ne yapabilirim?",
    a: "KYK yurtlarında öğrenci memnuniyetine ilişkin geri bildirimler yurt müdürlüğüne ya da KYK'nın resmi şikayet hattına iletebilirsiniz. Site olarak yemek kalitesine müdahale etme yetkimiz bulunmamaktadır.",
  },
  {
    q: "Site mobilde çalışıyor mu?",
    a: "Evet. KYK Yemek Listesi, mobil öncelikli tasarımıyla tüm akıllı telefon ve tabletlerde sorunsuz çalışmaktadır. Ayrıca Progressive Web App (PWA) özelliği sayesinde ana ekranınıza ekleyerek uygulama gibi kullanabilirsiniz.",
  },
  {
    q: "Uygulamayı ana ekranıma nasıl eklerim?",
    a: "Android'de Chrome tarayıcısında siteyi açın, sağ üstteki menüden 'Ana ekrana ekle'yi seçin. iOS'ta Safari ile açın, paylaş butonuna dokunup 'Ana Ekrana Ekle'yi seçin. Bu sayede internet bağlantısı olmadan da son görüntülenen menüye erişebilirsiniz.",
  },
];

export default function SSSPage() {
  const schema = {
    "@context": "https://schema.org",
    "@type": "FAQPage",
    mainEntity: faqs.map(({ q, a }) => ({
      "@type": "Question",
      name: q,
      acceptedAnswer: { "@type": "Answer", text: a },
    })),
  };

  return (
    <>
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(schema) }}
      />

      <header className="sticky top-0 z-50 brand-gradient px-4 pt-6 pb-3 shadow-md shadow-brand/10 rounded-b-3xl mb-4">
        <div className="flex items-center gap-3">
          <Link href="/" className="w-8 h-8 flex items-center justify-center bg-white/20 rounded-full text-white hover:bg-white/30 backdrop-blur-md transition-colors">
            <span className="text-lg leading-none transform -translate-x-0.5">←</span>
          </Link>
          <div>
            <h1 className="text-white font-bold text-xl drop-shadow-sm">Sık Sorulan Sorular</h1>
            <p className="text-white/80 text-xs font-medium">KYK menüsü hakkında her şey</p>
          </div>
        </div>
      </header>

      <div className="px-4 py-4 space-y-3">
        {faqs.map(({ q, a }, i) => (
          <div key={i} className="bg-white rounded-2xl p-4 shadow-sm border border-gray-100">
            <h2 className="font-semibold text-gray-800 mb-2 text-sm">{q}</h2>
            <p className="text-sm text-gray-600 leading-relaxed">{a}</p>
          </div>
        ))}
      </div>
    </>
  );
}


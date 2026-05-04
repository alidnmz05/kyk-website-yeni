import type { Metadata } from "next";
import Link from "next/link";

export const metadata: Metadata = {
  title: "Gizlilik Politikası | KYK Yemek Listesi",
};

export default function GizlilikPage() {
  return (
    <>
      <header className="sticky top-0 z-50 brand-gradient px-4 pt-10 pb-5 shadow-md shadow-brand/10 rounded-b-3xl mb-4">
        <div className="flex items-center gap-3">
          <Link href="/hakkinda" className="w-8 h-8 flex items-center justify-center bg-white/20 rounded-full text-white hover:bg-white/30 backdrop-blur-md transition-colors">
            <span className="text-lg leading-none transform -translate-x-0.5">←</span>
          </Link>
          <h1 className="text-white font-bold text-xl drop-shadow-sm">Gizlilik Politikası</h1>
        </div>
      </header>

      <div className="px-4 py-4 space-y-4">
        <div className="bg-white rounded-2xl p-5 shadow-sm border border-gray-100 space-y-4 text-sm text-gray-600 leading-relaxed">
          <section>
            <h2 className="font-semibold text-gray-800 mb-2">Toplanan Veriler</h2>
            <p>Bu uygulama, kişisel veri toplamamaktadır. Şehir seçimi gibi tercihler yalnızca tarayıcınızda saklanır.</p>
          </section>
          <section>
            <h2 className="font-semibold text-gray-800 mb-2">Çerezler</h2>
            <p>Uygulama, temel işlevsellik için zorunlu çerezler kullanabilir. Reklam amacıyla Google AdSense çerezleri kullanılmaktadır.</p>
          </section>
          <section>
            <h2 className="font-semibold text-gray-800 mb-2">Üçüncü Taraflar</h2>
            <p>Google Analytics ve Google AdSense hizmetleri, istatistik ve reklam amacıyla anonim kullanım verileri toplayabilir.</p>
          </section>
          <section>
            <h2 className="font-semibold text-gray-800 mb-2">İletişim</h2>
            <p>Gizlilik ile ilgili sorularınız için iletişim sayfasını ziyaret edin.</p>
          </section>
        </div>
      </div>
    </>
  );
}

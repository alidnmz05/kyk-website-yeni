import type { Metadata } from "next";
import Link from "next/link";

export const metadata: Metadata = {
  title: "Hakkında | KYK Yemek Listesi",
  description: "KYK Yemek Listesi uygulaması hakkında bilgi.",
};

export default function HakkindaPage() {
  return (
    <>
      <header className="sticky top-0 z-50 brand-gradient px-4 pt-10 pb-5 shadow-md shadow-brand/10 rounded-b-3xl mb-4">
        <div className="flex items-center gap-3">
          <Link href="/" className="w-8 h-8 flex items-center justify-center bg-white/20 rounded-full text-white hover:bg-white/30 backdrop-blur-md transition-colors">
            <span className="text-lg leading-none transform -translate-x-0.5">←</span>
          </Link>
          <h1 className="text-white font-bold text-xl drop-shadow-sm">Hakkında</h1>
        </div>
      </header>

      <div className="px-4 py-4 space-y-4">
        <div className="glass-panel rounded-3xl p-5 border border-white/40">
          <div className="flex items-center gap-3 mb-4">
            <div className="w-12 h-12 bg-brand/10 rounded-2xl flex items-center justify-center text-2xl">
              🍽️
            </div>
            <div>
              <h2 className="font-bold text-slate-800">KYK Yemek Listesi</h2>
              <p className="text-xs font-medium text-slate-500">81 il — Günlük Menü</p>
            </div>
          </div>
          <p className="text-sm text-slate-600 leading-relaxed font-medium">
            KYK Yemek Listesi, Türkiye genelindeki KYK (Kredi ve Yurtlar Kurumu) yurtlarının
            günlük kahvaltı ve akşam yemek menülerini kolayca görüntülemek için geliştirilmiş
            bir uygulamadır.
          </p>
        </div>

        <div className="glass-panel rounded-3xl p-5 border border-white/40 space-y-3">
          <h2 className="font-bold text-slate-800 mb-3">Özellikler</h2>
          {[
            "81 ilin KYK menüsü tek yerden",
            "Günlük, haftalık ve aylık menü takvimi",
            "Kalori bilgisi ile besin değerleri",
            "Hızlı şehir arama",
            "Mobil uygulama hissi",
          ].map((f, i) => (
            <div key={i} className="flex items-start gap-2.5">
              <span className="text-brand font-bold mt-0.5">✓</span>
              <span className="text-sm text-slate-600 font-medium">{f}</span>
            </div>
          ))}
        </div>

        <div className="glass-panel rounded-3xl p-5 border border-white/40">
          <h2 className="font-bold text-slate-800 mb-2">Veri Kaynağı</h2>
          <p className="text-sm text-slate-600 leading-relaxed font-medium">
            Menü verileri KYK resmi sistemiyle senkronize olarak güncellenmektedir.
            Veriler saatlik olarak yenilenir.
          </p>
        </div>

        <Link
          href="/gizlilik-politikasi"
          className="block glass-panel rounded-3xl px-5 py-4 border border-white/40 text-sm font-bold text-slate-600 hover:text-brand hover:border-brand/30 transition-all text-center"
        >
          Gizlilik Politikası →
        </Link>
      </div>
    </>
  );
}

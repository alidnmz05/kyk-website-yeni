"use client";

import { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { slugifyCity, defaultMealSlug } from "@/lib/utils";
import type { City } from "@/lib/types";
import { Search, X, ChevronRight, MapPin, Loader2 } from "lucide-react";

type Props = {
  cities: City[];
};

export default function CitySearch({ cities }: Props) {
  const [query, setQuery] = useState("");
  const [isLocating, setIsLocating] = useState(false);
  const router = useRouter();
  const meal = defaultMealSlug();

  const filtered = query.trim().length > 0
    ? cities.filter((c) =>
        c.name.toLowerCase().includes(query.toLowerCase()) ||
        slugifyCity(c.name).includes(slugifyCity(query))
      )
    : [];

  const handleLocate = () => {
    if (!navigator.geolocation) {
      alert("Tarayıcınız konum özelliğini desteklemiyor.");
      return;
    }

    setIsLocating(true);
    navigator.geolocation.getCurrentPosition(
      async (position) => {
        try {
          const { latitude, longitude } = position.coords;
          // Nominatim reverse geocoding
          const res = await fetch(`https://nominatim.openstreetmap.org/reverse?format=json&lat=${latitude}&lon=${longitude}&accept-language=tr`);
          const data = await res.json();
          
          if (data && data.address) {
            // Address object usually contains province, city, or state in Turkey
            let foundCityName = data.address.province || data.address.city || data.address.state || "";
            foundCityName = foundCityName.replace(" İli", "").replace(" Province", "").trim();
            
            const slugified = slugifyCity(foundCityName);
            const cityExists = cities.find(c => slugifyCity(c.name) === slugified);
            
            if (cityExists) {
              router.push(`/${slugified}/${meal}`);
            } else {
              alert("Bulunduğunuz konuma ait menü bulunamadı.");
            }
          }
        } catch (error) {
          alert("Konumunuz tespit edilemedi.");
        } finally {
          setIsLocating(false);
        }
      },
      () => {
        setIsLocating(false);
        alert("Konum izni reddedildi.");
      }
    );
  };

  return (
    <div className="relative">
      <div className="relative flex items-center">
        <Search size={18} className="absolute left-4 text-slate-400" />
        <input
          type="text"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="Şehir ara..."
          className="w-full pl-11 pr-[80px] py-3.5 bg-white/95 backdrop-blur-md rounded-2xl text-[13px] font-medium text-slate-800 placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-brand shadow-sm border border-transparent focus:border-brand/20 transition-all"
        />
        <div className="absolute right-2 flex items-center gap-1">
          {query ? (
            <button
              onClick={() => setQuery("")}
              className="p-2 text-slate-400 hover:text-brand transition-colors rounded-lg hover:bg-slate-100"
            >
              <X size={16} />
            </button>
          ) : (
            <button
              onClick={handleLocate}
              disabled={isLocating}
              title="Konumumu Göre Bul"
              className="flex items-center gap-1.5 px-3 py-1.5 text-[11px] font-bold text-brand bg-brand/10 hover:bg-brand/20 transition-colors rounded-xl disabled:opacity-50"
            >
              {isLocating ? <Loader2 size={14} className="animate-spin" /> : <MapPin size={14} />}
              <span className="hidden sm:inline">Konumumu Bul</span>
            </button>
          )}
        </div>
      </div>
      {filtered.length > 0 && (
        <div className="absolute top-full left-0 right-0 bg-white/95 backdrop-blur-xl rounded-2xl shadow-xl border border-slate-100 mt-2 overflow-hidden z-50 max-h-64 overflow-y-auto custom-scrollbar">
          {filtered.map((city) => (
            <Link
              key={city.id}
              href={`/${slugifyCity(city.name)}/${meal}`}
              className="group flex items-center justify-between px-4 py-3.5 hover:bg-brand/5 transition-colors border-b border-slate-50 last:border-0"
            >
              <span className="text-[13px] font-semibold text-slate-700 group-hover:text-brand">{city.name}</span>
              <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transform translate-x-2 group-hover:translate-x-0 transition-all">
                <span className="text-[10px] font-bold text-brand uppercase tracking-wider">Menü</span>
                <ChevronRight size={14} className="text-brand" />
              </div>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}


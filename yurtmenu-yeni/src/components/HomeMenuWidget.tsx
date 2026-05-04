"use client";

import { useEffect, useState } from "react";
import { getMenuForCityAction } from "@/app/actions";
import MenuCard from "./MenuCard";
import MealTabs from "./MealTabs";
import DateStrip from "./DateStrip";
import Link from "next/link";
import { defaultMealSlug, todayString } from "@/lib/utils";
import type { MenuItem } from "@/lib/types";

type Props = {
  fallbackCity: { name: string; slug: string };
  fallbackMenu: MenuItem | null;
  fallbackMealLabel: string;
};

export default function HomeMenuWidget({ fallbackCity, fallbackMenu, fallbackMealLabel }: Props) {
  const [city, setCity] = useState(fallbackCity);
  const [menu, setMenu] = useState<MenuItem | null>(fallbackMenu);
  const [mealLabel, setMealLabel] = useState(fallbackMealLabel);
  const [loading, setLoading] = useState(true);
  
  const today = todayString();
  const activeMealSlug = mealLabel === "Kahvaltı" ? "kahvalti" : "aksam";

  useEffect(() => {
    const lastCitySlug = localStorage.getItem("last_city_slug");
    
    if (lastCitySlug && lastCitySlug !== fallbackCity.slug) {
      setLoading(true);
      getMenuForCityAction(lastCitySlug).then((data) => {
        if (data && data.menuItem) {
          setCity(data.city);
          setMenu(data.menuItem);
          if (data.mealLabel) setMealLabel(data.mealLabel);
        }
        setLoading(false);
      });
    } else {
      setLoading(false);
    }
  }, [fallbackCity.slug]);

  return (
    <section className="mt-8 mb-6">
      <div className="flex items-center justify-between mb-2">
        <h2 className="text-[12px] font-bold text-slate-500 uppercase tracking-wider flex items-center gap-2 px-4">
          <span className="w-4 h-[1px] bg-brand/50"></span> Günün Menüsü: <span className="text-brand">{city.name}</span>
        </h2>
        <Link 
          href={`/${city.slug}/${activeMealSlug}`} 
          className="text-xs text-brand font-bold bg-brand/10 px-3 py-1.5 rounded-lg hover:bg-brand/20 transition-colors mx-4"
        >
          Tümünü Gör
        </Link>
      </div>

      <DateStrip citySlug={city.slug} mealSlug={activeMealSlug} selectedDate={today} />
      <MealTabs citySlug={city.slug} activeMeal={activeMealSlug} date={today} />
      
      <div className="px-4">
        {loading ? (
          <div className="animate-pulse bg-white/50 h-[300px] rounded-3xl border border-gray-100 shadow-sm flex items-center justify-center">
            <span className="text-brand/50 text-sm font-medium">Menü yükleniyor...</span>
          </div>
        ) : menu ? (
          <div className="transform scale-[0.98] origin-top">
             <MenuCard item={menu} mealLabel={`${mealLabel} (Bugün)`} />
          </div>
        ) : (
          <div className="bg-white rounded-3xl border border-gray-100 p-6 text-center shadow-sm">
            <p className="text-sm text-gray-500">Bugüne ait {mealLabel.toLowerCase()} bulunamadı.</p>
          </div>
        )}
      </div>
    </section>
  );
}



"use client";

import Link from "next/link";
import { motion } from "framer-motion";
import { Sun, Moon } from "lucide-react";

type Props = {
  citySlug: string;
  activeMeal: string;
  date: string;
};

export default function MealTabs({ citySlug, activeMeal, date }: Props) {
  const tabs = [
    { slug: "kahvalti", label: "Kahvaltı", icon: Sun },
    { slug: "aksam", label: "Akşam", icon: Moon },
  ];

  return (
    <div className="flex bg-slate-100/80 backdrop-blur-sm rounded-2xl p-1.5 mx-4 my-4 relative">
      {tabs.map((tab) => {
        const isActive = activeMeal === tab.slug;
        const Icon = tab.icon;
        return (
          <Link
            key={tab.slug}
            href={`/${citySlug}/${tab.slug}?date=${date}`}
            className={`flex-1 flex items-center justify-center gap-2 py-2.5 rounded-xl text-[13px] font-semibold transition-colors relative z-10 ${
              isActive ? "text-brand" : "text-slate-500 hover:text-slate-700"
            }`}
          >
            {isActive && (
              <motion.div
                layoutId="meal-tab-active"
                className="absolute inset-0 bg-white rounded-xl shadow-[0_2px_8px_rgba(0,0,0,0.06)]"
                transition={{ type: "spring", stiffness: 400, damping: 30 }}
              />
            )}
            <Icon size={16} className={`relative z-10 transition-transform ${isActive ? "scale-110" : ""}`} />
            <span className="relative z-10">{tab.label}</span>
          </Link>
        );
      })}
    </div>
  );
}

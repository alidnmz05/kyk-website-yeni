"use client";

import { parseMenuItems, parseCalories } from "@/lib/utils";
import type { MenuItem } from "@/lib/types";
import { motion } from "framer-motion";
import { Flame } from "lucide-react";

type Props = {
  item: MenuItem;
  mealLabel: string;
};

const courses = [
  { key: "first" as const, label: "1. Yemek" },
  { key: "second" as const, label: "2. Yemek" },
  { key: "third" as const, label: "3. Yemek" },
  { key: "fourth" as const, label: "4. Yemek" },
];

export default function MenuCard({ item, mealLabel }: Props) {
  return (
    <motion.div 
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, y: -20 }}
      transition={{ duration: 0.3 }}
      className="bg-white rounded-3xl shadow-[0_8px_30px_rgb(0,0,0,0.04)] border border-gray-100 overflow-hidden mb-6"
    >
      <div className="brand-gradient px-5 py-4 flex items-center justify-between">
        <span className="text-white font-bold text-lg tracking-wide">{mealLabel}</span>
        {item.totalCalories != null && (
          <div className="flex items-center gap-1 bg-white/20 px-3 py-1 rounded-full backdrop-blur-md">
            <Flame size={14} className="text-white" />
            <span className="text-white font-medium text-xs">
              {item.totalCalories} kcal
            </span>
          </div>
        )}
      </div>
      <div className="divide-y divide-gray-50/80 p-2">
        {courses.map(({ key, label }) => {
          const foods = parseMenuItems(item[key]);
          const cals = parseCalories(item[`${key}Calories` as keyof MenuItem] as string);
          if (foods.length === 0) return null;
          return (
            <div key={key} className="px-4 py-3 hover:bg-gray-50/50 transition-colors rounded-xl">
              <p className="text-[11px] font-semibold tracking-wider text-brand-light uppercase mb-1.5">{label}</p>
              <div className="space-y-2">
                {foods.map((food, i) => (
                  <div key={i} className="flex items-center justify-between gap-3">
                    <span className="text-sm text-slate-700 font-medium leading-tight">{food}</span>
                    {cals[i] && (
                      <span className="text-[11px] font-medium text-slate-400 shrink-0 bg-slate-50 px-2 py-0.5 rounded-md border border-slate-100">
                        {cals[i]} kcal
                      </span>
                    )}
                  </div>
                ))}
              </div>
            </div>
          );
        })}
      </div>
    </motion.div>
  );
}

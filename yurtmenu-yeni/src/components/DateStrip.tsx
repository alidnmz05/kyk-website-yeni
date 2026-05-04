"use client";

import Link from "next/link";
import { useEffect, useRef } from "react";
import { getDaysInCurrentMonth, formatDateTR } from "@/lib/utils";
import { motion } from "framer-motion";

const DAY_NAMES = ["Paz", "Pzt", "Sal", "Çar", "Per", "Cum", "Cmt"];

type Props = {
  citySlug: string;
  mealSlug: string;
  selectedDate: string;
};

export default function DateStrip({ citySlug, mealSlug, selectedDate }: Props) {
  const days = getDaysInCurrentMonth();
  const scrollRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const idx = days.indexOf(selectedDate);
    if (idx >= 0 && scrollRef.current) {
      const el = scrollRef.current.children[idx] as HTMLElement;
      el?.scrollIntoView({ behavior: "smooth", block: "nearest", inline: "center" });
    }
  }, [selectedDate, days]);

  return (
    <div
      ref={scrollRef}
      className="flex gap-2.5 overflow-x-auto scrollbar-hide px-4 py-4 glass-panel border-b border-white/50 sticky top-[60px] z-40"
    >
      {days.map((d) => {
        const date = new Date(d + "T00:00:00");
        const dayName = DAY_NAMES[date.getDay()];
        const dayNum = date.getDate();
        const isSelected = d === selectedDate;
        const isToday = d === new Date().toISOString().split("T")[0];

        return (
          <Link
            key={d}
            href={`/${citySlug}/${mealSlug}?date=${d}`}
            scroll={false}
            className={`relative flex flex-col items-center justify-center rounded-2xl min-w-[56px] h-[64px] transition-all shrink-0 overflow-hidden ${
              isSelected
                ? "text-white shadow-md shadow-brand/20 scale-105"
                : isToday
                ? "bg-brand/10 text-brand border border-brand/30"
                : "bg-white text-slate-500 shadow-sm border border-slate-100 hover:bg-slate-50"
            }`}
            title={formatDateTR(d)}
          >
            {isSelected && (
              <motion.div
                layoutId="active-date"
                className="absolute inset-0 brand-gradient -z-10"
                initial={false}
                transition={{ type: "spring", stiffness: 300, damping: 30 }}
              />
            )}
            <span className="text-[11px] font-medium opacity-90">{dayName}</span>
            <span className="text-xl font-bold leading-tight">{dayNum}</span>
            {isToday && !isSelected && (
              <span className="absolute bottom-1 w-1 h-1 rounded-full bg-brand"></span>
            )}
          </Link>
        );
      })}
    </div>
  );
}

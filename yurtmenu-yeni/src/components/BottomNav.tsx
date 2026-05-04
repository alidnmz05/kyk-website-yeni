"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { Home, MapPin, HelpCircle, Info } from "lucide-react";
import { motion } from "framer-motion";

const tabs = [
  { href: "/", label: "Ana Sayfa", icon: Home },
  { href: "/sehirler", label: "Şehirler", icon: MapPin },
  { href: "/sss", label: "SSS", icon: HelpCircle },
  { href: "/hakkinda", label: "Hakkında", icon: Info },
];

export default function BottomNav() {
  const pathname = usePathname();

  return (
    <nav className="fixed bottom-0 left-0 right-0 z-50 glass-panel border-t-0 safe-area-pb">
      <div className="max-w-lg mx-auto flex px-2 relative">
        {tabs.map((tab) => {
          const active = pathname === tab.href;
          const Icon = tab.icon;
          return (
            <Link
              key={tab.href}
              href={tab.href}
              className={`flex-1 flex flex-col items-center justify-center py-2 min-h-[64px] relative transition-colors ${
                active ? "text-brand" : "text-slate-400 hover:text-slate-500"
              }`}
            >
              {active && (
                <motion.div
                  layoutId="bottom-nav-indicator"
                  className="absolute top-0 w-8 h-1 bg-brand rounded-b-full"
                  initial={false}
                  transition={{ type: "spring", stiffness: 500, damping: 30 }}
                />
              )}
              <Icon 
                size={22} 
                className={`mb-1 transition-all duration-300 ${active ? "scale-110" : "scale-100"}`} 
                strokeWidth={active ? 2.5 : 2} 
              />
              <span className={`text-[10px] font-medium transition-all ${active ? "font-bold" : ""}`}>
                {tab.label}
              </span>
            </Link>
          );
        })}
      </div>
    </nav>
  );
}

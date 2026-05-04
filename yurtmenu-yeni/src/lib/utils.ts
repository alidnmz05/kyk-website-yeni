import { ALL_CITIES } from "./cities";
import type { City } from "./types";

export function slugifyCity(name: string): string {
  return name
    .toLowerCase()
    .replaceAll("İ", "i").replaceAll("ı", "i").replaceAll("ç", "c")
    .replaceAll("ğ", "g").replaceAll("ö", "o").replaceAll("ş", "s")
    .replaceAll("ü", "u").replaceAll("â", "a").replaceAll("î", "i")
    .replace(/\s+/g, "-")
    .replace(/[^a-z0-9-]/g, "")
    .replace(/-+/g, "-");
}

export function mealTypeFromSlug(slug: string): number {
  return slug === "kahvalti" ? 0 : 1;
}

export function mealSlugFromType(mealType: number): string {
  return mealType === 0 ? "kahvalti" : "aksam";
}

export function defaultMealSlug(): string {
  // Use UTC+3 for Turkey time to ensure server renders the correct meal
  const hour = (new Date().getUTCHours() + 3) % 24;
  return hour < 14 ? "kahvalti" : "aksam";
}

export function todayString(): string {
  return new Date().toISOString().split("T")[0];
}

export function formatDateTR(dateStr: string): string {
  const [year, month, day] = dateStr.split("-");
  const months = [
    "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran",
    "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık",
  ];
  return `${parseInt(day)} ${months[parseInt(month) - 1]} ${year}`;
}

export function getCurrentMonthName(): string {
  const months = [
    "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran",
    "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık",
  ];
  return months[new Date().getMonth()];
}

export function getCurrentYear(): number {
  return new Date().getFullYear();
}

export function findCityBySlug(cities: City[], slug: string): City | undefined {
  return cities.find((c) => slugifyCity(c.name) === slug);
}

export function findCityNameBySlug(slug: string): string {
  const found = ALL_CITIES.find((name) => slugifyCity(name) === slug);
  return found ?? slug;
}

export function parseMenuItems(value?: string): string[] {
  if (!value) return [];
  return value.split("/").map((s) => s.trim()).filter(Boolean);
}

export function parseCalories(value?: string): string[] {
  if (!value) return [];
  return value.split(",").map((s) => s.trim()).filter(Boolean);
}

export function getDaysInCurrentMonth(): string[] {
  const now = new Date();
  const year = now.getFullYear();
  const month = now.getMonth();
  const daysInMonth = new Date(year, month + 1, 0).getDate();
  const days: string[] = [];
  for (let d = 1; d <= daysInMonth; d++) {
    const mm = String(month + 1).padStart(2, "0");
    const dd = String(d).padStart(2, "0");
    days.push(`${year}-${mm}-${dd}`);
  }
  return days;
}

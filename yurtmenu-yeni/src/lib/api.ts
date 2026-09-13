import type { City, MenuItem } from "./types";

const isServer = typeof window === 'undefined';
const BACKEND_BASE = process.env.API_BASE || "http://localhost:5000";

export async function fetchCities(): Promise<City[]> {
  try {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 5000);

    const url = isServer
      ? `${BACKEND_BASE}/api/city`
      : `/api/proxy/city`;

    const res = await fetch(url, {
      headers: {
        "x-api-key": process.env.INTERNAL_API_SECRET || "",
      },
      next: { revalidate: 86400 },
      signal: controller.signal,
    });
    clearTimeout(timeoutId);
    if (!res.ok) return [];
    return res.json();
  } catch (error) {
    return [];
  }
}

export async function fetchMenu(
  cityId: number,
  mealType: number,
  date: string
): Promise<MenuItem[]> {
  try {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 5000);

    const url = isServer
      ? `${BACKEND_BASE}/api/menu/liste?cityId=${cityId}&mealType=${mealType}`
      : `/api/proxy/menu?cityId=${cityId}&mealType=${mealType}&date=${date}`;

    const res = await fetch(url, {
      headers: {
        "x-api-key": process.env.INTERNAL_API_SECRET || "",
      },
      next: { revalidate: 3600 },
      signal: controller.signal,
    });
    clearTimeout(timeoutId);
    if (!res.ok) return [];
    return res.json();
  } catch (error) {
    return [];
  }
}

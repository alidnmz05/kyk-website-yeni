import type { City, MenuItem } from "./types";

const isServer = typeof window === 'undefined';
const BASE_URL = isServer 
  ? (process.env.API_BASE || "http://localhost:5000") 
  : ""; 

const getUrl = (path: string) => isServer ? `${BASE_URL}${path}` : `/api/proxy${path}`;

export async function fetchCities(): Promise<City[]> {
  try {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 5000); // 5 saniye sonra iptal et

    const res = await fetch(getUrl("/city"), {
      headers: {
        "x-api-key": process.env.INTERNAL_API_SECRET || "",
      },
      next: { revalidate: 86400 },
      signal: controller.signal
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

    const res = await fetch(
      getUrl(`/menu?cityId=${cityId}&mealType=${mealType}&date=${date}`),
      {
        headers: {
          "x-api-key": process.env.INTERNAL_API_SECRET || "",
        },
        next: { revalidate: 3600 },
        signal: controller.signal
      }
    );
    clearTimeout(timeoutId);
    if (!res.ok) return [];
    return res.json();
  } catch (error) {
    return [];
  }
}

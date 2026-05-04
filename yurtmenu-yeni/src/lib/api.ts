import type { City, MenuItem } from "./types";

const BASE = process.env.NEXT_PUBLIC_SITE_URL ?? "http://localhost:3000";

export async function fetchCities(): Promise<City[]> {
  try {
    const res = await fetch(`${BASE}/api/proxy/city`, {
      headers: {
        "x-internal-secret": process.env.INTERNAL_API_SECRET || "",
      },
      next: { revalidate: 86400 },
    });
    if (!res.ok) return [];
    return res.json();
  } catch {
    return [];
  }
}

export async function fetchMenu(
  cityId: number,
  mealType: number,
  date: string
): Promise<MenuItem[]> {
  try {
    const res = await fetch(
      `${BASE}/api/proxy/menu?cityId=${cityId}&mealType=${mealType}&date=${date}`,
      {
        headers: {
          "x-internal-secret": process.env.INTERNAL_API_SECRET || "",
        },
        next: { revalidate: 3600 },
      }
    );
    if (!res.ok) return [];
    return res.json();
  } catch {
    return [];
  }
}

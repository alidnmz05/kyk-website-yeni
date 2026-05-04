import type { City, MenuItem } from "./types";

const isServer = typeof window === 'undefined';
const BASE_URL = isServer 
  ? (process.env.API_BASE || "http://localhost:5000") 
  : ""; 

const getUrl = (path: string) => isServer ? `${BASE_URL}${path}` : `/api/proxy${path}`;

export async function fetchCities(): Promise<City[]> {
  try {
    const res = await fetch(getUrl("/city"), {
      headers: {
        "x-internal-secret": process.env.INTERNAL_API_SECRET || "",
      },
      next: { revalidate: 86400 },
    });
    if (!res.ok) return [];
    return res.json();
  } catch (error) {
    console.error("Fetch Cities Error:", error);
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
      getUrl(`/menu?cityId=${cityId}&mealType=${mealType}&date=${date}`),
      {
        headers: {
          "x-internal-secret": process.env.INTERNAL_API_SECRET || "",
        },
        next: { revalidate: 3600 },
      }
    );
    if (!res.ok) return [];
    return res.json();
  } catch (error) {
    console.error("Fetch Menu Error:", error);
    return [];
  }
}

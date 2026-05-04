"use server";

import { fetchCities, fetchMenu } from "@/lib/api";
import { slugifyCity, todayString, defaultMealSlug, mealTypeFromSlug } from "@/lib/utils";

export async function getMenuForCityAction(slug: string) {
  try {
    const cities = await fetchCities();
    const city = cities.find((c) => slugifyCity(c.name) === slug);
    if (!city) return null;

    const date = todayString();
    const mealSlug = defaultMealSlug();
    const mealType = mealTypeFromSlug(mealSlug);
    const mealLabel = mealSlug === "kahvalti" ? "Kahvaltı" : "Akşam Yemeği";

    const menus = await fetchMenu(city.id, mealType, date);
    const menuItem = menus.find((m: any) => m.date === date) ?? menus[0] ?? null;

    return { 
      city: { name: city.name, slug: slugifyCity(city.name) }, 
      menuItem,
      mealLabel 
    };
  } catch (error) {
    return null;
  }
}

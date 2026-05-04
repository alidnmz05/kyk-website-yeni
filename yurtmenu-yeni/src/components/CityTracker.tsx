"use client";

import { useEffect } from "react";

export default function CityTracker({ slug }: { slug: string }) {
  useEffect(() => {
    localStorage.setItem("last_city_slug", slug);
  }, [slug]);

  return null;
}

import { checkSecurity } from "@/lib/security";

const ALLOWED_PARAMS = ["cityId", "mealType", "date"];

export async function GET(request: Request) {
  try {
    const sec = await checkSecurity();
    if (sec.error) return Response.json({ error: sec.error }, { status: sec.status });

    const incoming = new URL(request.url);
    const safe = new URLSearchParams();
    for (const key of ALLOWED_PARAMS) {
      const val = incoming.searchParams.get(key);
      if (val) safe.set(key, val);
    }
    const upstream = `${process.env.API_BASE}/api/menu/liste?${safe}`;
    const res = await fetch(upstream, { next: { revalidate: 3600 } });
    if (!res.ok) return Response.json([], { status: 502 });
    const data = await res.json();
    return Response.json(data);
  } catch {
    return Response.json([], { status: 503 });
  }
}

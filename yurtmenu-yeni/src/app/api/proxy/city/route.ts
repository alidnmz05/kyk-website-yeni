import { checkSecurity } from "@/lib/security";

export async function GET() {
  try {
    const sec = await checkSecurity();
    if (sec.error) return Response.json({ error: sec.error }, { status: sec.status });

    const res = await fetch(`${process.env.API_BASE}/api/city`, {
      next: { revalidate: 86400 },
    });
    if (!res.ok) return Response.json([], { status: 502 });
    const data = await res.json();
    return Response.json(data);
  } catch {
    return Response.json([], { status: 503 });
  }
}

import { headers } from "next/headers";

const rateLimitMap = new Map<string, { count: number; lastReset: number }>();
const RATE_LIMIT_WINDOW = 60 * 1000; // 1 minute
const MAX_REQUESTS_PER_WINDOW = 30; // 30 requests per minute

export async function checkSecurity(): Promise<{ error?: string; status?: number }> {
  const reqHeaders = await headers();
  
  // 1. Internal Secret Check
  const secret = reqHeaders.get("x-internal-secret");
  if (secret !== process.env.INTERNAL_API_SECRET) {
    return { error: "Forbidden", status: 403 };
  }

  // 2. Rate Limiting Check
  const ip = reqHeaders.get("x-forwarded-for") ?? "127.0.0.1";
  const now = Date.now();
  let rateInfo = rateLimitMap.get(ip);

  if (!rateInfo || now - rateInfo.lastReset > RATE_LIMIT_WINDOW) {
    rateInfo = { count: 1, lastReset: now };
  } else {
    rateInfo.count++;
  }

  rateLimitMap.set(ip, rateInfo);

  if (rateInfo.count > MAX_REQUESTS_PER_WINDOW) {
    return { error: "Too Many Requests", status: 429 };
  }

  return {};
}

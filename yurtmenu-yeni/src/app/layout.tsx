import type { Metadata, Viewport } from "next";
import { Geist } from "next/font/google";
import "./globals.css";
import BottomNav from "@/components/BottomNav";

const geist = Geist({ subsets: ["latin"], variable: "--font-geist" });

const SITE_URL = process.env.NEXT_PUBLIC_SITE_URL ?? "https://kykyemekliste.com";
const OG_IMAGE = `${SITE_URL}/og-default.png`;

export const viewport: Viewport = {
  themeColor: "#E11D48",
  width: "device-width",
  initialScale: 1,
  minimumScale: 1,
};

export const metadata: Metadata = {
  title: {
    default: "KYK Yemek Listesi: Bugün Ne Var? | 81 İl Yurt Menüsü",
    template: "%s | KYK Yemek Listesi",
  },
  description:
    "2026 KYK yemek listesi bugün ne var? Ankara, İstanbul, İzmir ve 81 ilin güncel KYK yurt menüsü. Günlük yemek takibi ve yurt yemek saatleri.",
  manifest: "/manifest.json",
  metadataBase: new URL(SITE_URL),
  openGraph: {
    siteName: "KYK Yemek Listesi",
    locale: "tr_TR",
    type: "website",
    images: [
      {
        url: OG_IMAGE,
        width: 1200,
        height: 630,
        alt: "KYK Yemek Listesi — Bugün Ne Var?",
      },
    ],
  },
  twitter: {
    card: "summary_large_image",
    images: [OG_IMAGE],
    title: "KYK Yemek Listesi | Güncel Yurt Menüsü",
    description: "81 il KYK yurtları için en hızlı yemek takip platformu.",
  },


  verification: {
    google: process.env.NEXT_PUBLIC_GOOGLE_SITE_VERIFICATION,
  },
  icons: {
    icon: "/favicon.ico",
    apple: "/icon-192.png",
  },
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="tr" className={geist.variable}>
      <body className="bg-gray-50 font-sans antialiased">
        <main className="max-w-lg mx-auto min-h-screen pb-20">
          {children}
        </main>
        <BottomNav />
      </body>
    </html>
  );
}

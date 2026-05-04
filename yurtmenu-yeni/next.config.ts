import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  async redirects() {
    return [
      { source: "/:city/sabah", destination: "/:city/kahvalti", permanent: true },
      { source: "/:city/ogle", destination: "/:city/aksam", permanent: true },
    ];
  },
};

export default nextConfig;

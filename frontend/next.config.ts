import type { NextConfig } from 'next'

const nextConfig: NextConfig = {
  // Required for deployment to Azure App Service on Linux.
  // The CD pipeline copies public/ and .next/static/ into the standalone output.
  output: 'standalone',

  // Add trusted image domains here as needed
  images: {
    remotePatterns: [],
  },
}

export default nextConfig

'use client'

import { SessionProvider as NextAuthSessionProvider } from 'next-auth/react'

// SessionProvider must be a Client Component because it uses React context.
// Wrapping it here keeps layout.tsx a Server Component.
export function SessionProvider({ children }: { children: React.ReactNode }) {
  return <NextAuthSessionProvider>{children}</NextAuthSessionProvider>
}

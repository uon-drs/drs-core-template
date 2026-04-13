import NextAuth, { NextAuthOptions } from 'next-auth'
import KeycloakProvider from 'next-auth/providers/keycloak'

// Extend the built-in session/token types
declare module 'next-auth' {
  interface Session {
    accessToken?: string
    error?: 'RefreshAccessTokenError'
  }
}

declare module 'next-auth/jwt' {
  interface JWT {
    accessToken?: string
    refreshToken?: string
    accessTokenExpires?: number
    error?: 'RefreshAccessTokenError'
  }
}

async function refreshAccessToken(refreshToken: string) {
  try {
    const url = `${process.env.KEYCLOAK_ISSUER}/protocol/openid-connect/token`
    const response = await fetch(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body: new URLSearchParams({
        grant_type: 'refresh_token',
        client_id: process.env.KEYCLOAK_CLIENT_ID!,
        client_secret: process.env.KEYCLOAK_CLIENT_SECRET ?? '',
        refresh_token: refreshToken,
      }),
    })

    const refreshed = await response.json()
    if (!response.ok) throw refreshed

    return {
      accessToken: refreshed.access_token as string,
      refreshToken: (refreshed.refresh_token ?? refreshToken) as string,
      accessTokenExpires: Date.now() + (refreshed.expires_in as number) * 1000,
    }
  } catch {
    return { error: 'RefreshAccessTokenError' as const }
  }
}

export const authOptions: NextAuthOptions = {
  providers: [
    KeycloakProvider({
      clientId: process.env.KEYCLOAK_CLIENT_ID!,
      clientSecret: process.env.KEYCLOAK_CLIENT_SECRET ?? '',
      issuer: process.env.KEYCLOAK_ISSUER!,
    }),
  ],

  callbacks: {
    async jwt({ token, account }) {
      // Initial sign-in: store tokens from Keycloak
      if (account) {
        return {
          ...token,
          accessToken: account.access_token,
          refreshToken: account.refresh_token,
          accessTokenExpires: account.expires_at
            ? account.expires_at * 1000
            : Date.now() + 60_000,
        }
      }

      // Token still valid
      if (Date.now() < (token.accessTokenExpires ?? 0)) {
        return token
      }

      // Token expired — attempt refresh
      if (!token.refreshToken) {
        return { ...token, error: 'RefreshAccessTokenError' as const }
      }
      const refreshed = await refreshAccessToken(token.refreshToken)
      return { ...token, ...refreshed }
    },

    async session({ session, token }) {
      session.accessToken = token.accessToken
      session.error = token.error
      return session
    },
  },

  // Redirect to Keycloak login page on unauthenticated access
  pages: {
    signIn: '/api/auth/signin',
  },
}

const handler = NextAuth(authOptions)
export { handler as GET, handler as POST }

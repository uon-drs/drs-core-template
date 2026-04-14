import { auth } from '@/auth'
import type { Session } from 'next-auth'

/**
 * Returns the current server-side session, or null if unauthenticated.
 * Use in Server Components and Route Handlers.
 */
export async function getSession(): Promise<Session | null> {
  return auth()
}

/**
 * Returns the Keycloak access token from the current session.
 * Returns null if the user is not signed in or if token refresh failed.
 *
 * Use this token as a Bearer token when calling the backend API:
 *   Authorization: Bearer <token>
 */
export async function getAccessToken(): Promise<string | null> {
  const session = await auth()
  if (!session || session.error === 'RefreshAccessTokenError') return null
  return session.accessToken ?? null
}

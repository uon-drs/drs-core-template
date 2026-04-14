import { SignInButton, SignOutButton } from '@/lib/auth/components/auth-buttons'
import { getSession } from '@/lib/auth'

export default async function HomePage() {
  const session = await getSession()

  return (
    <main style={{ fontFamily: 'sans-serif', maxWidth: 640, margin: '4rem auto', padding: '0 1rem' }}>
      <h1>TemplateApp</h1>

      {session ? (
        <>
          <p>
            Signed in as <strong>{session.user?.email ?? session.user?.name}</strong>
          </p>
          {session.error === 'RefreshAccessTokenError' && (
            <p style={{ color: 'red' }}>
              Your session has expired. Please sign in again.
            </p>
          )}
          <SignOutButton />
        </>
      ) : (
        <>
          <p>You are not signed in.</p>
          <SignInButton />
        </>
      )}
    </main>
  )
}

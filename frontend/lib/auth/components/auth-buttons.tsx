'use client'

import { signIn, signOut } from 'next-auth/react'

export function SignInButton() {
  return (
    <button onClick={() => signIn('keycloak')}>
      Sign in with Keycloak
    </button>
  )
}

export function SignOutButton() {
  return (
    <button onClick={() => signOut()}>
      Sign out
    </button>
  )
}

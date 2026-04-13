export { default } from 'next-auth/middleware'

export const config = {
  // Protect all routes except the homepage, NextAuth routes, and public assets.
  // Adjust this matcher to suit your application's public/private route structure.
  matcher: [
    '/((?!$|api/auth|_next/static|_next/image|favicon.ico).*)',
  ],
}

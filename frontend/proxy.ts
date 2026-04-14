import { auth } from '@/auth'

export default auth

export const config = {
  // Protect all routes except the homepage, Auth.js routes, and public assets.
  // Adjust this matcher to suit your application's public/private route structure.
  // See: https://nextjs.org/docs/app/api-reference/file-conventions/proxy
  matcher: [
    '/((?!$|api/auth|_next/static|_next/image|favicon.ico).*)',
  ],
}

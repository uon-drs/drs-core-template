import type { Metadata } from 'next'
import { SessionProvider } from './providers'

export const metadata: Metadata = {
  title: 'TemplateApp',
  description: 'TemplateApp application',
}

export default function RootLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return (
    <html lang="en">
      <body>
        <SessionProvider>{children}</SessionProvider>
      </body>
    </html>
  )
}

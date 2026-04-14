const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? ''

/**
 * Makes an authenticated request to the backend API.
 *
 * Attach a Bearer token from the current session (via {@link getAccessToken})
 * and call this from service modules in `api/`.
 *
 * @param path - Path relative to the API base URL, e.g. `"/users/123"`
 * @param token - Bearer token obtained from the session
 * @param init - Optional fetch options (method, body, additional headers, etc.)
 * @returns The parsed JSON response typed as `T`
 * @throws {Error} If the response status is not 2xx
 */
export async function apiRequest<T>(
  path: string,
  token: string,
  init?: RequestInit,
): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
      ...init?.headers,
    },
  })

  if (!response.ok) {
    throw new Error(`API error: ${response.status} ${response.statusText}`)
  }

  return response.json() as Promise<T>
}

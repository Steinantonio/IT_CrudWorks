import { vi } from 'vitest'

export type FetchHandler = (
  url: string,
  init: RequestInit | undefined,
) => Response | Promise<Response>

export function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

function requestUrl(input: RequestInfo | URL): string {
  if (typeof input === 'string') {
    return input
  }

  if (input instanceof URL) {
    return input.toString()
  }

  return input.url
}

export function installFetchMock(handler: FetchHandler) {
  const mock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) =>
    handler(requestUrl(input), init),
  )

  vi.stubGlobal('fetch', mock)

  return mock
}

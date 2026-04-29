import { apiRequest } from '~/api/client'
import type { IntegrationApiToken, IntegrationApiTokenSecret, RotateIntegrationApiTokenSecret } from '~/api/types'

export const integrationTokensClient = {
  list() {
    return apiRequest<IntegrationApiToken[]>('/integration/tokens')
  },

  create(name: string, scope: string, expiresUtc?: string | null, projectIds?: string[]) {
    return apiRequest<IntegrationApiTokenSecret>('/integration/tokens', {
      method: 'POST',
      body: JSON.stringify({ name, scope, expiresUtc: expiresUtc ?? null, projectIds: projectIds ?? [] }),
    })
  },

  revoke(tokenId: string) {
    return apiRequest<{ status: string }>('/integration/tokens/revoke', {
      method: 'POST',
      body: JSON.stringify({ tokenId }),
    })
  },

  rotate(tokenId: string, newExpiresUtc?: string | null) {
    return apiRequest<RotateIntegrationApiTokenSecret>('/integration/tokens/rotate', {
      method: 'POST',
      body: JSON.stringify({ tokenId, newExpiresUtc: newExpiresUtc ?? null }),
    })
  },

  extend(tokenId: string, newExpiresUtc: string) {
    return apiRequest<{ status: string; id: string; expiresUtc: string }>('/integration/tokens/extend', {
      method: 'POST',
      body: JSON.stringify({ tokenId, newExpiresUtc }),
    })
  },
}

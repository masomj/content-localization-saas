import { apiRequest } from '~/api/client'
import type { EnvironmentVariant, ProjectEnvironment, UpsertEnvironmentVariantRequest } from '~/api/types'

export const environmentVariantsClient = {
  /** Environments that have at least one variant in the project (production is implicit). */
  listEnvironments(projectId: string) {
    return apiRequest<ProjectEnvironment[]>(`/projects/${encodeURIComponent(projectId)}/environments`)
  },
  list(contentItemId: string) {
    return apiRequest<EnvironmentVariant[]>(`/content-items/${encodeURIComponent(contentItemId)}/environment-variants`)
  },
  upsert(contentItemId: string, payload: UpsertEnvironmentVariantRequest) {
    return apiRequest<EnvironmentVariant>(`/content-items/${encodeURIComponent(contentItemId)}/environment-variants`, {
      method: 'PUT',
      body: JSON.stringify(payload),
    })
  },
  remove(contentItemId: string, variantId: string) {
    return apiRequest<void>(
      `/content-items/${encodeURIComponent(contentItemId)}/environment-variants/${encodeURIComponent(variantId)}`,
      { method: 'DELETE' },
    )
  },
}

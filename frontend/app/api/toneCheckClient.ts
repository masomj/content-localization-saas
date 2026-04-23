import { apiRequest } from '~/api/client'
import type { ProjectToneConfig, ToneCheckResponse, ToneCheckResultDto } from '~/api/types'

export const toneCheckClient = {
  getConfig: (projectId: string) =>
    apiRequest<ProjectToneConfig>(`/projects/${projectId}/tone-config`),

  createConfig: (projectId: string, toneDescription: string) =>
    apiRequest<ProjectToneConfig>(`/projects/${projectId}/tone-config`, {
      method: 'POST',
      body: JSON.stringify({ toneDescription }),
    }),

  updateConfig: (projectId: string, toneDescription: string) =>
    apiRequest<ProjectToneConfig>(`/projects/${projectId}/tone-config`, {
      method: 'PUT',
      body: JSON.stringify({ toneDescription }),
    }),

  deleteConfig: (projectId: string) =>
    apiRequest<void>(`/projects/${projectId}/tone-config`, { method: 'DELETE' }),

  check: (payload: { contentItemLanguageTaskId: string; text: string; language: string; projectId: string }) =>
    apiRequest<ToneCheckResponse>('/tone-check', {
      method: 'POST',
      body: JSON.stringify(payload),
    }),

  apply: (resultId: string) =>
    apiRequest<{ translationText: string }>(`/tone-check/${resultId}/apply`, {
      method: 'POST',
    }),

  getResults: (contentItemLanguageTaskId: string) =>
    apiRequest<ToneCheckResultDto[]>(`/tone-check/results?contentItemLanguageTaskId=${encodeURIComponent(contentItemLanguageTaskId)}`),
}

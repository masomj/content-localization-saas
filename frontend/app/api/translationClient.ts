import { apiRequest } from '~/api/client'
import type { LanguageTask, TranslationSuggestion, LocalizationGridResponse } from '~/api/types'

export const translationClient = {
  getTasks(contentItemId: string) {
    return apiRequest<LanguageTask[]>(`/language-tasks?contentItemId=${encodeURIComponent(contentItemId)}`)
  },
  getSuggestion(contentItemId: string, languageCode: string) {
    return apiRequest<TranslationSuggestion>(
      `/language-tasks/suggestions?contentItemId=${encodeURIComponent(contentItemId)}&languageCode=${encodeURIComponent(languageCode)}`,
    )
  },
  upsert(task: { contentItemId: string; languageCode: string; status: string; translationText: string; assigneeEmail?: string; dueUtc?: string | null }) {
    return apiRequest<LanguageTask>('/language-tasks', {
      method: 'POST',
      body: JSON.stringify(task),
    })
  },
  applyMemory(payload: { contentItemId: string; languageCode: string; acceptSuggestion: boolean; manualText?: string }) {
    return apiRequest<LanguageTask>('/language-tasks/apply-memory', {
      method: 'POST',
      body: JSON.stringify(payload),
    })
  },
  getGrid(projectId: string, opts?: { page?: number; pageSize?: number; status?: string; search?: string }) {
    const params = new URLSearchParams({ projectId })
    if (opts?.page != null) params.set('page', String(opts.page))
    if (opts?.pageSize != null) params.set('pageSize', String(opts.pageSize))
    if (opts?.status) params.set('status', opts.status)
    if (opts?.search) params.set('search', opts.search)
    return apiRequest<LocalizationGridResponse>(`/localization-grid?${params.toString()}`)
  },
  exportNeutral(projectId: string) {
    return apiRequest<unknown>(`/exports/neutral?projectId=${encodeURIComponent(projectId)}`)
  },
  exportBundle(projectId: string, language: string) {
    return apiRequest<unknown>(
      `/integration/exports/bundle?projectId=${encodeURIComponent(projectId)}&language=${encodeURIComponent(language)}`,
    )
  },
}

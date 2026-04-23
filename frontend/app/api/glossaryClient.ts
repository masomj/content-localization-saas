import { apiRequest } from '~/api/client'
import type { ForbiddenMatch } from '~/api/types'

export interface GlossaryDto {
  id: string
  workspaceId: string
  name: string
  description: string
  createdUtc: string
  updatedUtc: string
}

export interface GlossaryTermDto {
  id: string
  glossaryId: string
  sourceTerm: string
  definition: string
  isForbidden: boolean
  forbiddenReplacement: string
  caseSensitive: boolean
  translations: GlossaryTermTranslationDto[]
  createdUtc: string
  updatedUtc: string
}

export interface GlossaryTermTranslationDto {
  id: string
  languageCode: string
  translatedTerm: string
}

export interface GlossarySuggestion {
  term: string
  definition: string
  translatedTerm: string
  glossaryName: string
}

export const glossaryClient = {
  list: () => apiRequest<GlossaryDto[]>('/glossaries'),

  create: (name: string, description: string) =>
    apiRequest<GlossaryDto>('/glossaries', {
      method: 'POST',
      body: JSON.stringify({ name, description }),
    }),

  update: (id: string, name: string, description: string) =>
    apiRequest<GlossaryDto>(`/glossaries/${id}`, {
      method: 'PUT',
      body: JSON.stringify({ name, description }),
    }),

  delete: (id: string) =>
    apiRequest<void>(`/glossaries/${id}`, { method: 'DELETE' }),

  listTerms: (glossaryId: string, page = 1, pageSize = 50, search = '') =>
    apiRequest<{ items: GlossaryTermDto[]; total: number }>(
      `/glossaries/${glossaryId}/terms?page=${page}&pageSize=${pageSize}&q=${encodeURIComponent(search)}`,
    ),

  createTerm: (glossaryId: string, term: Record<string, unknown>) =>
    apiRequest<GlossaryTermDto>(`/glossaries/${glossaryId}/terms`, {
      method: 'POST',
      body: JSON.stringify(term),
    }),

  updateTerm: (glossaryId: string, termId: string, term: Record<string, unknown>) =>
    apiRequest<GlossaryTermDto>(`/glossaries/${glossaryId}/terms/${termId}`, {
      method: 'PUT',
      body: JSON.stringify(term),
    }),

  deleteTerm: (glossaryId: string, termId: string) =>
    apiRequest<void>(`/glossaries/${glossaryId}/terms/${termId}`, { method: 'DELETE' }),

  importCsv: (glossaryId: string, file: File) => {
    const form = new FormData()
    form.append('file', file)
    return apiRequest<{ imported: number }>(`/glossaries/${glossaryId}/terms/import/csv`, {
      method: 'POST',
      body: form,
    })
  },

  exportCsv: (glossaryId: string) =>
    apiRequest<Blob>(`/glossaries/${glossaryId}/terms/export/csv`),

  importTbx: (glossaryId: string, file: File) => {
    const form = new FormData()
    form.append('file', file)
    return apiRequest<{ imported: number }>(`/glossaries/${glossaryId}/terms/import/tbx`, {
      method: 'POST',
      body: form,
    })
  },

  exportTbx: (glossaryId: string) =>
    apiRequest<Blob>(`/glossaries/${glossaryId}/terms/export/tbx`),

  suggest: (sourceText: string, languageCode: string) =>
    apiRequest<GlossarySuggestion[]>('/glossary-suggestions', {
      method: 'POST',
      body: JSON.stringify({ sourceText, languageCode }),
    }),

  forbiddenCheck: (text: string, languageCode: string) =>
    apiRequest<ForbiddenMatch[]>('/forbidden-check', {
      method: 'POST',
      body: JSON.stringify({ text, languageCode }),
    }),
}

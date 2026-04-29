import { apiRequest } from '~/api/client'
import type { LocaleImportResult } from '~/api/types'

export const localeImportsClient = {
  import(projectId: string, formData: FormData) {
    return apiRequest<LocaleImportResult>(`/locale-imports?projectId=${encodeURIComponent(projectId)}`, {
      method: 'POST',
      body: formData,
    })
  },
}

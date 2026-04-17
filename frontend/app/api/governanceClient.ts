import { apiRequest } from '~/api/client'
import type { GovernanceDashboard } from '~/api/types'

export const governanceClient = {
  getDashboard: (workspaceId: string) =>
    apiRequest<GovernanceDashboard>(`/governance/dashboard?workspaceId=${encodeURIComponent(workspaceId)}`),

  exportCsv: (workspaceId: string) =>
    apiRequest<Blob>(`/governance/export/csv?workspaceId=${encodeURIComponent(workspaceId)}`),
}

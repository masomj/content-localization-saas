import { apiRequest } from '~/api/client'
import type { StyleRuleDto, StyleViolation, StyleOverrideDto } from '~/api/types'

export type { StyleRuleDto, StyleViolation, StyleOverrideDto }

export const styleRulesClient = {
  list: (projectId: string) =>
    apiRequest<StyleRuleDto[]>(`/projects/${projectId}/style-rules`),

  create: (projectId: string, rule: Partial<StyleRuleDto>) =>
    apiRequest<StyleRuleDto>(`/projects/${projectId}/style-rules`, {
      method: 'POST',
      body: JSON.stringify(rule),
    }),

  update: (projectId: string, ruleId: string, rule: Partial<StyleRuleDto>) =>
    apiRequest<StyleRuleDto>(`/projects/${projectId}/style-rules/${ruleId}`, {
      method: 'PUT',
      body: JSON.stringify(rule),
    }),

  delete: (projectId: string, ruleId: string) =>
    apiRequest<void>(`/projects/${projectId}/style-rules/${ruleId}`, {
      method: 'DELETE',
    }),

  check: (text: string, projectId: string, contentType: string) =>
    apiRequest<StyleViolation[]>('/style-check', {
      method: 'POST',
      body: JSON.stringify({ text, projectId, contentType }),
    }),
}

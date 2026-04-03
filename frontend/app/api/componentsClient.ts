import { apiRequest } from '~/api/client'
import type { DesignComponent, DesignComponentTextField } from '~/api/types'

export interface CreateDesignComponentRequest {
  figmaFileId: string
  figmaFrameId: string
  figmaFrameName: string
  thumbnailUrl?: string
  frameWidth: number
  frameHeight: number
  textFields?: Array<{
    figmaLayerId: string
    figmaLayerName: string
    currentText: string
    x: number
    y: number
    width: number
    height: number
    fontFamily: string
    fontSize: number
    fontWeight: string
    textAlign: string
    color: string
  }>
}

export interface DesignComponentWithFields extends DesignComponent {
  textFields: DesignComponentTextField[]
}

export const componentsClient = {
  list(projectId: string) {
    return apiRequest<DesignComponent[]>(
      `/projects/${encodeURIComponent(projectId)}/components`,
    )
  },

  get(projectId: string, id: string) {
    return apiRequest<DesignComponentWithFields>(
      `/projects/${encodeURIComponent(projectId)}/components/${encodeURIComponent(id)}`,
    )
  },

  create(projectId: string, data: CreateDesignComponentRequest) {
    return apiRequest<DesignComponentWithFields>(
      `/projects/${encodeURIComponent(projectId)}/components`,
      {
        method: 'POST',
        body: JSON.stringify(data),
      },
    )
  },

  updateTextField(textFieldId: string, currentText: string, contentItemId?: string | null) {
    return apiRequest<DesignComponentTextField>(
      `/component-text-fields/${encodeURIComponent(textFieldId)}`,
      {
        method: 'PUT',
        body: JSON.stringify({ currentText, contentItemId }),
      },
    )
  },

  linkContentKey(textFieldId: string, contentItemId: string) {
    return apiRequest<void>(
      `/component-text-fields/${encodeURIComponent(textFieldId)}/link-content-key`,
      {
        method: 'POST',
        body: JSON.stringify({ contentItemId }),
      },
    )
  },

  unlinkContentKey(textFieldId: string) {
    return apiRequest<void>(
      `/component-text-fields/${encodeURIComponent(textFieldId)}/link-content-key`,
      {
        method: 'DELETE',
      },
    )
  },

  delete(projectId: string, id: string) {
    return apiRequest<void>(
      `/projects/${encodeURIComponent(projectId)}/components/${encodeURIComponent(id)}`,
      {
        method: 'DELETE',
      },
    )
  },
}

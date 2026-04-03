import { apiRequest } from '~/api/client'
import type {
  LibraryComponent,
  LibraryComponentVariant,
  LibraryComponentTextField,
  LibraryComponentWithVariants,
} from '~/api/types'

export interface CreateLibraryComponentRequest {
  projectId: string
  figmaFileId: string
  figmaComponentKey: string
  figmaComponentId: string
  figmaComponentSetId?: string
  name: string
  description?: string
  thumbnailUrl?: string
  frameWidth: number
  frameHeight: number
}

export interface UpdateLibraryComponentRequest {
  name?: string
  description?: string
  thumbnailUrl?: string
  frameWidth?: number
  frameHeight?: number
}

export interface CreateLibraryVariantRequest {
  figmaNodeId: string
  variantName: string
  variantProperties?: string
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

export const libraryClient = {
  list(projectId: string) {
    return apiRequest<LibraryComponent[]>(
      `/library-components?projectId=${encodeURIComponent(projectId)}`,
    )
  },

  get(id: string) {
    return apiRequest<LibraryComponentWithVariants>(
      `/library-components/${encodeURIComponent(id)}`,
    )
  },

  create(data: CreateLibraryComponentRequest) {
    return apiRequest<LibraryComponent>(
      `/library-components`,
      {
        method: 'POST',
        body: JSON.stringify(data),
      },
    )
  },

  update(id: string, data: UpdateLibraryComponentRequest) {
    return apiRequest<LibraryComponent>(
      `/library-components/${encodeURIComponent(id)}`,
      {
        method: 'PUT',
        body: JSON.stringify(data),
      },
    )
  },

  delete(id: string) {
    return apiRequest<void>(
      `/library-components/${encodeURIComponent(id)}`,
      {
        method: 'DELETE',
      },
    )
  },

  // Variants
  listVariants(componentId: string) {
    return apiRequest<LibraryComponentVariant[]>(
      `/library-components/${encodeURIComponent(componentId)}/variants`,
    )
  },

  getVariant(componentId: string, variantId: string) {
    return apiRequest<LibraryComponentVariant>(
      `/library-components/${encodeURIComponent(componentId)}/variants/${encodeURIComponent(variantId)}`,
    )
  },

  createVariant(componentId: string, data: CreateLibraryVariantRequest) {
    return apiRequest<LibraryComponentVariant>(
      `/library-components/${encodeURIComponent(componentId)}/variants`,
      {
        method: 'POST',
        body: JSON.stringify(data),
      },
    )
  },

  deleteVariant(componentId: string, variantId: string) {
    return apiRequest<void>(
      `/library-components/${encodeURIComponent(componentId)}/variants/${encodeURIComponent(variantId)}`,
      {
        method: 'DELETE',
      },
    )
  },

  // Text fields
  updateTextField(textFieldId: string, currentText: string, contentItemId?: string | null) {
    return apiRequest<LibraryComponentTextField>(
      `/library-text-fields/${encodeURIComponent(textFieldId)}`,
      {
        method: 'PUT',
        body: JSON.stringify({ currentText, contentItemId }),
      },
    )
  },

  linkContentKey(textFieldId: string, contentItemId: string) {
    return apiRequest<void>(
      `/library-text-fields/${encodeURIComponent(textFieldId)}/link-content-key`,
      {
        method: 'POST',
        body: JSON.stringify({ contentItemId }),
      },
    )
  },

  unlinkContentKey(textFieldId: string) {
    return apiRequest<void>(
      `/library-text-fields/${encodeURIComponent(textFieldId)}/link-content-key`,
      {
        method: 'DELETE',
      },
    )
  },
}

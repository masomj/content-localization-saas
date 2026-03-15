import { describe, it, expect, vi, beforeEach } from 'vitest'

const AUTH_STORAGE_KEY = 'locflow_auth_token'
const USER_STORAGE_KEY = 'locflow_user'
const ORG_STORAGE_KEY = 'locflow_organization'

describe('Auth Storage Utilities', () => {
  let localStorageMock: Record<string, string>

  beforeEach(() => {
    localStorageMock = {}
    vi.stubGlobal('localStorage', {
      getItem: (key: string) => localStorageMock[key] || null,
      setItem: (key: string, value: string) => { localStorageMock[key] = value },
      removeItem: (key: string) => { delete localStorageMock[key] },
    })
  })

  describe('Login Validation', () => {
    it('should reject login with missing email', async () => {
      const result = await validateLogin('', 'password123')
      expect(result.valid).toBe(false)
      expect(result.error).toContain('required')
    })

    it('should reject login with missing password', async () => {
      const result = await validateLogin('test@example.com', '')
      expect(result.valid).toBe(false)
      expect(result.error).toContain('required')
    })

    it('should accept valid credentials', async () => {
      const result = await validateLogin('test@example.com', 'password123')
      expect(result.valid).toBe(true)
    })
  })

  describe('Registration Validation', () => {
    it('should reject registration with missing first name', async () => {
      const result = await validateRegistration({
        firstName: '',
        lastName: 'Doe',
        email: 'test@example.com',
        password: 'password123',
      })
      expect(result.valid).toBe(false)
      expect(result.error).toContain('First name')
    })

    it('should reject registration with missing last name', async () => {
      const result = await validateRegistration({
        firstName: 'John',
        lastName: '',
        email: 'test@example.com',
        password: 'password123',
      })
      expect(result.valid).toBe(false)
      expect(result.error).toContain('Last name')
    })

    it('should reject registration with missing email', async () => {
      const result = await validateRegistration({
        firstName: 'John',
        lastName: 'Doe',
        email: '',
        password: 'password123',
      })
      expect(result.valid).toBe(false)
      expect(result.error).toContain('Email')
    })

    it('should reject registration with short password', async () => {
      const result = await validateRegistration({
        firstName: 'John',
        lastName: 'Doe',
        email: 'test@example.com',
        password: 'short',
      })
      expect(result.valid).toBe(false)
      expect(result.error).toContain('8 characters')
    })

    it('should accept valid registration data', async () => {
      const result = await validateRegistration({
        firstName: 'John',
        lastName: 'Doe',
        email: 'test@example.com',
        password: 'password123',
      })
      expect(result.valid).toBe(true)
    })
  })

  describe('Organization Validation', () => {
    it('should reject empty organization name', async () => {
      const result = await validateOrganization('')
      expect(result.valid).toBe(false)
      expect(result.error).toContain('required')
    })

    it('should reject whitespace-only organization name', async () => {
      const result = await validateOrganization('   ')
      expect(result.valid).toBe(false)
      expect(result.error).toContain('required')
    })

    it('should accept valid organization name', async () => {
      const result = await validateOrganization('Acme Corp')
      expect(result.valid).toBe(true)
    })
  })
})

async function validateLogin(email: string, password: string): Promise<{ valid: boolean; error?: string }> {
  if (!email || !password) {
    return { valid: false, error: 'Email and password are required' }
  }
  return { valid: true }
}

interface RegistrationData {
  firstName: string
  lastName: string
  email: string
  password: string
}

async function validateRegistration(data: RegistrationData): Promise<{ valid: boolean; error?: string }> {
  if (!data.firstName) {
    return { valid: false, error: 'First name is required' }
  }
  if (!data.lastName) {
    return { valid: false, error: 'Last name is required' }
  }
  if (!data.email) {
    return { valid: false, error: 'Email is required' }
  }
  if (!data.password) {
    return { valid: false, error: 'Password is required' }
  }
  if (data.password.length < 8) {
    return { valid: false, error: 'Password must be at least 8 characters' }
  }
  return { valid: true }
}

async function validateOrganization(name: string): Promise<{ valid: boolean; error?: string }> {
  if (!name.trim()) {
    return { valid: false, error: 'Organization name is required' }
  }
  return { valid: true }
}

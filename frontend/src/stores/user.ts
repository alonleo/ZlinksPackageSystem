import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { User } from '@/types/user'
import { authApi } from '@/api/auth'

export const useUserStore = defineStore('user', () => {
  const token = ref<string>(localStorage.getItem('token') || '')
  const user = ref<User | null>(null)

  const isAuthenticated = computed(() => !!token.value)
  const currentUser = computed(() => user.value)

  async function login(username: string, password: string) {
    const response = await authApi.login({ username, password })
    token.value = response.data
    localStorage.setItem('token', response.data)
    await fetchUserInfo()
  }

  async function fetchUserInfo() {
    try {
      const response = await authApi.getUserInfo()
      user.value = response.data
    } catch (error) {
      logout()
    }
  }

  function logout() {
    token.value = ''
    user.value = null
    localStorage.removeItem('token')
  }

  function setToken(newToken: string) {
    token.value = newToken
    localStorage.setItem('token', newToken)
  }

  return {
    token,
    user,
    isAuthenticated,
    currentUser,
    login,
    fetchUserInfo,
    logout,
    setToken,
  }
})
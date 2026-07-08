import { ref } from 'vue'
import { defineStore } from 'pinia'
import type { RouteLocationNormalized } from 'vue-router'

export interface Tab {
  path: string
  title: string
  name: string
}

export const useTabsStore = defineStore('tabs', () => {
  const tabs = ref<Tab[]>([])
  const activeTab = ref<string>('')

  function addTab(route: RouteLocationNormalized) {
    const title = (route.meta.title as string) || ''
    if (!title || route.name === 'home') return

    const existing = tabs.value.find(t => t.path === route.path)
    if (!existing) {
      tabs.value.push({
        path: route.path,
        title,
        name: route.name as string,
      })
    }
    activeTab.value = route.path
  }

  function removeTab(path: string) {
    const idx = tabs.value.findIndex(t => t.path === path)
    if (idx === -1) return

    tabs.value.splice(idx, 1)

    if (activeTab.value === path) {
      if (tabs.value.length > 0) {
        const next = tabs.value[Math.min(idx, tabs.value.length - 1)]
        activeTab.value = next.path
      } else {
        activeTab.value = ''
      }
    }
  }

  function closeOtherTabs(path: string) {
    tabs.value = tabs.value.filter(t => t.path === path)
    activeTab.value = path
  }

  function closeAllTabs() {
    tabs.value = []
    activeTab.value = ''
  }

  return { tabs, activeTab, addTab, removeTab, closeOtherTabs, closeAllTabs }
})

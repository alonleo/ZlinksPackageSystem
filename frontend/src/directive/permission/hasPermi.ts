import type { Directive, DirectiveBinding } from 'vue'
import { useUserStore } from '@/stores/user'

const SUPER_PERMISSION = '*:*:*'

export default {
  mounted(el: HTMLElement, binding: DirectiveBinding<string | string[]>) {
    const flags = Array.isArray(binding.value) ? binding.value : [binding.value]
    const permissions = useUserStore().permissions
    const hasPermission = flags.some(
      (f) => permissions.includes(SUPER_PERMISSION) || permissions.includes(f),
    )
    if (!hasPermission) {
      el.parentNode?.removeChild(el)
    }
  },
} as Directive<HTMLElement, string | string[]>
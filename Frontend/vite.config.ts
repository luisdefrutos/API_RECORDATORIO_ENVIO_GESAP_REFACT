import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { designSystemIconsVitePlugin } from '@tuvsud/design-system/vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), ...designSystemIconsVitePlugin()],
})

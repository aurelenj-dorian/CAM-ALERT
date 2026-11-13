import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Serveur ASP.NET Core en développement (voir Properties/launchSettings.json)
const serveur = 'http://localhost:5109'

export default defineConfig(({ command }) => ({
  plugins: [react()],
  // En production, les fichiers sont servis par ASP.NET depuis wwwroot/app/
  base: command === 'build' ? '/app/' : '/',
  build: {
    outDir: '../wwwroot/app',
    emptyOutDir: true,
    rollupOptions: {
      input: {
        main: 'index.html',
        // Scripts et styles de l'espace admin (Razor), à nom fixe
        admin: 'src/admin/main.ts',
      },
      output: {
        entryFileNames: (chunk) =>
          chunk.name === 'admin' ? 'assets/admin.js' : 'assets/[name]-[hash].js',
        assetFileNames: (asset) =>
          (asset.names ?? []).includes('admin.css')
            ? 'assets/admin.css'
            : 'assets/[name]-[hash][extname]',
      },
    },
  },
  server: {
    port: 5173,
    proxy: {
      '/api': serveur,
      '/admin': serveur,
      '/uploads': serveur,
      '/Admin/image': serveur,
    },
  },
}))

// @ts-check
import { defineConfig } from 'astro/config';
import tailwindcss from '@tailwindcss/vite';

// https://astro.build/config
export default defineConfig({
  vite: {
    plugins: [tailwindcss()],
  },
  trailingSlash: 'ignore',
  build: { format: 'directory' }, // Generates clean, crawlable folders
});
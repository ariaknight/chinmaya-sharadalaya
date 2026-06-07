/** @type {import('tailwindcss').Config} */
export default {
  content: ['./src/**/*.{astro,html,js,jsx,md,mdx,svelte,ts,tsx,vue}'],
  theme: {
    extend: {
      fontFamily: {
        serif: ['Cormorant Garamond', 'serif'],
        sans: ['Inter', 'sans-serif'],
      },
      colors: {
        maroon: {
          50: '#faf5f4',
          100: '#f5ebe8',
          200: '#ead7d1',
          300: '#e0c3ba',
          400: '#d5afa3',
          500: '#8B2F39',
          600: '#7a2730',
          700: '#6B2027',
          800: '#5c1a1f',
          900: '#4d1217',
          950: '#2d0a0e',
        },
        burgundy: {
          50: '#faf8f7',
          100: '#f5f1ef',
          200: '#ebe3df',
          300: '#e1d5cf',
          400: '#d7c7bf',
          500: '#A0373D',
          600: '#8a2f34',
          700: '#74272b',
          800: '#5e1f22',
          900: '#481719',
          950: '#2a0e10',
        },
        gold: '#d4af37',
        cream: '#faf5f0',
        charcoal: '#2a2a2a',
      },
    },
  },
  plugins: [],
}

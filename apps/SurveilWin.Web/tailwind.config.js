/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      colors: {
        base:     '#1e1e2e',
        mantle:   '#181825',
        crust:    '#11111b',
        surface0: '#313244',
        surface1: '#45475a',
        subtext0: '#a6adc8',
        text:     '#cdd6f4',
        blue:     '#89b4fa',
        mauve:    '#cba6f7',
        red:      '#f38ba8',
        green:    '#a6e3a1',
        yellow:   '#f9e2af',
        teal:     '#94e2d5',
        sky:      '#89dceb',
      }
    }
  },
  plugins: [],
}


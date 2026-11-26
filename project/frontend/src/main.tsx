import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import './tailwind.css'
import App from './App.tsx'
import { applyMode, initialMode } from './theme/colorMode'

// The same resolution runs in /theme-init.js before the stylesheet paints; this
// call keeps the React tree in sync when the page is rendered without that
// script (tests, component previews).
applyMode(initialMode())

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)

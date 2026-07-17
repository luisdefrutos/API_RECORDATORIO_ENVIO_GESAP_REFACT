import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'

// TÜV SÜD Design System: Registrar los iconos de Material Symbols
import { registerGoogleMaterial } from '@tuvsud/design-system/icon-libraries'
registerGoogleMaterial()

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)

import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { TsIcon } from '@tuvsud/design-system/react';
import { DebugLauncher } from './pages/DebugLauncher';
import { RecordatorioForm } from './pages/RecordatorioForm';

function App() {
  return (
    <BrowserRouter>
      {/* Layout Principal muy sencillo */}
      <header style={{ 
        backgroundColor: 'var(--tuv-color-primary-600, #005a9c)', 
        color: 'white', 
        padding: '1rem 2rem',
        display: 'flex',
        alignItems: 'center',
        gap: '1rem'
      }}>
        <TsIcon name="notifications" style={{ fontSize: '24px' }} />
        <span style={{ fontSize: '1.25rem', fontWeight: 'bold' }}>GESAP - Recordatorios</span>
      </header>
      
      <main style={{ minHeight: 'calc(100vh - 70px)', backgroundColor: 'var(--tuv-color-neutral-50, #f8f9fa)' }}>
        <Routes>
          <Route path="/debug" element={<DebugLauncher />} />
          <Route path="/respuesta" element={<RecordatorioForm />} />
          {/* Si alguien entra a la raíz, por defecto le mandamos al debug en modo desarrollo */}
          <Route path="/" element={<Navigate to="/debug" replace />} />
        </Routes>
      </main>
    </BrowserRouter>
  )
}

export default App

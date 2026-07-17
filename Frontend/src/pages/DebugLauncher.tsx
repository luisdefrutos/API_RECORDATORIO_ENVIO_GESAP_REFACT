import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { TsCard, TsInput, TsButton, TsIcon, TsAlert } from '@tuvsud/design-system/react';
import { apiService } from '../services/apiService';

export function DebugLauncher() {
  const [id, setId] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const navigate = useNavigate();

  const handleGenerate = async () => {
    if (!id.trim()) {
      setError('Por favor, introduce un ID válido.');
      return;
    }

    try {
      setLoading(true);
      setError('');
      const data = await apiService.generarTokenDebug(id);
      
      const token = data.Token || data.token;
      if (token) {
        // Redirigir a la vista del formulario pasándole el token por Query Params (como hacía el proxy)
        navigate(`/respuesta?id=${encodeURIComponent(token)}`);
      } else {
        setError('El servidor no devolvió un token válido.');
      }
    } catch (err: any) {
      setError(err.message || 'Ocurrió un error al generar el token.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{ padding: '2rem', display: 'flex', justifyContent: 'center' }}>
      <TsCard style={{ width: '100%', maxWidth: '500px' }}>
        <h2 style={{ marginTop: 0 }}>Lanzador de Pruebas (Debug)</h2>
        <p style={{ marginBottom: '1.5rem', color: 'var(--tuv-color-neutral-600)' }}>
          Introduce un ID de recordatorio de la base de datos local para generar un link simulando el correo electrónico.
        </p>

        {error && (
          <TsAlert variant="danger" open style={{ marginBottom: '1rem' }}>
            <TsIcon slot="icon" name="error" />
            {error}
          </TsAlert>
        )}

        <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
          <TsInput 
            label="ID de Recordatorio" 
            placeholder="Ej. 1234" 
            value={id}
            onTsInput={(e: any) => setId(e.target.value)}
          />
          
          <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: '1rem' }}>
            <TsButton variant="primary" loading={loading} onClick={handleGenerate}>
              <TsIcon name="link" slot="prefix" />
              Generar Link y Abrir
            </TsButton>
          </div>
        </div>
      </TsCard>
    </div>
  );
}

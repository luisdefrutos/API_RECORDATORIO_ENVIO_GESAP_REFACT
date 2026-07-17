import { useState, useEffect } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { TsCard, TsInput, TsButton, TsIcon, TsAlert, TsSpinner } from '@tuvsud/design-system/react';
import { apiService } from '../services/apiService';

// Tipado básico del DTO devuelto por la API (camelCase)
interface RecordatorioDto {
  idRecordatorioEnvio: number;
  identificadorRecEnvio: string;
  titularRazonSocial: string;
  titularEmail: string;
  titularTelefono: string;
  facturarA: string;
  estadoDescripcion: string;
}

export function RecordatorioForm() {
  const location = useLocation();
  const navigate = useNavigate();
  const queryParams = new URLSearchParams(location.search);
  const token = queryParams.get('id'); // Token cifrado recibido en la URL

  const [dto, setDto] = useState<RecordatorioDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [successMsg, setSuccessMsg] = useState('');

  useEffect(() => {
    if (!token) {
      setError('Acceso denegado: No se ha proporcionado un token válido.');
      setLoading(false);
      return;
    }

    const cargarDatos = async () => {
      try {
        setLoading(true);
        const data = await apiService.obtenerRecordatorio(token);
        setDto(data);
      } catch (err: any) {
        setError(err.message || 'No se pudo cargar el recordatorio.');
      } finally {
        setLoading(false);
      }
    };

    cargarDatos();
  }, [token]);

  const handleChange = (field: keyof RecordatorioDto, value: string) => {
    if (dto) {
      setDto({ ...dto, [field]: value });
    }
  };

  const handleSave = async () => {
    if (!dto || !token) return;

    try {
      setSaving(true);
      setError('');
      setSuccessMsg('');
      
      const response = await apiService.enviarRespuesta(dto, token);
      
      if (response && response.success) {
        setSuccessMsg(response.message); // El backend manda HTML en el message
      }
    } catch (err: any) {
      setError(err.message || 'Ocurrió un error al guardar los datos.');
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', padding: '4rem' }}>
        <TsSpinner style={{ fontSize: '3rem' }} />
      </div>
    );
  }

  return (
    <div style={{ padding: '2rem', display: 'flex', justifyContent: 'center' }}>
      <TsCard style={{ width: '100%', maxWidth: '800px' }}>
        <h2 style={{ marginTop: 0 }}>
          Respuesta a Recordatorio 
          {dto?.identificadorRecEnvio && ` - ${dto.identificadorRecEnvio}`}
        </h2>

        {error && (
          <TsAlert variant="danger" open style={{ marginBottom: '1rem' }}>
            <TsIcon slot="icon" name="error" />
            {error}
          </TsAlert>
        )}

        {successMsg && (
          <TsAlert variant="success" open style={{ marginBottom: '1rem' }}>
            <TsIcon slot="icon" name="check_circle" />
            <div dangerouslySetInnerHTML={{ __html: successMsg }} />
          </TsAlert>
        )}

        {!dto ? (
          <div style={{ textAlign: 'center', margin: '2rem 0' }}>
            <p>No hay datos disponibles. El enlace podría haber caducado o ser incorrecto.</p>
            <TsButton variant="default" onClick={() => navigate('/debug')}>
              Ir al Lanzador Debug
            </TsButton>
          </div>
        ) : (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
              <TsInput 
                label="Razón Social (Titular)" 
                value={dto.titularRazonSocial || ''}
                onTsInput={(e: any) => handleChange('titularRazonSocial', e.target.value)}
              />
              <TsInput 
                label="Email (Titular)" 
                type="email"
                value={dto.titularEmail || ''}
                onTsInput={(e: any) => handleChange('titularEmail', e.target.value)}
              />
              <TsInput 
                label="Teléfono (Titular)" 
                value={dto.titularTelefono || ''}
                onTsInput={(e: any) => handleChange('titularTelefono', e.target.value)}
              />
              <TsInput 
                label="Estado Actual" 
                value={dto.estadoDescripcion || ''}
                disabled
              />
            </div>
            
            <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: '2rem', gap: '1rem' }}>
              <TsButton variant="primary" loading={saving} onClick={handleSave}>
                <TsIcon name="save" slot="prefix" />
                Guardar Respuesta
              </TsButton>
            </div>
          </div>
        )}
      </TsCard>
    </div>
  );
}

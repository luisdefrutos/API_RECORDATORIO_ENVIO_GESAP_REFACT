const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:44321/api';
const API_KEY = import.meta.env.VITE_API_KEY || '';

export const apiService = {
  /**
   * Obtiene los datos del recordatorio a partir del token (ID cifrado)
   */
  async obtenerRecordatorio(token: string) {
    const response = await fetch(`${API_BASE_URL}/recordatorio/${encodeURIComponent(token)}`, {
      method: 'GET',
      headers: {
        'X-API-Key': API_KEY,
        'Accept': 'application/json'
      }
    });

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(errorText || 'Error al obtener el recordatorio');
    }

    return response.json();
  },

  /**
   * Envía los datos actualizados del recordatorio
   */
  async enviarRespuesta(dto: any, token: string) {
    const response = await fetch(`${API_BASE_URL}/recordatorio`, {
      method: 'POST',
      headers: {
        'X-API-Key': API_KEY,
        'Content-Type': 'application/json',
        'X-Inmutability-Token': token
      },
      body: JSON.stringify(dto)
    });

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(errorText || 'Error al guardar el recordatorio');
    }

    return response.json();
  },

  /**
   * Genera un token cifrado para pruebas en local (sustituye al debug_launcher.aspx)
   */
  async generarTokenDebug(id: string) {
    const response = await fetch(`${API_BASE_URL}/recordatorio/encrypt/${encodeURIComponent(id)}`, {
      method: 'GET',
      headers: {
        'X-API-Key': API_KEY,
        'Accept': 'application/json'
      }
    });

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(errorText || 'Error al generar el token (¿Estás en EsDesarrollo=true?)');
    }

    return response.json();
  }
};

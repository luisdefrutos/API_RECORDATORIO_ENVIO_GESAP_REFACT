using System;

namespace RecordatorioEnvio.Domain.Enums
{
    /// <summary>
    /// Estados permitidos para la respuesta del recordatorio de envío (Tabla maestra).
    /// </summary>
    public enum EstadosRespuesta : long
    {
        /// <summary>1 - Estado inicial al enviar el recordatorio.</summary>
        Emitido = 1,
        
        /// <summary>2 - Estado tras la revisión/guardado del cliente.</summary>
        PendienteRevision = 2,
        
        /// <summary>3 - Estado final tras la tramitación administrativa.</summary>
        Tramitado = 3
    }
}

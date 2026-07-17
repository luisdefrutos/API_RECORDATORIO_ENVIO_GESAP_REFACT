using System;
using System.Linq;
using System.Text;
using RecordatorioEnvio.Application.DTOs;

namespace RecordatorioEnvio.Application.Services
{
    public static class EmailTemplateBuilder
    {
        public static string ConstruirCuerpoEmail(RecordatorioEnvioRespDto dto)
        {
            var sb = new StringBuilder();

            // Font & Colors
            string fontFamily = "font-family: 'Noto Sans', Arial, sans-serif;";
            string mainColor = "#005a9c"; // TÜV SÜD Blue
            string borderColor = "#a0aab5"; // Un poco más oscuro para que se vea bien el borde
            string bgBody = "#f4f4f4";
            string bgContainer = "#ffffff";
            string labelColor = "#333333";
            string inputBg = "#f4f6f8";
            string inputText = "#333333";
            
            // Reemplazamos los fieldsets por tablas completas para evitar que Outlook recorte el borde derecho al usar padding
            string sFieldsetTable = $"width: 100%; border: 1px solid {borderColor}; border-radius: 4px; margin-bottom: 8px; background-color: #ffffff; border-collapse: separate; border-spacing: 0;";
            string sFieldsetTd = "padding: 6px 12px;";
            
            string sLegend = $"color: {mainColor}; font-weight: bold; font-size: 13px; margin-bottom: 6px; display: block; border-bottom: 1px solid #eaeaea; padding-bottom: 3px;";
            string sLabel = $"display: block; font-size: 11px; color: {labelColor}; margin-bottom: 1px; font-weight: 600;";
            
            // Para asegurar que los inputs muestren SIEMPRE sus 4 bordes y no se recorten en Outlook, usamos tablas también para ellos
            string sInputTable = $"width: 100%; border-collapse: separate; border-spacing: 0; border: 1px solid {borderColor}; border-radius: 3px; background-color: {inputBg};";
            string sInputTd = $"padding: 4px 6px; font-size: 11px; color: {inputText};";
            
            string sThFirst = $"background-color: {mainColor}; color: white; padding: 6px 8px; font-size: 11px; font-weight: 600; border-top: 1px solid {mainColor}; border-left: 1px solid {mainColor}; border-bottom: 1px solid {mainColor}; border-right: 1px solid #004a80;";
            string sThMiddle = $"background-color: {mainColor}; color: white; padding: 6px 8px; font-size: 11px; font-weight: 600; border-top: 1px solid {mainColor}; border-bottom: 1px solid {mainColor}; border-right: 1px solid #004a80;";
            string sThLast = $"background-color: {mainColor}; color: white; padding: 6px 8px; font-size: 11px; font-weight: 600; border-top: 1px solid {mainColor}; border-bottom: 1px solid {mainColor}; border-right: 1px solid {mainColor};";
            
            string sTdFirst = $"padding: 4px 8px; border-left: 1px solid {borderColor}; border-bottom: 1px solid {borderColor}; border-right: 1px solid {borderColor}; font-size: 11px;";
            string sTdMiddle = $"padding: 4px 8px; border-bottom: 1px solid {borderColor}; border-right: 1px solid {borderColor}; font-size: 11px;";
            string sTdLast = $"padding: 4px 8px; border-bottom: 1px solid {borderColor}; border-right: 1px solid {borderColor}; font-size: 11px;";
            string sTableWrap = "width:100%; border-collapse:collapse;";

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine($"<html lang='es' style='background-color: {bgBody}; margin: 0; padding: 5px;'>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset='utf-8'>");
            sb.AppendLine("</head>");
            sb.AppendLine($"<body style='{fontFamily} background-color: {bgBody}; padding: 5px; margin: 0;'>");
            
            // Envolver todo en una tabla de ancho fijo para Outlook/Móviles
            sb.AppendLine("<table width='800' align='center' cellpadding='0' cellspacing='0' style='margin: 0 auto; background-color: #ffffff; border-radius: 4px; box-shadow: 0 2px 10px rgba(0,0,0,0.05);'><tr><td style='padding: 15px;'>");
            
            // Header
            sb.AppendLine($"<div style='border-bottom: 2px solid {mainColor}; padding-bottom: 6px; margin-bottom: 10px;'>");
            sb.AppendLine("<table width='100%' cellpadding='0' cellspacing='0' border='0'><tr>");
            sb.AppendLine("<td style='width: 40px; vertical-align: middle;'>");
            sb.AppendLine("<a href='https://www.tuvsud.com/es-es' target='_blank' style='text-decoration:none; display: block;'>");
            sb.AppendLine("<img src='cid:logotuv' alt='TÜV SÜD Logo' style='height: 35px; display: block; border: 0;' />");
            sb.AppendLine("</a>");
            sb.AppendLine("</td>");
            sb.AppendLine("<td style='vertical-align: middle; padding-left: 10px;'>");
            sb.AppendLine($"<h1 style='color:{mainColor}; font-family:\"Noto Sans\",Arial,sans-serif; margin:0; font-size: 16px;'>Oferta aceptada : {dto.IdentificadorRecEnvio}</h1>");
            sb.AppendLine("</td>");
            sb.AppendLine("</tr></table>");
            sb.AppendLine("</div>");

            string Safe(string val) => string.IsNullOrWhiteSpace(val) ? "&nbsp;" : System.Net.WebUtility.HtmlEncode(val);
            string BuildInput(string label, string val) => $"<label style='{sLabel}'>{label}</label><table style='{sInputTable}'><tr><td style='{sInputTd}'>{Safe(val)}</td></tr></table>";
            string sSpacer = "<div style='height: 2px; line-height: 2px; font-size: 2px;'>&nbsp;</div>";
            string sSpacerBig = "<div style='height: 4px; line-height: 4px; font-size: 4px;'>&nbsp;</div>";

            // BLOQUE 1: DATOS GENERALES
            sb.AppendLine($"<table style='{sFieldsetTable}'><tr><td style='{sFieldsetTd}'>");
            sb.AppendLine($"<div style='{sLegend}'>Datos Generales</div>");
            sb.AppendLine($"<table style='{sTableWrap}'><tr>");
            sb.AppendLine($"<td style='width:35%; padding-right:15px;vertical-align:top; border:none;'>{BuildInput("Nombre Emplazamiento", dto.EmplazamientoNombre)}</td>");
            sb.AppendLine($"<td style='width:65%; vertical-align:top; border:none;'>{BuildInput("Dirección Emplazamiento", dto.EmplazamientoDireccion)}</td>");
            sb.AppendLine("</tr></table>");
            sb.AppendLine("</td></tr></table>");
            sb.AppendLine(sSpacerBig);

            // EQUIPOS Y ACTUACIONES
            sb.AppendLine($"<table style='{sFieldsetTable}'><tr><td style='{sFieldsetTd}'>");
            sb.AppendLine($"<div style='{sLegend}'>Equipos y Actuaciones</div>");
            sb.AppendLine($"<table style='width: 100%; border-collapse: separate; border-spacing: 0; margin-top: 10px; table-layout: fixed;'>");
            sb.AppendLine($"<thead><tr><th style='{sThFirst} text-align:left; width:45%;'>Equipo</th><th style='{sThMiddle} text-align:center; width:20%;'>Tipo Inspección</th><th style='{sThMiddle} text-align:center; width:20%;'>Fecha Prox. Actuación</th><th style='{sThLast} text-align:right; width:15%;'>Precio</th></tr></thead>");
            sb.AppendLine("<tbody>");
            if (dto.Detalles != null && dto.Detalles.Any())
            {
                foreach (var item in dto.Detalles)
                {
                    string fecha = item.FechaProximaActuacion.HasValue ? item.FechaProximaActuacion.Value.ToString("dd/MM/yyyy") : "-";
                    string precio = item.PrecioTotal.HasValue ? item.PrecioTotal.Value.ToString("N2") + " €" : "-";
                    sb.AppendLine($"<tr><td style='{sTdFirst}'>{Safe(item.NombreEquipo)}</td><td style='{sTdMiddle} text-align:center;'>{Safe(item.TipoInspeccion)}</td><td style='{sTdMiddle} text-align:center;'>{fecha}</td><td style='{sTdLast} text-align:right;'>{precio}</td></tr>");
                }
            }
            else
            {
                sb.AppendLine($"<tr><td colspan='4' style='{sTdFirst} border-right: 1px solid {borderColor}; text-align:center;'>No hay equipos detallados</td></tr>");
            }
            sb.AppendLine("</tbody>");
            sb.AppendLine("</table>");
            
            // NOTAS
            if (dto.Notas != null && dto.Notas.Any(n => !string.IsNullOrWhiteSpace(n.DescRecordatorioEnvioNota)))
            {
                sb.AppendLine("<div style='margin-top: 10px; border-top: 1px dashed #ccc; padding-top: 8px;'>");
                sb.AppendLine($"<h4 style='color: {mainColor}; margin: 0 0 6px 0; font-size: 12px;'>Notas Adicionales</h4>");
                foreach (var nota in dto.Notas.Where(n => !string.IsNullOrWhiteSpace(n.DescRecordatorioEnvioNota)))
                {
                    sb.AppendLine($"<table style='{sInputTable} margin-bottom:6px;'><tr><td style='{sInputTd}'>{nota.DescRecordatorioEnvioNota}</td></tr></table>");
                }
                sb.AppendLine("</div>");
            }
            sb.AppendLine("</td></tr></table>");
            sb.AppendLine(sSpacerBig);

            // CONDICIONES ECONÓMICAS
            sb.AppendLine($"<table style='{sFieldsetTable}'><tr><td style='{sFieldsetTd}'>");
            sb.AppendLine($"<div style='{sLegend}'>Condiciones económicas</div>");
            decimal precioTotal = dto.Detalles != null ? dto.Detalles.Sum(d => d.PrecioTotal ?? 0) : 0;
            
            // PRECIO (Inline sin caja)
            sb.AppendLine($"<div style='margin-bottom:8px; font-size:13px; color:{inputText};'><span style='font-weight:bold; color:{labelColor}; margin-right:8px;'>Precio total inspección:</span> <strong>{precioTotal.ToString("N2")} €</strong></div>");
            
            // CONDICIONES DE BBDD
            string condGenerales = dto.RecordatorioEnvio?.CondicionesEcoGenerales;
            string condCorreccion = dto.RecordatorioEnvio?.CondicionesEcoCorreccion;
            
            if (!string.IsNullOrWhiteSpace(condGenerales) || !string.IsNullOrWhiteSpace(condCorreccion))
            {
                sb.AppendLine($"<div style='font-size: 11px; color: {labelColor}; line-height: 1.4; white-space: pre-line;'>");
                if (!string.IsNullOrWhiteSpace(condGenerales)) sb.AppendLine(Safe(condGenerales));
                if (!string.IsNullOrWhiteSpace(condCorreccion)) sb.AppendLine(Safe(condCorreccion));
                sb.AppendLine("</div>");
            }
            
            sb.AppendLine("</td></tr></table>");
            sb.AppendLine(sSpacerBig);

            // DATOS DEL TITULAR
            sb.AppendLine($"<table style='{sFieldsetTable}'><tr><td style='{sFieldsetTd}'>");
            sb.AppendLine($"<div style='{sLegend}'>Datos del Titular</div>");
            sb.AppendLine($"<table style='{sTableWrap}'><tr>");
            sb.AppendLine($"<td style='width:15%; padding-right:10px; vertical-align:top; border:none;'>{BuildInput("NIF", dto.TitularNif)}</td>");
            sb.AppendLine($"<td style='width:50%; padding-right:10px; vertical-align:top; border:none;'>{BuildInput("Razón Social", dto.TitularRazonSocial)}</td>");
            sb.AppendLine($"<td style='width:35%; vertical-align:top; border:none;'>{BuildInput("Persona de Contacto", dto.TitularPersonaContacto)}</td>");
            sb.AppendLine("</tr></table>");
            sb.AppendLine(sSpacer);
            sb.AppendLine($"<table style='{sTableWrap}'><tr>");
            sb.AppendLine($"<td style='width:50%; padding-right:10px; vertical-align:top; border:none;'>{BuildInput("Dirección", dto.TitularDireccion)}</td>");
            sb.AppendLine($"<td style='width:15%; padding-right:10px; vertical-align:top; border:none;'>{BuildInput("Teléfono", dto.TitularTelefono)}</td>");
            sb.AppendLine($"<td style='width:35%; vertical-align:top; border:none;'>{BuildInput("Email", dto.TitularEmail)}</td>");
            sb.AppendLine("</tr></table>");
            sb.AppendLine("</td></tr></table>");
            sb.AppendLine(sSpacerBig);

            // DATOS DEL REPRESENTANTE
            sb.AppendLine($"<table style='{sFieldsetTable}'><tr><td style='{sFieldsetTd}'>");
            sb.AppendLine($"<div style='{sLegend}'>Datos del Representante/Gestor</div>");
            sb.AppendLine($"<table style='{sTableWrap}'><tr>");
            sb.AppendLine($"<td style='width:15%; padding-right:10px; vertical-align:top; border:none;'>{BuildInput("NIF", dto.RepresentanteNif)}</td>");
            sb.AppendLine($"<td style='width:50%; padding-right:10px; vertical-align:top; border:none;'>{BuildInput("Razón Social", dto.RepresentanteRazonSocial)}</td>");
            sb.AppendLine($"<td style='width:35%; vertical-align:top; border:none;'>{BuildInput("Persona de Contacto", dto.RepresentantePersonaContacto)}</td>");
            sb.AppendLine("</tr></table>");
            sb.AppendLine(sSpacer);
            sb.AppendLine($"<table style='{sTableWrap}'><tr>");
            sb.AppendLine($"<td style='width:50%; padding-right:10px; vertical-align:top; border:none;'>{BuildInput("Dirección", dto.RepresentanteDireccion)}</td>");
            sb.AppendLine($"<td style='width:15%; padding-right:10px; vertical-align:top; border:none;'>{BuildInput("Teléfono", dto.RepresentanteTelefono)}</td>");
            sb.AppendLine($"<td style='width:35%; vertical-align:top; border:none;'>{BuildInput("Email", dto.RepresentanteEmail)}</td>");
            sb.AppendLine("</tr></table>");
            sb.AppendLine("</td></tr></table>");
            sb.AppendLine(sSpacerBig);

            // DATOS DE FACTURACIÓN
            sb.AppendLine($"<table style='{sFieldsetTable}'><tr><td style='{sFieldsetTd}'>");
            sb.AppendLine($"<div style='{sLegend}'>Datos de Facturación</div>");
            sb.AppendLine($"<table style='{sTableWrap}'><tr>");
            sb.AppendLine($"<td style='width:20%; padding-right:10px; vertical-align:top; border:none;'>{BuildInput("Facturar A", dto.FacturarA)}</td>");
            sb.AppendLine($"<td style='width:30%; padding-right:10px; vertical-align:top; border:none;'>{BuildInput("Descripción Otro", dto.FacturarAOtroDescripcion)}</td>");
            sb.AppendLine($"<td style='width:20%; padding-right:10px; vertical-align:top; border:none;'>{BuildInput("Forma de Pago", dto.FacturarFormaPago)}</td>");
            sb.AppendLine($"<td style='width:30%; vertical-align:top; border:none;'>{BuildInput("Cuenta Bancaria (IBAN)", dto.FacturarCuentaBanco)}</td>");
            sb.AppendLine("</tr></table>");
            sb.AppendLine("</td></tr></table>");

            sb.AppendLine("</td></tr></table>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }
    }
}

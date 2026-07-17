using RecordatorioEnvio.Application.DTOs;
using System;
using System.Collections.Generic;

namespace RecordatorioEnvio.Application.Services
{
    /// <summary>
    /// Responsabilidad única: validar las reglas de negocio del formulario.
    /// Extraído de RecordatorioService para cumplir SRP.
    /// Retorna una lista de mensajes de error; si la lista está vacía, todo es válido.
    /// </summary>
    internal static class RecordatorioValidator
    {
        internal static List<string> Validate(RecordatorioEnvioRespDto dto)
        {
            var errors = new List<string>();

            // --- LONGITUDES MÁXIMAS (según DDL Oracle) ---
            CheckLength(errors, dto.EmplazamientoNombre,          255, "Nombre Emplazamiento");
            CheckLength(errors, dto.EmplazamientoDireccion,       255, "Dirección Emplazamiento");
            CheckLength(errors, dto.TitularNif,                    25, "NIF Titular");
            CheckLength(errors, dto.TitularRazonSocial,           155, "Razón Social Titular");
            CheckLength(errors, dto.TitularDireccion,             255, "Dirección Titular");
            CheckLength(errors, dto.TitularPersonaContacto,       255, "Contacto Titular");
            CheckLength(errors, dto.TitularTelefono,               15, "Teléfono Titular");
            CheckLength(errors, dto.TitularEmail,                 155, "Email Titular");
            CheckLength(errors, dto.RepresentanteNif,              25, "NIF Representante");
            CheckLength(errors, dto.RepresentanteRazonSocial,     155, "Razón Social Representante");
            CheckLength(errors, dto.RepresentanteDireccion,       255, "Dirección Representante");
            CheckLength(errors, dto.RepresentantePersonaContacto, 255, "Contacto Representante");
            CheckLength(errors, dto.RepresentanteTelefono,         15, "Teléfono Representante");
            CheckLength(errors, dto.RepresentanteEmail,           155, "Email Representante");
            CheckLength(errors, dto.FacturarA,                     20, "Facturar A");
            CheckLength(errors, dto.FacturarAOtroDescripcion,     255, "Descripción Facturar Otro");
            CheckLength(errors, dto.FacturarFormaPago,             20, "Forma de Pago");
            CheckLength(errors, dto.FacturarCuentaBanco,           34, "Cuenta Bancaria (IBAN)");

            // --- REGLAS DE NEGOCIO ---

            // 1. Email Titular
            if (dto.FacturarA == "TITULAR")
            {
                if (string.IsNullOrEmpty(dto.TitularEmail) || !IsValidEmail(dto.TitularEmail))
                    errors.Add("Si factura al Titular, el Email del Titular es obligatorio y debe ser válido.");
            }
            else if (!string.IsNullOrEmpty(dto.TitularEmail) && !IsValidEmail(dto.TitularEmail))
            {
                errors.Add($"El Email del Titular ({dto.TitularEmail}) no tiene un formato válido.");
            }

            // 2. Email Representante
            if (dto.FacturarA == "GESTOR")
            {
                if (string.IsNullOrEmpty(dto.RepresentanteEmail) || !IsValidEmail(dto.RepresentanteEmail))
                    errors.Add("Si factura al Representante, el Email del Representante es obligatorio y debe ser válido.");
            }
            else if (!string.IsNullOrEmpty(dto.RepresentanteEmail) && !IsValidEmail(dto.RepresentanteEmail))
            {
                errors.Add($"El Email del Representante ({dto.RepresentanteEmail}) no tiene un formato válido.");
            }

            // 3. Cuenta Bancaria (IBAN) - solo si Domiciliación
            if (dto.FacturarFormaPago == "DOMICILIACION")
            {
                if (string.IsNullOrEmpty(dto.FacturarCuentaBanco) || !IsValidIBAN(dto.FacturarCuentaBanco))
                    errors.Add("Para Domiciliación, la Cuenta Bancaria (IBAN) es obligatoria y debe ser válida.");
            }

            // 4. Notas
            ValidateNotes(dto.Notas, errors);

            return errors;
        }

        internal static void ValidateNotes(IEnumerable<RecordatorioEnvioRespNotaDto> notas, List<string> errors)
        {
            if (notas == null) return;
            foreach (var nota in notas)
            {
                if (string.IsNullOrWhiteSpace(nota.DescRecordatorioEnvioNota)) continue;

                var txt = nota.DescRecordatorioEnvioNota.Trim();
                if (txt.Length > 255)
                    errors.Add($"Una nota supera los 255 caracteres permitidos (tiene {txt.Length}).");
                
                if (txt.IndexOf('<') >= 0 || txt.IndexOf('>') >= 0)
                    errors.Add("El texto de una nota contiene caracteres no permitidos (< >).");
            }
        }

        private static void CheckLength(List<string> errors, string value, int max, string label)
        {
            if (!string.IsNullOrEmpty(value) && value.Length > max)
                errors.Add($"'{label}' supera el máximo permitido de {max} caracteres (tiene {value.Length}).");
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsValidIBAN(string iban)
        {
            if (string.IsNullOrEmpty(iban)) return false;
            iban = iban.Replace(" ", "").Replace("-", "").ToUpper();

            if (!System.Text.RegularExpressions.Regex.IsMatch(iban, @"^[A-Z]{2}\d{2}[A-Z0-9]{1,30}$"))
                return false;

            // Algoritmo Módulo 97
            string reordered = iban.Substring(4) + iban.Substring(0, 4);
            var numericIban = new System.Text.StringBuilder();
            foreach (char c in reordered)
                numericIban.Append(char.IsLetter(c) ? (c - 'A' + 10).ToString() : c.ToString());

            int remainder = 0;
            foreach (char c in numericIban.ToString())
                remainder = (remainder * 10 + (c - '0')) % 97;

            return remainder == 1;
        }
    }
}

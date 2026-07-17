using System;

namespace RecordatorioEnvio.Application.DTOs
{
    public class LogEntryDto
    {
        public long LogId { get; set; }
        public DateTime LogDate { get; set; }
        public string LogLevel { get; set; }
        public string Message { get; set; }
        public string ExceptionMsg { get; set; }
        public string IpAddress { get; set; }
        public string Endpoint { get; set; }
        public string SqlQuery { get; set; }
    }
}

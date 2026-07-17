using System;
using System.Collections.Generic;
using System.Web.Http.Dependencies;
using RecordatorioEnvio.Application.Services;
using RecordatorioEnvio.Domain.Interfaces;
using RecordatorioEnvio.Infrastructure.Data;
using RecordatorioEnvio.Infrastructure.Encryption;
using RecordatorioEnvio.Infrastructure.Repositories;

namespace RecordatorioEnvio.API.App_Start
{
    public class SimpleDependencyResolver : IDependencyResolver
    {
        // Singleton instances
        private readonly OracleConnectionFactory _connectionFactory;
        private readonly IEncryptionService      _encryptionService; // DIP: ahora es la abstracción
        
        public SimpleDependencyResolver()
        {
            _connectionFactory = new OracleConnectionFactory();
            _encryptionService = new EncryptionService(); // La implementación concreta se asigna aquí (raíz de composición)
        }

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(Controllers.RecordatorioController))
            {
                return new Controllers.RecordatorioController(CreateRecordatorioService(), _encryptionService, CreateEmailNotificationService());
            }

            return null;
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return new List<object>();
        }

        public IDependencyScope BeginScope()
        {
            return this;
        }

        public void Dispose()
        {
            // No resources to dispose for now
        }

        // Factory Method / Raíz de Composición
        private RecordatorioService CreateRecordatorioService()
        {
            var recordatorioRepo = new RecordatorioRepository(_connectionFactory);
            IEstadoRepository estadoRepo = new EstadoRepository(_connectionFactory);

            // RecordatorioRepository implementa tanto IRecordatorioRepository como IAuditoriaRepository (ISP)
            return new RecordatorioService(recordatorioRepo, estadoRepo, _encryptionService, recordatorioRepo);
        }

        private IEmailNotificationService CreateEmailNotificationService()
        {
            ISysConfiguracionRepository configRepo = new SysConfiguracionRepository(_connectionFactory);
            return new RecordatorioEnvio.Infrastructure.Services.EmailNotificationService(configRepo);
        }
    }
}

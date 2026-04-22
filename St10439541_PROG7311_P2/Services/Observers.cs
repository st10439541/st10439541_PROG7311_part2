using St10439541_PROG7311_P2.Models;

namespace St10439541_PROG7311_P2.Services
{

    namespace Observers
    {
        // OBSERVER INTERFACES

        public interface IContractObserver
        {
            Task OnContractCreated(Contract contract);
            Task OnContractSigned(Contract contract);
            Task OnContractExpired(Contract contract);
            Task OnContractStatusChanged(Contract contract, ContractStatus oldStatus, ContractStatus newStatus);
        }

        public interface IServiceRequestObserver
        {
            Task OnServiceRequestCreated(ServiceRequest request);
            Task OnServiceRequestStatusChanged(ServiceRequest request, RequestStatus oldStatus, RequestStatus newStatus);
        }

        public interface IClientObserver
        {
            Task OnClientCreated(Client client);
            Task OnClientUpdated(Client client);
            Task OnClientDeleted(Client client);
        }


        // LOGGING OBSERVER

        public class LoggingObserver : IContractObserver, IServiceRequestObserver, IClientObserver
        {
            private readonly ILogger<LoggingObserver> _logger;

            public LoggingObserver(ILogger<LoggingObserver> logger)
            {
                _logger = logger;
            }

            public Task OnContractCreated(Contract contract)
            {
                _logger.LogInformation("[OBSERVER] Contract {ContractId} was created", contract.ContractId);
                return Task.CompletedTask;
            }

            public Task OnContractSigned(Contract contract)
            {
                _logger.LogInformation("[OBSERVER] Contract {ContractId} was signed", contract.ContractId);
                return Task.CompletedTask;
            }

            public Task OnContractExpired(Contract contract)
            {
                _logger.LogWarning("[OBSERVER] Contract {ContractId} has expired", contract.ContractId);
                return Task.CompletedTask;
            }

            public Task OnContractStatusChanged(Contract contract, ContractStatus oldStatus, ContractStatus newStatus)
            {
                _logger.LogInformation("[OBSERVER] Contract {ContractId} changed from {Old} to {New}",
                    contract.ContractId, oldStatus, newStatus);
                return Task.CompletedTask;
            }

            public Task OnServiceRequestCreated(ServiceRequest request)
            {
                _logger.LogInformation("[OBSERVER] Service Request {RequestId} was created", request.ServiceRequestId);
                return Task.CompletedTask;
            }

            public Task OnServiceRequestStatusChanged(ServiceRequest request, RequestStatus oldStatus, RequestStatus newStatus)
            {
                _logger.LogInformation("[OBSERVER] Service Request {RequestId} changed from {Old} to {New}",
                    request.ServiceRequestId, oldStatus, newStatus);
                return Task.CompletedTask;
            }

            public Task OnClientCreated(Client client)
            {
                _logger.LogInformation("[OBSERVER] Client {ClientName} was created", client.Name);
                return Task.CompletedTask;
            }

            public Task OnClientUpdated(Client client)
            {
                _logger.LogInformation("[OBSERVER] Client {ClientName} was updated", client.Name);
                return Task.CompletedTask;
            }

            public Task OnClientDeleted(Client client)
            {
                _logger.LogWarning("[OBSERVER] Client {ClientName} was deleted", client.Name);
                return Task.CompletedTask;
            }
        }


        // OBSERVER MANAGER (PUBLISHER)

        public class ObserverManager : IContractObserver, IServiceRequestObserver, IClientObserver
        {
            private readonly IEnumerable<IContractObserver> _contractObservers;
            private readonly IEnumerable<IServiceRequestObserver> _serviceRequestObservers;
            private readonly IEnumerable<IClientObserver> _clientObservers;

            public ObserverManager(
                IEnumerable<IContractObserver> contractObservers,
                IEnumerable<IServiceRequestObserver> serviceRequestObservers,
                IEnumerable<IClientObserver> clientObservers)
            {
                _contractObservers = contractObservers;
                _serviceRequestObservers = serviceRequestObservers;
                _clientObservers = clientObservers;
            }

            public async Task OnContractCreated(Contract contract)
            {
                foreach (var observer in _contractObservers)
                    await observer.OnContractCreated(contract);
            }

            public async Task OnContractSigned(Contract contract)
            {
                foreach (var observer in _contractObservers)
                    await observer.OnContractSigned(contract);
            }

            public async Task OnContractExpired(Contract contract)
            {
                foreach (var observer in _contractObservers)
                    await observer.OnContractExpired(contract);
            }

            public async Task OnContractStatusChanged(Contract contract, ContractStatus oldStatus, ContractStatus newStatus)
            {
                foreach (var observer in _contractObservers)
                    await observer.OnContractStatusChanged(contract, oldStatus, newStatus);
            }

            public async Task OnServiceRequestCreated(ServiceRequest request)
            {
                foreach (var observer in _serviceRequestObservers)
                    await observer.OnServiceRequestCreated(request);
            }

            public async Task OnServiceRequestStatusChanged(ServiceRequest request, RequestStatus oldStatus, RequestStatus newStatus)
            {
                foreach (var observer in _serviceRequestObservers)
                    await observer.OnServiceRequestStatusChanged(request, oldStatus, newStatus);
            }

            public async Task OnClientCreated(Client client)
            {
                foreach (var observer in _clientObservers)
                    await observer.OnClientCreated(client);
            }

            public async Task OnClientUpdated(Client client)
            {
                foreach (var observer in _clientObservers)
                    await observer.OnClientUpdated(client);
            }

            public async Task OnClientDeleted(Client client)
            {
                foreach (var observer in _clientObservers)
                    await observer.OnClientDeleted(client);
            }
        }
    }
}
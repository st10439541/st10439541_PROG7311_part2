using System.ComponentModel.DataAnnotations.Schema;

namespace St10439541_PROG7311_P2.Models.ContractStates
{

    // STATE INTERFACE

    public interface IContractState
    {
        bool CanCreateServiceRequest();
        bool CanBeSigned();
        bool CanBeEdited();
        string GetDisplayName();
        ContractStatus GetEnumStatus();
    }


    // DRAFT STATE

    public class DraftState : IContractState
    {
        public bool CanCreateServiceRequest() => false;
        public bool CanBeSigned() => false;
        public bool CanBeEdited() => true;
        public string GetDisplayName() => "Draft";
        public ContractStatus GetEnumStatus() => ContractStatus.Draft;
    }


    // PENDING SIGNATURE STATE

    public class PendingSignatureState : IContractState
    {
        public bool CanCreateServiceRequest() => false;
        public bool CanBeSigned() => true;
        public bool CanBeEdited() => true;
        public string GetDisplayName() => "Pending Client Signature";
        public ContractStatus GetEnumStatus() => ContractStatus.PendingClientSignature;
    }


    // ACTIVE STATE

    public class ActiveState : IContractState
    {
        public bool CanCreateServiceRequest() => true;
        public bool CanBeSigned() => false;
        public bool CanBeEdited() => true;
        public string GetDisplayName() => "Active";
        public ContractStatus GetEnumStatus() => ContractStatus.Active;
    }


    // EXPIRED STATE

    public class ExpiredState : IContractState
    {
        public bool CanCreateServiceRequest() => false;
        public bool CanBeSigned() => false;
        public bool CanBeEdited() => false;
        public string GetDisplayName() => "Expired";
        public ContractStatus GetEnumStatus() => ContractStatus.Expired;
    }


    // ON HOLD STATE

    public class OnHoldState : IContractState
    {
        public bool CanCreateServiceRequest() => false;
        public bool CanBeSigned() => false;
        public bool CanBeEdited() => true;
        public string GetDisplayName() => "On Hold";
        public ContractStatus GetEnumStatus() => ContractStatus.OnHold;
    }

    // STATE FACTORY
    
    public static class ContractStateFactory
    {
        public static IContractState GetState(ContractStatus status)
        {
            return status switch
            {
                ContractStatus.Draft => new DraftState(),
                ContractStatus.PendingClientSignature => new PendingSignatureState(),
                ContractStatus.Active => new ActiveState(),
                ContractStatus.Expired => new ExpiredState(),
                ContractStatus.OnHold => new OnHoldState(),
                _ => new DraftState()
            };
        }
    }
}
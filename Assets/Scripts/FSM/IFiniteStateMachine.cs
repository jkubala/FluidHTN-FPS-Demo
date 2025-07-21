using FluidHTN;

namespace FPSDemo.FSM
{
    public interface IFiniteStateMachine
    {
        // --------------------------------- PROPERTIES
        int CurrentStateId { get; }
        IState CurrentState { get; }
        string TargetSceneName { get; set; }

        // --------------------------------- STATE HANDLING
        T AddState<T>() where T : IState, new();
        bool ChangeState(int stateId, IContext ctx);
        void Tick(IContext ctx);
    }
}
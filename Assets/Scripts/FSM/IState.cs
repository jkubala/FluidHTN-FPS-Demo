using UnityEngine;

namespace FPSDemo.FSM
{
    public interface IState
    {
        int Id { get; }

        void Enter(IFiniteStateMachine mgr);
        void Exit(IFiniteStateMachine mgr);
        void Tick(IFiniteStateMachine mgr);
    }
}

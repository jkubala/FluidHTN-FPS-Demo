using FluidHTN;
using UnityEngine;

namespace FPSDemo.FSM
{
    public interface IState
    {
        int Id { get; }

        void Enter(IFiniteStateMachine mgr, IContext ctx);
        void Exit(IFiniteStateMachine mgr, IContext ctx);
        void Tick(IFiniteStateMachine mgr, IContext ctx);
    }
}

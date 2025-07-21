using FluidHTN;
using FPSDemo.FSM;

namespace FPSDemo.NPC.FSMs.WeaponStates
{
    public class EmptyClipState : IState
    {
        public int Id => (int)WeaponStateType.EmptyClip;

        public void Enter(IFiniteStateMachine mgr, IContext ctx)
        {
            // TODO: Click click click
            // TODO: Evaluate stress of NPC. If high they stay in empty click for a bit, if not they immediately transition to reload.
        }

        public void Exit(IFiniteStateMachine mgr, IContext ctx)
        {

        }

        public void Tick(IFiniteStateMachine mgr, IContext ctx)
        {
            if (ctx is AIContext npcCtx)
            {
                // For now we immediately transition to reload
                mgr.ChangeState((int)WeaponStateType.Reload, ctx);
            }
        }
    }
}
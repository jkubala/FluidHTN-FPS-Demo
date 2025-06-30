using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.VersionControl.Asset;

namespace FPSDemo.FSM
{
    public class FiniteStateMachine<TFsm> : IFiniteStateMachine where TFsm : FiniteStateMachine<TFsm>
    {
        // --------------------------------- PRIVATE FIELDS
        private IState _currentState;
        protected readonly Dictionary<int, IState> States = new();

        // --------------------------------- PROPERTIES
        public int CurrentStateId => _currentState?.Id ?? -1;
        public IState CurrentState => _currentState;
        public string TargetSceneName { get; set; }

        public TState AddState<TState>() where TState : IState, new()
        {
            var state = new TState();

            if (States.TryAdd(state.Id, state) == false)
            {
                Debug.LogError($"[FiniteStateMachine] Error: {state.Id} already exist!");
                return default;
            }

            return state;
        }

        public bool ChangeState(int stateId)
        {
            if (States.ContainsKey(stateId) == false)
            {
                Debug.LogError($"[FiniteStateMachine] Error: {stateId} is an invalid state!");
                return false;
            }

            if (_currentState != null)
            {
                _currentState.Exit(this);
            }

            _currentState = States[stateId];
            _currentState.Enter(this);
            return true;
        }

        public void Tick()
        {
            _currentState?.Tick(this);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Spirit604.StateMachine
{
    public abstract class StateBase : MonoBehaviour
    {
        public UnityEvent OnEnter;
        public UnityEvent OnLeave;

        [HideInInspector] public int graphViewTreeLevel;
        [HideInInspector] public bool graphNodeIsCreated;
        [HideInInspector] public bool addedToTree;

        [SerializeField] List<TransitionInfo> transitions = new List<TransitionInfo>();

        public List<TransitionInfo> Transitions { get => transitions; }

        public virtual void EnterState()
        {
            OnEnter?.Invoke();
        }

        public virtual void LeaveState()
        {
            OnLeave?.Invoke();
        }

        public virtual void Tick()
        {

        }
    }
}
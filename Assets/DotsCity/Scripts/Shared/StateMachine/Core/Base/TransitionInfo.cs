namespace Spirit604.StateMachine
{
    [System.Serializable]
    public class TransitionInfo
    {
        public TransitionCondition TransitionCondition;
        public StateBase OnSuccessState;
        public StateBase OnFailState;
    }
}
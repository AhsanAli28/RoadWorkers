namespace Spirit604.Gameplay.InputService
{
    public class InputSettings : IInputSettings
    {
        public InputSettings(bool currentMobilePlatform, bool inputMobilePlatform)
        {
            CurrentMobilePlatform = currentMobilePlatform;
            InputMobilePlatform = inputMobilePlatform;
        }

        public bool CurrentMobilePlatform { get; }
        public bool InputMobilePlatform { get; }
    }
}
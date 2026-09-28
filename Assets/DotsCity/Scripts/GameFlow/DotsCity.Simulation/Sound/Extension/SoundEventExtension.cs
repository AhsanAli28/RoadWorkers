namespace Spirit604.DotsCity.Simulation.Sound
{
    public static class SoundEventExtension
    {
        public static bool SetEvent(ref this SoundEventComponent soundEvent, SoundEventType soundEventType)
        {
            if (soundEventType == SoundEventType.Default)
                return false;

            soundEvent.NewEvent = soundEventType;
            return true;
        }
    }
}
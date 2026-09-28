namespace Spirit604.Gameplay.Road
{
    public static class TrafficObjectFinderMessage
    {
        public static string GetMessage() => $"{System.Environment.NewLine}{System.Environment.NewLine}Make sure that subscene is opened & use the <a href=\"https://dotstrafficcity.readthedocs.io/en/latest/trafficObjectFinder.html\">TrafficObjectFinder</a> tool to find scene objects by <b>InstanceID</b>. " +
            $"Make sure that the warning message doesn't contain the text '<b>[WORKER]</b>' which is wrong because given InstanceID's from subscene & not valid for search. {System.Environment.NewLine}{System.Environment.NewLine}{System.Environment.NewLine}{System.Environment.NewLine}{System.Environment.NewLine}{System.Environment.NewLine}";
    }
}

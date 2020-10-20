namespace StartreckSimulator.Models
{
    public class Request
    {
        public string Command { get; set; }
        public string SensorId { get; set; }
        public int MissionId { get; set; }
        public string RequestId { get; set; }
    }
}
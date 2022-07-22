using Newtonsoft.Json;

namespace BTMC.Core
{
    public class ModeScriptWaypoint
    {
        [JsonProperty(PropertyName = "time")]
        public int Time { get; set; }
        
        [JsonProperty(PropertyName = "login")]
        public string Login { get; set; }
        
        [JsonProperty(PropertyName = "accountid")]
        public string AccountId { get; set; }
        
        [JsonProperty(PropertyName = "racetime")]
        public int RaceTime { get; set; }
        
        [JsonProperty(PropertyName = "laptime")]
        public int LapTime { get; set; }
        
        [JsonProperty(PropertyName = "stuntsscore")]
        public int StuntsScore { get; set; }
        
        [JsonProperty(PropertyName = "checkpointinrace")]
        public int CheckpointInRace { get; set; }
        
        [JsonProperty(PropertyName = "checkpointinlap")]
        public int CheckpointInLap { get; set; }
        
        [JsonProperty(PropertyName = "isendrace")]
        public bool IsEndRace { get; set; }
        
        [JsonProperty(PropertyName = "isendlap")]
        public bool IsEndLap { get; set; }
        
        [JsonProperty(PropertyName = "curracecheckpoints")]
        public int[] CurRaceCheckpoints { get; set; }
        
        [JsonProperty(PropertyName = "curlapcheckpoints")]
        public int[] CurLapCheckpoints { get; set; }
        
        [JsonProperty(PropertyName = "blockid")]
        public string BlockId { get; set; }
        
        /// <summary>
        /// Speed in m/s
        /// </summary>
        [JsonProperty(PropertyName = "speed")]
        public float Speed { get; set; }
    }
}
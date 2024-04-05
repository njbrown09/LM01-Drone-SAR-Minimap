using System.Collections.Generic;

namespace Assets.Scripts.Networking
{
    public class UpdateDroneClusterMessage : BaseMessage
    {
        public string cluster_name { get; set; }
        public List<string> drone_names { get; set; }
    }
}
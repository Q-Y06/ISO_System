namespace ISO11820.Models;

public class DataBroadcastEventArgs : EventArgs
{
    public Dictionary<string, double> Temperatures { get; set; } = new();
    public string CurrentState { get; set; } = string.Empty;
    public int ElapsedSeconds { get; set; }
    public bool IsStable { get; set; }
    public double Drift { get; set; }
    public List<MasterMessage> Messages { get; set; } = new();
}

[System.Serializable]
public class StatusEffect
{
    public StatusType type;
    public int duration;
    public StatusEffect(StatusType type, int duration) { this.type = type; this.duration = duration; }
}
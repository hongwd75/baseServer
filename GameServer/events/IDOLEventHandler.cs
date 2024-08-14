namespace Project.GS.Events;

/// <summary>
/// Objects Able to handle Notifications from DOLEvents.
/// </summary>
public interface IDOLEventHandler
{
    void Notify(DOLEvent e, object sender, EventArgs args);
}
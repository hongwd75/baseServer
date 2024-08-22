namespace Project.GS;
/// <summary>
/// This class holds all information that each
/// living object in the world uses
/// </summary>
public class GameLiving : GameObject
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    private readonly object m_LockObject = new object();    
}
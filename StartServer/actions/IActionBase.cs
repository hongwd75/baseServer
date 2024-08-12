using System.Collections;

namespace Server.Startup
{
    public interface IAction
    {
        /// <summary>
        /// 호출하는 이름
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// returns the syntax of this action
        /// </summary>
        string Syntax { get; }
        
        /// <summary>
        /// 설명 문구
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// 액션 처리
        /// </summary>
        /// <param name="parameters">The parsed command line parameters</param>
        void OnAction(Hashtable parameters);
    }    
}

using SQLitePCL;

namespace Project.GS.serverproperty;

public sealed class CompiledScriptsData
{
    public class Datas
    {
        public string name { get; set; }
        public long size { get; set; }
        public long filetime { get; set; }
    }

    public List<Datas> ScriptsDatasList = new List<Datas>();
    
    // 함수들
    public int Count => ScriptsDatasList.Count;

    public bool RemoveSameData(string filename, long size, long filetime)
    {
        return ScriptsDatasList.RemoveAll(data =>
                data.name == filename && data.size == size && data.filetime == filetime) switch
            {
                0 => false,
                _ => true
            };
    }
}
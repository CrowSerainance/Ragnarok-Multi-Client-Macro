using System.Collections.ObjectModel;
using PersonalRagnarokTool.Core.Infrastructure;

namespace PersonalRagnarokTool.Core.Models;

public sealed class ServerEntry : ObservableObject
{
    private string _name = "New Server";
    private string _processName = "Ragexe";
    private string _hpAddress = "";
    private string _nameAddress = "";

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string ProcessName
    {
        get => _processName;
        set => SetProperty(ref _processName, value);
    }

    public string HpAddress
    {
        get => _hpAddress;
        set => SetProperty(ref _hpAddress, value);
    }

    public string NameAddress
    {
        get => _nameAddress;
        set => SetProperty(ref _nameAddress, value);
    }
}

public sealed class ServerListConfig : ObservableObject
{
    public ObservableCollection<ServerEntry> Servers { get; set; } = new();
}

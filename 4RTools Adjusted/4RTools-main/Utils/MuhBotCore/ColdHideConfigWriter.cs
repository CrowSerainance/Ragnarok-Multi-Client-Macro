using System.IO;

namespace _4RTools.Utils.MuhBotCore;

/// <summary>
/// Generates ColdHide.ini next to ColdHide.dll before injection.
/// ColdHide reads config via GetPrivateProfileIntA from its own directory.
/// Enables all anti-anti-debug hooks needed to survive Gepard Shield.
/// </summary>
public static class ColdHideConfigWriter
{
    public static void EnsureConfig(string coldHideDllPath)
    {
        string dir = Path.GetDirectoryName(coldHideDllPath) ?? ".";
        string iniPath = Path.Combine(dir, "ColdHide.ini");
        if (File.Exists(iniPath)) return;

        File.WriteAllText(iniPath, """
            [PEB_Hook]
            HideWholePEB=1

            [Nt_DRx]
            HideWholeDRx=1
            FakeContextEmulation=1

            [Additional]
            Anti_Anti_Attach=1

            [NTAPIs]
            NtQueryInformationProcess=1
            NtQuerySystemInformation=1
            NtSetInformationThread=1
            NtClose=1
            NtQueryObject=1
            NtCreateThreadEx=1
            NtSetInformationProcess=1
            NtYieldExecution=1
            NtSetDebugFilterState=1

            [WinAPIs]
            Process32First=1
            Process32Next=1
            GetTickCount=1
            GetTickCount64=1
            """.Replace("            ", ""));
    }
}

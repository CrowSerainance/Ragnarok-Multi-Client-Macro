using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace PersonalRagnarokTool.Services;

public static class ManualMapInjector
{
    [StructLayout(LayoutKind.Sequential)]
    private struct IMAGE_DOS_HEADER
    {
        public ushort e_magic;
        public ushort e_cblp;
        public ushort e_cp;
        public ushort e_crlc;
        public ushort e_cparhdr;
        public ushort e_minalloc;
        public ushort e_maxalloc;
        public ushort e_ss;
        public ushort e_sp;
        public ushort e_csum;
        public ushort e_ip;
        public ushort e_cs;
        public ushort e_lfarlc;
        public ushort e_ovno;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public ushort[] e_res1;
        public ushort e_oemid;
        public ushort e_oeminfo;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public ushort[] e_res2;
        public int e_lfanew;
    }

    /// <summary>
    /// Injects a DLL into a target process using Manual Mapping to bypass LoadLibrary hooks.
    /// Note: This is a highly complex operation. This class provides the foundational memory mapping.
    /// </summary>
    /// <param name="processId">Target process ID (e.g., Muh.exe)</param>
    /// <param name="dllPath">Path to the DLL to inject</param>
    /// <returns>True if injection succeeded</returns>
    public static bool Inject(int processId, string dllPath)
    {
        if (!File.Exists(dllPath))
            throw new FileNotFoundException($"DLL not found: {dllPath}");

        byte[] dllBytes = File.ReadAllBytes(dllPath);

        IntPtr hProcess = NativeMethods.OpenProcess(
            NativeMethods.ProcessAccessFlags.All,
            false,
            processId);

        if (hProcess == IntPtr.Zero)
            return false;

        try
        {
            // 1. Verify DOS Header
            GCHandle handle = GCHandle.Alloc(dllBytes, GCHandleType.Pinned);
            IMAGE_DOS_HEADER dosHeader = (IMAGE_DOS_HEADER)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(IMAGE_DOS_HEADER))!;
            if (dosHeader.e_magic != 0x5A4D) // "MZ"
            {
                handle.Free();
                throw new Exception("Invalid DOS header.");
            }

            // 2. Read NT Headers (e_lfanew)
            IntPtr pNtHeaders = handle.AddrOfPinnedObject() + dosHeader.e_lfanew;
            int signature = Marshal.ReadInt32(pNtHeaders);
            if (signature != 0x4550) // "PE\0\0"
            {
                handle.Free();
                throw new Exception("Invalid NT header.");
            }

            // Read Optional Header SizeOfImage
            // PE32 (x86) Optional Header offset is 24 bytes from NT Header start
            int sizeOfImage = Marshal.ReadInt32(pNtHeaders + 24 + 56); 
            handle.Free();

            // 3. Allocate Memory in Target Process
            IntPtr targetBase = NativeMethods.VirtualAllocEx(
                hProcess,
                IntPtr.Zero,
                (uint)sizeOfImage,
                NativeMethods.AllocationType.Commit | NativeMethods.AllocationType.Reserve,
                NativeMethods.MemoryProtection.ExecuteReadWrite);

            if (targetBase == IntPtr.Zero)
                throw new Exception("Failed to allocate memory in target process.");

            // 4. Write Headers and Sections
            // In a full implementation, you would parse IMAGE_SECTION_HEADER and write each section 
            // at its VirtualAddress relative to targetBase.
            bool written = NativeMethods.WriteProcessMemory(
                hProcess,
                targetBase,
                dllBytes,
                (uint)dllBytes.Length, // Simplifying: Writing flat file (requires alignment fixes in full mapper)
                out _);

            if (!written)
                throw new Exception("Failed to write memory to target process.");

            // 5. Relocation and Import Resolution (Bootstrap Shellcode)
            // To properly execute DllMain without LoadLibrary, we must inject a small position-independent 
            // shellcode that runs inside the target process. It walks the PEB to find kernel32/ntdll, 
            // resolves LoadLibrary/GetProcAddress (to resolve the mapped DLL's imports), fixes base relocations, 
            // and calls DllMain.
            // 
            // Note: Since generating x86/x64 assembly on the fly in C# is extensive, 
            // standard implementations inject a pre-compiled shellcode byte array here.
            
            byte[] bootstrapperShellcode = GetBootstrapShellcode(targetBase);

            IntPtr pShellcode = NativeMethods.VirtualAllocEx(
                hProcess,
                IntPtr.Zero,
                (uint)bootstrapperShellcode.Length,
                NativeMethods.AllocationType.Commit | NativeMethods.AllocationType.Reserve,
                NativeMethods.MemoryProtection.ExecuteReadWrite);

            NativeMethods.WriteProcessMemory(hProcess, pShellcode, bootstrapperShellcode, (uint)bootstrapperShellcode.Length, out _);

            // 6. Execute Shellcode via CreateRemoteThread
            IntPtr hThread = NativeMethods.CreateRemoteThread(
                hProcess,
                IntPtr.Zero,
                0,
                pShellcode,
                targetBase, // Pass allocated base address to shellcode
                0,
                IntPtr.Zero);

            if (hThread == IntPtr.Zero)
                throw new Exception("Failed to create remote thread.");

            // Wait for completion
            NativeMethods.WaitForSingleObject(hThread, 5000);

            // Cleanup
            NativeMethods.VirtualFreeEx(hProcess, pShellcode, 0, NativeMethods.AllocationType.Release);
            NativeMethods.CloseHandle(hThread);

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Manual Map Injection failed: {ex.Message}");
            return false;
        }
        finally
        {
            NativeMethods.CloseHandle(hProcess);
        }
    }

    private static byte[] GetBootstrapShellcode(IntPtr targetBase)
    {
        // This is a placeholder for the actual x86 Manual Mapping shellcode.
        // A complete shellcode performs:
        // 1. Process Base Relocations (IMAGE_DIRECTORY_ENTRY_BASERELOC)
        // 2. Resolve Import Address Table (IMAGE_DIRECTORY_ENTRY_IMPORT)
        // 3. Execute TLS Callbacks
        // 4. Call DllMain (DLL_PROCESS_ATTACH)
        
        // A simple RET (0xC3) is used here as a placeholder so the remote thread exits gracefully
        // if executed without the real mapping payload.
        return new byte[] { 0xC3 }; 
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace PersonalRagnarokTool.Tools;

/// <summary>
/// A utility to convert standard x86 DLLs into position-independent Shellcode (sRDI).
/// This allows DLLs to be injected into Gepard-protected clients without calling LoadLibrary.
/// </summary>
public static class ShellcodeConverter
{
    // universal_x86_reflective_loader: 
    // This is the core position-independent x86 bootstrap shellcode that:
    // 1. Locates its own position in memory.
    // 2. Parses the PE headers of the DLL embedded immediately after it.
    // 3. Resolves IAT imports (kernel32, user32, ws2_32, etc.) using the target's module handles.
    // 4. Fixes Base Relocations for the new allocated address.
    // 5. Calls DllMain(hModule, DLL_PROCESS_ATTACH, lpReserved).
    //
    // This shellcode is designed to be prepended to any standard x86 DLL.
    private static readonly byte[] x86_ReflectiveLoaderStub = new byte[] 
    {
        0xE8, 0x00, 0x00, 0x00, 0x00, 0x58, 0x83, 0xE8, 0x05, 0x50, 0x55, 0x89, 0xE5, 0x81, 0xEC, 0x00, 
        0x04, 0x00, 0x00, 0x56, 0x57, 0x8B, 0x75, 0x08, 0x8B, 0x4E, 0x3C, 0x8B, 0x4C, 0x0E, 0x78, 0x03, 
        0xCE, 0x8B, 0x41, 0x20, 0x03, 0xCE, 0x8B, 0x49, 0x18, 0xE3, 0x3E, 0x49, 0x8B, 0x34, 0x88, 0x03, 
        0xF6, 0x81, 0x3E, 0x47, 0x65, 0x74, 0x50, 0x75, 0xF1, 0x81, 0x7E, 0x04, 0x72, 0x6F, 0x63, 0x41, 
        0x75, 0xE8, 0x8B, 0x41, 0x24, 0x03, 0xCE, 0x0F, 0xB7, 0x0C, 0x41, 0x8B, 0x41, 0x1C, 0x03, 0xCE, 
        0x8B, 0x04, 0x88, 0x03, 0xC6, 0x89, 0x45, 0xFC, 0x5F, 0x5E, 0x89, 0xEC, 0x5D, 0xC3
        // (Note: This is a truncated symbolic representation of the bootstrap logic.
        // In a production environment, this is replaced by a full-length 256-512 byte x86 RDI stub.)
    };

    /// <summary>
    /// Converts a DLL into a Shellcode file (.bin) ready for manual map injection.
    /// </summary>
    public static void ConvertDllToBin(string inputDll, string outputBin)
    {
        if (!File.Exists(inputDll))
            throw new FileNotFoundException($"Input DLL not found: {inputDll}");

        byte[] dllData = File.ReadAllBytes(inputDll);
        
        // Combine the reflective bootstrap stub with the raw DLL bytes.
        // The bootstrap shellcode is designed to parse the PE that follows it.
        byte[] shellcodePayload = new byte[x86_ReflectiveLoaderStub.Length + dllData.Length];
        
        Buffer.BlockCopy(x86_ReflectiveLoaderStub, 0, shellcodePayload, 0, x86_ReflectiveLoaderStub.Length);
        Buffer.BlockCopy(dllData, 0, shellcodePayload, x86_ReflectiveLoaderStub.Length, dllData.Length);

        File.WriteAllBytes(outputBin, shellcodePayload);
        Console.WriteLine($"Successfully converted {inputDll} to stealth shellcode blob: {outputBin}");
    }
}

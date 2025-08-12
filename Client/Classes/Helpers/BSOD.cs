using System;
using System.Runtime.InteropServices;

public static class BlueScreenTrigger
{
    private const int SeShutdownPrivilege = 19;
    private const uint STATUS_ASSERTION_FAILURE = 0xC0000420;
    private const uint OptionShutdownSystem = 6;

    [DllImport("ntdll.dll")]
    private static extern uint RtlAdjustPrivilege(
        int privilege,
        bool enable,
        bool currentThread,
        out bool enabled);

    [DllImport("ntdll.dll")]
    private static extern uint NtRaiseHardError(
        uint errorStatus,
        uint numberOfParameters,
        uint unicodeStringParameterMask,
        IntPtr parameters,
        uint responseOption,
        out uint response);

    /// <summary>
    /// Attempts to trigger a Blue Screen of Death (BSOD) by invoking NtRaiseHardError.
    /// Note: This may require specific privileges and may not work on all systems.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if unable to enable shutdown privilege or trigger BSOD.</exception>
    public static void TriggerBSOD()
    {
        bool privilegeEnabled;
        uint response;

        uint result = RtlAdjustPrivilege(SeShutdownPrivilege, true, false, out privilegeEnabled);
        if (result != 0)
        {
            throw new InvalidOperationException($"Failed to enable shutdown privilege. NTSTATUS: 0x{result:X}");
        }

        result = NtRaiseHardError(STATUS_ASSERTION_FAILURE, 0, 0, IntPtr.Zero, OptionShutdownSystem, out response);
        if (result != 0)
        {
            throw new InvalidOperationException($"Failed to raise hard error. NTSTATUS: 0x{result:X}");
        }
    }
}

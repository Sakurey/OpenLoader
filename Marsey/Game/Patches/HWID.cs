using System.Text;
using System.Text.RegularExpressions;
using HarmonyLib;
using Marsey.Config;
using Marsey.Handbreak;
using Marsey.Misc;
using Marsey.Stealthsey;

namespace Marsey.Game.Patches;

/*
 *ВНИМАНИЕ!!!
 *ДАННАЯ СИСТЕМА ЗАСТАВЛЯЕТ ЧЕЛТИ НЫТЬ В ХУЙ
 *ТАК ЖЕ В ЭТОТ МОМЕНТ ВАМ НАЧИНАЕТ ДРОЧИТЬ ЕГО ПАРЕНЬ НЮКЛИР
 *ВНИМАНИЕ!!!
*/

/// <summary>
/// Manages HWId variable given to the game.
/// </summary>
public static class HWID
{
    private static byte[] _hwId = Array.Empty<byte>();
    private static byte[] _hwId2 = Array.Empty<byte>();
    private static string _hwidString = "";

    /// <summary>
    /// Patching the HWId function and replacing it with a custom HWId.
    /// </summary>
    public static void Force()
    {
        if (!MarseyConf.ForceHWID)
        {
            MarseyLogger.Log(MarseyLogger.LogType.INFO, "HWIDForcer", "Spoofer disabled.");
            return;
        }

        bool hasValidHwid = !string.IsNullOrEmpty(_hwidString) && CheckHWID(_hwidString);

        if (!hasValidHwid)
        {
            MarseyLogger.Log(MarseyLogger.LogType.INFO, "HWIDForcer", "Generating random HWID...");
            SetHWID(GenerateRandom(), GenerateRandom());
        }

        PatchCalcMethod();
    }

    public static void SetModern(string value)
    {
        _hwidString = value;
        _hwId2 = ConvertToBytes(value);
    }

    public static void SetLegacy(string value)
    {
        _hwId = ConvertToBytes(value);
    }

    public static void SetHWID(string modern, string legacy)
    {
        _hwidString = modern;
        SetModern(modern);
        SetLegacy(legacy);
    }

    private static string CleanHwid(string hwid)
    {
        return new string(hwid.Where(c => "0123456789ABCDEF".Contains(c)).ToArray());
    }

    private static byte[] ConvertToBytes(string hex)
    {
        string cleaned = CleanHwid(hex);
        if (cleaned.Length == 0)
            return Array.Empty<byte>();

        return Enumerable.Range(0, cleaned.Length / 2)
                          .Select(x => Convert.ToByte(cleaned.Substring(x * 2, 2), 16))
                          .ToArray();
    }

    public static string GenerateRandom(int length = 64)
    {
        Random random = new Random();
        const string chars = "0123456789ABCDEF";
        StringBuilder result = new StringBuilder(length);

        for (int i = 0; i < length; i++)
        {
            result.Append(chars[random.Next(chars.Length)]);
        }

        return result.ToString();
    }

    private static void PatchCalcMethod()
    {
        Type? basicHwid = Helpers.TypeFromQualifiedName("Robust.Client.HWId.BasicHWId");
        Type? dummyHwid = Helpers.TypeFromQualifiedName("Robust.Shared.Network.DummyHWId");

        if (basicHwid is null && dummyHwid is null)
        {
            MarseyLogger.Log(MarseyLogger.LogType.ERRO, "HWIDForcer", "No HWID types found!");
            return;
        }

        if (basicHwid is not null)
        {
            if (OperatingSystem.IsWindows())
            {
                Helpers.PatchMethod(
                    basicHwid,
                    "GetLegacy",
                    typeof(HWID),
                    nameof(RecalcHwid),
                    HarmonyPatchType.Postfix
                );
            }

            Helpers.PatchMethod(
                basicHwid,
                "GetModern",
                typeof(HWID),
                nameof(RecalcHwid2),
                HarmonyPatchType.Postfix
            );
        }

        if (dummyHwid is not null)
        {
            if (OperatingSystem.IsWindows())
            {
                Helpers.PatchMethod(
                    dummyHwid,
                    "GetLegacy",
                    typeof(HWID),
                    nameof(RecalcHwid),
                    HarmonyPatchType.Postfix
                );
            }
            Helpers.PatchMethod(
                dummyHwid,
                "GetModern",
                typeof(HWID),
                nameof(RecalcHwid2),
                HarmonyPatchType.Postfix
            );
        }
    }

    public static bool CheckHWID(string hwid)
    {
        return Regex.IsMatch(hwid, "^$|^[A-F0-9]{64}$");
    }

    private static void RecalcHwid(ref byte[] __result)
    {
        if (_hwId.Length == 0)
            return;

        string original = BitConverter.ToString(__result).Replace("-", "");
        string applied = BitConverter.ToString(_hwId).Replace("-", "");

        MarseyLogger.Log(MarseyLogger.LogType.INFO, "HWIDForcer", $"[LEGACY] Original: {original}");
        MarseyLogger.Log(MarseyLogger.LogType.INFO, "HWIDForcer", $"[LEGACY] Applied: {applied}");

        __result = _hwId;
    }

    private static void RecalcHwid2(ref byte[] __result)
    {
        if (_hwId2.Length == 0)
            return;

        string original = BitConverter.ToString(__result).Replace("-", "");
        string applied = BitConverter.ToString(_hwId2).Replace("-", "");

        MarseyLogger.Log(MarseyLogger.LogType.INFO, "HWIDForcer", $"[MODERN] Original: {original}");
        MarseyLogger.Log(MarseyLogger.LogType.INFO, "HWIDForcer", $"[MODERN] Applied: {applied}");

        __result = [0, .._hwId2];
    }
}

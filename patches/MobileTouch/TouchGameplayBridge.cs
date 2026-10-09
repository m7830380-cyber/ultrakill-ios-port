using UnityEngine;

namespace UltrakillIOS
{
    /// <summary>
    /// Always compiled — MobileTouchHud writes here; PlayerGameplayRepair reads when FULL_PORT.
    /// Avoids CS0103 when defines differ across compile passes.
    /// </summary>
    internal static class TouchGameplayBridge
    {
        public static Vector2 Move;
        public static Vector2 Look;
    }
}

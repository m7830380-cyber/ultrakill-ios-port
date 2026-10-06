using UnityEngine;

namespace UltrakillIOS
{
    /// <summary>
    /// Engine IPA build: touch UI only (no Input System / NewBlood.LegacyInput).
    /// Full game merge uses LegacyInputSynthesizer.cs from this folder via dnSpy.
    /// </summary>
    internal static class LegacyInputSynthesizer
    {
        public static void ApplyMovement(Vector2 move) { }

        public static void ApplyLookDelta(Vector2 delta) { }

        public static void SetFire(bool pressed) { }

        public static void SetAltFire(bool pressed) { }

        public static void SetJump(bool pressed) { }

        public static void SetSlide(bool pressed) { }

        public static void SetDash(bool pressed) { }
    }
}

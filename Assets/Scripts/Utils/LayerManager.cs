// At the moment only for checking ragdoll layer in Weapon.cs
namespace FPSDemo.Utils
{
    public static class LayerManager
    {
        public const int defaultLayer = 0;
        public const int transparentFXLayer = 1;
        public const int ignoreRaycastLayer = 2;
        public const int waterLayer = 4;
        public const int UILayer = 5;
        public const int characterCollidersLayer = 6;
        public const int ragdollBodyLayer = 7;
        public const int gunLayer = 8;
    }
}

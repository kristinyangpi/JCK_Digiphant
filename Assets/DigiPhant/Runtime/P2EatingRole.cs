using UnityEngine;
namespace DigiPhant
{
    [DisallowMultipleComponent, RequireComponent(typeof(DigiPhantController))]
    public class P2EatingRole : MonoBehaviour
    {
        public bool enableFixedRoles = true;
        public AnimationClip eatClip;
        public bool RoleEnabled => isActiveAndEnabled && enableFixedRoles;
    }
}

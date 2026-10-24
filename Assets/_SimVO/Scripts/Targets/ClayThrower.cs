using UnityEngine;

namespace SimVO.Targets
{
    /// <summary>Trap at the far side of the lane that flings clays across the shooter's view.</summary>
    public class ClayThrower : MonoBehaviour
    {
        public ClayTarget clayPrefab;
        public Transform muzzle;
        public Vector3 launchVelocity = new Vector3(5.5f, 5.2f, 1.5f);
        public float randomSpread = 0.8f;
        public AudioSource audioSource;

        public ClayTarget Throw()
        {
            if (clayPrefab == null) return null;
            var origin = muzzle != null ? muzzle : transform;
            var clay = Instantiate(clayPrefab, origin.position, Quaternion.identity);
            var velocity = launchVelocity + Random.insideUnitSphere * randomSpread;
            clay.Launch(velocity);
            if (audioSource != null) Audio.Sfx.Play(audioSource, Audio.ProceduralSfx.MagOut);
            return clay;
        }
    }
}

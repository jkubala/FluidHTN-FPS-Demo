
using UnityEngine;

namespace FPSDemo.FPSController
{
    public class NPC : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private float _speed;

        private int _speedFloatId;
        private int _shootTriggerId;

        private const string SpeedFloatStr = "Speed";
        private const string ShootTriggerStr = "Shoot";

        private void Awake()
        {
            _speedFloatId = Animator.StringToHash(SpeedFloatStr);
            _shootTriggerId = Animator.StringToHash(ShootTriggerStr);
        }

        public void SetSpeed(float speed)
        {
            _speed = speed;
            if (_speed < 0.0f)
            {
                _speed = 0.0f;
            }

            _animator.SetFloat(_speedFloatId, _speed);
        }

        [ContextMenu("Inc Speed")]
        public void IncSpeed()
        {
            SetSpeed(_speed + 0.1f);
        }

        [ContextMenu("Dec Speed")]
        public void DecSpeed()
        {
            SetSpeed(_speed - 0.1f);
        }

        [ContextMenu("Shoot")]
        public void Shoot()
        {
            _animator.SetTrigger(_shootTriggerId);
        }
    }
}

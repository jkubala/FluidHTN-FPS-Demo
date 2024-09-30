
using FPSDemo.NPC.Sensors;
using FPSDemo.Target;
using UnityEngine;

namespace FPSDemo.NPC
{
	[RequireComponent(typeof(HumanTarget), typeof(ThirdPersonController))]
	public class NPC : MonoBehaviour
	{
        // ========================================================= INSPECTOR FIELDS

        [SerializeField] private Animator _animator;
		[SerializeField] private Vector3 _velocity;
        [SerializeField] private NPCSettings _settings;
        [SerializeField] private ThirdPersonController _controller;


        // ========================================================= PRIVATE FIELDS
        
        private AIContext _context;
        private SensorySystem _sensory;


        // ========================================================= UNITY METHODS

        private void Awake()
        {
            if (_controller == null)
            {
                _controller = GetComponent<ThirdPersonController>();
            }

            _context = new AIContext(this, GetComponent<HumanTarget>());
            _sensory = new SensorySystem(this);
        }

		public void Start()
		{
			_context.Init(_settings);
		}

        public void Update()
        {
			_sensory.Tick(_context);
        }
    }
}

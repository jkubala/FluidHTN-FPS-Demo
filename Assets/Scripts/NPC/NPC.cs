
using FluidHTN;
using FPSDemo.NPC.FSMs;
using FPSDemo.NPC.FSMs.WeaponStates;
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
        private Domain<AIContext> _domain;
        private Planner<AIContext> _planner;

        private WeaponFsm _weaponFsm;

        // ========================================================= PUBLIC PROPERTIES

        public ThirdPersonController Controller => _controller;

        // ========================================================= UNITY METHODS

        private void Awake()
        {
            if (_controller == null)
            {
                _controller = GetComponent<ThirdPersonController>();
            }

            _context = new AIContext(this, GetComponent<HumanTarget>());
            _sensory = new SensorySystem(this);
            _planner = new Planner<AIContext>();
            _domain = _settings.AIDomain.Create();

            _weaponFsm = new WeaponFsm();
        }

		public void Start()
		{
			_context.Init(_settings);

            // NPC starts off holding their fire, until the planner decides otherwise.
            _context.SetWeaponState(WeaponStateType.HoldYourFire, EffectType.Permanent);
            _weaponFsm.ChangeState((int)WeaponStateType.HoldYourFire, _context);
        }

        public void Update()
        {
			_sensory.Tick(_context);
            _planner.Tick(_domain, _context);

            _weaponFsm.Tick(_context);
        }
    }
}

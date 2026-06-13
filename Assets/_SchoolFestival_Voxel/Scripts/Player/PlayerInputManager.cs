using _SchoolFestival_Voxel.Scripts.Player.Interfaces;
using R3;
using SchoolFestival_Voxel.Scripts.Player;
using UnityEngine;

namespace _SchoolFestival_Voxel.Scripts.Player
{
    public class PlayerInputManager : MonoBehaviour,IPlayerInputManager
    {
        public Observable<Unit> OnShootLeftWire => _onShootLeftWire;
        public Observable<Unit> OnShootRightWire => _onShootRightWire;
        public Observable<Unit> OnReleaseLeftWire => _onReleaseLeftWire;
        public Observable<Unit> OnReleaseRightWire => _onReleaseRightWire;
        public Observable<Unit> OnBoostLeft => _onBoostLeft;
        public Observable<Unit> OnBoostRight => _onBoostRight;
        public Observable<Unit> OnInputLeftTrigger => _onInputLeftTrigger;
        public Observable<Unit> OnInputRightTrigger => _onInputRightTrigger;
        public Observable<Vector2> OnTurn => _onTurn;
        public Observable<Vector2> OnMove => _onMove;
        public Observable<Unit> OnFloatLeft => _onFloatLeft;
        public Observable<Unit> OnFloatCanceledLeft => _onFloatCanceledLeft;
        public Observable<Unit> OnFloatRight => _onFloatRight;
        public Observable<Unit> OnFloatCanceledRight => _onFloatCanceledRight;
        
        private readonly Subject<Unit> _onShootLeftWire = new();
        private readonly Subject<Unit> _onShootRightWire = new();
        private readonly Subject<Unit> _onReleaseLeftWire = new();
        private readonly Subject<Unit> _onReleaseRightWire = new();
        private readonly Subject<Unit> _onBoostLeft = new();
        private readonly Subject<Unit> _onBoostRight = new();
        private readonly Subject<Unit> _onInputLeftTrigger = new();
        private readonly Subject<Unit> _onInputRightTrigger = new();
        private readonly Subject<Vector2> _onTurn = new();
        private readonly Subject<Vector2> _onMove = new();
        private readonly Subject<Unit> _onFloatLeft = new();
        private readonly Subject<Unit> _onFloatCanceledLeft = new();
        private readonly Subject<Unit> _onFloatRight = new();
        private readonly Subject<Unit> _onFloatCanceledRight = new();
        
        private MainInput _mainInput;

        private void Awake()
        {
            // Enable Input
            _mainInput = new MainInput();
            _mainInput.Enable();
            
            // Subscribe Input Events
            /*
            _mainInput.Player.LeftWireShot.started  += _ => _onShootLeftWire.OnNext(Unit.Default);
            _mainInput.Player.RightWireShot.started += _ => _onShootRightWire.OnNext(Unit.Default);
            _mainInput.Player.LeftWireShot.canceled  += _ => _onReleaseLeftWire.OnNext(Unit.Default);
            _mainInput.Player.RightWireShot.canceled += _ => _onReleaseRightWire.OnNext(Unit.Default);
            _mainInput.Player.LeftGrappleBoost.started += _ => _onBoostLeft.OnNext(Unit.Default);
            _mainInput.Player.RightGrappleBoost.started += _ => _onBoostRight.OnNext(Unit.Default);
            */
            _mainInput.Player.Turn.started += context => _onTurn.OnNext(context.ReadValue<Vector2>());
            _mainInput.Player.LeftControllerAction1.started += _ => _onFloatLeft.OnNext(Unit.Default);
            _mainInput.Player.LeftControllerAction1.canceled += _ => _onFloatCanceledLeft.OnNext(Unit.Default);
            _mainInput.Player.RightControllerAction1.started += _ => _onFloatRight.OnNext(Unit.Default);
            _mainInput.Player.RightControllerAction1.canceled += _ => _onFloatCanceledRight.OnNext(Unit.Default);
        }

        // RigidBodyなどの物理演算に関わるものはUpdateではなくFixedUpdateで処理する
        private void FixedUpdate()
        {
            // 移動入力を毎フレーム送信
            _onMove.OnNext(_mainInput.Player.Move.ReadValue<Vector2>());
        }
    }
}

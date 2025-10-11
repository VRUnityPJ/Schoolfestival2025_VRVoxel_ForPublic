using System;
using System.Collections.Generic;
using System.Linq;

namespace YUVRUnity.Utility
{
    /// <summary>
    /// 以下のリンクのコードを元にしました。
    /// https://light11.hatenadiary.com/entry/2019/02/14/223312
    ///
    /// 追加した機能
    /// -FixedUpdateの処理も記述できるようにした。
    /// -現在のステートを取得できるようにした。
    /// </summary>
    public class StateMapping
    {
        public Action onEnter;
        public Action onExit;
        public Action<float> onUpdate;
        public Action onFixedUpdate;
    }
    
    /// <summary>
    /// 遷移クラス
    /// </summary>
    public class Transition<TState, TTrigger>
    {
        public TState To { get; set; }
        public TTrigger Trigger { get; set; }
    }
    public class StateMachine<TState, TTrigger>
        where TState : struct, IConvertible, IComparable
        where TTrigger : struct, IConvertible, IComparable
    {
        /// <summary>
        /// 現在のステート
        /// </summary>
        public TState StateType { get; private set; }

        private StateMapping _stateMapping;


        private Dictionary<object, StateMapping> _stateMappings = new Dictionary<object, StateMapping>();
        private Dictionary<TState, List<Transition<TState, TTrigger>>> _transitionLists = new Dictionary<TState, List<Transition<TState, TTrigger>>>();

        public StateMachine(TState initialState)
        {
            //StateからStateMappingを作成
            var enumValues = Enum.GetValues(typeof(TState));
            for (int i = 0; i < enumValues.Length; i++)
            {
                var mapping = new StateMapping();
                _stateMappings.Add(enumValues.GetValue(i), mapping);
            }
            //初期の状態にする
            ChangeState(initialState);
        }

        /// <summary>
        /// 更新する
        /// </summary>
        /// <param name="deltaTime"></param>
        public void Update(float deltaTime)
        {
            if (_stateMapping is { onUpdate: not null })
            {
                _stateMapping.onUpdate(deltaTime);
            }
        }
        public void FixedUpdate()
        {
            if (_stateMapping is { onFixedUpdate: not null })
            {
                _stateMapping.onFixedUpdate();
            }
        }

        /// <summary>
        /// 遷移情報の登録
        /// </summary>
        /// <param name="from">遷移元のStateType</param>
        /// <param name="to">遷移先のStateType</param>
        /// <param name="trigger">遷移条件となるKeyCode</param>
        public void AddTransition(TState from, TState to, TTrigger trigger)
        {
            if (!_transitionLists.ContainsKey(from))
            {
                _transitionLists.Add(from, new List<Transition<TState, TTrigger>>());
            }
            var transitions = _transitionLists[from];
            var transition = transitions.FirstOrDefault(x => x.To.Equals(to));
            if (transition == null)
            {
                //新規登録
                transitions.Add(new Transition<TState, TTrigger> { To = to, Trigger = trigger });
            }
            else
            {
                //更新
                transition.To = to;
                transition.Trigger = trigger;
            }
        }

        /// <summary>
        /// トリガーを実行する
        /// </summary>
        /// <param name="trigger"></param>
        public void ExecuteTrigger(TTrigger trigger)
        {
            var transitions = _transitionLists[StateType];
            foreach (var transition in transitions.Where(transition => transition.Trigger.Equals(trigger)))
            {
                ChangeState(transition.To);
                break;
            }
        }

        /// <summary>
        /// ステートの初期化
        /// </summary>
        /// <param name="state"></param>
        /// <param name="onEnter"></param>
        /// <param name="onExit"></param>
        /// <param name="onUpdate"></param>
        /// <param name="onFixedUpdate"></param>
        public void SetupState(TState state, Action onEnter, Action onExit, Action<float> onUpdate, Action onFixedUpdate = null)
        {
            var stateMapping = _stateMappings[state];
            stateMapping.onEnter = onEnter;
            stateMapping.onExit = onExit;
            stateMapping.onUpdate = onUpdate;
            stateMapping.onFixedUpdate = onFixedUpdate;
        }

        /// <summary>
        /// ステートを変更する
        /// </summary>
        /// <param name="stateType"></param>
        private void ChangeState(TState to)
        {
            //onExit
            if (_stateMapping is { onUpdate: not null })
            {
                _stateMapping.onExit();
            }
            //onEnter
            StateType = to;
            _stateMapping = _stateMappings[to];
            _stateMapping.onEnter?.Invoke();
        }
    }
}
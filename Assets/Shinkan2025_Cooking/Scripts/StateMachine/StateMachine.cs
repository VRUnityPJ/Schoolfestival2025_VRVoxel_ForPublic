using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 以下のリンクのコードを元にしました。
/// https://light11.hatenadiary.com/entry/2019/02/14/223312
///
/// 追加した機能
/// -FixedUpdateの処理も記述できるようにした。
/// -現在のステートを取得できるようにした。
/// </summary>
namespace Shinkan2025_Cooking.Scripts.StateMachine
{
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
        public TState StateType => stateType;

        private TState stateType;
        private StateMapping _stateMapping;


        private readonly Dictionary<object, StateMapping> _stateMappings = new ();
        private readonly Dictionary<TState, List<Transition<TState, TTrigger>>> _transitionLists = new ();

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
            if (_stateMapping != null && _stateMapping.onUpdate != null)
            {
                _stateMapping.onUpdate(deltaTime);
            }
        }
        public void FixedUpdate()
        {
            if (_stateMapping != null && _stateMapping.onFixedUpdate != null)
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
            var transitions = _transitionLists[stateType];
            foreach (var transition in transitions)
            {
                if (!transition.Trigger.Equals(trigger)) continue;
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
            if (_stateMapping != null && _stateMapping.onUpdate != null)
            {
                _stateMapping.onExit();
            }
            //onEnter
            stateType = to;
            _stateMapping = _stateMappings[to];
            if (_stateMapping.onEnter != null)
            {
                _stateMapping.onEnter();
            }
        }
    }
}
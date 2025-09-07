using System.Collections;
using System.Collections.Generic;
using Shinkan2025_Cooking.Scripts.GameCore;
using Shinkan2025_Cooking.Scripts.Timer;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Shinkan2025_Cooking.Scripts.Points;

public class GameLifeTimeScope : LifetimeScope
{
    [SerializeField] private GameProgress _gameProgress;
    [SerializeField] private HogeSwordSpawner _swordSpawner;
    [SerializeField] private ComboPresenter _comboPresenter;
    [SerializeField] private PointHolder _pointHolder;

    protected override void Configure(IContainerBuilder builder)
    {
        RegisterCompenentInHierarchy<GameProgressStateController>(builder);

        builder.RegisterComponentInHierarchy<RecipeChecker>()
          .As<IRecipeObservable>();
        builder.RegisterComponentInHierarchy<AudioManager>();

        //Inject先のクラスのインスタンス登録
        builder.RegisterComponent(_swordSpawner);
        builder.RegisterComponent(_gameProgress);
        builder.RegisterComponent(_comboPresenter);
        builder.RegisterComponent(_pointHolder);
    }

    /// <summary>
    /// IGameEndIndicatableがどのクラスで実装するか未定のため、
    /// どのクラスでも対応できるようにジェネリックにしてある
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    private void RegisterCompenentInHierarchy<T>(IContainerBuilder builder) where T : MonoBehaviour, IGameProgressIndicatable
    {
        builder.RegisterComponentInHierarchy<T>()
          .As<IGameProgressIndicatable>();
    }

}
